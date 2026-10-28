using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Trazalibre.Core;
using Trazalibre.Services;

namespace Trazalibre.Tools {
    /// <summary>
    /// Herramienta interactiva de Ejes de Coordenadas Cartesianos ((🌐).
    /// Permite colocar un sistema de ejes coordenados (X e Y) graduados en cualquier lugar de la pantalla.
    /// Incorpora un punto explícito en el origen (0, 0) que se integra con SnapService,
    /// permitiendo imantar y anclar con total precisión el primer punto de vectores y rectas técnicas.
    /// Soporta incremento dinámico de tamaño:
    /// 1. Por arrastre interactivo con el ratón (clic en origen y arrastrar para extender los ejes).
    /// 2. Con la rueda del ratón (+ / - pasos de a 10 mm).
    /// 3. Con las teclas + / - y flechas del teclado (+10 mm o +50 mm con Shift).
    /// 4. Mediante botones dedicados [➕] / [➖] en la barra de herramientas.
    /// </summary>
    public class AxesTool : ITool {
        public string Id { get { return "geom_axes"; } }
        public string DisplayName { get { return "Ejes Coordenados"; } }
        public string Icon { get { return "🌐"; } }

        public static double SharedAxisHalfLength = 200.0; // 40 mm hacia cada cuadrante (80 mm total)
        public static event EventHandler<double> AxisLengthChanged;

        private IAnnotationContext _context;
        private AxesOverlayVisual _visual;

        private MouseButtonEventHandler _mouseDownHandler;
        private MouseEventHandler _mouseMoveHandler;
        private MouseButtonEventHandler _mouseUpHandler;
        private MouseWheelEventHandler _mouseWheelHandler;
        private KeyEventHandler _keyDownHandler;

        private Point _currentPosition = new Point(400, 300);
        private Point _dragOrigin = new Point(400, 300);
        private bool _isDragging = false;

        public double AxisHalfLength {
            get { return SharedAxisHalfLength; }
            set {
                double clamped = Math.Max(50.0, Math.Min(900.0, value));
                if (Math.Abs(SharedAxisHalfLength - clamped) > 0.001) {
                    SharedAxisHalfLength = clamped;
                    OnAxisLengthChanged(SharedAxisHalfLength);
                }
            }
        }

        public void IncreaseSize(double mm = 10.0) {
            AxisHalfLength += MetricUnits.MmToPx(mm);
            if (_visual != null) {
                _visual.HalfLength = AxisHalfLength;
                _visual.InvalidateVisual();
            }
        }

        public void DecreaseSize(double mm = 10.0) {
            AxisHalfLength -= MetricUnits.MmToPx(mm);
            if (_visual != null) {
                _visual.HalfLength = AxisHalfLength;
                _visual.InvalidateVisual();
            }
        }

        private static void OnAxisLengthChanged(double newLength) {
            EventHandler<double> handler = AxisLengthChanged;
            if (handler != null) {
                handler(null, newLength);
            }
        }

        public void Activate(IAnnotationContext context) {
            _context = context;
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.None;
            context.Canvas.Cursor = Cursors.Cross;
            context.Canvas.Focusable = true;
            context.Canvas.Focus();

            if (_visual == null) {
                _visual = new AxesOverlayVisual(this);
            }

            if (!_context.Canvas.Children.Contains(_visual)) {
                _context.Canvas.Children.Add(_visual);
            }

            _visual.Visibility = Visibility.Visible;
            _visual.Color = _context.CurrentColor;
            _visual.Size = _context.CurrentSize;
            _visual.Center = _currentPosition;
            _visual.HalfLength = AxisHalfLength;
            _isDragging = false;

            if (_mouseDownHandler == null) {
                _mouseDownHandler = OnCanvasMouseDown;
                _mouseMoveHandler = OnCanvasMouseMove;
                _mouseUpHandler = OnCanvasMouseUp;
                _mouseWheelHandler = OnCanvasMouseWheel;
                _keyDownHandler = OnCanvasKeyDown;
            }

            AttachEvents();
        }

        public void Deactivate(IAnnotationContext context) {
            DetachEvents();

            if (_visual != null && _context != null && _context.Canvas != null) {
                _context.Canvas.Children.Remove(_visual);
            }

            _isDragging = false;
            _context = null;
        }

        public void OnColorChanged(IAnnotationContext context, Color newColor) {
            if (_visual != null) {
                _visual.Color = newColor;
                _visual.InvalidateVisual();
            }
        }

