using System.Windows.Media;

namespace Trazalibre.Core {
    /// <summary>
    /// Contrato para cualquier herramienta de dibujo o interacción.
    /// Permite añadir nuevas herramientas de forma completamente aislada sin tocar el resto del código.
    /// </summary>
    public interface ITool {
        string Id { get; }
        string DisplayName { get; }
        string Icon { get; }

        void Activate(IAnnotationContext context);
        void Deactivate(IAnnotationContext context);
        void OnColorChanged(IAnnotationContext context, Color newColor);
        void OnSizeChanged(IAnnotationContext context, double newSize);
    }
}

