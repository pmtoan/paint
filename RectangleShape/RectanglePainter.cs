using IContract;
using System;
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

                rectangle.LeftTop = rectangle.AnchorPoint;
                rectangle.RightBottom = rectangle.MousePoint;
            }
            else if (height < 0 && width < 0)
            {
                // TopLeft is MousePoint

                rectangle.LeftTop = rectangle.MousePoint;
                rectangle.RightBottom = rectangle.AnchorPoint;
            }
            else if (height < 0 && width >= 0)
            {
                // Topleft.X is Anchor.X
                // Topleft.Y is MousePoint.Y

                rectangle.LeftTop =
                    new Point(rectangle.AnchorPoint.X, rectangle.MousePoint.Y);
                rectangle.RightBottom =
                    new Point(rectangle.MousePoint.X, rectangle.AnchorPoint.Y);
            }
            else if (height >= 0 && width < 0)
            {
                // Topleft.X is MousePoint.X
                // Topleft.Y is Anchor.Y

                rectangle.LeftTop =
                    new Point(rectangle.MousePoint.X, rectangle.AnchorPoint.Y);
                rectangle.RightBottom =
                    new Point(rectangle.AnchorPoint.X, rectangle.MousePoint.Y);
            }

            var element = new Rectangle()
            {
                Width = Math.Abs(width),
                Height = Math.Abs(height),
                StrokeThickness = rectangle.Size,
                Stroke = new SolidColorBrush(rectangle.ColorStroke),
                Fill = new SolidColorBrush(rectangle.ColorFill),
                StrokeDashArray = rectangle.StrokeType
            };
            Canvas.SetLeft(element, rectangle.GetLeftTop().X);
            Canvas.SetTop(element, rectangle.GetLeftTop().Y);
            Canvas.SetRight(element, rectangle.GetRightBottom().X);
            Canvas.SetBottom(element, rectangle.GetRightBottom().Y);

            return element;
        }
    }
}