        public void OnSizeChanged(IAnnotationContext context, double newSize) {
            if (_visual != null) {
                _visual.Size = newSize;
                _visual.InvalidateVisual();
            }
        }

        private void AttachEvents() {
            if (_context == null || _context.Canvas == null) return;
            DetachEvents();

            _context.Canvas.PreviewMouseDown += _mouseDownHandler;
            _context.Canvas.PreviewMouseMove += _mouseMoveHandler;
            _context.Canvas.PreviewMouseUp += _mouseUpHandler;
            _context.Canvas.PreviewMouseWheel += _mouseWheelHandler;
            _context.Canvas.PreviewKeyDown += _keyDownHandler;
        }

        private void DetachEvents() {
            if (_context == null || _context.Canvas == null) return;

            _context.Canvas.PreviewMouseDown -= _mouseDownHandler;
            _context.Canvas.PreviewMouseMove -= _mouseMoveHandler;
            _context.Canvas.PreviewMouseUp -= _mouseUpHandler;
            _context.Canvas.PreviewMouseWheel -= _mouseWheelHandler;
            _context.Canvas.PreviewKeyDown -= _keyDownHandler;
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e) {
            if (_context == null) return;

            Point mousePos = e.GetPosition(_context.Canvas);

            if (_isDragging) {
                // Modo arrastre interactivo para incrementar los ejes
                double dx = mousePos.X - _dragOrigin.X;
                double dy = mousePos.Y - _dragOrigin.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if ((Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) {
                    // Cuantizar incremento a 10 mm = 1 cm (50 px)
                    dist = Math.Max(50.0, Math.Round(dist / 50.0) * 50.0);
                }

                AxisHalfLength = Math.Max(50.0, dist);

                if (_visual != null) {
                    _visual.Center = _dragOrigin;
                    _visual.HalfLength = AxisHalfLength;
                    _visual.Color = _context.CurrentColor;
                    _visual.Size = _context.CurrentSize;
                    _visual.InvalidateVisual();
                }
            } else {
                _currentPosition = mousePos;

                if (_visual != null) {
                    _visual.Center = _currentPosition;
                    _visual.Color = _context.CurrentColor;
                    _visual.Size = _context.CurrentSize;
                    _visual.HalfLength = AxisHalfLength;
                    _visual.InvalidateVisual();
                }
            }
        }

        private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e) {
            // Regular/incrementar el tamaño de los ejes con la rueda del ratón
            double stepMm = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 25.0 : 10.0;
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
                stepMm = 1.0; // Ajuste fino de 1 mm
            }

            if (e.Delta > 0) {
                IncreaseSize(stepMm);
            } else {
                DecreaseSize(stepMm);
            }

            e.Handled = true;
        }

        private void OnCanvasKeyDown(object sender, KeyEventArgs e) {
            // 1. Teclas para incrementar y reducir el tamaño de los ejes
            double stepMm = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 50.0 : 10.0;

            if (e.Key == Key.Add || e.Key == Key.OemPlus || e.Key == Key.Up || e.Key == Key.Right) {
                IncreaseSize(stepMm);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Subtract || e.Key == Key.OemMinus || e.Key == Key.Down || e.Key == Key.Left) {
                DecreaseSize(stepMm);
                e.Handled = true;
                return;
            }

            // Tecla Enter: Estampar ejes en la posición actual
            if (e.Key == Key.Enter || e.Key == Key.Return) {
                if (_context != null) {
                    StrokeCollection strokes = CreateAxesStrokes(_currentPosition, AxisHalfLength, _context.CurrentColor, _context.CurrentSize);
                    _context.AddStrokes(strokes);
                }
                e.Handled = true;
            }
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed || _context == null) return;

            Point clickPos = e.GetPosition(_context.Canvas);
            SnapResult snap = SnapService.FindSnapPoint(_context.Canvas.Strokes, clickPos);

            _dragOrigin = snap.IsSnapped ? snap.Point : clickPos;
            _isDragging = true;

            if (_visual != null) {
                _visual.Center = _dragOrigin;
                _visual.HalfLength = AxisHalfLength;
                _visual.InvalidateVisual();
            }

