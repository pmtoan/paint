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
        public Color ColorStroke { get; set; }
        public DoubleCollection StrokeType { get; set; }
        public string Name => "Line";

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

        public void HandleStrokeColor(Color color)
        {
            ColorStroke = color;
        }


        public void HandleFillColor(Color color)
        {
            return;
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
            return Start;
        }

        public Point GetRightBottom()
        {
            return End;
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
            return new Color();
        }

        public DoubleCollection GetStrokeType()
        {
            return StrokeType;
        }
    }
}
