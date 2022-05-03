using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
<<<<<<< Updated upstream
=======
        void HandleFillColor(Color color);

>>>>>>> Stashed changes
    }
}
