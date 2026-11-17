using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Trazalibre.Core;
using Trazalibre.Services;

namespace Trazalibre.Tools {
    public class PenTool : ITool {
        public string Id { get { return "pen"; } }
        public string DisplayName { get { return "Lápiz"; } }
        public string Icon { get { return "✏️"; } }

        public void Activate(IAnnotationContext context) {
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.Ink;
            context.Canvas.Cursor = Cursors.Pen;
            context.Canvas.ContextMenu = ToolContextMenuHelper.CreateGeometryContextMenu(context);
            ApplyAttributes(context);
        }

        public void Deactivate(IAnnotationContext context) {
            if (context != null && context.Canvas != null) {
                context.Canvas.ContextMenu = null;
            }
        }

        public void OnColorChanged(IAnnotationContext context, Color newColor) {
            ApplyAttributes(context);
        }

        public void OnSizeChanged(IAnnotationContext context, double newSize) {
            ApplyAttributes(context);
        }

        private void ApplyAttributes(IAnnotationContext context) {
            DrawingAttributes attrs = context.Canvas.DefaultDrawingAttributes;
            attrs.IsHighlighter = false;
            attrs.Color = context.CurrentColor;
            attrs.Width = context.CurrentSize;
            attrs.Height = context.CurrentSize;
            attrs.FitToCurve = true;
        }
    }
}

