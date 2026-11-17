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
        public static bool ManualLabelPlacement = true;

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
        /// Alterna la ubicación manual con el ratón de la etiqueta de medida en VectorTool.
        /// </summary>
        public static bool ToggleManualLabelPlacement() {
            ManualLabelPlacement = !ManualLabelPlacement;
            return ManualLabelPlacement;
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
        /// Construye el texto formateado de una cota técnica (módulo en mm y ángulo).
        /// </summary>
        public static string GetDimensionText(double lengthPixels, double angleDeg) {
            double mm = MetricUnits.PxToMm(lengthPixels);
            return string.Format("{0:0.#} mm  ∠ {1:0.#}°", mm, angleDeg);
        }

        /// <summary>
        /// Mide el texto y las dimensiones de la placa de la etiqueta técnica.
        /// </summary>
        public static void MeasureDimensionBox(string text, out FormattedText ft, out double boxWidth, out double boxHeight) {
            Typeface tf = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
#pragma warning disable 618
            ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 13.0, Brushes.Gold);
#pragma warning restore 618
            boxWidth = ft.Width + 16.0;
            boxHeight = ft.Height + 10.0;
        }

        /// <summary>
        /// Calcula la mejor posición libre de colisiones para ubicar la etiqueta de medida respecto al vector.
        /// </summary>
        public static Point FindOptimalLabelCenterForDimension(Point start, Point end, double lengthPixels, double angleDeg, StrokeCollection existingStrokes) {
            string text = GetDimensionText(lengthPixels, angleDeg);
            FormattedText ft;
            double boxWidth;
            double boxHeight;
            MeasureDimensionBox(text, out ft, out boxWidth, out boxHeight);
            return FindOptimalLabelCenter(start, end, boxWidth, boxHeight, existingStrokes);
        }

        /// <summary>
        /// Calcula la mejor posición libre de colisiones para una caja de medidas de tamaño conocido.
        /// </summary>
        public static Point FindOptimalLabelCenter(Point start, Point end, double boxWidth, double boxHeight, StrokeCollection existingStrokes) {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1.0) {
                return new Point(start.X, start.Y - 28.0);
            }

            double nx = -dy / dist;
            double ny = dx / dist;
            if (ny > 0) {
                nx = -nx;
                ny = -ny;
            }

            double[] tValues = new double[] { 0.5, 0.35, 0.65 };
            double[] offsets = new double[] { 28.0, -28.0, 50.0, -50.0, 72.0, -72.0 };

            Point bestCenter = new Point((start.X + end.X) / 2.0 + nx * 28.0, (start.Y + end.Y) / 2.0 + ny * 28.0);
            int minCollisions = int.MaxValue;

            foreach (double offset in offsets) {
                foreach (double t in tValues) {
                    Point anchor = new Point(start.X + dx * t, start.Y + dy * t);
                    Point candidate = new Point(anchor.X + nx * offset, anchor.Y + ny * offset);
                    Rect candRect = new Rect(candidate.X - boxWidth / 2.0, candidate.Y - boxHeight / 2.0, boxWidth, boxHeight);

                    int collisions = 0;

                    // Evitar que la propia caja pise al segmento recién trazado
                    if (SegmentIntersectsRect(start, end, Rect.Inflate(candRect, 3.0, 3.0))) {
                        collisions++;
                    }

                    if (existingStrokes != null) {
                        for (int i = 0; i < existingStrokes.Count; i++) {
                            if (DoesStrokeIntersectRect(existingStrokes[i], candRect)) {
                                collisions++;
                            }
                        }
                    }

                    if (collisions == 0) {
                        return candidate;
                    }

                    if (collisions < minCollisions) {
                        minCollisions = collisions;
                        bestCenter = candidate;
                    }
                }
            }

            return bestCenter;
        }

        /// <summary>
        /// Dibuja en tiempo real una placa flotante de cota técnica en la posición automática óptima del vector.
        /// </summary>
        public static void DrawDimensionPreview(DrawingContext dc, Point start, Point end, double lengthPixels, double angleDeg) {
            if (!IsVectorMode || !IsDimensionEnabled) return;

            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 12.0) return;

            Point labelCenter = FindOptimalLabelCenterForDimension(start, end, lengthPixels, angleDeg, null);
            DrawDimensionPreviewAt(dc, start, end, labelCenter, lengthPixels, angleDeg, Colors.Gold);
        }

        /// <summary>
        /// Dibuja en tiempo real la etiqueta de medida en una coordenada específica junto con su línea guía conectora.
        /// </summary>
        public static void DrawDimensionPreviewAt(DrawingContext dc, Point start, Point end, Point labelCenter, double lengthPixels, double angleDeg, Color vectorColor) {
            string text = GetDimensionText(lengthPixels, angleDeg);
            FormattedText ft;
            double boxWidth;
            double boxHeight;
            MeasureDimensionBox(text, out ft, out boxWidth, out boxHeight);

            Rect boxRect = new Rect(labelCenter.X - boxWidth / 2.0, labelCenter.Y - boxHeight / 2.0, boxWidth, boxHeight);

            // Línea guía conectora si la etiqueta está separada del vector
            if (DistancePointToSegment(labelCenter, start, end) > 30.0) {
                Point vecMid = new Point((start.X + end.X) / 2.0, (start.Y + end.Y) / 2.0);
                Point boxAnchor = GetBoxEdgeAnchor(boxRect, vecMid);
                Color guideCol = Color.FromArgb(190, vectorColor.R, vectorColor.G, vectorColor.B);
                Brush guideBrush = new SolidColorBrush(guideCol);
                Pen guidePen = new Pen(guideBrush, 1.2);
                guidePen.DashStyle = new DashStyle(new double[] { 4.0, 3.0 }, 0.0);
                dc.DrawLine(guidePen, boxAnchor, vecMid);
                dc.DrawEllipse(guideBrush, null, vecMid, 2.5, 2.5);
            }

            Brush bgBrush = new SolidColorBrush(Color.FromArgb(230, 18, 22, 30));
            Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(220, vectorColor.R, vectorColor.G, vectorColor.B)), 1.4);

            dc.DrawRoundedRectangle(bgBrush, borderPen, boxRect, 4.0, 4.0);
            dc.DrawText(ft, new Point(labelCenter.X - ft.Width / 2.0, labelCenter.Y - ft.Height / 2.0));
        }

        /// <summary>
        /// Sobrecarga conveniente para estampar la cota técnica con grosor predeterminado.
        /// </summary>
        public static StrokeCollection CreateDimensionStrokes(Point start, Point end, double lengthPixels, double angleDeg, Color color) {
            return CreateDimensionStrokes(start, end, lengthPixels, angleDeg, color, 1.5, false, null);
        }

        /// <summary>
        /// Genera trazos permanentes en el lienzo para estampar la cota técnica (módulo en mm y ángulo).
        /// </summary>
        public static StrokeCollection CreateDimensionStrokes(Point start, Point end, double lengthPixels, double angleDeg, Color color, double thickness) {
            return CreateDimensionStrokes(start, end, lengthPixels, angleDeg, color, thickness, false, null);
        }

        /// <summary>
        /// Genera trazos permanentes evitando colisiones con los trazos ya existentes en el lienzo.
        /// </summary>
        public static StrokeCollection CreateDimensionStrokes(Point start, Point end, double lengthPixels, double angleDeg, Color color, double thickness, StrokeCollection existingStrokes) {
            return CreateDimensionStrokes(start, end, lengthPixels, angleDeg, color, thickness, false, existingStrokes);
        }

        /// <summary>
        /// Genera trazos permanentes en el lienzo para estampar la cota técnica con medidas (módulo en mm y ángulo sexagesimal).
        /// Permite forzar la creación cuando una herramienta dedicada (como VectorTool) lo solicita explícitamente.
        /// </summary>
        public static StrokeCollection CreateDimensionStrokes(Point start, Point end, double lengthPixels, double angleDeg, Color color, double thickness, bool forceCreate) {
            return CreateDimensionStrokes(start, end, lengthPixels, angleDeg, color, thickness, forceCreate, null);
        }

        /// <summary>
        /// Genera trazos permanentes buscando automáticamente una posición libre de colisiones en existingStrokes.
        /// </summary>
        public static StrokeCollection CreateDimensionStrokes(Point start, Point end, double lengthPixels, double angleDeg, Color color, double thickness, bool forceCreate, StrokeCollection existingStrokes) {
            if (!forceCreate && (!IsVectorMode || !IsDimensionEnabled)) {
                return new StrokeCollection();
            }

            Point labelCenter = FindOptimalLabelCenterForDimension(start, end, lengthPixels, angleDeg, existingStrokes);
            return CreateDimensionStrokesAt(start, end, labelCenter, lengthPixels, angleDeg, color, thickness, forceCreate);
        }

        /// <summary>
        /// Genera los trazos permanentes completos de la etiqueta técnica (línea guía, placa protectora,
        /// marco redondeado y texto nítido) exactamente en la coordenada labelCenter indicada.
        /// </summary>
        public static StrokeCollection CreateDimensionStrokesAt(Point start, Point end, Point labelCenter, double lengthPixels, double angleDeg, Color color, double thickness, bool forceCreate) {
            StrokeCollection strokes = new StrokeCollection();
            if (!forceCreate && (!IsVectorMode || !IsDimensionEnabled)) return strokes;

            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 8.0) return strokes;

            string text = GetDimensionText(lengthPixels, angleDeg);
            FormattedText ft;
            double boxWidth;
            double boxHeight;
            MeasureDimensionBox(text, out ft, out boxWidth, out boxHeight);

            Rect boxRect = new Rect(
                labelCenter.X - (boxWidth / 2.0),
                labelCenter.Y - (boxHeight / 2.0),
                boxWidth,
                boxHeight
            );

            // 1. Línea guía conectora hasta el punto medio del vector si está separada > 30 px
            if (DistancePointToSegment(labelCenter, start, end) > 30.0) {
                Point vecMid = new Point((start.X + end.X) / 2.0, (start.Y + end.Y) / 2.0);
                Point boxAnchor = GetBoxEdgeAnchor(boxRect, vecMid);
                Color guideColor = Color.FromArgb(190, color.R, color.G, color.B);

                StylusPointCollection guidePts = new StylusPointCollection();
                guidePts.Add(new StylusPoint(boxAnchor.X, boxAnchor.Y));
                guidePts.Add(new StylusPoint(vecMid.X, vecMid.Y));

                DrawingAttributes guideAttrs = new DrawingAttributes {
                    Color = guideColor,
                    Width = 1.2,
                    Height = 1.2,
                    FitToCurve = false,
                    IsHighlighter = false
                };
                strokes.Add(new Stroke(guidePts, guideAttrs));

                StylusPointCollection dotPts = new StylusPointCollection();
                dotPts.Add(new StylusPoint(vecMid.X, vecMid.Y));
                DrawingAttributes dotAttrs = new DrawingAttributes {
                    Color = guideColor,
                    Width = 4.5,
                    Height = 4.5,
                    FitToCurve = false,
                    IsHighlighter = false
                };
                strokes.Add(new Stroke(dotPts, dotAttrs));
            }

            // 2. Placa de fondo oscura protectora (oculta cuadrículas o trazos detrás del texto)
            StylusPointCollection bgPts = new StylusPointCollection();
            bgPts.Add(new StylusPoint(boxRect.Left + 2.0, labelCenter.Y));
            bgPts.Add(new StylusPoint(boxRect.Right - 2.0, labelCenter.Y));

            DrawingAttributes bgAttrs = new DrawingAttributes {
                Color = Color.FromArgb(230, 18, 22, 30),
                StylusTip = StylusTip.Rectangle,
                Width = 4.0,
                Height = boxRect.Height,
                FitToCurve = false,
                IsHighlighter = false
            };
            strokes.Add(new Stroke(bgPts, bgAttrs));

            // 3. Marco redondeado fino con el color del vector
            double r = 4.0;
            StylusPointCollection borderPts = new StylusPointCollection();
            borderPts.Add(new StylusPoint(boxRect.Left + r, boxRect.Top));
            borderPts.Add(new StylusPoint(boxRect.Right - r, boxRect.Top));
            borderPts.Add(new StylusPoint(boxRect.Right - r * 0.3, boxRect.Top + r * 0.3));
            borderPts.Add(new StylusPoint(boxRect.Right, boxRect.Top + r));
            borderPts.Add(new StylusPoint(boxRect.Right, boxRect.Bottom - r));
            borderPts.Add(new StylusPoint(boxRect.Right - r * 0.3, boxRect.Bottom - r * 0.3));
            borderPts.Add(new StylusPoint(boxRect.Right - r, boxRect.Bottom));
            borderPts.Add(new StylusPoint(boxRect.Left + r, boxRect.Bottom));
            borderPts.Add(new StylusPoint(boxRect.Left + r * 0.3, boxRect.Bottom - r * 0.3));
            borderPts.Add(new StylusPoint(boxRect.Left, boxRect.Bottom - r));
            borderPts.Add(new StylusPoint(boxRect.Left, boxRect.Top + r));
            borderPts.Add(new StylusPoint(boxRect.Left + r * 0.3, boxRect.Top + r * 0.3));
            borderPts.Add(new StylusPoint(boxRect.Left + r, boxRect.Top));

            DrawingAttributes borderAttrs = new DrawingAttributes {
                Color = color,
                Width = 1.4,
                Height = 1.4,
                FitToCurve = false,
                IsHighlighter = false
            };
            strokes.Add(new Stroke(borderPts, borderAttrs));

            // 4. Texto vectorizado nítido de alto contraste
            Point textOrigin = new Point(labelCenter.X - (ft.Width / 2.0), labelCenter.Y - (ft.Height / 2.0));
            Geometry textGeom = ft.BuildGeometry(textOrigin);
            PathGeometry flatGeom = textGeom.GetFlattenedPathGeometry();

            DrawingAttributes textAttrs = new DrawingAttributes {
                Color = Colors.Gold,
                Width = 1.15,
                Height = 1.15,
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

        private static double DistancePointToSegment(Point p, Point a, Point b) {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lenSq = dx * dx + dy * dy;
            if (lenSq < 0.0001) {
                return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));
            }
            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq;
            if (t < 0.0) t = 0.0;
            else if (t > 1.0) t = 1.0;
            double projX = a.X + t * dx;
            double projY = a.Y + t * dy;
            return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
        }

        private static Point GetBoxEdgeAnchor(Rect rect, Point target) {
            Point center = new Point(rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0);
            double dx = target.X - center.X;
            double dy = target.Y - center.Y;
            if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001) {
                return center;
            }

            double halfW = rect.Width / 2.0;
            double halfH = rect.Height / 2.0;
            double scaleX = Math.Abs(dx) > 0.0001 ? halfW / Math.Abs(dx) : double.MaxValue;
            double scaleY = Math.Abs(dy) > 0.0001 ? halfH / Math.Abs(dy) : double.MaxValue;
            double scale = Math.Min(scaleX, scaleY);

            return new Point(center.X + dx * scale, center.Y + dy * scale);
        }

        private static bool DoesStrokeIntersectRect(Stroke s, Rect rect) {
            if (s == null) return false;

            // Ignorar las líneas tenues de la cuadrícula milimetrada de fondo
            if (s.DrawingAttributes.Color.A < 140 && s.DrawingAttributes.Width <= 1.1) {
                return false;
            }

            Rect sb = s.GetBounds();
            if (sb.IsEmpty) return false;

            double strokePad = Math.Min(12.0, Math.Max(s.DrawingAttributes.Width, s.DrawingAttributes.Height) * 0.5) + 3.0;
            Rect padded = Rect.Inflate(rect, strokePad, strokePad);
            if (!sb.IntersectsWith(padded)) {
                return false;
            }

            if (s.StylusPoints.Count <= 1 || (sb.Width <= rect.Width * 1.5 && sb.Height <= rect.Height * 1.5)) {
                return true;
            }

            for (int i = 0; i < s.StylusPoints.Count - 1; i++) {
                Point p1 = new Point(s.StylusPoints[i].X, s.StylusPoints[i].Y);
                Point p2 = new Point(s.StylusPoints[i + 1].X, s.StylusPoints[i + 1].Y);
                if (SegmentIntersectsRect(p1, p2, padded)) {
                    return true;
                }
            }

            return false;
        }

        private static bool SegmentIntersectsRect(Point a, Point b, Rect r) {
            if (r.Contains(a) || r.Contains(b)) return true;
            if (Math.Max(a.X, b.X) < r.Left || Math.Min(a.X, b.X) > r.Right) return false;
            if (Math.Max(a.Y, b.Y) < r.Top || Math.Min(a.Y, b.Y) > r.Bottom) return false;

            Point tl = new Point(r.Left, r.Top);
            Point tr = new Point(r.Right, r.Top);
            Point br = new Point(r.Right, r.Bottom);
            Point bl = new Point(r.Left, r.Bottom);

            return SegmentsIntersect(a, b, tl, tr) ||
                   SegmentsIntersect(a, b, tr, br) ||
                   SegmentsIntersect(a, b, br, bl) ||
                   SegmentsIntersect(a, b, bl, tl);
        }

        private static bool SegmentsIntersect(Point p1, Point p2, Point q1, Point q2) {
            double d1 = CrossProduct(q1, q2, p1);
            double d2 = CrossProduct(q1, q2, p2);
            double d3 = CrossProduct(p1, p2, q1);
            double d4 = CrossProduct(p1, p2, q2);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) {
                return true;
            }
            return false;
        }

        private static double CrossProduct(Point a, Point b, Point c) {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }
    }
}
