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
        bool _drawShape = false;

        string _currentType = "";
        int _currentThickness = 1;
        Color _currentStrokeColor = Colors.Black;
        Color _currentFillColor = Colors.Black;
        string _currentStrokeType = null;
        IShapeEntity _preview = null;

        Point _start;
        List<IShapeEntity> _drawnShapes = new List<IShapeEntity>();
        List<IShapeEntity> _stackUndoShape = new List<IShapeEntity>();

        // Cấu hình
        Dictionary<string, IPaintBusiness> _painterPrototypes = new Dictionary<string, IPaintBusiness>();
        Dictionary<string, IShapeEntity> _shapesPrototypes = new Dictionary<string, IShapeEntity>();

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

        class PluginItems
        {
            public IShapeEntity PluginEntity { get; set; }
            public string PluginIconPath { get; set; }
        }

        private void border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_drawShape)
            {
                _isDrawing = true;
                _start = e.GetPosition(canvas);

                _preview.HandleStart(_start);
            }
        }

        private void border_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing && _drawShape)
            {
                var end = e.GetPosition(canvas);
                _preview.HandleEnd(end);

                // Xóa đi tất cả bản vẽ cũ và vẽ lại những đường thẳng trước đó
                canvas.Children.Clear(); // Xóa đi toàn bộ

                // Vẽ lại những hình đã vẽ trước đó
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
            if (_drawShape)
            {
                _isDrawing = false;

                var end = e.GetPosition(canvas); // Điểm kết thúc

                _preview.HandleEnd(end);

                _drawnShapes.Add(_preview.Clone() as IShapeEntity);
            }
        }

        private void chooseShapeBtnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entity = button.Tag as IShapeEntity;
            if(entity != null)
            {
                if (_currentType != entity.Name || _currentType == "")
                {
                    _drawShape = true;
                    _currentType = entity!.Name;

                    _preview = (_shapesPrototypes[entity.Name].Clone() as IShapeEntity)!;
                    _preview.HandleColor(_currentStrokeColor);
                    _preview.HandleThickness(_currentThickness);
                    _preview.HandleStrokeType(_currentStrokeType);
                }
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write);
                BinaryWriter br = new BinaryWriter(fs);
                foreach (var item in _drawnShapes)
                {
                    IPaintBusiness painter = _painterPrototypes[item.Name];
                    br.Write(item.Name);
                    br.Write(painter.PositionX1(item));
                    br.Write(painter.PositionY1(item));
                    br.Write(painter.PositionX2(item));
                    br.Write(painter.PositionY2(item));
                }
                br.Close();
            }
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
                    Point p1, p2;
                    string name = br.ReadString();
                    p1.X = br.ReadDouble();
                    p1.Y = br.ReadDouble();
                    p2.X = br.ReadDouble();
                    p2.Y = br.ReadDouble();
                    IShapeEntity shape = null;
                    shape = (_shapesPrototypes[name].Clone() as IShapeEntity)!;
                    shape.HandleStart(p1);
                    shape.HandleEnd(p2);
                    _drawnShapes.Add(shape);
                }
                canvas.Children.Clear(); // Xóa đi toàn bộ

                // Vẽ lại những hình đã vẽ trước đó
                foreach (var item in _drawnShapes)
                {
                    IPaintBusiness painter = _painterPrototypes[item.Name];
                    UIElement shape = painter.Draw(item);

                    canvas.Children.Add(shape);
                }
                br.Close();
            }
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
        }
        private void importButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage theImage = new BitmapImage
                (new Uri(openFileDialog.FileName, UriKind.Relative));

                ImageBrush myImageBrush = new ImageBrush(theImage);

                Canvas myCanvas = new Canvas();
                myCanvas.Width = 400;
                myCanvas.Height = 266;
                myCanvas.Background = myImageBrush;

                canvas.Children.Add(myCanvas);
        }

        private void undoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_drawnShapes.Count >= 1)
            {
                _stackUndoShape.Add(_drawnShapes[_drawnShapes.Count - 1]);
                _drawnShapes.RemoveAt(_drawnShapes.Count - 1);

                canvas.Children.Clear();

                foreach (var item in _drawnShapes)
                {
                    IPaintBusiness painter = _painterPrototypes[item.Name];
                    UIElement shape = painter.Draw(item);

                    canvas.Children.Add(shape);
                }
            }

        }

        private void redoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_stackUndoShape.Count >= 1)
            {
                _drawnShapes.Add(_stackUndoShape[_stackUndoShape.Count - 1]);
                _stackUndoShape.RemoveAt(_stackUndoShape.Count - 1);

                canvas.Children.Clear();

                foreach (var item in _drawnShapes)
                {
                    IPaintBusiness painter = _painterPrototypes[item.Name];
                    UIElement shape = painter.Draw(item);

                    canvas.Children.Add(shape);
                }
            }
        }


        private void SizeGallery_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            foreach(RibbonGalleryItem item in SizeCategory.Items)
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
    }
}
