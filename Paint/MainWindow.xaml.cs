using IContract;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Paint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // State
        bool _isDrawing = false;
        bool _drawMode = false;
        bool _isDrag = false;
        bool _isChooseObject = false;
        bool _isFill = false;

        string _currentType = "";
        int _currentThickness = 1;
        Color _currentStrokeColor = Colors.Black;
        Color _currentFillColor = Colors.Transparent;
        string _currentStrokeType = null;
        IShapeEntity _preview = null;

        UIElementObject tempStoreUIElement = null;

        int _chosenElementIndex = -1;
        Border _frameChosen = null;
        Point _offsetLeftTop;
        Point _offsetRightBottom;
        int positionStepPaste = 1;

        List<UIElementObject> _listObject = new List<UIElementObject>();
        List<UIElementObject> _stack = new List<UIElementObject>();

        // Cấu hình
        Dictionary<string, IPaintBusiness> _painterPrototypes = new Dictionary<string, IPaintBusiness>();
        Dictionary<string, IShapeEntity> _shapesPrototypes = new Dictionary<string, IShapeEntity>();


        class PluginItems
        {
            public IShapeEntity PluginEntity { get; set; }
            public string PluginIconPath { get; set; }
        }

        class ImageStore : ICloneable
        {
            public Image image = new Image();
            public Point LeftTop;
            public Point RightBottom;

            public object Clone()
            {
                return MemberwiseClone();
            }
        }

        private interface UIElementObject : ICloneable
        {
        }

        class ShapeObject : UIElementObject
        { 
            public IShapeEntity ShapeEntity { get; set; }

            public ShapeObject(IShapeEntity shapeEntity)
            {
                ShapeEntity = shapeEntity;
            }

            public object Clone()
            {
                return MemberwiseClone() as ShapeObject;
            }
        }

        class ImageObject : UIElementObject
        {
            public ImageStore ImageStore { get; set; }

            public ImageObject(ImageStore imageStore)
            {
                ImageStore = imageStore;
            }

            public object Clone()
            {
                return MemberwiseClone() as ImageObject;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var grid = (ribbonApplicationMenu.Template.FindName("MainPaneBorder", ribbonApplicationMenu) as Border).Parent as Grid;
            grid.ColumnDefinitions[2].Width = new GridLength(0);

            var exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            var folderInfo = new DirectoryInfo(exeFolder);
            var dllFiles = folderInfo.GetFiles("*.dll");

            foreach (var dll in dllFiles)
            {
                Assembly assembly = Assembly.LoadFrom(dll.FullName);

                Type[] types = assembly.GetTypes();

                // Giả định: 1 dll chỉ có 1 entity và 1 business tương ứng
                IShapeEntity? entity = null;
                IPaintBusiness? business = null;

                foreach (Type type in types)
                {
                    if (type.IsClass)
                    {
                        if (typeof(IShapeEntity).IsAssignableFrom(type))
                        {
                            entity = (Activator.CreateInstance(type) as IShapeEntity)!;
                        }

                        if (typeof(IPaintBusiness).IsAssignableFrom(type))
                        {
                            business = (Activator.CreateInstance(type) as IPaintBusiness)!;
                        }
                    }
                }

                //var img = new Bitmap
                if (entity != null)
                {
                    _shapesPrototypes.Add(entity!.Name, entity);
                    _painterPrototypes.Add(entity!.Name, business!);
                }
            }

            BindingList<PluginItems> shapeItemSource = new BindingList<PluginItems>();

            //// Tạo ra các nút bấm tương ứng
            foreach (var (name, entity) in _shapesPrototypes)
            {
                shapeItemSource.Add(new PluginItems()
                {
                    PluginEntity = entity,
                    PluginIconPath = "./images/" + name + ".png",
                });
            }
            shapeList.ItemsSource = shapeItemSource;
        }
        private void turnOffAllMode()
        {
            clearChoooseObject();
            _isChooseObject = false;
            _drawMode = false;
            _isFill = false;
            _chosenElementIndex = -1;
            _frameChosen = null;
            clearChooseButton();
        }
        private void reDrawCanvas()
        {
            canvas.Children.Clear();

            foreach (var item in _listObject)
            {
                if (item is ShapeObject)
                {
                    IPaintBusiness painter = _painterPrototypes[((ShapeObject)item).ShapeEntity.Name];
                    UIElement shape = painter.Draw(((ShapeObject)item).ShapeEntity);

                    canvas.Children.Add(shape);
                }
                else if (item is ImageObject)
                {
                    canvas.Children.Add(((ImageObject)item).ImageStore.image);
                }
            }
        }
        private void clearChoooseObject()
        {
            if (_frameChosen != null && _chosenElementIndex != -1)
            {
                canvas.Children.Remove(_frameChosen);
                _frameChosen.Child = null;
                _frameChosen = null;

                if (_listObject[_chosenElementIndex] is ShapeObject)
                {
                    var painter = _painterPrototypes[((ShapeObject)_listObject[_chosenElementIndex]).ShapeEntity.Name];
                    var ele = painter.Draw(((ShapeObject)_listObject[_chosenElementIndex]).ShapeEntity);

                    canvas.Children.Add(ele);
                }
                else if (_listObject[_chosenElementIndex] is ImageObject)
                {
                    setPositionToChosenImage();

                    canvas.Children.Add(((ImageObject)_listObject[_chosenElementIndex]).ImageStore.image);
                }

                _chosenElementIndex = -1;
            }
        }
        private bool isEqual(UIElement sourceInCanvas, Point LeftTop, Point RightBottom)
        {
            bool rs = false;

            bool equalLeft = Canvas.GetLeft(sourceInCanvas) == LeftTop.X;
            bool equalTop = Canvas.GetTop(sourceInCanvas) == LeftTop.Y;
            bool equalRight = Canvas.GetRight(sourceInCanvas) == RightBottom.X;
            bool equalBottom = Canvas.GetBottom(sourceInCanvas) == RightBottom.Y;

            rs = equalLeft && equalTop && equalRight && equalBottom;

            return rs;
        }
        private int getIndexOfElement(UIElement element)
        {
            int idx = -1;
            for (int i = 0; i < _listObject.Count; i++)
            {
                if (_listObject[i] is ShapeObject)
                {
                    if (isEqual(element, ((ShapeObject)_listObject[i]).ShapeEntity.GetLeftTop(), ((ShapeObject)_listObject[i]).ShapeEntity.GetRightBottom()))
                    {
                        idx = i;
                    }
                }
                else if (_listObject[i] is ImageObject)
                {
                    if (isEqual(element, ((ImageObject)_listObject[i]).ImageStore.LeftTop, ((ImageObject)_listObject[i]).ImageStore.RightBottom))
                    {
                        idx = i;
                    }
                }
            }

            return idx;
        }
        private (Point, Point) getOffsetOfElement(Point pointInElement)
        {
            Point offsetLeftTop = pointInElement;
            Point offsetRightBottom = pointInElement;

            offsetLeftTop.X -= Canvas.GetLeft(_frameChosen);
            offsetLeftTop.Y -= Canvas.GetTop(_frameChosen);

            offsetRightBottom.X = Canvas.GetRight(_frameChosen) - offsetRightBottom.X;
            offsetRightBottom.Y = Canvas.GetBottom(_frameChosen) - offsetRightBottom.Y;

            return (offsetLeftTop, offsetRightBottom);
        }
        private void setPositionToChosenImage()
        {
            Canvas.SetLeft(((ImageObject)_listObject[_chosenElementIndex]).ImageStore.image, ((ImageObject)_listObject[_chosenElementIndex]).ImageStore.LeftTop.X);
            Canvas.SetTop(((ImageObject)_listObject[_chosenElementIndex]).ImageStore.image, ((ImageObject)_listObject[_chosenElementIndex]).ImageStore.LeftTop.Y);
            Canvas.SetRight(((ImageObject)_listObject[_chosenElementIndex]).ImageStore.image, ((ImageObject)_listObject[_chosenElementIndex]).ImageStore.RightBottom.X);
            Canvas.SetBottom(((ImageObject)_listObject[_chosenElementIndex]).ImageStore.image, ((ImageObject)_listObject[_chosenElementIndex]).ImageStore.RightBottom.Y);
        }
        private void newFileButton_Click(object sender, RoutedEventArgs e)
        {
            _isChooseObject = false;
            _isFill = false;

            _chosenElementIndex = -1;
            _frameChosen = null;

            _listObject.Clear();
            canvas.Children.Clear();
            positionStepPaste = 1;

            clearChooseButton();

            Grid.SetZIndex(canvas, 0);
            Grid.SetZIndex(border, 1);
        }
        private void clearChooseButton()
        {
            List<RadioButton> radioButtons = new List<RadioButton>();
            WalkLogicalTree(radioButtons, RibbonBar);
            foreach (RadioButton rb in radioButtons)
            {
                if (rb.GroupName == "radioButton")
                {
                    rb.IsChecked = false;
                }
            }
        }
        private void WalkLogicalTree(List<RadioButton> radioButtons, object parent)
        {
            DependencyObject doParent = parent as DependencyObject;
            if (doParent == null) return;
            foreach (object child in LogicalTreeHelper.GetChildren(doParent))
            {
                if (child is RadioButton)
                {
                    radioButtons.Add(child as RadioButton);
                }
                WalkLogicalTree(radioButtons, child);
            }
        }
        public static void SaveCanvasToFile(Window window, Canvas canvas, int dpi, string filename)
        {
            Size size = new Size(window.Width, window.Height);
            canvas.Measure(size);
            //canvas.Arrange(new Rect(size));

            var rtb = new RenderTargetBitmap(
                (int)window.Width, //width
                (int)window.Height, //height
                dpi, //dpi x
                dpi, //dpi y
                PixelFormats.Pbgra32 // pixelformat
                );
            rtb.Render(canvas);

            SaveRTBAsPNGBMP(rtb, filename);
        }
        private static void SaveRTBAsPNGBMP(RenderTargetBitmap bmp, string filename)
        {
            var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));

            using (var stm = System.IO.File.Create(filename))
            {
                enc.Save(stm);
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            turnOffAllMode();
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write);
                BinaryWriter br = new BinaryWriter(fs);

                foreach (var item in _listObject)
                {
                    if (item is ShapeObject)
                    {
                        string strokeString = "";
                        DoubleCollection list = ((ShapeObject)item).ShapeEntity.GetStrokeType();
                        if (list.Count != 0)
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                double stroke = list[i];
                                strokeString = strokeString + stroke.ToString() + " ";
                            }
                        }
                        else
                        {
                            strokeString = "null";
                        }

                        br.Write("shape");
                        br.Write(((ShapeObject)item).ShapeEntity.Name);
                        br.Write(((ShapeObject)item).ShapeEntity.GetLeftTop().X);
                        br.Write(((ShapeObject)item).ShapeEntity.GetLeftTop().Y);
                        br.Write(((ShapeObject)item).ShapeEntity.GetRightBottom().X);
                        br.Write(((ShapeObject)item).ShapeEntity.GetRightBottom().Y);
                        br.Write(((ShapeObject)item).ShapeEntity.GetThickness());
                        br.Write(((ShapeObject)item).ShapeEntity.GetStrokeColor().ToString());
                        br.Write(((ShapeObject)item).ShapeEntity.GetFillColor().ToString());
                        br.Write(strokeString.Trim());
                    }
                    else if(item is ImageObject)
                    {
                        br.Write("image");
                        br.Write(((ImageObject)item).ImageStore.image.Width);
                        br.Write(((ImageObject)item).ImageStore.image.Height);
                        br.Write(((ImageObject)item).ImageStore.image.Source.ToString());
                        br.Write(((ImageObject)item).ImageStore.LeftTop.X);
                        br.Write(((ImageObject)item).ImageStore.LeftTop.Y);
                        br.Write(((ImageObject)item).ImageStore.RightBottom.X);
                        br.Write(((ImageObject)item).ImageStore.RightBottom.Y);
                    }
                }
                br.Close();
            }

            e.Handled = true;
        }
        private void openButton_Click(object sender, RoutedEventArgs e)
        {
            positionStepPaste = 0;
            turnOffAllMode();
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);

                _listObject.Clear();
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    string typeCanvas = br.ReadString();

                    //Biến cho Shape
                    Point p1, p2;
                    Color strokeColor, fillColor;
                    int size;
                    string name, typeStroke;

                    //Biến cho Image
                    double widthImage, heightImage,imageTop, imageLeft, imageBottom, imageRight;
                    string imageBrush;

                    if (typeCanvas == "shape")
                    {
                        name = br.ReadString();
                        p1.X = br.ReadDouble();
                        p1.Y = br.ReadDouble();
                        p2.X = br.ReadDouble();
                        p2.Y = br.ReadDouble();
                        size = br.ReadInt32();
                        strokeColor = (Color)ColorConverter.ConvertFromString(br.ReadString());
                        fillColor = (Color)ColorConverter.ConvertFromString(br.ReadString());
                        typeStroke = br.ReadString();

                        IShapeEntity shape = null;
                        shape = (_shapesPrototypes[name].Clone() as IShapeEntity)!;
                        shape.HandleStart(p1);
                        shape.HandleEnd(p2);
                        shape.HandleThickness(size);
                        shape.HandleStrokeColor(strokeColor);
                        shape.HandleFillColor(fillColor );
                        if (typeStroke == "null")
                        {
                            typeStroke = null;
                        }
                        shape.HandleStrokeType(typeStroke);

                        _listObject.Add(new ShapeObject(shape.Clone() as IShapeEntity));
                    }
                    else if(typeCanvas == "image")
                    {
                        widthImage = br.ReadDouble();
                        heightImage = br.ReadDouble();
                        imageBrush = br.ReadString();
                        imageLeft = br.ReadDouble();
                        imageTop = br.ReadDouble();
                        imageRight = br.ReadDouble();
                        imageBottom = br.ReadDouble();

                        BitmapImage theImage = new BitmapImage();
                        theImage.BeginInit();
                        theImage.UriSource = new Uri(imageBrush, UriKind.RelativeOrAbsolute);
                        theImage.EndInit();

                        ImageStore imageSave = new ImageStore();

                        imageSave.image.Width = widthImage;
                        imageSave.image.Height = heightImage;
                        imageSave.image.Source = theImage;

                        Canvas.SetLeft(imageSave.image, imageLeft);
                        Canvas.SetTop(imageSave.image, imageTop);
                        Canvas.SetRight(imageSave.image, imageRight);
                        Canvas.SetBottom(imageSave.image, imageBottom);

                        imageSave.LeftTop = new Point(Canvas.GetLeft(imageSave.image), Canvas.GetTop(imageSave.image));
                        imageSave.RightBottom = new Point(Canvas.GetRight(imageSave.image), Canvas.GetBottom(imageSave.image));

                        _listObject.Add(new ImageObject(imageSave.Clone() as ImageStore));
                    }
                }
            }

            reDrawCanvas();
            e.Handled = true;
        }
        private void exportButton_Click(object sender, RoutedEventArgs e)
        {
            turnOffAllMode();

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Image"; // Default file name
            dlg.DefaultExt = ".png"; // Default file extension
            dlg.Filter = "PNG File (.png)|*.png"; // Filter files by extension
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string filename = dlg.FileName;
                SaveCanvasToFile(this, canvas, 96, filename);
            }

            e.Handled = true;
        }
        private void importButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage theImage = new BitmapImage(new Uri(openFileDialog.FileName, UriKind.Relative));
               
                ImageStore newImage = new ImageStore();

                if (theImage.Width > canvas.ActualWidth || theImage.Height > canvas.ActualHeight)
                {
                    if (theImage.Width > canvas.ActualWidth)
                    {
                        double scaleSize = theImage.Width / (canvas.ActualWidth / 4);
                        newImage.image.Height = theImage.Height / scaleSize;
                        newImage.image.Width = canvas.ActualWidth / 4;
                    }
                    if(theImage.Height > canvas.ActualHeight)
                    {  
                        double scaleSize = theImage.Height / (canvas.ActualHeight / 4);
                        newImage.image.Width = theImage.Width / scaleSize;
                        newImage.image.Height = canvas.ActualHeight / 4;
                    }
                }
                else
                {
                    newImage.image.Width = theImage.Width / 3;
                    newImage.image.Height = theImage.Height / 3;
                }
                newImage.image.Source = theImage;

                Canvas.SetTop(newImage.image, 5 * positionStepPaste);
                Canvas.SetLeft(newImage.image, 5 * positionStepPaste);
                Canvas.SetBottom(newImage.image, newImage.image.Height + 5 * positionStepPaste);
                Canvas.SetRight(newImage.image, newImage.image.Width + 5 * positionStepPaste);

                newImage.LeftTop = new Point(Canvas.GetTop(newImage.image), Canvas.GetLeft(newImage.image));
                newImage.RightBottom = new Point(Canvas.GetRight(newImage.image), Canvas.GetBottom(newImage.image));

                _listObject.Add(new ImageObject(newImage.Clone() as ImageStore));
                canvas.Children.Add(((ImageObject)_listObject[_listObject.Count - 1]).ImageStore.image);
            }
            positionStepPaste++;

            e.Handled = true;
        }

        private void undoButton_Click(object sender, RoutedEventArgs e)
        {
            if (canvas.Children.Count > 0)
            {
                //if (_drawnShapes[_drawnShapes.Count - 1] == null)
                //{
                //    undoImage.Add(imageImport[imageImport.Count - 1]);
                //    imageImport.RemoveAt(imageImport.Count - 1);
                //    undoBitmapImage.Add(bitmapImageImport[bitmapImageImport.Count - 1]);
                //    bitmapImageImport.RemoveAt(bitmapImageImport.Count - 1);
                //}

                //_stackUndoShape.Add(_drawnShapes[_drawnShapes.Count - 1]);
                //_drawnShapes.RemoveAt(_drawnShapes.Count - 1);

                //canvas.Children.RemoveAt(canvas.Children.Count - 1);
            }

        }
        private void redoButton_Click(object sender, RoutedEventArgs e)
        {
            //if (_stackUndoShape.Count > 0)
            //{
                //_drawnShapes.Add(_stackUndoShape[_stackUndoShape.Count - 1]);
                //_stackUndoShape.RemoveAt(_stackUndoShape.Count - 1);

                //if (_drawnShapes[_drawnShapes.Count - 1] == null)
                //{
                //    imageImport.Add(undoImage[undoImage.Count - 1]);
                //    undoImage.RemoveAt(undoImage.Count - 1);
                //    bitmapImageImport.Add(undoBitmapImage[undoBitmapImage.Count - 1]);
                //    undoBitmapImage.RemoveAt(undoBitmapImage.Count - 1);

                //    canvas.Children.Add(imageImport[imageImport.Count - 1].image);
                //} 
                //else if(_drawnShapes[_drawnShapes.Count - 1] != null)
                //{
                //    IPaintBusiness painter = _painterPrototypes[_drawnShapes[_drawnShapes.Count-1].Name];
                //    UIElement shape = painter.Draw(_drawnShapes[_drawnShapes.Count - 1]);
                //    canvas.Children.Add(shape);
                //}
            //}
        }

        private void SizeGallery_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            foreach (RibbonGalleryItem item in SizeCategory.Items)
            {
                if (item.IsSelected)
                {
                    _currentThickness = Convert.ToInt32(item.Tag);
                    if (_preview != null)
                    {
                        _preview.HandleThickness(_currentThickness);
                    }
                }
            }
        }
        private void StrokeColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (StrokeColor.SelectedColor != null)
            {
                _currentStrokeColor = (Color)StrokeColor.SelectedColor;
                if (_preview != null)
                {
                    _preview.HandleStrokeColor(_currentStrokeColor);
                }
            }
        }
        private void FillColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (FillColor.SelectedColor != null)
            {
                _currentFillColor = (Color)FillColor.SelectedColor;
                if (_preview != null)
                {
                    _preview.HandleFillColor(_currentFillColor);
                }
            }
        }
        private void StrokeTypeGallery_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            foreach (RibbonGalleryItem item in StrokeTypeCategory.Items)
            {
                if (item.IsSelected)
                {
                    _currentStrokeType = (string)item.Tag;
                    if (_preview != null)
                    {
                        _preview.HandleStrokeType(_currentStrokeType);
                    }
                }
            }
        }

        private void border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (_drawMode)
            {
                _isDrawing = true;
                _preview.HandleStart(e.GetPosition(canvas));
            } 
        }
        private void border_MouseMove(object sender, MouseEventArgs e)
        {
            var end = e.GetPosition(canvas);
            if(_drawMode)
                this.Cursor = Cursors.Cross;

            if (_drawMode && _isDrawing)
            {
                _preview.HandleEnd(end);

                reDrawCanvas();

                var previewPainter = _painterPrototypes[_preview.Name];
                var previewElement = previewPainter.Draw(_preview);
                canvas.Children.Add(previewElement);
            }
        }
        private void border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_drawMode)
            {
                _isDrawing = false;

                var end = e.GetPosition(canvas); // Điểm kết thúc
                _preview.HandleEnd(end);

                var previewPainter = _painterPrototypes[_preview.Name];
                var previewElement = previewPainter.Draw(_preview);

                _listObject.Add(new ShapeObject(_preview.Clone() as IShapeEntity));
            }
        }

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var canvasControl = sender as Canvas;
            if (canvasControl == null)
                return;
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(canvasControl, e.GetPosition(canvasControl));
            UIElement element = hitTestResult.VisualHit as UIElement;

            // Chỉ xét trường hợp nhấn vào các object trên canvas
            if (element != null && _listObject.Count > 0 && 
                (_shapesPrototypes.ContainsKey(element.GetType().Name) || element.GetType().Name == "Image") && (_isChooseObject || _isFill))
            {
                int idx = getIndexOfElement(element);

                if (_isFill && element.GetType().Name != "Image")
                {
                    Brush br = new SolidColorBrush(_currentFillColor);

                    ((ShapeObject)_listObject[idx]).ShapeEntity.HandleFillColor(_currentFillColor);

                    element.GetType().GetProperty("Fill").SetValue(element, br);

                    return;
                }

                if (idx == _chosenElementIndex && _isChooseObject)
                {
                    _isDrag = true;

                    (_offsetLeftTop, _offsetRightBottom) = getOffsetOfElement(e.GetPosition(canvas));

                    return;
                }

                if (idx != _chosenElementIndex && _chosenElementIndex != -1 && _isChooseObject)
                {
                    if (_listObject[_chosenElementIndex] is ImageObject)
                    {
                        canvas.Children.Remove(_frameChosen);
                        _frameChosen.Child = null;

                        setPositionToChosenImage();
                        canvas.Children.Add(((ImageObject)_listObject[_chosenElementIndex]).ImageStore.image);
                    }
                    else if (_listObject[_chosenElementIndex] is ShapeObject)
                    {
                        canvas.Children.Remove(_frameChosen);
                        _frameChosen.Child = null;

                        var painter = _painterPrototypes[((ShapeObject)_listObject[_chosenElementIndex]).ShapeEntity.Name];
                        var ele = painter.Draw(((ShapeObject)_listObject[_chosenElementIndex]).ShapeEntity);

                        canvas.Children.Add(ele);
                    }
                }
                _chosenElementIndex = idx;
                canvas.Children.Remove(element);

                _frameChosen = new Border();
                _frameChosen.BorderBrush = Brushes.SkyBlue;
                _frameChosen.BorderThickness = new Thickness(3);
                _frameChosen.Padding = new Thickness(2);
                _frameChosen.Child = element;

                Canvas.SetLeft(_frameChosen, Canvas.GetLeft(element) - 5);
                Canvas.SetTop(_frameChosen, Canvas.GetTop(element) - 5);
                Canvas.SetRight(_frameChosen, Canvas.GetRight(element) - 5);
                Canvas.SetBottom(_frameChosen, Canvas.GetBottom(element) - 5);

                canvas.Children.Add(_frameChosen);

            }
            else
            {
                // Xét trường hợp không nhấn vào object nữa mà nhấn ra vùng trắng để bỏ chế độ chọn object
                clearChoooseObject();
            }
        }
        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isFill)
            {
                this.Cursor = ((TextBlock)this.Resources["CursorPaint"]).Cursor;
            }
            if (_isDrag)
            {
                var position = e.GetPosition(canvas);

                var left = position.X - _offsetLeftTop.X;
                var top = position.Y - _offsetLeftTop.Y > 0 ? position.Y - _offsetLeftTop.Y : 1;
                var right = position.X + _offsetRightBottom.X;
                var bottom = position.Y + _offsetRightBottom.Y;

                Canvas.SetLeft(_frameChosen, left);
                Canvas.SetTop(_frameChosen, top);
                Canvas.SetRight(_frameChosen, right);
                Canvas.SetBottom(_frameChosen, bottom);
            }
        }
        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_chosenElementIndex == -1 || _frameChosen == null)
            {
                _isDrag = false;
                return;
            }

            if (_isDrag)
            {
                if(_listObject[_chosenElementIndex] is ShapeObject)
                {
                    Point top_left = new Point(Canvas.GetLeft(_frameChosen) + 5, Canvas.GetTop(_frameChosen) + 5);
                    Point right_bottom = new Point(Canvas.GetRight(_frameChosen) + 5, Canvas.GetBottom(_frameChosen) + 5);

                    ((ShapeObject)_listObject[_chosenElementIndex]).ShapeEntity.HandleStart(top_left);
                    ((ShapeObject)_listObject[_chosenElementIndex]).ShapeEntity.HandleEnd(right_bottom);
                }
                else if(_listObject[_chosenElementIndex] is ImageObject)
                {
                    ((ImageObject)_listObject[_chosenElementIndex]).ImageStore.LeftTop = new Point(Canvas.GetLeft(_frameChosen) + 5, Canvas.GetTop(_frameChosen) + 5);
                    ((ImageObject)_listObject[_chosenElementIndex]).ImageStore.RightBottom = new Point(Canvas.GetRight(_frameChosen) + 5, Canvas.GetBottom(_frameChosen) + 5);

                    setPositionToChosenImage();
                } 
            }
            _isDrag = false;
        }

        private void chooseShapeBtnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as RibbonRadioButton;
            var entity = button.Tag as IShapeEntity;

            if (entity != null)
            {
                if (_currentType != entity.Name || _currentType == "")
                {
                    _drawMode = true;
                    _isDrag = false;
                    _isChooseObject = false;
                    _isFill = false;

                    _currentType = entity!.Name;

                    _preview = (_shapesPrototypes[_currentType].Clone() as IShapeEntity)!;
                    _preview.HandleStrokeColor(_currentStrokeColor);
                    _preview.HandleFillColor(_currentFillColor);
                    _preview.HandleThickness(_currentThickness);
                    _preview.HandleStrokeType(_currentStrokeType);

                    Grid.SetZIndex(canvas, 0);
                    Grid.SetZIndex(border, 1);

                    clearChoooseObject();

                    _chosenElementIndex = -1;
                    _frameChosen = null;
                    _isChooseObject = false;
                    _isFill = false;
                }
            }
        }
        private void cursorButton_Click(object sender, RoutedEventArgs e)
        {
            _drawMode = false;
            _isChooseObject = true;
            _isFill = false;

            _currentType = "";

            Grid.SetZIndex(canvas, 1);
            Grid.SetZIndex(border, 0);
        }
        private void fillButton_Click(object sender, RoutedEventArgs e)
        {
            _drawMode = false;
            _isChooseObject = false;
            _isFill = true;

            _currentType = "";

            Grid.SetZIndex(canvas, 1);
            Grid.SetZIndex(border, 0);

            clearChoooseObject();
        }

        private void Ribbon_MouseMove(object sender, MouseEventArgs e)
        {

            this.Cursor = Cursors.Arrow;
        }

        private void zoomInButton_Click(object sender, RoutedEventArgs e)
        {
            if (canvas.ActualWidth > 300)
            {
                canvas.Width = canvas.ActualWidth * 0.8;
                canvas.Height = canvas.ActualHeight * 0.8;
                border.Width = border.ActualWidth * 0.8;
                border.Height = border.ActualHeight * 0.8;
            }
        }
        private void zoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (canvas.ActualWidth < 1500)
            {
                canvas.Width = canvas.ActualWidth / 0.8;
                canvas.Height = canvas.ActualHeight / 0.8;
                border.Width = border.ActualWidth / 0.8;
                border.Height = border.ActualHeight / 0.8;
            }
        }

        private void pasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (tempStoreUIElement == null)
                return;

            if (tempStoreUIElement is ShapeObject)
            {
                IShapeEntity shape = ((ShapeObject)tempStoreUIElement).ShapeEntity.Clone() as IShapeEntity;
                IPaintBusiness painter = _painterPrototypes[shape.Name];

                shape.HandleStart(new Point(5 * positionStepPaste, 5 * positionStepPaste));
                shape.HandleEnd(new Point(
                    shape.GetRightBottom().X - (shape.GetLeftTop().X - 5 * positionStepPaste),
                    shape.GetRightBottom().Y - (shape.GetLeftTop().Y - 5 * positionStepPaste)));

                _listObject.Add(new ShapeObject(shape.Clone() as IShapeEntity));

                reDrawCanvas();
            }
            if(tempStoreUIElement is ImageObject)
            {
                ImageStore pasteImage = new ImageStore();
                pasteImage.image.Source = ((ImageObject)tempStoreUIElement).ImageStore.image.Source;
                pasteImage.image.Width = ((ImageObject)tempStoreUIElement).ImageStore.image.Width;
                pasteImage.image.Height = ((ImageObject)tempStoreUIElement).ImageStore.image.Height;

                Canvas.SetLeft(pasteImage.image, 5 * positionStepPaste);
                Canvas.SetTop(pasteImage.image, 5 * positionStepPaste);
                Canvas.SetRight(pasteImage.image, pasteImage.image.Width + 5 * positionStepPaste);
                Canvas.SetBottom(pasteImage.image, pasteImage.image.Height + 5 * positionStepPaste);
     

                pasteImage.LeftTop = new Point(Canvas.GetLeft(pasteImage.image), Canvas.GetTop(pasteImage.image));
                pasteImage.RightBottom = new Point(Canvas.GetRight(pasteImage.image), Canvas.GetBottom(pasteImage.image));

                _listObject.Add(new ImageObject(pasteImage.Clone() as ImageStore));

                reDrawCanvas();
            }
            positionStepPaste++;
        }
        private void cutButton_Click(object sender, RoutedEventArgs e)
        {
            if (_chosenElementIndex != -1)
            {
                tempStoreUIElement = _listObject[_chosenElementIndex].Clone() as UIElementObject;

                canvas.Children.Remove(_frameChosen);
                _frameChosen.Child = null;
                _frameChosen = null;

                _listObject.RemoveAt(_chosenElementIndex);
                _chosenElementIndex = -1;
            }
        }
        private void copyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_chosenElementIndex != -1)
            {
                tempStoreUIElement = _listObject[_chosenElementIndex];

                clearChoooseObject();
            }
        }
    }
}
