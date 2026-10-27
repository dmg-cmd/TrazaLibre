using System;
using System.Globalization;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Trazalibre.Services {
    /// <summary>
    /// Servicio modular para diagramas vectoriales y trazado de vectores con flechas orientadas.
    /// Permite:
    /// - Alternar entre trazo de línea recta simple y vector con punta de flecha (magnitud + dirección).
    /// - Acotar automáticamente el vector con su módulo en milímetros y su ángulo en grados.
    /// </summary>
    public static class VectorService {
        private static bool _isVectorMode = false;
        private static bool _isDimensionEnabled = true;

        /// <summary>
        /// Indica si el modo de diagramas vectoriales (con flecha en el extremo) está activo.
        /// </summary>
        public static bool IsVectorMode {
            get { return _isVectorMode; }
            set {
                if (_isVectorMode != value) {
                    _isVectorMode = value;
                    OnVectorModeChanged();
                }
            }
        }

        /// <summary>
        /// Indica si se debe estampar la cota técnica con el valor de módulo y ángulo junto al vector.
        /// </summary>
        public static bool IsDimensionEnabled {
            get { return _isDimensionEnabled; }
            set {
                if (_isDimensionEnabled != value) {
                    _isDimensionEnabled = value;
                    OnDimensionModeChanged();
                }
            }
        }

        public static event EventHandler VectorModeChanged;
        public static event EventHandler DimensionModeChanged;

        private static void OnVectorModeChanged() {
            EventHandler handler = VectorModeChanged;
            if (handler != null) {
                handler(null, EventArgs.Empty);
            }
        }

        private static void OnDimensionModeChanged() {
            EventHandler handler = DimensionModeChanged;
            if (handler != null) {
                handler(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Alterna el modo vector (activa o desactiva la punta de flecha).
        /// </summary>
        public static bool ToggleVectorMode() {
            IsVectorMode = !IsVectorMode;
            return IsVectorMode;
        }

        /// <summary>
        /// Alterna la visualización y grabado de cotas de módulo y ángulo en el vector.
        /// </summary>
        public static bool ToggleDimensionMode() {
            IsDimensionEnabled = !IsDimensionEnabled;
            return IsDimensionEnabled;
        }

        /// <summary>
        /// Construye los puntos geométricos (Point[]) para una línea o vector con punta de flecha.
        /// </summary>
        public static Point[] CreateVectorPoints(Point start, Point end, double thickness) {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist < 8.0) {
                return new Point[] { start, end };
            }

            double angle = Math.Atan2(dy, dx);
            double headLength = Math.Min(dist * 0.35, Math.Max(14.0, thickness * 3.8));
            double headAngle = Math.PI / 6.0; // 30 grados

            Point wing1 = new Point(
                end.X - headLength * Math.Cos(angle - headAngle),
                end.Y - headLength * Math.Sin(angle - headAngle)
            );

            Point wing2 = new Point(
                end.X - headLength * Math.Cos(angle + headAngle),
                end.Y - headLength * Math.Sin(angle + headAngle)
            );

            return new Point[] {
                start,
                end,
                wing1,
                end,
                wing2
            };
        }

        /// <summary>
        /// Construye los puntos de trazo (StylusPointCollection) para una línea simple o un vector con punta de flecha.
        /// </summary>
        public static StylusPointCollection CreateStrokePoints(Point start, Point end, double thickness, bool asVector) {
            StylusPointCollection points = new StylusPointCollection();
            if (!asVector) {
                points.Add(new StylusPoint(start.X, start.Y));
                points.Add(new StylusPoint(end.X, end.Y));
                return points;
            }

            Point[] pts = CreateVectorPoints(start, end, thickness);
            for (int i = 0; i < pts.Length; i++) {
                points.Add(new StylusPoint(pts[i].X, pts[i].Y));
            }
            return points;
        }

        /// <summary>
        /// Dibuja una punta de flecha vectorial en tiempo real en el contexto gráfico de previsualización.
        /// </summary>
        public static void DrawArrowHead(DrawingContext dc, Point start, Point end, Brush brush, double thickness) {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 4.0) return;

            double angle = Math.Atan2(dy, dx);
            double arrowSize = Math.Min(dist * 0.35, Math.Max(12.0, thickness * 3.2));

            Point p1 = new Point(
                end.X - arrowSize * Math.Cos(angle - Math.PI / 6.0),
                end.Y - arrowSize * Math.Sin(angle - Math.PI / 6.0)
            );
            Point p2 = new Point(
                end.X - arrowSize * Math.Cos(angle + Math.PI / 6.0),
                end.Y - arrowSize * Math.Sin(angle + Math.PI / 6.0)
            );

            StreamGeometry geom = new StreamGeometry();
            using (StreamGeometryContext ctx = geom.Open()) {
                ctx.BeginFigure(end, true, true);
                ctx.LineTo(p1, true, false);
                ctx.LineTo(p2, true, false);
            }
            geom.Freeze();
            dc.DrawGeometry(brush, null, geom);
        }

        /// <summary>
        /// Dibuja en tiempo real una placa flotante de cota técnica en el punto medio del vector.
        /// </summary>
        public static void DrawDimensionPreview(DrawingContext dc, Point start, Point end, double lengthPixels, double angleDeg) {
            if (!IsVectorMode || !IsDimensionEnabled) return;

            double mm = MetricUnits.PxToMm(lengthPixels);
            string text = string.Format("{0:0.#} mm  ∠ {1:0.#}°", mm, angleDeg);

            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 22.0) return;

            // Desplazar 20 px perpendicularmente a la recta
            double nx = -dy / dist;
            double ny = dx / dist;
            if (ny > 0) { nx = -nx; ny = -ny; }
            double offsetDist = 20.0;

            Point midPoint = new Point(
                (start.X + end.X) / 2.0 + nx * offsetDist,
                (start.Y + end.Y) / 2.0 + ny * offsetDist
            );

            Typeface tf = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
#pragma warning disable 618
            FormattedText ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 10.5, Brushes.Gold);
#pragma warning restore 618

            Rect bgRect = new Rect(midPoint.X - ft.Width / 2.0 - 6, midPoint.Y - ft.Height / 2.0 - 3, ft.Width + 12, ft.Height + 6);
            Brush bgBrush = new SolidColorBrush(Color.FromArgb(220, 20, 24, 30));
            Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 215, 0)), 1.0);

            dc.DrawRoundedRectangle(bgBrush, borderPen, bgRect, 4.0, 4.0);
            dc.DrawText(ft, new Point(midPoint.X - ft.Width / 2.0, midPoint.Y - ft.Height / 2.0));
        }

        /// <summary>
        /// Sobrecarga conveniente para estampar la cota técnica con grosor predeterminado.
        /// </summary>
        public static StrokeCollection CreateDimensionStrokes(Point start, Point end, double lengthPixels, double angleDeg, Color color) {
            return CreateDimensionStrokes(start, end, lengthPixels, angleDeg, color, 1.5);
        }

        /// <summary>
        /// Genera trazos permanentes en el lienzo para estampar la cota técnica (módulo en mm y ángulo).
        /// </summary>
        public static StrokeCollection CreateDimensionStrokes(Point start, Point end, double lengthPixels, double angleDeg, Color color, double thickness) {
            StrokeCollection strokes = new StrokeCollection();
            if (!IsVectorMode || !IsDimensionEnabled) return strokes;

            double mm = MetricUnits.PxToMm(lengthPixels);
            string text = string.Format("{0:0.#} mm ∠ {1:0.#}°", mm, angleDeg);

            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 20.0) return strokes;

            double nx = -dy / dist;
            double ny = dx / dist;
            if (ny > 0) { nx = -nx; ny = -ny; }
            double offsetDist = 18.0;

            Point midPoint = new Point(
                (start.X + end.X) / 2.0 + nx * offsetDist,
                (start.Y + end.Y) / 2.0 + ny * offsetDist
            );

            Typeface typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
