using System.Collections.Generic;
using System.Windows;
using Trazalibre.Core;
using Trazalibre.Services;

namespace Trazalibre.Tools {
    /// <summary>
    /// Herramienta interactiva de Línea Recta / Vector (📏).
    /// Si el Modo Vector está activo, genera una flecha vectorial orientada.
    /// </summary>
    public class LineShapeTool : BaseShapeTool {
        public override string Id { get { return "shape_line"; } }
        public override string DisplayName { get { return "Línea"; } }
        public override string Icon { get { return "📏"; } }

        public override void Activate(IAnnotationContext context) {
            base.Activate(context);
            if (context != null && context.Canvas != null) {
                context.Canvas.ContextMenu = ToolContextMenuHelper.CreateLineContextMenu(context);
            }
        }

        public override void Deactivate(IAnnotationContext context) {
            if (context != null && context.Canvas != null) {
                context.Canvas.ContextMenu = null;
            }
            base.Deactivate(context);
        }

        protected override IEnumerable<Point> GeneratePoints(Point start, Point current) {
            if (VectorService.IsVectorMode) {
                return VectorService.CreateVectorPoints(start, current, 3.0);
            }

            return new Point[] {
                start,
                current
            };
        }
    }
}

