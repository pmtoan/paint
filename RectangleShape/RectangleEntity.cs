using IContract;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RectangleShape
{
    public class RectangleEntity : IShapeEntity, ICloneable
    {
        public Point AnchorPoint { get; set; }
        public Point TopLeft { get; set; }
        public Point MousePoint { get; set; }

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
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
