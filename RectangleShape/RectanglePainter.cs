using IContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RectangleShape
{
    public class RectanglePainter : IPaintBusiness
    {
        public UIElement Draw(IShapeEntity shape)
        {
            var rectangle = shape as RectangleEntity;

            // TODO: chú ý việc đảo lại rightbottom và topleft 

            double width = rectangle.MousePoint.X - rectangle.AnchorPoint.X;
            double height = rectangle.MousePoint.Y - rectangle.AnchorPoint.Y;

            if (height >= 0 && width >= 0)
            {
                // Nomrmal case
                // Topleft is Anchor

                rectangle.TopLeft = rectangle.AnchorPoint;
            }
            else if (height < 0 && width < 0)
            {
                // TopLeft is MousePoint

                rectangle.TopLeft = rectangle.MousePoint;
            }
            else if (height < 0 && width >= 0)
            {
                // Topleft.X is Anchor.X
                // Topleft.Y is MousePoint.Y

                rectangle.TopLeft =
                    new Point(rectangle.AnchorPoint.X, rectangle.MousePoint.Y);
            }
            else if (height >= 0 && width < 0)
            {
                // Topleft.X is MousePoint.X
                // Topleft.Y is Anchor.Y

                rectangle.TopLeft =
                    new Point(rectangle.MousePoint.X, rectangle.AnchorPoint.Y);
            }

            var element = new Rectangle()
            {
                Width = Math.Abs(width),
                Height = Math.Abs(height),
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Colors.Red)
            };
            Canvas.SetLeft(element, rectangle.TopLeft.X);
            Canvas.SetTop(element, rectangle.TopLeft.Y);

            return element;
        }
    }
}
