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
        public Color ColorStroke { get; set; }
        public Color ColorFill { get; set; }

        public string Name => "Ellipse";

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

        public void HandleStrokeColor(Color color)
        {
            ColorStroke = color;
        }

        public void HandleFillColor(Color color)
        {
            ColorFill = color;
        }

        public void HandleStrokeType(string type)
        {
            StrokeType = new DoubleCollection();
            if (type != null)
            {
                foreach (string s in type.Split(" "))
                {
                    StrokeType.Add(Convert.ToDouble(s));
                }
            }
        }
        public object Clone()
        {
            return MemberwiseClone();
        }

        public Point GetTopLeft()
        {
            return TopLeft;
        }

        public Point GetRightBottom()
        {
            return MousePoint;
        }

        public int GetThickness()
        {
            return Size;
        }

        public Color GetStrokeColor()
        {
            return ColorStroke;
        }

        public Color GetFillColor()
        {
            return ColorFill;
        }

        public DoubleCollection GetStrokeType()
        {
            return StrokeType;
        }
    }
}
