using System;
using System.Globalization;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Trazalibre.Services {
    /// <summary>
    /// Estructura con la información de captura magnética de un punto.
    /// </summary>
    public struct SnapResult {
        public bool IsSnapped;
        public Point Point;
        public string SnapType;
    }

    /// <summary>
    /// Servicio modular de Imantación y Captura de Puntos Finales (Endpoint Snapping).
    /// Detecta automáticamente los extremos finales e iniciales de líneas y trazos en el lienzo,
    /// permitiendo encadenar figuras geométricas y continuar líneas desde el punto exacto de la anterior.
    /// </summary>
    public static class SnapService {
        public const double DefaultTolerance = 20.0; // Radio de captura magnética en píxeles

        /// <summary>
        /// Busca el extremo más cercano en todos los trazos existentes en el lienzo.
        /// </summary>
        public static SnapResult FindSnapPoint(StrokeCollection strokes, Point mousePos, double tolerance = DefaultTolerance) {
            SnapResult result = new SnapResult {
                IsSnapped = false,
                Point = mousePos,
                SnapType = ""
            };

            if (strokes == null || strokes.Count == 0) return result;

            double minDistanceSquared = tolerance * tolerance;
            Point bestPoint = mousePos;
            bool found = false;
            string bestType = "";

            // Recorrer los trazos en orden inverso (los más recientes primero)
            for (int i = strokes.Count - 1; i >= 0; i--) {
                Stroke stroke = strokes[i];
                if (stroke == null || stroke.StylusPoints.Count == 0) continue;

                // 1. Probar el punto final del trazo (final de la línea)
                StylusPoint endPt = stroke.StylusPoints[stroke.StylusPoints.Count - 1];
                double dx = mousePos.X - endPt.X;
                double dy = mousePos.Y - endPt.Y;
                double distSq = (dx * dx) + (dy * dy);

                if (distSq < minDistanceSquared) {
                    minDistanceSquared = distSq;
                    bestPoint = new Point(endPt.X, endPt.Y);
                    bestType = "Punto Final";
                    found = true;
                }

                // 2. Probar el punto inicial del trazo (origen de la línea)
                StylusPoint startPt = stroke.StylusPoints[0];
                double dx0 = mousePos.X - startPt.X;
                double dy0 = mousePos.Y - startPt.Y;
                double distSq0 = (dx0 * dx0) + (dy0 * dy0);

                if (distSq0 < minDistanceSquared) {
                    minDistanceSquared = distSq0;
                    bestPoint = new Point(startPt.X, startPt.Y);
                    bestType = "Punto Inicial";
                    found = true;
                }

                // 3. Si el trazo es un vector/flecha (vértice en punto 1), probar la punta del vector
                if (stroke.StylusPoints.Count == 5) {
                    StylusPoint tipPt = stroke.StylusPoints[1];
                    double dxTip = mousePos.X - tipPt.X;
                    double dyTip = mousePos.Y - tipPt.Y;
                    double distSqTip = (dxTip * dxTip) + (dyTip * dyTip);

                    if (distSqTip < minDistanceSquared) {
                        minDistanceSquared = distSqTip;
                        bestPoint = new Point(tipPt.X, tipPt.Y);
                        bestType = "Punta de Vector";
                        found = true;
                    }
                }
            }

            if (found) {
                result.IsSnapped = true;
                result.Point = bestPoint;
                result.SnapType = bestType;
            }

            return result;
        }

        /// <summary>
        /// Dibuja el marcador visual magnético de captura en pantalla (estilo CAD).
        /// </summary>
        public static void DrawSnapIndicator(DrawingContext dc, Point p, string label = "Punto Final") {
            // Cuadro magnético verde lima brillante (12x12 px)
            Pen snapPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 255, 128)), 1.8);
            Brush snapFill = new SolidColorBrush(Color.FromArgb(60, 0, 255, 128));
            Rect snapRect = new Rect(p.X - 6.0, p.Y - 6.0, 12.0, 12.0);
            dc.DrawRectangle(snapFill, snapPen, snapRect);

            // Diana central
            dc.DrawEllipse(Brushes.White, null, p, 2.5, 2.5);

            // Cartel flotante "🧲 Punto Final"
            Typeface tf = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
#pragma warning disable 618
            FormattedText ft = new FormattedText(
                "🧲 " + label,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                tf,
                9.5,
                Brushes.White
            );
#pragma warning restore 618

            Rect labelRect = new Rect(p.X + 9.0, p.Y - 18.0, ft.Width + 8.0, ft.Height + 4.0);
            Brush labelBg = new SolidColorBrush(Color.FromArgb(230, 20, 24, 30));
            Pen labelBorder = new Pen(new SolidColorBrush(Color.FromRgb(0, 255, 128)), 1.0);

            dc.DrawRoundedRectangle(labelBg, labelBorder, labelRect, 3.0, 3.0);
            dc.DrawText(ft, new Point(p.X + 13.0, p.Y - 16.0));
        }
    }
}

