using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Trazalibre.Core;
using Trazalibre.Services;

namespace Trazalibre.Tools {
    public class EraserTool : ITool {
        public string Id { get { return "eraser"; } }
        public string DisplayName { get { return "Borra"; } }
        public string Icon { get { return "🧹"; } }

        public void Activate(IAnnotationContext context) {
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
            context.Canvas.Cursor = Cursors.Hand;
            context.Canvas.ContextMenu = ToolContextMenuHelper.CreateGeometryContextMenu(context);
        }

        public void Deactivate(IAnnotationContext context) {
            if (context != null && context.Canvas != null) {
                context.Canvas.ContextMenu = null;
            }
        }

        public void OnColorChanged(IAnnotationContext context, Color newColor) { }
        public void OnSizeChanged(IAnnotationContext context, double newSize) { }
    }
}

