using System;
using System.Windows;

namespace IContract
{
    public interface IPaintBusiness
    {
        UIElement Draw(IShapeEntity entity);
    }
}
