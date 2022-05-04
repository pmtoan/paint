using IContract;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LineShape
{
    public class LinePainter : IPaintBusiness
    {
        public UIElement Draw(IShapeEntity shape)
        {
            var line = shape as LineEntity;

            var element = new Line()
            {
                X1 = line.Start.X,
                Y1 = line.Start.Y,
                X2 = line.End.X,
                Y2 = line.End.Y,
                StrokeThickness = line.Size,
                Stroke = new SolidColorBrush(line.ColorStroke),
                StrokeDashArray = line.StrokeType
            };

            return element;
        }
    }
}
