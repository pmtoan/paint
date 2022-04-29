using IContract;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LineShape
{
    public class LineEntity : IShapeEntity, ICloneable
    {
        public Point Start { get; set; }
        public Point End { get; set; }
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

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
