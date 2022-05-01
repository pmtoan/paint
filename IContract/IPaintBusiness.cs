using System;
using System.Windows;

namespace IContract
{
    public interface IPaintBusiness
    {
        UIElement Draw(IShapeEntity entity);
        double PositionX1(IShapeEntity shape);
        double PositionY1(IShapeEntity shape);
        double PositionX2(IShapeEntity shape);
        double PositionY2(IShapeEntity shape);
    }
}
