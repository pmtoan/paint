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
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Colors.Black)
            };

            return element;
        }
    }
}
