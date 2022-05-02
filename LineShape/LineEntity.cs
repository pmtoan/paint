using IContract;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LineShape
{
    public class LineEntity : IShapeEntity, ICloneable
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        public int Size { get; set; }
        public Color ColorApply { get; set; }
        public DoubleCollection StrokeType { get; set; }
        public string Name => "Line";
        public BitmapImage Icon => new BitmapImage(new Uri("", UriKind.Relative));


        public void HandleStart(Point point)
        {
            Start = point;
        }
        public void HandleEnd(Point point)
        {
            End = point;
        }

        public void HandleThickness(int size)
        {
            Size = size;
        }

        public void HandleColor(Color color)
        {
            ColorApply = color;
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
