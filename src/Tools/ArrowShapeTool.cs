using System;
using System.Collections.Generic;
using System.Windows;

namespace Trazalibre.Tools {
    /// <summary>
    /// Herramienta interactiva de Flecha Dinámica ((🏹).
    /// Haces clic y arrastras; la punta de la flecha se orienta y escala en la dirección del trazo.
    /// </summary>
    public class ArrowShapeTool : BaseShapeTool {
        public override string Id { get { return "shape_arrow"; } }
        public override string DisplayName { get { return "Flecha"; } }
        public override string Icon { get { return "🏹"; } }

        protected override IEnumerable<Point> GeneratePoints(Point start, Point current) {
            double dx = current.X - start.X;
            double dy = current.Y - start.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 2.0) return new Point[0];

            double angle = Math.Atan2(dy, dx);
            double headLength = Math.Min(length * 0.4, Math.Max(16.0, 24.0));
            double headAngle = Math.PI / 6.0; // 30 grados

            Point leftWing = new Point(
                current.X - headLength * Math.Cos(angle - headAngle),
                current.Y - headLength * Math.Sin(angle - headAngle)
            );

            Point rightWing = new Point(
                current.X - headLength * Math.Cos(angle + headAngle),
                current.Y - headLength * Math.Sin(angle + headAngle)
            );

            // Trazo continuo: Inicio -> Punta -> Ala Izquierda -> Punta -> Ala Derecha
            return new Point[] {
                start,
                current,
                leftWing,
                current,
                rightWing
            };
        }
    }
}

