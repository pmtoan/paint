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
        bool _finishShape = false;
        bool _isDrag = false;
        bool _isChooseObject = false;
        bool _isFill = false;

        string _currentType = "";
        int _currentThickness = 1;
        Color _currentStrokeColor = Colors.Black;
        Color _currentFillColor = Colors.Transparent;
        string _currentStrokeType = null;

        IShapeEntity _preview = null;
        IShapeEntity _prevShape = null;

        int _chosenElementIndex = -1;
        Border _frameChosen = null;
        Point _offsetLeftTop;
        Point _offsetRightBottom;

        IShapeEntity tempStoreShapeEntity = null;

        List<IShapeEntity> _drawnShapes = new List<IShapeEntity>();
        List<IShapeEntity> _stackUndoShape = new List<IShapeEntity>();

        List<Canvas> imageImport = new List<Canvas>();
        List<BitmapImage> bitmapImageImport = new List<BitmapImage>();

        List<Canvas> undoImage = new List<Canvas>();
        List<BitmapImage> undoBitmapImage = new List<BitmapImage>();

        RibbonRadioButton _button;

        // Cấu hình
        Dictionary<string, IPaintBusiness> _painterPrototypes = new Dictionary<string, IPaintBusiness>();
        Dictionary<string, IShapeEntity> _shapesPrototypes = new Dictionary<string, IShapeEntity>();

        class PluginItems
        {
            public IShapeEntity PluginEntity { get; set; }
            public string PluginIconPath { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write);
                BinaryWriter br = new BinaryWriter(fs);
                int count = 0;
                foreach (var item in _drawnShapes)
                {
                    if (item != null)
                    {
                        string strokeString = "";
                        DoubleCollection list = item.GetStrokeType();
                        if (item.GetStrokeType().Count != 0)
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
                        br.Write(item.Name);
                        br.Write(item.GetLeftTop().X);
                        br.Write(item.GetLeftTop().Y);
                        br.Write(item.GetRightBottom().X);
                        br.Write(item.GetRightBottom().Y);
                        br.Write(item.GetThickness());
                        br.Write(item.GetStrokeColor().ToString());
                        br.Write(item.GetFillColor().ToString());
                        br.Write(strokeString.Trim());
                    }
                    else if(item == null)
                    {
                        br.Write("image");
                        br.Write(imageImport[count].Width.ToString());
                        br.Write(imageImport[count].Height.ToString());
                        br.Write(bitmapImageImport[count].ToString());
                        count++;
                    }
                }
                br.Close();
            }

            e.Handled = true;
        }
        private void openButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                _isDrawing = false;
                _drawMode = false;
                _finishShape = false;

                _currentType = "";

                _drawnShapes = new List<IShapeEntity>();
                _stackUndoShape = new List<IShapeEntity>();
                imageImport = new List<Canvas>();
                bitmapImageImport = new List<BitmapImage>();

                undoImage = new List<Canvas>();
                undoBitmapImage = new List<BitmapImage>();
                canvas.Children.Clear();


                FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);
                _drawnShapes.Clear();
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    string typeCanvas = br.ReadString();

                    //Biến cho Shape
                    Point p1, p2;
                    Color strokeColor, fillColor;
                    int size;
                    string name, typeStroke;

                    //Biến cho Image
                    string widthImage, heightImage, imageBrush;

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
                        _drawnShapes.Add(shape);
                    }
                    else if(typeCanvas == "image")
                    {
                        widthImage = br.ReadString();
                        heightImage = br.ReadString();
                        imageBrush = br.ReadString();

                        IShapeEntity shape = null;
                        _drawnShapes.Add(shape);

                        Canvas imageSave = new Canvas();
                        imageSave.Width = Convert.ToDouble(widthImage);
                        imageSave.Height = Convert.ToDouble(heightImage);

                        Uri imageUri = new Uri(imageBrush, UriKind.Relative);
                        BitmapImage theImage = new BitmapImage(imageUri);
                        ImageBrush myImageBrush = new ImageBrush(theImage);
                        imageSave.Background = myImageBrush;
                        imageImport.Add(imageSave);
                        bitmapImageImport.Add(theImage);
                    }
                }

                int _count = 0;
                
                foreach (var item in _drawnShapes)
                {
                    if (item == null && imageImport[_count] != null)
                    {
                        canvas.Children.Add(imageImport[_count]);
                        _count++;
                    }
                    else
                    {
                        IPaintBusiness painter = _painterPrototypes[item.Name];
                        UIElement shape = painter.Draw(item);

                        canvas.Children.Add(shape);
                    }
                }
            }

            e.Handled = true;
        }
        private void exportButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                Size size = new Size(canvas.ActualWidth, canvas.ActualHeight);
                canvas.Measure(size);
                canvas.Arrange(new Rect(size));

                RenderTargetBitmap renderBitmap =
                new RenderTargetBitmap(
                   (int)size.Width,
                   (int)size.Height,
                   96d,
                   96d,
                   PixelFormats.Pbgra32);
                renderBitmap.Render(canvas);

                using (FileStream outStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    BmpBitmapEncoder encoder = new BmpBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    encoder.Save(outStream);
                }
            }

            e.Handled = true;
        }
        private void importButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage theImage = new BitmapImage
                (new Uri(openFileDialog.FileName, UriKind.Relative));

                ImageBrush myImageBrush = new ImageBrush(theImage);
                Canvas newImage = new Canvas();
                newImage.Width = theImage.Width / 3;
                newImage.Height = theImage.Height / 3;
                newImage.Background = myImageBrush;
                imageImport.Add(newImage);
                bitmapImageImport.Add(theImage);
                IShapeEntity shapeImage = null;
                _drawnShapes.Add(shapeImage);
            }

            int _count = 0;
            canvas.Children.Clear();
            foreach (var item in _drawnShapes)
            {
                if (item == null && imageImport[_count] != null)
                {
                    canvas.Children.Add(imageImport[_count]);
                    _count++;
                }
                else
                {
                    IPaintBusiness painter = _painterPrototypes[item.Name];
                    UIElement shape = painter.Draw(item);

                    canvas.Children.Add(shape);
                }
            }

            e.Handled = true;
        }

        private void undoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_drawnShapes.Count > 0 && canvas.Children.Count > 0)
            {
                if (_drawnShapes[_drawnShapes.Count - 1] == null)
                {
                    undoImage.Add(imageImport[imageImport.Count - 1]);
                    imageImport.RemoveAt(imageImport.Count - 1);
                    undoBitmapImage.Add(bitmapImageImport[bitmapImageImport.Count - 1]);
                    bitmapImageImport.RemoveAt(bitmapImageImport.Count - 1);
                }

                _stackUndoShape.Add(_drawnShapes[_drawnShapes.Count - 1]);
                _drawnShapes.RemoveAt(_drawnShapes.Count - 1);

                canvas.Children.RemoveAt(canvas.Children.Count - 1);
            }

        }
        private void redoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_stackUndoShape.Count > 0)
            {
                _drawnShapes.Add(_stackUndoShape[_stackUndoShape.Count - 1]);
                _stackUndoShape.RemoveAt(_stackUndoShape.Count - 1);

                if (_drawnShapes[_drawnShapes.Count - 1] == null)
                {
                    imageImport.Add(undoImage[undoImage.Count - 1]);
                    undoImage.RemoveAt(undoImage.Count - 1);
                    bitmapImageImport.Add(undoBitmapImage[undoBitmapImage.Count - 1]);
                    undoBitmapImage.RemoveAt(undoBitmapImage.Count - 1);

                    canvas.Children.Add(imageImport[imageImport.Count - 1]);
                } 
                else if(_drawnShapes[_drawnShapes.Count - 1] != null)
                {
                    IPaintBusiness painter = _painterPrototypes[_drawnShapes[_drawnShapes.Count-1].Name];
                    UIElement shape = painter.Draw(_drawnShapes[_drawnShapes.Count - 1]);
                    canvas.Children.Add(shape);
                }
            }
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

        private void chooseShapeBtnClick(object sender, RoutedEventArgs e)
        {
            _button = sender as RibbonRadioButton;
            var entity = _button.Tag as IShapeEntity;
            if (_prevShape == entity)
            {
                _drawMode = !_drawMode;
                _button.IsChecked = !_button.IsChecked;
            }
            else
            {
                if (entity != null)
                {
                    if (_currentType != entity.Name || _currentType == "")
                    {
                        _drawMode = true;
                        _currentType = entity!.Name;

                        _preview = (_shapesPrototypes[_currentType].Clone() as IShapeEntity)!;
                        _preview.HandleStrokeColor(_currentStrokeColor);
                        _preview.HandleFillColor(_currentFillColor);
                        _preview.HandleThickness(_currentThickness);
                        _preview.HandleStrokeType(_currentStrokeType);

                        Grid.SetZIndex(canvas, 0);
                        Grid.SetZIndex(border, 1);

                        clearChoooseMode();

                        _chosenElementIndex = -1;
                        _frameChosen = null;
                        _isChooseObject = false;
                        _isFill = false;
                    }
                    _prevShape = entity;
                }
            }
        }

        private void border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (_drawMode)
            {
                _isDrawing = true;
                _finishShape = false;

                _preview.HandleStart(e.GetPosition(canvas));
            } 
        }
        private void border_MouseMove(object sender, MouseEventArgs e)

        {
            var end = e.GetPosition(canvas);
            if (_drawMode && _isDrawing && !_finishShape)
            {
                
                _preview.HandleEnd(end);

                canvas.Children.Clear();

                foreach (var item in _drawnShapes)
                {
                    IPaintBusiness painter = _painterPrototypes[item.Name];
                    UIElement shape = painter.Draw(item);

                    canvas.Children.Add(shape);
                }

                var previewPainter = _painterPrototypes[_preview.Name];
                var previewElement = previewPainter.Draw(_preview);
                canvas.Children.Add(previewElement);
            }

            if (_drawMode)
            {
                this.Cursor = Cursors.Cross;
            }
        }
        private void border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_drawMode)
            {
                _isDrawing = false;
                _finishShape = true;

                var end = e.GetPosition(canvas); // Điểm kết thúc

                _preview.HandleEnd(end);

                _drawnShapes.Add(_preview.Clone() as IShapeEntity);
            }
        }

        private void clearChoooseMode()
        {
            if (_frameChosen != null && _chosenElementIndex != -1)
            {
                canvas.Children.Remove(_frameChosen);
                _frameChosen.Child = null;

                var painter = _painterPrototypes[_drawnShapes[_chosenElementIndex].Name];
                var ele = painter.Draw(_drawnShapes[_chosenElementIndex]);

                canvas.Children.Add(ele);

                _chosenElementIndex = -1;
                _frameChosen = null;
            }

        }

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var canvasControl = sender as Canvas;
            if (canvasControl == null)
                return;

            HitTestResult hitTestResult = VisualTreeHelper.HitTest(canvasControl, e.GetPosition(canvasControl));
            var element = hitTestResult.VisualHit;
          
            // Chỉ xét trường hợp nhấn vào các object trên canvas
            if (element != null && _shapesPrototypes.ContainsKey(element.GetType().Name))
            {
                int idx = -1;
                double left = Canvas.GetLeft(element as UIElement);
                double top = Canvas.GetTop(element as UIElement);

                idx = _drawnShapes.FindIndex(s => s.GetLeftTop().X == left && s.GetLeftTop().Y == top);

                double right = _drawnShapes[idx].GetRightBottom().X;
                double bottom = _drawnShapes[idx].GetRightBottom().Y;

                if (_isFill)
                {
                    Brush br = new SolidColorBrush(_currentFillColor);
                    _drawnShapes[idx].HandleFillColor(_currentFillColor);
                    element.GetType().GetProperty("Fill").SetValue(element, br);

                    return;
                }

                if (idx == _chosenElementIndex && _isChooseObject)
                {
                    _isDrag = true;

                    _offsetLeftTop = e.GetPosition(canvas);
                    _offsetRightBottom = e.GetPosition(canvas);

                    _offsetLeftTop.X -= Canvas.GetLeft(_frameChosen);
                    _offsetLeftTop.Y -= Canvas.GetTop(_frameChosen);

                    _offsetRightBottom.X = Canvas.GetRight(_frameChosen) - _offsetRightBottom.X;
                    _offsetRightBottom.Y = Canvas.GetBottom(_frameChosen) - _offsetRightBottom.Y;

                    return;
                }

                if (idx != _chosenElementIndex && _chosenElementIndex != -1 && _isChooseObject)
                {
                    canvas.Children.Remove(_frameChosen);
                    _frameChosen.Child = null;

                    var painter = _painterPrototypes[_drawnShapes[_chosenElementIndex].Name];
                    var ele = painter.Draw(_drawnShapes[_chosenElementIndex]);
                    canvas.Children.Add(ele);
                }
                _chosenElementIndex = idx;
                canvas.Children.Remove(element as UIElement);

                _frameChosen = new Border();
                _frameChosen.BorderBrush = Brushes.SkyBlue;
                _frameChosen.BorderThickness = new Thickness(3);
                _frameChosen.Padding = new Thickness(2);
                _frameChosen.Child = element as UIElement;

                Canvas.SetLeft(_frameChosen, left - 5);
                Canvas.SetTop(_frameChosen, top - 5);
                Canvas.SetRight(_frameChosen, right - 5);
                Canvas.SetBottom(_frameChosen, bottom - 5);

                canvas.Children.Add(_frameChosen);

                
            }
            else
            {
                // Xét trường hợp không nhấn vào object nữa mà nhấn ra vùng trắng để bỏ chế độ chọn object
                clearChoooseMode();
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
                return;

            if (_isDrag)
            {
                Point top_left = _drawnShapes[_chosenElementIndex].GetLeftTop();
                top_left.X = Canvas.GetLeft(_frameChosen) + 5;
                top_left.Y = Canvas.GetTop(_frameChosen) + 5;

                Point right_bottom = _drawnShapes[_chosenElementIndex].GetRightBottom();
                right_bottom.X = Canvas.GetRight(_frameChosen) + 5;
                right_bottom.Y = Canvas.GetBottom(_frameChosen) + 5;

                _drawnShapes[_chosenElementIndex].HandleStart(top_left);
                _drawnShapes[_chosenElementIndex].HandleEnd(right_bottom);
            }
            _isDrag = false;
        }

        private void Ribbon_MouseMove(object sender, MouseEventArgs e)
        {

            this.Cursor = Cursors.Arrow;
        }

        private void cursorButton_Click(object sender, RoutedEventArgs e)
        {
            _button = sender as RibbonRadioButton;
            if (_isChooseObject == true)
            {
                _isChooseObject = !_isChooseObject;
                Grid.SetZIndex(canvas, 0);
                Grid.SetZIndex(border, 1);

                clearChoooseMode();

                _chosenElementIndex = -1;
                _frameChosen = null;
                _isChooseObject = false;
                _button.IsChecked = !_button.IsChecked;
            }
            else
            {
                _isChooseObject = true;
                Grid.SetZIndex(canvas, 1);
                Grid.SetZIndex(border, 0);
            }
            _drawMode = false;
            _isFill = false;
            _prevShape = null;

        }
        private void fillButton_Click(object sender, RoutedEventArgs e)
        {
            _isFill = true;
            _isChooseObject = false;
            _drawMode = false;

            Grid.SetZIndex(canvas, 1);
            Grid.SetZIndex(border, 0);

            clearChoooseMode();
        }
        private void eraserButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void pasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (tempStoreShapeEntity != null)
            {
                var painter = _painterPrototypes[tempStoreShapeEntity.Name];

                tempStoreShapeEntity.HandleStart(new Point(10, 10));
                tempStoreShapeEntity.HandleEnd(new Point(
                  tempStoreShapeEntity.GetRightBottom().X - (tempStoreShapeEntity.GetLeftTop().X - 10),
                  tempStoreShapeEntity.GetRightBottom().Y - (tempStoreShapeEntity.GetLeftTop().Y - 10)));

                var ele = painter.Draw(tempStoreShapeEntity);

                canvas.Children.Add(ele);
                _drawnShapes.Add(tempStoreShapeEntity.Clone() as IShapeEntity);
            }
        }
        private void cutButton_Click(object sender, RoutedEventArgs e)
        {

            Grid.SetZIndex(canvas, 0);
            Grid.SetZIndex(border, 1);

            clearChoooseMode();

            _chosenElementIndex = -1;
            _frameChosen = null;
            _isChooseObject = false;
            _isFill = false;

            if (_chosenElementIndex != -1)
            {
                tempStoreShapeEntity = _drawnShapes[_chosenElementIndex].Clone() as IShapeEntity;
                UIElement canvasDeleteShape = null;
                foreach (UIElement element in canvas.Children)
                {
                    Point LeftTop = new Point(Canvas.GetLeft(element) + 5, Canvas.GetTop(element) + 5);

                    if (LeftTop == tempStoreShapeEntity.GetLeftTop())
                    {
                        canvasDeleteShape = element;
                    }
                }
                if(canvasDeleteShape != null)
                {
                    canvas.Children.Remove(canvasDeleteShape);
                }

                _drawnShapes.RemoveAt(_chosenElementIndex);
            }
        }
        private void copyButton_Click(object sender, RoutedEventArgs e)
        {

            Grid.SetZIndex(canvas, 0);
            Grid.SetZIndex(border, 1);

            clearChoooseMode();

            _chosenElementIndex = -1;
            _frameChosen = null;
            _isChooseObject = false;
            _isFill = false;

            if (_chosenElementIndex != -1)
            {
                tempStoreShapeEntity = _drawnShapes[_chosenElementIndex].Clone() as IShapeEntity;
            }
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
    }
}
