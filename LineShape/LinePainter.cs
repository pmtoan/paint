using IContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                Stroke = new SolidColorBrush(line.ColorApply),
                StrokeDashArray = line.StrokeType
            };

            return element;
        }
        public double PositionX1(IShapeEntity shape)
        {
            var line = shape as LineEntity;
            return line.Start.X;
        }
        public double PositionY1(IShapeEntity shape)
        {
            var line = shape as LineEntity;
            return line.Start.Y;
        }
        public double PositionX2(IShapeEntity shape)
        {
            var line = shape as LineEntity;
            return line.End.X;
        }
        public double PositionY2(IShapeEntity shape)
        {
            var line = shape as LineEntity;
            return line.End.Y;
        }

        public int Thickness(IShapeEntity shape)
        {
            throw new NotImplementedException();
        }

        public string Color(IShapeEntity shape)
        {
            throw new NotImplementedException();
        }

        public DoubleCollection StrokeType(IShapeEntity shape)
        {
            throw new NotImplementedException();
        }
    }
}
