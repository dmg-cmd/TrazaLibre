using System;
using System.Collections.Generic;
using System.Windows;

namespace Trazalibre.Tools {
    /// <summary>
    /// Herramienta interactiva de Globo de Texto Ovalado / Elíptico con Flecha ((🗨️).
    /// Arrastra en la pantalla para crear una viñeta elíptica con flecha y escribe tu mensaje adentro.
    /// </summary>
    public class OvalCalloutTool : BaseCalloutTool {
        public override string Id { get { return "shape_callout_oval"; } }
        public override string DisplayName { get { return "Globo Ovalado"; } }
        public override string Icon { get { return "🗨️"; } }

        protected override IEnumerable<Point> GeneratePoints(Point start, Point current) {
            double minX = Math.Min(start.X, current.X);
            double maxX = Math.Max(start.X, current.X);
            double minY = Math.Min(start.Y, current.Y);
            double maxY = Math.Max(start.Y, current.Y);

            // Asegurar tamaño mínimo para legibilidad
            if (maxX - minX < 140) maxX = minX + 140;
            if (maxY - minY < 75) maxY = minY + 75;

            double tailHeight = Math.Max(22.0, (maxY - minY) * 0.26);
            double bodyHeight = (maxY - minY) - tailHeight;

            double cx = (minX + maxX) / 2.0;
            double cy = minY + bodyHeight / 2.0;
            double rx = (maxX - minX) / 2.0;
            double ry = bodyHeight / 2.0;

            List<Point> pts = new List<Point>();

            double startTailAngle;
            double endTailAngle;
            double tailTipX;

            if (start.X <= current.X) {
                // Cola apuntando abajo a la izquierda (en radianes: ~105° a 135°)
                startTailAngle = 1.85;
                endTailAngle = 2.30;
                tailTipX = minX + 12;
            } else {
                // Cola apuntando abajo a la derecha (en radianes: ~45° a 75°)
                startTailAngle = 0.85;
                endTailAngle = 1.30;
                tailTipX = maxX - 12;
            }

            int steps = 44;
            bool tailAdded = false;

            for (int i = 0; i <= steps; i++) {
                double angle = (2.0 * Math.PI * i) / steps;

                if (angle >= startTailAngle && angle <= endTailAngle) {
                    if (!tailAdded) {
                        tailAdded = true;

                        // Punto de inicio de la cola sobre la elipse
                        double p1X = cx + rx * Math.Cos(startTailAngle);
                        double p1Y = cy + ry * Math.Sin(startTailAngle);
                        pts.Add(new Point(p1X, p1Y));

                        // Punta de la flecha
                        pts.Add(new Point(tailTipX, maxY));

                        // Punto de regreso sobre la elipse
                        double p2X = cx + rx * Math.Cos(endTailAngle);
                        double p2Y = cy + ry * Math.Sin(endTailAngle);
                        pts.Add(new Point(p2X, p2Y));
                    }
                    continue;
                }

                double x = cx + rx * Math.Cos(angle);
                double y = cy + ry * Math.Sin(angle);
                pts.Add(new Point(x, y));
            }

            if (pts.Count > 0) {
                pts.Add(pts[0]);
            }

            return pts;
        }

        public override Rect GetBodyRect(Point start, Point current) {
            double minX = Math.Min(start.X, current.X);
            double maxX = Math.Max(start.X, current.X);
            double minY = Math.Min(start.Y, current.Y);
            double maxY = Math.Max(start.Y, current.Y);

            if (maxX - minX < 140) maxX = minX + 140;
            if (maxY - minY < 75) maxY = minY + 75;

            double tailHeight = Math.Max(22.0, (maxY - minY) * 0.26);
            double bodyHeight = (maxY - minY) - tailHeight;

            // Margen proporcional interno de la elipse
            double padX = (maxX - minX) * 0.16;
            double padY = bodyHeight * 0.16;
            return new Rect(minX + padX, minY + padY, (maxX - minX) - (2 * padX), bodyHeight - (2 * padY));
        }
    }
}

