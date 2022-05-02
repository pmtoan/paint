using IContract;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RectangleShape
{
    public class RectangleEntity : IShapeEntity, ICloneable
    {
        public Point AnchorPoint { get; set; }
        public Point TopLeft { get; set; }
        public Point MousePoint { get; set; }
        public int Size { get; set; }
        public DoubleCollection StrokeType { get; set; }
        public Color ColorApply { get; set; }

        public string Name => "Rectangle";

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
            }  else
            {
                StrokeType = new DoubleCollection();
            }
        }

        public void HandleColor(Color color)
        {
            ColorApply = color;
        }
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
