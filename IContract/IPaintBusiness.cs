using System.Windows;
using System.Windows.Media;

namespace IContract
{
    public interface IPaintBusiness
    {
        UIElement Draw(IShapeEntity entity);

    }
}
