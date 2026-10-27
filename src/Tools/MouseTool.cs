using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Trazalibre.Core;

namespace Trazalibre.Tools {
    public class MouseTool : ITool {
        public string Id { get { return "mouse"; } }
        public string DisplayName { get { return "Ratón"; } }
        public string Icon { get { return "🖱️"; } }

        public void Activate(IAnnotationContext context) {
            context.SetClickThrough(true);
            context.Canvas.EditingMode = InkCanvasEditingMode.None;
            context.Canvas.Cursor = Cursors.Arrow;
        }

        public void Deactivate(IAnnotationContext context) {
            context.SetClickThrough(false);
        }

        public void OnColorChanged(IAnnotationContext context, Color newColor) { }
        public void OnSizeChanged(IAnnotationContext context, double newSize) { }
    }
}

