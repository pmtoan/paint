using IContract;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
        bool _isZoomIn = false;
        bool _isZoomOut = false;

        string _currentType = "";
        int _currentThickness = 1;
        Color _currentStrokeColor = Colors.Black;
        Color _currentFillColor = Colors.Transparent;
        string _currentStrokeType = null;
        IShapeEntity _preview = null;

        Point _start;
        List<IShapeEntity> _drawnShapes = new List<IShapeEntity>();
        List<IShapeEntity> _stackUndoShape = new List<IShapeEntity>();

        // Cấu hình
        Dictionary<string, IPaintBusiness> _painterPrototypes = new Dictionary<string, IPaintBusiness>();
        Dictionary<string, IShapeEntity> _shapesPrototypes = new Dictionary<string, IShapeEntity>();

        List<Canvas> imageImport = new List<Canvas>();
        List<BitmapImage> bitmapImageImport = new List<BitmapImage>();

        List<Canvas> undoImage = new List<Canvas>();
        List<BitmapImage> undoBitmapImage = new List<BitmapImage>();

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

            Title = $"Tìm thấy {_shapesPrototypes.Count} hình";

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
                        IPaintBusiness painter = _painterPrototypes[item.Name];
                        string strokeString = "";
                        DoubleCollection list = painter.StrokeType(item);
                        if (painter.StrokeType(item).Count != 0)
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
                        br.Write(painter.PositionX1(item));
                        br.Write(painter.PositionY1(item));
                        br.Write(painter.PositionX2(item));
                        br.Write(painter.PositionY2(item));
                        br.Write(painter.Thickness(item));
                        br.Write(painter.Color(item));
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
                FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);
                _drawnShapes.Clear();
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    string typeCanvas = br.ReadString();

                    //Biến cho Shape
                    Point p1, p2;
                    Color color;
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
                        color = (Color)ColorConverter.ConvertFromString(br.ReadString());
                        typeStroke = br.ReadString();

                        IShapeEntity shape = null;
                        shape = (_shapesPrototypes[name].Clone() as IShapeEntity)!;
                        shape.HandleStart(p1);
                        shape.HandleEnd(p2);
                        shape.HandleThickness(size);
                        shape.HandleColor(color);
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
            if (_drawnShapes.Count >= 1)
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
                    _preview.HandleColor(_currentStrokeColor);
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
                    _currentStrokeType = item.Tag as string;
                    if (_preview != null)
                    {
                        _preview.HandleStrokeType(_currentStrokeType);
                    }
                }
            }
        }

        private void eraserButton_Click(object sender, RoutedEventArgs e)
        {
            _drawMode = false;
            Grid.SetZIndex(canvas, 1);
            Grid.SetZIndex(border, 0);
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
                    _currentType = entity!.Name;

                    _preview = (_shapesPrototypes[_currentType].Clone() as IShapeEntity)!;
                    _preview.HandleColor(_currentStrokeColor);
                    _preview.HandleThickness(_currentThickness);
                    _preview.HandleStrokeType(_currentStrokeType);

                    Grid.SetZIndex(canvas, 0);
                    Grid.SetZIndex(border, 1);
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

                _start = e.GetPosition(canvas);
                _preview.HandleStart(_start);
            } 
            else
            {
                //Point pt = e.GetPosition((UIElement)sender);

                //// Perform the hit test against a given portion of the visual object tree.
                //HitTestResult result = VisualTreeHelper.HitTest(canvas, pt);

                //if (result != null)
                //{
                //    if (e.OriginalSource is UIElement)
                //    {
                //        UIElement a = (UIElement)e.OriginalSource;
                //        Debug.WriteLine(a.GetType());
                //        foreach (UIElement f in canvas.Children)
                //        {
                //            if (f.Equals(a))
                //            {
                //                Brush br = new SolidColorBrush(Colors.Yellow);
                //                f.GetType().GetProperty("Fill").SetValue(f, br);

                //                Debug.WriteLine(canvas.Children.IndexOf(f));

                //            }

                //        }

                //    }
                //}
            }
        }

        private void border_MouseMove(object sender, MouseEventArgs e)

        {
            if (_drawMode && _isDrawing && !_finishShape)
            {
                var end = e.GetPosition(canvas);
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
        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("Canvas mouse down");

            if(e.OriginalSource is UIElement)
            {
                UIElement a = (UIElement)e.OriginalSource;
                Debug.WriteLine(a.GetType());
                foreach(UIElement f in canvas.Children)
                {
                    if(f.Equals(a))
                    {
                        Brush br = new SolidColorBrush(Colors.Yellow);
                        f.GetType().GetProperty("Fill").SetValue(f, br);

                        Debug.WriteLine(canvas.Children.IndexOf(f));
                        
                    }
                 
                }
                
            }
        }

        private void zoomInButton_Click(object sender, RoutedEventArgs e)
        {
            _isZoomIn = !_isZoomIn;
        }

        private void zoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            _isZoomOut = !_isZoomOut;
        }

    }
}