            e.Handled = true;
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e) {
            if (!_isDragging || _context == null) return;
            _isDragging = false;

            // Generar trazos permanentes en el lienzo con el punto de origen (0, 0) y el tamaño incrementado
            StrokeCollection axesStrokes = CreateAxesStrokes(_dragOrigin, AxisHalfLength, _context.CurrentColor, _context.CurrentSize);
            _context.AddStrokes(axesStrokes);

            e.Handled = true;
        }

        /// <summary>
        /// Construye el conjunto de trazos que representan los ejes cartesianos, marcas milimétricas,
        /// números incrementales en cada centímetro, flechas y el punto explícito en el origen (0, 0).
        /// </summary>
        public static StrokeCollection CreateAxesStrokes(Point origin, double halfLength, Color color, double thickness) {
            StrokeCollection strokes = new StrokeCollection();
            double mainThick = Math.Max(2.0, thickness);
            double tickThick = Math.Max(1.2, mainThick * 0.7);

            DrawingAttributes mainAttrs = new DrawingAttributes {
                Color = color,
                Width = mainThick,
                Height = mainThick,
                FitToCurve = false,
                IsHighlighter = false
            };

            DrawingAttributes tickAttrs = new DrawingAttributes {
                Color = color,
                Width = tickThick,
                Height = tickThick,
                FitToCurve = false,
                IsHighlighter = false
            };

            // 1. PUNTO EN EL ORIGEN (0, 0) - Elemento clave para imantación con SnapService
            StylusPointCollection originPts = new StylusPointCollection();
            double r = Math.Max(3.5, mainThick * 1.3);
            for (double a = 0; a <= 2 * Math.PI + 0.1; a += Math.PI / 8.0) {
                originPts.Add(new StylusPoint(origin.X + r * Math.Cos(a), origin.Y + r * Math.Sin(a)));
            }
            originPts.Add(new StylusPoint(origin.X, origin.Y));

            Stroke originStroke = new Stroke(originPts, new DrawingAttributes {
                Color = Color.FromArgb(255, 255, 215, 0), // Resaltado dorado para el origen
                Width = r * 2.2,
                Height = r * 2.2,
                FitToCurve = false
            });
            originStroke.AddPropertyData(SnapService.AxesOriginGuid, true);
            strokes.Add(originStroke);

            // 2. EJE HORIZONTAL X (Desde -L hasta +L pasando por el origen)
            // Semieje +X
            StylusPointCollection posX = new StylusPointCollection();
            Point xEnd = new Point(origin.X + halfLength, origin.Y);
            Point[] arrowX = VectorService.CreateVectorPoints(origin, xEnd, mainThick);
            foreach (Point p in arrowX) posX.Add(new StylusPoint(p.X, p.Y));
            strokes.Add(new Stroke(posX, mainAttrs));

            // Semieje -X
            StylusPointCollection negX = new StylusPointCollection();
            negX.Add(new StylusPoint(origin.X, origin.Y));
            negX.Add(new StylusPoint(origin.X - halfLength, origin.Y));
            strokes.Add(new Stroke(negX, mainAttrs));

            // 3. EJE VERTICAL Y (Desde -L hasta +L pasando por el origen)
            // Semieje +Y
            StylusPointCollection posY = new StylusPointCollection();
            Point yEnd = new Point(origin.X, origin.Y - halfLength);
            Point[] arrowY = VectorService.CreateVectorPoints(origin, yEnd, mainThick);
            foreach (Point p in arrowY) posY.Add(new StylusPoint(p.X, p.Y));
            strokes.Add(new Stroke(posY, mainAttrs));

            // Semieje -Y
            StylusPointCollection negY = new StylusPointCollection();
            negY.Add(new StylusPoint(origin.X, origin.Y));
            negY.Add(new StylusPoint(origin.X, origin.Y + halfLength));
            strokes.Add(new Stroke(negY, mainAttrs));

            // 4. GRADUACIÓN MILIMÉTRICA Y NÚMEROS INCREMENTALES (Cada 10 mm / 50 px = 1 cm)
            double tickStep = MetricUnits.PixelsPerMm * 10.0; // 50 px
            double tickSize = 6.0;
            int cmCounter = 1;

            // Marcas y números en Eje X
            for (double d = tickStep; d <= halfLength - 18.0; d += tickStep, cmCounter++) {
                // +X
                StylusPointCollection tPos = new StylusPointCollection();
                tPos.Add(new StylusPoint(origin.X + d, origin.Y - tickSize));
                tPos.Add(new StylusPoint(origin.X + d, origin.Y + tickSize));
                strokes.Add(new Stroke(tPos, tickAttrs));
                AddTextStroke(strokes, cmCounter.ToString(), new Point(origin.X + d - 3.0, origin.Y + tickSize + 2.0), color, 8.5, mainThick);

                // -X
                StylusPointCollection tNeg = new StylusPointCollection();
                tNeg.Add(new StylusPoint(origin.X - d, origin.Y - tickSize));
                tNeg.Add(new StylusPoint(origin.X - d, origin.Y + tickSize));
                strokes.Add(new Stroke(tNeg, tickAttrs));
                AddTextStroke(strokes, "-" + cmCounter.ToString(), new Point(origin.X - d - 6.0, origin.Y + tickSize + 2.0), color, 8.5, mainThick);
            }

            // Marcas y números en Eje Y
            cmCounter = 1;
            for (double d = tickStep; d <= halfLength - 18.0; d += tickStep, cmCounter++) {
                // +Y
                StylusPointCollection tPos = new StylusPointCollection();
                tPos.Add(new StylusPoint(origin.X - tickSize, origin.Y - d));
                tPos.Add(new StylusPoint(origin.X + tickSize, origin.Y - d));
                strokes.Add(new Stroke(tPos, tickAttrs));
                AddTextStroke(strokes, cmCounter.ToString(), new Point(origin.X - tickSize - 12.0, origin.Y - d - 6.0), color, 8.5, mainThick);

                // -Y
                StylusPointCollection tNeg = new StylusPointCollection();
                tNeg.Add(new StylusPoint(origin.X - tickSize, origin.Y + d));
                tNeg.Add(new StylusPoint(origin.X + tickSize, origin.Y + d));
                strokes.Add(new Stroke(tNeg, tickAttrs));
                AddTextStroke(strokes, "-" + cmCounter.ToString(), new Point(origin.X - tickSize - 16.0, origin.Y + d - 6.0), color, 8.5, mainThick);
            }

            // 5. RÓTULOS TIPOGRÁFICOS "X", "Y" y "(0,0)" VECTORIZADOS
            AddTextStroke(strokes, "X", new Point(origin.X + halfLength + 12.0, origin.Y - 8.0), color, 13.0, mainThick);
            AddTextStroke(strokes, "Y", new Point(origin.X + 8.0, origin.Y - halfLength - 16.0), color, 13.0, mainThick);
            AddTextStroke(strokes, "(0,0)", new Point(origin.X - 32.0, origin.Y + 8.0), Color.FromArgb(230, 255, 215, 0), 10.0, mainThick);

            return strokes;
        }

        private static void AddTextStroke(StrokeCollection strokes, string text, Point location, Color color, double fontSize, double thickness) {
            Typeface typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
#pragma warning disable 618
            FormattedText ft = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.White
            );
