using System.Collections.Generic;
using System.Windows;
using Trazalibre.Services;

namespace Trazalibre.Tools {
    /// <summary>
    /// Herramienta interactiva de Línea Recta / Vector ((📏).
    /// Si el Modo Vector está activo, genera una flecha vectorial orientada.
    /// </summary>
    public class LineShapeTool : BaseShapeTool {
        public override string Id { get { return "shape_line"; } }
        public override string DisplayName { get { return "Línea"; } }
        public override string Icon { get { return "📏"; } }

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

