using System;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace Trazalibre.Core {
    /// <summary>
    /// Contexto de anotación expuesto a las herramientas y servicios.
    /// Desacopla la lógica interna de la ventana OverlayWindow del resto de la aplicación.
    /// </summary>
    public interface IAnnotationContext {
        InkCanvas Canvas { get; }
        Color CurrentColor { get; set; }
        double CurrentSize { get; set; }
        IntPtr WindowHandle { get; }

        void SetClickThrough(bool enabled);
        void SetBackgroundMode(BackgroundMode mode);
        void Undo();
        void Clear();
        string SaveScreenshot();

        // Registro atómico de trazos para soporte perfecto de Deshacer (Undo)
        void AddStroke(Stroke stroke);
        void AddStrokes(StrokeCollection strokes);
    }
}