#pragma warning restore 618

            Geometry textGeom = ft.BuildGeometry(location);
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
        }
    }

    /// <summary>
    /// Elemento visual interactivo de previsualización para la herramienta de Ejes Coordenados.
    /// Muestra los ejes cartesianos, graduación con números incrementales en cm, y el panel HUD.
    /// </summary>
    public class AxesOverlayVisual : FrameworkElement {
        private readonly AxesTool _tool;

        public Point Center { get; set; }
        public double HalfLength { get; set; }
        public Color Color { get; set; }
        public double Size { get; set; }

        public AxesOverlayVisual(AxesTool tool) {
            _tool = tool;
            IsHitTestVisible = false;
            Center = new Point(400, 300);
            HalfLength = 200.0;
            Color = Colors.Red;
            Size = 3.0;
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);

            Point c = Center;
            double l = HalfLength;
            double mm = MetricUnits.PxToMm(l);
            double cm = mm / 10.0;

            // Ejes de previsualización
            Brush axisBrush = new SolidColorBrush(Color.FromArgb(200, Color.R, Color.G, Color.B));
            Pen axisPen = new Pen(axisBrush, Math.Max(1.8, Size));

            // Eje X
            dc.DrawLine(axisPen, new Point(c.X - l, c.Y), new Point(c.X + l, c.Y));
            VectorService.DrawArrowHead(dc, c, new Point(c.X + l, c.Y), axisBrush, Size);

            // Eje Y
            dc.DrawLine(axisPen, new Point(c.X, c.Y + l), new Point(c.X, c.Y - l));
            VectorService.DrawArrowHead(dc, c, new Point(c.X, c.Y - l), axisBrush, Size);

            // Marcas y números cada 50 px (1 cm = 10 mm)
            Pen tickPen = new Pen(new SolidColorBrush(Color.FromArgb(160, Color.R, Color.G, Color.B)), 1.2);
            double tickStep = MetricUnits.PixelsPerMm * 10.0;
            double tickSize = 6.0;

            Typeface numTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            int cmIdx = 1;

            for (double d = tickStep; d <= l - 15.0; d += tickStep, cmIdx++) {
                // X
                dc.DrawLine(tickPen, new Point(c.X + d, c.Y - tickSize), new Point(c.X + d, c.Y + tickSize));
                dc.DrawLine(tickPen, new Point(c.X - d, c.Y - tickSize), new Point(c.X - d, c.Y + tickSize));

#pragma warning disable 618
                FormattedText ftNumXPos = new FormattedText(cmIdx.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, numTypeface, 8.5, Brushes.White);
                FormattedText ftNumXNeg = new FormattedText("-" + cmIdx.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, numTypeface, 8.5, Brushes.White);
#pragma warning restore 618
                dc.DrawText(ftNumXPos, new Point(c.X + d - 3.0, c.Y + tickSize + 2.0));
                dc.DrawText(ftNumXNeg, new Point(c.X - d - 6.0, c.Y + tickSize + 2.0));

                // Y
                dc.DrawLine(tickPen, new Point(c.X - tickSize, c.Y - d), new Point(c.X + tickSize, c.Y - d));
                dc.DrawLine(tickPen, new Point(c.X - tickSize, c.Y + d), new Point(c.X + tickSize, c.Y + d));

#pragma warning disable 618
                FormattedText ftNumYPos = new FormattedText(cmIdx.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, numTypeface, 8.5, Brushes.White);
                FormattedText ftNumYNeg = new FormattedText("-" + cmIdx.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, numTypeface, 8.5, Brushes.White);
#pragma warning restore 618
                dc.DrawText(ftNumYPos, new Point(c.X - tickSize - 12.0, c.Y - d - 6.0));
                dc.DrawText(ftNumYNeg, new Point(c.X - tickSize - 16.0, c.Y + d - 6.0));
            }

            // Diana brillante en el Origen (0, 0)
            Pen originRingPen = new Pen(Brushes.Gold, 2.0);
            Brush originFill = new SolidColorBrush(Color.FromArgb(180, 255, 215, 0));
            dc.DrawEllipse(originFill, originRingPen, c, 5.0, 5.0);
            dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 215, 0)), 1.5), c, 12.0, 12.0);

            // Letras X, Y y (0,0)
            Typeface labelTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
