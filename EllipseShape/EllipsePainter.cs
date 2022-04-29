﻿using IContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            double width = ellipse.MousePoint.X - ellipse.AnchorPoint.X;
            double height = ellipse.MousePoint.Y - ellipse.AnchorPoint.Y;

            if (height >= 0 && width >= 0)
            {
                // Nomrmal case
                // Topleft is Anchor

                ellipse.TopLeft = ellipse.AnchorPoint;
            }
            else if (height < 0 && width < 0)
            {
                // TopLeft is MousePoint

                ellipse.TopLeft = ellipse.MousePoint;
            }
            else if (height < 0 && width >= 0)
            {
                // Topleft.X is Anchor.X
                // Topleft.Y is MousePoint.Y

                ellipse.TopLeft =
                    new Point(ellipse.AnchorPoint.X, ellipse.MousePoint.Y);
            }
            else if (height >= 0 && width < 0)
            {
                // Topleft.X is MousePoint.X
                // Topleft.Y is Anchor.Y
                ellipse.TopLeft =
                    new Point(ellipse.MousePoint.X, ellipse.AnchorPoint.Y);
            }

            var element = new Ellipse()
            {
                Width = Math.Abs(width),
                Height = Math.Abs(height),
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Colors.Green)
            };
            Canvas.SetLeft(element, ellipse.TopLeft.X);
            Canvas.SetTop(element, ellipse.TopLeft.Y);

            return element;
        }
    }
}
