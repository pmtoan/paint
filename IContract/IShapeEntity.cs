using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IContract
{
    public interface IShapeEntity : ICloneable
    {
        string Name { get; }
        BitmapImage Icon { get; }

        void HandleStart(Point point);
        void HandleEnd(Point point);
        void HandleThickness(int size);
        void HandleColor(Color color);
        void HandleStrokeType(string type);
        void HandleFillColor(Color color);

    }
}