#pragma warning disable 618
            FormattedText ftX = new FormattedText("X", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, labelTypeface, 13.0, axisBrush);
            FormattedText ftY = new FormattedText("Y", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, labelTypeface, 13.0, axisBrush);
            FormattedText ft0 = new FormattedText("(0,0)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, labelTypeface, 10.5, Brushes.Gold);
#pragma warning restore 618

            dc.DrawText(ftX, new Point(c.X + l + 12.0, c.Y - 9.0));
            dc.DrawText(ftY, new Point(c.X + 8.0, c.Y - l - 18.0));
            dc.DrawText(ft0, new Point(c.X - 32.0, c.Y + 8.0));

            // Cartel HUD flotante con instrucciones claras de incremento
            string hudText = string.Format("🌐 Ejes Coordenados | Semieje: {0:0} mm ({1:0.0} cm) | 🖱️ Arrastra o rueda para incrementar | ⌨️ [+] / [-]", mm, cm);
#pragma warning disable 618
            FormattedText ftHud = new FormattedText(
                hudText,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                labelTypeface,
                10.0,
                Brushes.White
            );
#pragma warning restore 618

            Rect hudRect = new Rect(c.X - (ftHud.Width / 2.0) - 8.0, c.Y + l + 16.0, ftHud.Width + 16.0, ftHud.Height + 6.0);
            Brush hudBg = new SolidColorBrush(Color.FromArgb(230, 20, 24, 30));
            Pen hudBorder = new Pen(new SolidColorBrush(Color.FromArgb(180, 0, 162, 255)), 1.0);

            dc.DrawRoundedRectangle(hudBg, hudBorder, hudRect, 4.0, 4.0);
            dc.DrawText(ftHud, new Point(c.X - (ftHud.Width / 2.0), c.Y + l + 19.0));
        }
    }
}
