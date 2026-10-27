using System;
using System.Collections.Generic;
using System.Windows;

namespace Trazalibre.Tools {
    /// <summary>
    /// Herramienta interactiva de Globo de Texto Nube / Pensamiento con Flecha ((💭).
    /// Arrastra para generar una nube festoneada con flecha y escribe tu reflexión o enunciado adentro.
    /// </summary>
    public class CloudCalloutTool : BaseCalloutTool {
        public override string Id { get { return "shape_callout_cloud"; } }
        public override string DisplayName { get { return "Globo Nube"; } }
        public override string Icon { get { return "💭"; } }

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

            int numLobes = 10;
            double lobeWave = 0.16; // Amplitud de ondas de la nube

            double startTailAngle;
            double endTailAngle;
            double tailTipX;

            if (start.X <= current.X) {
                startTailAngle = 1.85;
                endTailAngle = 2.30;
                tailTipX = minX + 10;
            } else {
                startTailAngle = 0.85;
                endTailAngle = 1.30;
                tailTipX = maxX - 10;
            }

            int totalSteps = 60;
            bool tailAdded = false;

            for (int i = 0; i <= totalSteps; i++) {
                double angle = (2.0 * Math.PI * i) / totalSteps;

                if (angle >= startTailAngle && angle <= endTailAngle) {
                    if (!tailAdded) {
                        tailAdded = true;

                        double rMod1 = 1.0 + lobeWave * Math.Sin(startTailAngle * numLobes);
                        pts.Add(new Point(cx + rx * rMod1 * Math.Cos(startTailAngle), cy + ry * rMod1 * Math.Sin(startTailAngle)));

                        // Punta de la flecha
                        pts.Add(new Point(tailTipX, maxY));

                        double rMod2 = 1.0 + lobeWave * Math.Sin(endTailAngle * numLobes);
                        pts.Add(new Point(cx + rx * rMod2 * Math.Cos(endTailAngle), cy + ry * rMod2 * Math.Sin(endTailAngle)));
                    }
                    continue;
                }

                double rMod = 1.0 + lobeWave * Math.Sin(angle * numLobes);
                double x = cx + rx * rMod * Math.Cos(angle);
                double y = cy + ry * rMod * Math.Sin(angle);
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

            double padX = (maxX - minX) * 0.20;
            double padY = bodyHeight * 0.20;
            return new Rect(minX + padX, minY + padY, (maxX - minX) - (2 * padX), bodyHeight - (2 * padY));
        }
    }
}

