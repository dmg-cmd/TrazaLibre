using System;
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
    /// Herramienta interactiva de Escuadra 30° - 60° - 90° o Cartabón Técnico ((📐).
    /// Muestra un cartabón técnico con graduación milimétrica precisa en sus bordes.
    /// Permite:
    /// - Rotación precisa con rueda del ratón (Shift: ±5°).
    /// - Regulación de longitud milímetro a milímetro con flechas (📐).
    /// - Ingreso numérico directo de la longitud tecleando cifras y pulsando Enter.
    /// - Trazado guiado exacto a 30°, 60°, 90° y 0° sobre los bordes del instrumento.
    /// </summary>
    public class Triangle30Tool : ITool {
        public string Id { get { return "geom_triangle30"; } }
        public string DisplayName { get { return "Escuadra 30°"; } }
        public string Icon { get { return "📐"; } }

        private IAnnotationContext _context;
        private Triangle30OverlayVisual _visual;

        private MouseButtonEventHandler _mouseDownHandler;
        private MouseEventHandler _mouseMoveHandler;
        private MouseButtonEventHandler _mouseUpHandler;
        private MouseWheelEventHandler _mouseWheelHandler;
        private KeyEventHandler _keyDownHandler;

        private double _baseAngle = 0.0;
        private double _chosenAngle = 30.0; // Ángulo fijado (30°, 60°, 90° o 0°)
        private Point _vertex = new Point(250, 250);
        private bool _isDrawing = false;
        private double _lineLength = 200.0; // 40 mm iniciales
        private readonly MetricInputController _metric = new MetricInputController(40.0);

        public MetricInputController Metric {
            get { return _metric; }
        }

        public void Activate(IAnnotationContext context) {
            _context = context;
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.None;
            context.Canvas.Cursor = Cursors.Cross;
            context.Canvas.Focusable = true;
            context.Canvas.Focus();

            if (_visual == null) {
                _visual = new Triangle30OverlayVisual(this);
            }

            if (!_context.Canvas.Children.Contains(_visual)) {
                _context.Canvas.Children.Add(_visual);
            }

            _visual.Visibility = Visibility.Visible;
            _visual.Color = _context.CurrentColor;
            _visual.Size = _context.CurrentSize;
            _visual.BaseAngle = _baseAngle;
            _visual.EffectiveAngle = _baseAngle + 30.0;
            _visual.SnappedRelAngle = 30.0;
            _visual.Vertex = _vertex;
            _visual.IsDrawing = false;
            _lineLength = _metric.ValuePixels;
            _visual.LineLength = _lineLength;

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

            if (_visual != null && context != null && context.Canvas != null) {
                if (context.Canvas.Children.Contains(_visual)) {
                    context.Canvas.Children.Remove(_visual);
                }
            }

            _isDrawing = false;
            _metric.ClearInput();
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

        private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e) {
            // 1. Regular longitud en mm con Ctrl
            if (_metric.ProcessMouseWheel(e.Delta, Keyboard.Modifiers)) {
                _lineLength = _metric.ValuePixels;
                if (_visual != null) {
                    _visual.LineLength = _lineLength;
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            // 2. Si no hay Ctrl, regular la rotación angular del cartabón
            double step = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 5.0 : 1.0;
            double dir = Math.Sign(e.Delta);

            _baseAngle += dir * step;
            _baseAngle = (_baseAngle % 360.0);
            if (_baseAngle < 0) _baseAngle += 360.0;

            if (_visual != null) {
                _visual.BaseAngle = _baseAngle;
                _visual.EffectiveAngle = _baseAngle + _visual.SnappedRelAngle;
                _visual.InvalidateVisual();
            }

            e.Handled = true;
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (_context == null) return;
            _context.Canvas.Focus();

            if (e.RightButton == MouseButtonState.Pressed) {
                _isDrawing = false;
                _metric.ClearInput();
                if (_visual != null) {
                    _visual.IsDrawing = false;
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed) {
                Point clickPos = e.GetPosition(_context.Canvas);
                SnapResult snap = SnapService.FindSnapPoint(_context.Canvas.Strokes, clickPos);
                _vertex = snap.IsSnapped ? snap.Point : clickPos;
                _isDrawing = true;
                _lineLength = _metric.ValuePixels;
                _chosenAngle = _baseAngle + 30.0; // Predeterminado: 30°

                if (_visual != null) {
                    _visual.Vertex = _vertex;
                    _visual.IsDrawing = true;
                    _visual.IsSnapped = false;
                    _visual.LineLength = _lineLength;
                    _visual.EffectiveAngle = _chosenAngle;
                    _visual.SnappedRelAngle = 30.0;
                    _visual.InvalidateVisual();
                }

                _context.Canvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e) {
            if (_context == null || _visual == null) return;

            Point mousePos = e.GetPosition(_context.Canvas);

            if (_isDrawing) {
                double dx = mousePos.X - _vertex.X;
                double dy = _vertex.Y - mousePos.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                _metric.UpdateFromMouse(dist, (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt);
                _lineLength = _metric.ValuePixels;

                double mouseAngle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                if (mouseAngle < 0) mouseAngle += 360.0;

                double relAngle = (mouseAngle - _baseAngle) % 360.0;
                if (relAngle < 0) relAngle += 360.0;

                // Acoplamiento magnético a 30°, 60°, 90° o 0°
                double snappedRel;
                if (relAngle >= 15.0 && relAngle < 45.0) {
                    snappedRel = 30.0; // Borde de 30°
                } else if (relAngle >= 45.0 && relAngle < 75.0) {
                    snappedRel = 60.0; // Borde de 60°
                } else if (relAngle >= 75.0 && relAngle < 120.0) {
                    snappedRel = 90.0; // Cateto vertical a 90°
                } else if (relAngle < 15.0 || relAngle >= 345.0) {
                    snappedRel = 0.0;  // Base horizontal a 0°
                } else {
                    snappedRel = 30.0;
                }

                _chosenAngle = (_baseAngle + snappedRel) % 360.0;
                if (_chosenAngle < 0) _chosenAngle += 360.0;

                _visual.LineLength = _lineLength;
                _visual.EffectiveAngle = _chosenAngle;
                _visual.SnappedRelAngle = snappedRel;
                _visual.IsSnapped = false;
            } else {
                SnapResult snap = SnapService.FindSnapPoint(_context.Canvas.Strokes, mousePos);
                if (snap.IsSnapped) {
                    _vertex = snap.Point;
                    _visual.IsSnapped = true;
                    _visual.SnapPoint = snap.Point;
                } else {
                    _vertex = mousePos;
                    _visual.IsSnapped = false;
                }

                _visual.Vertex = _vertex;
                _lineLength = _metric.ValuePixels;
                _visual.LineLength = _lineLength;
                _visual.EffectiveAngle = _baseAngle + 30.0;
                _visual.SnappedRelAngle = 30.0;
            }

            _visual.InvalidateVisual();
            e.Handled = true;
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e) {
            if (!_isDrawing || _context == null) return;

            _context.Canvas.ReleaseMouseCapture();
            _isDrawing = false;

            if (_visual != null) {
                _visual.IsDrawing = false;
                _visual.InvalidateVisual();
            }

            CommitLineStroke();
            e.Handled = true;
        }

        private void OnCanvasKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) {
                VectorService.ToggleVectorMode();
                if (_visual != null) {
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) {
                VectorService.ToggleDimensionMode();
                if (_visual != null) {
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            bool committed;
            if (_metric.ProcessKeyDown(e.Key, Keyboard.Modifiers, out committed)) {
                _lineLength = _metric.ValuePixels;
                if (committed) {
                    CommitLineStroke();
                    _isDrawing = false;
                }

                if (_visual != null) {
                    _visual.LineLength = _lineLength;
                    _visual.IsDrawing = _isDrawing;
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape) {
                _isDrawing = false;
                _metric.ClearInput();
                if (_visual != null) {
                    _visual.IsDrawing = false;
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
            }
        }

        private void CommitLineStroke() {
            if (_context == null || _lineLength < 5.0) return;

            double rad = _chosenAngle * Math.PI / 180.0;
            double targetX = _vertex.X + _lineLength * Math.Cos(rad);
            double targetY = _vertex.Y - _lineLength * Math.Sin(rad);

            double thickness = Math.Max(1.5, _context.CurrentSize);
            StylusPointCollection points = VectorService.CreateStrokePoints(
                _vertex,
                new Point(targetX, targetY),
                thickness,
                VectorService.IsVectorMode
            );

            DrawingAttributes attrs = new DrawingAttributes {
                Color = _context.CurrentColor,
                Width = thickness,
                Height = thickness,
                FitToCurve = false,
                IsHighlighter = false
            };

            Stroke lineStroke = new Stroke(points, attrs);
            _context.AddStroke(lineStroke);

            if (VectorService.IsVectorMode && VectorService.IsDimensionEnabled) {
                StrokeCollection dimStrokes = VectorService.CreateDimensionStrokes(
                    _vertex,
                    new Point(targetX, targetY),
                    _lineLength,
                    _chosenAngle,
                    _context.CurrentColor,
                    thickness
                );
                if (dimStrokes != null && dimStrokes.Count > 0) {
                    _context.AddStrokes(dimStrokes);
                }
            }
        }
    }

    /// <summary>
    /// Elemento visual que dibuja la Escuadra 30° / 60° (Cartabón) y la línea acoplada.
    /// </summary>
    public class Triangle30OverlayVisual : FrameworkElement {
        private readonly Triangle30Tool _tool;

        public Point Vertex { get; set; }
        public double BaseAngle { get; set; }
        public double EffectiveAngle { get; set; }
        public double SnappedRelAngle { get; set; }
        public double LineLength { get; set; }
        public bool IsDrawing { get; set; }
        public bool IsSnapped { get; set; }
        public Point SnapPoint { get; set; }
        public Color Color { get; set; }
        public double Size { get; set; }

        public Triangle30OverlayVisual(Triangle30Tool tool) {
            _tool = tool;
            IsHitTestVisible = false;
            Vertex = new Point(250, 250);
            BaseAngle = 0.0;
            EffectiveAngle = 30.0;
            SnappedRelAngle = 30.0;
            LineLength = 200.0;
            Color = Colors.Red;
            Size = 3.0;
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);

            Point v = Vertex;
            double L_base = Math.Max(180.0, LineLength);
            // En un triángulo rectángulo 30°-60°-90°:
            // Base horizontal = L_base
            // Cateto vertical perpendicular = L_base * tan(30°) = L_base / sqrt(3)
            // Hipotenusa desde el origen = L_base / cos(30°)
            double H = L_base * Math.Tan(30.0 * Math.PI / 180.0);

            dc.PushTransform(new TranslateTransform(v.X, v.Y));
            dc.PushTransform(new RotateTransform(-BaseAngle));

            // Vértices del Cartabón:
            // Vértice 30° en el origen (0, 0)
            // Vértice 90° en la base (L_base, 0)
            // Vértice 60° superior en (L_base, -H)
            Point pOrigin = new Point(0, 0);
            Point pBase90 = new Point(L_base, 0);
            Point pTop60 = new Point(L_base, -H);

            PathGeometry triangleGeom = new PathGeometry();

            // Silueta exterior
            PathFigure outerFigure = new PathFigure { StartPoint = pOrigin, IsClosed = true };
            outerFigure.Segments.Add(new LineSegment(pBase90, true));
            outerFigure.Segments.Add(new LineSegment(pTop60, true));
            triangleGeom.Figures.Add(outerFigure);

            // Orificio interior triangular proporcional
            double off = 26.0;
            if (L_base > 95.0 && H > 45.0) {
                double inL = L_base - (off * 2.4);
                double inH = inL * Math.Tan(30.0 * Math.PI / 180.0);
                if (inL > 30.0 && inH > 15.0) {
                    Point inOrigin = new Point(off * 1.6, -off * 0.7);
                    Point inBase = new Point(off * 1.6 + inL, -off * 0.7);
                    Point inTop = new Point(off * 1.6 + inL, -(off * 0.7 + inH));

                    PathFigure innerFigure = new PathFigure { StartPoint = inOrigin, IsClosed = true };
                    innerFigure.Segments.Add(new LineSegment(inBase, true));
                    innerFigure.Segments.Add(new LineSegment(inTop, true));
                    triangleGeom.Figures.Add(innerFigure);
                }
            }

            Brush triangleBrush = new SolidColorBrush(Color.FromArgb(195, 20, 25, 32));
            Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(170, 0, 162, 255)), 1.5);
            dc.DrawGeometry(triangleBrush, borderPen, triangleGeom);

            // Marcas graduadas milimétricas en la base horizontal (5 px = 1 mm)
            Pen markPenMajor = new Pen(Brushes.White, 1.2);
            Pen markPenMed = new Pen(new SolidColorBrush(Color.FromArgb(200, 220, 220, 220)), 1.0);
            Pen markPenMinor = new Pen(new SolidColorBrush(Color.FromArgb(120, 180, 180, 180)), 0.8);

            for (double x = 10.0; x < L_base - 5.0; x += MetricUnits.PixelsPerMm) {
                double markH;
                Pen tickPen;
                if (x % (MetricUnits.PixelsPerMm * 10.0) == 0) { // 1 cm
                    markH = 12.0;
                    tickPen = markPenMajor;
                } else if (x % (MetricUnits.PixelsPerMm * 5.0) == 0) { // 5 mm
                    markH = 8.0;
                    tickPen = markPenMed;
                } else {
                    markH = 5.0;
                    tickPen = markPenMinor;
                }
                dc.DrawLine(tickPen, new Point(x, 0), new Point(x, -markH));
            }

            // Marcas graduadas en el cateto vertical
            for (double y = 10.0; y < H - 5.0; y += MetricUnits.PixelsPerMm) {
                double markW;
                Pen tickPen;
                if (y % (MetricUnits.PixelsPerMm * 10.0) == 0) {
                    markW = 12.0;
                    tickPen = markPenMajor;
                } else if (y % (MetricUnits.PixelsPerMm * 5.0) == 0) {
                    markW = 8.0;
                    tickPen = markPenMed;
                } else {
                    markW = 5.0;
                    tickPen = markPenMinor;
                }
                dc.DrawLine(tickPen, new Point(L_base, -y), new Point(L_base - markW, -y));
            }

            // Rótulos de los ángulos del Cartabón
            Typeface font = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
#pragma warning disable 618
            FormattedText ft30 = new FormattedText("30°", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, 11.0, Brushes.Gold);
            FormattedText ft90 = new FormattedText("90°", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, 10.0, Brushes.White);
            FormattedText ft60 = new FormattedText("60°", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, 10.0, Brushes.LightGreen);
#pragma warning restore 618

            dc.DrawText(ft30, new Point(22, -18));
            dc.DrawText(ft90, new Point(L_base - 32, -18));
            dc.DrawText(ft60, new Point(L_base - 32, -H + 8));

            // Resaltar la hipotenusa a 30°
            Pen hipPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 215, 0)), 1.5);
            hipPen.DashStyle = new DashStyle(new double[] { 4, 2 }, 0);
            dc.DrawLine(hipPen, pOrigin, pTop60);

            // Línea trazadora acoplada al ángulo (30°, 60°, 90° o 0°)
            double drawRad = (SnappedRelAngle) * Math.PI / 180.0;
            Point drawEnd = new Point(LineLength * Math.Cos(drawRad), -LineLength * Math.Sin(drawRad));

            Brush needleBrush = new SolidColorBrush(Color);
            Pen needlePen = new Pen(needleBrush, Math.Max(2.5, Size * 0.8));
            dc.DrawLine(needlePen, pOrigin, drawEnd);

            if (VectorService.IsVectorMode) {
                VectorService.DrawArrowHead(dc, pOrigin, drawEnd, needleBrush, Size);
            } else {
                dc.DrawEllipse(Brushes.SkyBlue, new Pen(Brushes.White, 1.2), drawEnd, 4.5, 4.5);
            }

            dc.Pop(); // RotateTransform
            dc.Pop(); // TranslateTransform

            // Cota técnica de módulo y ángulo
            double radAngle = EffectiveAngle * Math.PI / 180.0;
            Point targetPt = new Point(v.X + LineLength * Math.Cos(radAngle), v.Y - LineLength * Math.Sin(radAngle));
            VectorService.DrawDimensionPreview(dc, v, targetPt, LineLength, EffectiveAngle);

            // Marcador visual de captura magnética en el extremo
            if (IsSnapped) {
                SnapService.DrawSnapIndicator(dc, SnapPoint, "Punto Final");
            }

            // Cartel informativo HUD
            string bordeNombre = SnappedRelAngle == 30.0 ? "Hipotenusa 30°" :
                                 (SnappedRelAngle == 60.0 ? "Guía 60°" :
                                 (SnappedRelAngle == 90.0 ? "Cateto 90°" : "Base 0°"));

            string prefix = VectorService.IsVectorMode ? "Vector Cartabón 🏹" : "Cartabón 30°";
            string dimStatus = VectorService.IsDimensionEnabled ? "[D: Cota Sí]" : "[D: Cota No]";
            string mainText = _tool.Metric.GetMainStatusText(prefix + " " + dimStatus + " [" + bordeNombre + "]", EffectiveAngle);
            string subText = _tool.Metric.GetHelpSubText() + (VectorService.IsVectorMode ? "  •  [V] Recta" : "  •  [V] Vector") + "  •  [D] Acotar";

            Typeface tfBold = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            Typeface tfNormal = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            Brush mainTextBrush = _tool.Metric.IsEntering ? Brushes.Gold : Brushes.White;
#pragma warning disable 618
            FormattedText ftMain = new FormattedText(mainText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tfBold, 11.0, mainTextBrush);
            FormattedText ftSub = new FormattedText(subText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tfNormal, 9.5, new SolidColorBrush(Color.FromArgb(220, 200, 230, 255)));
#pragma warning restore 618

            double badgeWidth = Math.Max(ftMain.Width, ftSub.Width) + 24.0;
            double badgeHeight = ftMain.Height + ftSub.Height + 14.0;

            double badgeX = v.X - badgeWidth / 2.0;
            double badgeY = v.Y + 20.0;

            if (badgeY + badgeHeight > SystemParameters.VirtualScreenHeight - 40) {
                badgeY = v.Y - badgeHeight - 20.0;
            }

            Rect badgeRect = new Rect(badgeX, badgeY, badgeWidth, badgeHeight);
            Brush badgeBg = new SolidColorBrush(Color.FromArgb(235, 20, 24, 30));
            Pen badgeBorder = new Pen(new SolidColorBrush(_tool.Metric.IsEntering ? Colors.Gold : Color.FromArgb(140, 0, 162, 255)), _tool.Metric.IsEntering ? 1.8 : 1.0);

            dc.DrawRoundedRectangle(badgeBg, badgeBorder, badgeRect, 6.0, 6.0);
            dc.DrawText(ftMain, new Point(badgeX + 12.0, badgeY + 6.0));
            dc.DrawText(ftSub, new Point(badgeX + 12.0, badgeY + 6.0 + ftMain.Height + 2.0));
        }
    }
}

