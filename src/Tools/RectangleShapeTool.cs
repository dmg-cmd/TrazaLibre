using System;
using System.Collections.Generic;
using System.Windows;

namespace Trazalibre.Tools {
    /// <summary>
    /// Herramienta interactiva de Rectángulo / Cuadro ((⬜).
    /// Haces clic en una esquina y arrastras para encuadrar cualquier elemento.
    /// </summary>
    public class RectangleShapeTool : BaseShapeTool {
        public override string Id { get { return "shape_rect"; } }
        public override string DisplayName { get { return "Rectángulo"; } }
        public override string Icon { get { return "⬜"; } }

        protected override IEnumerable<Point> GeneratePoints(Point start, Point current) {
            double minX = Math.Min(start.X, current.X);
            double maxX = Math.Max(start.X, current.X);
            double minY = Math.Min(start.Y, current.Y);
            double maxY = Math.Max(start.Y, current.Y);

            // Trazo cerrado de las 4 esquinas del rectángulo
            return new Point[] {
                new Point(minX, minY),
                new Point(maxX, minY),
                new Point(maxX, maxY),
                new Point(minX, maxY),
                new Point(minX, minY)
            };
        }
    }
}

