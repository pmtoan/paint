using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IContract
{
    public interface IShapeEntity : ICloneable
    {
        string Name { get; }

        void HandleStart(Point point);
        void HandleEnd(Point point);
        void HandleThickness(int size);
        void HandleStrokeType(string type);
        void HandleStrokeColor(Color color);
        void HandleFillColor(Color color);

        Point GetTopLeft();
        Point GetRightBottom();
        int GetThickness();
        Color GetStrokeColor();
        Color GetFillColor();
        DoubleCollection GetStrokeType();

    }
}
