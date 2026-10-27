using System.Windows.Controls;
using System.Windows.Media;
using Trazalibre.Core;

namespace Trazalibre.Services {
    /// <summary>
    /// Servicio que administra los estilos y fondos del lienzo de anotaciones.
    /// </summary>
    public static class BackgroundService {
        private static readonly SolidColorBrush DarkBoardBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        // Un pincel casi transparente (alfa = 1) permite al motor DWM de Windows interceptar clics para dibujar
        // sobre el escritorio sin oscurecer nada (completamente invisible para el ojo humano).
        public static readonly SolidColorBrush HitTestInvisibleBrush = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));

        public static void ApplyBackground(InkCanvas canvas, BackgroundMode mode, bool canDraw) {
            if (canvas == null) return;

            switch (mode) {
                case BackgroundMode.Transparent:
                    canvas.Background = canDraw ? HitTestInvisibleBrush : Brushes.Transparent;
                    break;
                case BackgroundMode.Whiteboard:
                    canvas.Background = Brushes.White;
                    break;
                case BackgroundMode.Blackboard:
                    canvas.Background = DarkBoardBrush;
                    break;
            }
        }
    }
}

