using IContract;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EllipseShape
{
    public class EllipsePainter : IPaintBusiness
    {
        public UIElement Draw(IShapeEntity shape)
        {
            var ellipse = shape as EllipseEntity;

            // TODO: chú ý việc đảo lại rightbottom và topleft 

            double width = ellipse.MousePoint.X - ellipse.AnchorPoint.X;
            double height = ellipse.MousePoint.Y - ellipse.AnchorPoint.Y;

            if (height >= 0 && width >= 0)
            {
                // Nomrmal case
                // Topleft is Anchor

                ellipse.LeftTop = ellipse.AnchorPoint;
                ellipse.RightBottom = ellipse.MousePoint;
            }
            else if (height < 0 && width < 0)
            {
                // TopLeft is MousePoint

                ellipse.LeftTop = ellipse.MousePoint;
                ellipse.RightBottom = ellipse.AnchorPoint;
            }
            else if (height < 0 && width >= 0)
            {
                // Topleft.X is Anchor.X
                // Topleft.Y is MousePoint.Y

                ellipse.LeftTop =
                    new Point(ellipse.AnchorPoint.X, ellipse.MousePoint.Y);
                ellipse.RightBottom =
                    new Point(ellipse.MousePoint.X, ellipse.AnchorPoint.Y);
            }
            else if (height >= 0 && width < 0)
            {
                // Topleft.X is MousePoint.X
                // Topleft.Y is Anchor.Y

                ellipse.LeftTop =
                    new Point(ellipse.MousePoint.X, ellipse.AnchorPoint.Y);
                ellipse.RightBottom =
                    new Point(ellipse.AnchorPoint.X, ellipse.MousePoint.Y);
            }

            var element = new Ellipse()
            {
                Width = Math.Abs(width),
                Height = Math.Abs(height),
                StrokeThickness = ellipse.Size,
                Stroke = new SolidColorBrush(ellipse.ColorStroke),
                Fill = new SolidColorBrush(ellipse.ColorFill),
                StrokeDashArray = ellipse.StrokeType
            };
            Canvas.SetLeft(element, ellipse.GetLeftTop().X);
            Canvas.SetTop(element, ellipse.GetLeftTop().Y);
            Canvas.SetRight(element, ellipse.GetRightBottom().X);
            Canvas.SetBottom(element, ellipse.GetRightBottom().Y);

            return element;
        }
    }
}
