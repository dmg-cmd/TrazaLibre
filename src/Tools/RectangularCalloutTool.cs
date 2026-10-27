using System;
using System.Collections.Generic;
using System.Windows;

namespace Trazalibre.Tools {
    /// <summary>
    /// Herramienta interactiva de Globo de Texto Rectangular con Flecha ((💬).
    /// Arrastra en la pantalla para definir el tamaño del globo y su flecha apuntadora,
    /// y escribe de inmediato el enunciado en su interior.
    /// </summary>
    public class RectangularCalloutTool : BaseCalloutTool {
        public override string Id { get { return "shape_callout_rect"; } }
        public override string DisplayName { get { return "Globo Rectangular"; } }
        public override string Icon { get { return "💬"; } }

        protected override IEnumerable<Point> GeneratePoints(Point start, Point current) {
            double minX = Math.Min(start.X, current.X);
            double maxX = Math.Max(start.X, current.X);
            double minY = Math.Min(start.Y, current.Y);
            double maxY = Math.Max(start.Y, current.Y);

            // Asegurar tamaño mínimo para legibilidad
            if (maxX - minX < 130) maxX = minX + 130;
            if (maxY - minY < 70) maxY = minY + 70;

            double tailHeight = Math.Max(20.0, (maxY - minY) * 0.25);
            double bodyBottom = maxY - tailHeight;
            double radius = 8.0;

            double tailTipX;
            double tailBase1;
            double tailBase2;

            if (start.X <= current.X) {
                // Cola apuntando hacia la izquierda
                tailTipX = minX + 12;
                tailBase1 = minX + 32;
                tailBase2 = minX + 62;
            } else {
                // Cola apuntando hacia la derecha
                tailTipX = maxX - 12;
                tailBase1 = maxX - 62;
                tailBase2 = maxX - 32;
            }

            List<Point> pts = new List<Point>();

            // Borde superior
            pts.Add(new Point(minX + radius, minY));
            pts.Add(new Point(maxX - radius, minY));

            // Esquina superior derecha
            pts.Add(new Point(maxX, minY + radius));

            // Borde derecho
            pts.Add(new Point(maxX, bodyBottom - radius));

            // Esquina inferior derecha
            pts.Add(new Point(maxX - radius, bodyBottom));

            // Borde inferior con la cola / flecha
            pts.Add(new Point(tailBase2, bodyBottom));
            pts.Add(new Point(tailTipX, maxY));
            pts.Add(new Point(tailBase1, bodyBottom));

            // Borde inferior restante
            pts.Add(new Point(minX + radius, bodyBottom));

            // Esquina inferior izquierda
            pts.Add(new Point(minX, bodyBottom - radius));

            // Borde izquierdo
            pts.Add(new Point(minX, minY + radius));

            // Cerrar el polígono en la esquina superior izquierda
            pts.Add(new Point(minX + radius, minY));

            return pts;
        }

        public override Rect GetBodyRect(Point start, Point current) {
            double minX = Math.Min(start.X, current.X);
            double maxX = Math.Max(start.X, current.X);
            double minY = Math.Min(start.Y, current.Y);
            double maxY = Math.Max(start.Y, current.Y);

            if (maxX - minX < 130) maxX = minX + 130;
            if (maxY - minY < 70) maxY = minY + 70;

            double tailHeight = Math.Max(20.0, (maxY - minY) * 0.25);
            return new Rect(minX + 8, minY + 6, (maxX - minX) - 16, (maxY - minY - tailHeight) - 12);
        }
    }
}

