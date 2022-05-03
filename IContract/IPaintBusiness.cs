using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media;

namespace IContract
{
    public interface IPaintBusiness
    {
        UIElement Draw(IShapeEntity entity);
        double PositionX1(IShapeEntity shape);
        double PositionY1(IShapeEntity shape);
        double PositionX2(IShapeEntity shape);
        double PositionY2(IShapeEntity shape);
        int Thickness(IShapeEntity shape);
        string Color(IShapeEntity shape);
        DoubleCollection StrokeType(IShapeEntity shape);
    }
}
