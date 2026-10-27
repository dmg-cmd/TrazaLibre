using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Trazalibre.Core;

namespace Trazalibre.Tools {
    public class HighlighterTool : ITool {
        public string Id { get { return "highlighter"; } }
        public string DisplayName { get { return "Marca"; } }
        public string Icon { get { return "🖍️"; } }

        public void Activate(IAnnotationContext context) {
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.Ink;
            context.Canvas.Cursor = Cursors.Pen;
            ApplyAttributes(context);
        }

        public void Deactivate(IAnnotationContext context) { }

        public void OnColorChanged(IAnnotationContext context, Color newColor) {
            ApplyAttributes(context);
        }

        public void OnSizeChanged(IAnnotationContext context, double newSize) {
            ApplyAttributes(context);
        }

        private void ApplyAttributes(IAnnotationContext context) {
            DrawingAttributes attrs = context.Canvas.DefaultDrawingAttributes;
            attrs.IsHighlighter = true;
            Color baseColor = context.CurrentColor;
            attrs.Color = Color.FromArgb(120, baseColor.R, baseColor.G, baseColor.B);
            double hlSize = context.CurrentSize * 2.5;
            attrs.Width = hlSize;
            attrs.Height = hlSize;
            attrs.FitToCurve = true;
        }
    }
}

