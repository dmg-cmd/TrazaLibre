using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Trazalibre.Core;

namespace Trazalibre.Tools {
    public class EraserTool : ITool {
        public string Id { get { return "eraser"; } }
        public string DisplayName { get { return "Borra"; } }
        public string Icon { get { return "🧹"; } }

        public void Activate(IAnnotationContext context) {
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
            context.Canvas.Cursor = Cursors.Hand;
        }

        public void Deactivate(IAnnotationContext context) { }

        public void OnColorChanged(IAnnotationContext context, Color newColor) { }
        public void OnSizeChanged(IAnnotationContext context, double newSize) { }
    }
}

