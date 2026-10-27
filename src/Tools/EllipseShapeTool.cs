using System;
using System.Collections.Generic;
using System.Windows;

namespace Trazalibre.Tools {
    /// <summary>
    /// Herramienta interactiva de Círculo / Elipse (⭕).
    /// Arrastras para rodear con un círculo o elipse suave cualquier objeto en pantalla.
    /// </summary>
    public class EllipseShapeTool : BaseShapeTool {
        public override string Id { get { return "shape_ellipse"; } }
        public override string DisplayName { get { return "Círculo"; } }
        public override string Icon { get { return "⭕"; } }

        protected override IEnumerable<Point> GeneratePoints(Point start, Point current) {
            double cx = (start.X + current.X) / 2.0;
            double cy = (start.Y + current.Y) / 2.0;
            double rx = Math.Abs(current.X - start.X) / 2.0;
            double ry = Math.Abs(current.Y - start.Y) / 2.0;

            if (rx < 1.0 || ry < 1.0) return new Point[0];

            const int segments = 36;
            Point[] points = new Point[segments + 1];
            for (int i = 0; i <= segments; i++) {
                double angle = i * (2.0 * Math.PI / segments);
                points[i] = new Point(cx + rx * Math.Cos(angle), cy + ry * Math.Sin(angle));
            }

            return points;
        }
    }
}