#pragma warning disable 618
            FormattedText ft = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                11.0,
                Brushes.White
            );
#pragma warning restore 618

            Point textOrigin = new Point(midPoint.X - (ft.Width / 2.0), midPoint.Y - (ft.Height / 2.0));
            Geometry textGeom = ft.BuildGeometry(textOrigin);
            PathGeometry flatGeom = textGeom.GetFlattenedPathGeometry();

            DrawingAttributes textAttrs = new DrawingAttributes {
                Color = color,
                Width = Math.Max(1.2, thickness * 0.4),
                Height = Math.Max(1.2, thickness * 0.4),
                FitToCurve = false,
                IsHighlighter = false
            };

            foreach (PathFigure fig in flatGeom.Figures) {
                StylusPointCollection pts = new StylusPointCollection();
                pts.Add(new StylusPoint(fig.StartPoint.X, fig.StartPoint.Y));
                foreach (PathSegment seg in fig.Segments) {
                    PolyLineSegment poly = seg as PolyLineSegment;
                    if (poly != null) {
                        foreach (Point p in poly.Points) pts.Add(new StylusPoint(p.X, p.Y));
                    } else {
                        LineSegment line = seg as LineSegment;
                        if (line != null) pts.Add(new StylusPoint(line.Point.X, line.Point.Y));
                    }
                }
                strokes.Add(new Stroke(pts, textAttrs));
            }

            return strokes;
        }
    }
}
