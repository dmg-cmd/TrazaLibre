using System;
using System.Collections.Generic;
using System.Windows;

namespace Trazalibre.Tools {
    /// <summary>
    /// Sello de Estrella de 5 puntas (⭐)
    /// </summary>
    public class StarStampTool : BaseStampTool {
        public override string Id { get { return "stamp_star"; } }
        public override string DisplayName { get { return "Estrella"; } }
        public override string Icon { get { return "⭐"; } }

        protected override IEnumerable<Point> GetRelativePoints(double scale) {
            double radius = scale * 0.4;
            List<Point> points = new List<Point>();

            // Pentagrama regular: 5 vértices conectados saltando de 2 en 2
            for (int i = 0; i <= 5; i++) {
                double angle = -Math.PI / 2.0 + i * (4.0 * Math.PI / 5.0);
                points.Add(new Point(radius * Math.Cos(angle), radius * Math.Sin(angle)));
            }

            return points;
        }
    }
}

