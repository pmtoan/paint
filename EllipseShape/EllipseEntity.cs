using IContract;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EllipseShape
{
    public class EllipseEntity : IShapeEntity
    {
        public Point AnchorPoint { get; set; }
        public Point TopLeft { get; set; }
        public Point MousePoint { get; set; }
        public int Size { get; set; }
        public DoubleCollection StrokeType { get; set; }
        public Color ColorApply { get; set; }
        public Color ColorFill { get; set; }

        public string Name => "Ellipse";

        public BitmapImage Icon => new BitmapImage(new Uri("", UriKind.Relative));

        public void HandleStart(Point point)
        {
            AnchorPoint = point;
        }
        public void HandleEnd(Point point)
        {
            MousePoint = point;
        }
        public void HandleThickness(int size)
        {
            Size = size;
        }

        public void HandleColor(Color color)
        {
            ColorApply = color;
        }

        public void HandleFillColor(Color color)
        {
            ColorFill = color;
        }

        public void HandleStrokeType(string type)
        {
            if (type != null)
            {
                var strokeDashArray = new DoubleCollection();
                foreach (string s in type.Split(" "))
                {
                    strokeDashArray.Add(Convert.ToDouble(s));
                }
                StrokeType = strokeDashArray;
            }
            else
            {
                StrokeType = new DoubleCollection();
            }
        }
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
