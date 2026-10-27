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
    /// Herramienta interactiva de Transportador 360° (ProtractorTool) ((📐).
    /// Permite:
    /// - Visualizar un transportador de 360° en pantalla.
    /// - Regular el ángulo grado por grado con la rueda del ratón (Shift: pasos de 5°).
    /// - Regular el largo de la línea directriz en milímetros con flechas (📐).
    /// - Ingresar la longitud exacta tecleando cifras y pulsando Enter.
    /// - Imantación al extremo de líneas dibujadas previamente.
    /// </summary>
    public class ProtractorTool : ITool {
        public string Id { get { return "geom_protractor"; } }
        public string DisplayName { get { return "Transportador"; } }
        public string Icon { get { return "📐"; } }

        private IAnnotationContext _context;
        private ProtractorOverlayVisual _visual;

        private MouseButtonEventHandler _mouseDownHandler;
        private MouseEventHandler _mouseMoveHandler;
        private MouseButtonEventHandler _mouseUpHandler;
        private MouseWheelEventHandler _mouseWheelHandler;
        private KeyEventHandler _keyDownHandler;

        private double _currentAngle = 45.0; // Ángulo actual en grados [0, 360)
        private Point _centerPoint = new Point(200, 200);
        private bool _isPlaced = false;
        private double _lineLength = 150.0; // 30 mm iniciales
        private readonly MetricInputController _metric = new MetricInputController(30.0);

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
                _visual = new ProtractorOverlayVisual(this);
            }

            if (!_context.Canvas.Children.Contains(_visual)) {
                _context.Canvas.Children.Add(_visual);
            }

            _visual.Visibility = Visibility.Visible;
            _visual.Color = _context.CurrentColor;
            _visual.Size = _context.CurrentSize;
            _visual.Angle = _currentAngle;
            _visual.Center = _centerPoint;
            _visual.IsPlaced = false;
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

            _isPlaced = false;
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
            // 1. Con Ctrl: regular longitud en milímetros
            if (_metric.ProcessMouseWheel(e.Delta, Keyboard.Modifiers)) {
                _lineLength = _metric.ValuePixels;
                if (_visual != null) {
                    _visual.LineLength = _lineLength;
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            // 2. Sin Ctrl: regular ángulo grado a grado
            double step = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 5.0 : 1.0;
            double direction = Math.Sign(e.Delta);

            _currentAngle += direction * step;
            _currentAngle = (_currentAngle % 360.0);
            if (_currentAngle < 0) _currentAngle += 360.0;

            if (_visual != null) {
                _visual.Angle = _currentAngle;
                _visual.InvalidateVisual();
            }

            e.Handled = true;
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (_context == null) return;
            _context.Canvas.Focus();

            if (e.RightButton == MouseButtonState.Pressed) {
                _isPlaced = false;
                _metric.ClearInput();
                if (_visual != null) {
                    _visual.IsPlaced = false;
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed) {
                Point clickPos = e.GetPosition(_context.Canvas);
                SnapResult snap = SnapService.FindSnapPoint(_context.Canvas.Strokes, clickPos);
                _centerPoint = snap.IsSnapped ? snap.Point : clickPos;
                _isPlaced = true;
                _lineLength = _metric.ValuePixels;

                if (_visual != null) {
                    _visual.Center = _centerPoint;
                    _visual.IsPlaced = true;
                    _visual.IsSnapped = false;
                    _visual.LineLength = _lineLength;
                    _visual.InvalidateVisual();
                }

                _context.Canvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e) {
            if (_context == null || _visual == null) return;

            Point mousePos = e.GetPosition(_context.Canvas);

            if (_isPlaced) {
                double dx = mousePos.X - _centerPoint.X;
                double dy = _centerPoint.Y - mousePos.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                _metric.UpdateFromMouse(dist, (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt);
                _lineLength = _metric.ValuePixels;

                _visual.LineLength = _lineLength;
                _visual.IsSnapped = false;
            } else {
                SnapResult snap = SnapService.FindSnapPoint(_context.Canvas.Strokes, mousePos);
                if (snap.IsSnapped) {
                    _centerPoint = snap.Point;
                    _visual.IsSnapped = true;
                    _visual.SnapPoint = snap.Point;
                } else {
                    _centerPoint = mousePos;
                    _visual.IsSnapped = false;
                }

                _visual.Center = _centerPoint;
                _lineLength = _metric.ValuePixels;
                _visual.LineLength = _lineLength;
            }

            _visual.InvalidateVisual();
            e.Handled = true;
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e) {
            if (!_isPlaced || _context == null) return;

            _context.Canvas.ReleaseMouseCapture();
            _isPlaced = false;

            if (_visual != null) {
                _visual.IsPlaced = false;
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
                    _isPlaced = false;
                }

                if (_visual != null) {
                    _visual.LineLength = _lineLength;
                    _visual.IsPlaced = _isPlaced;
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape) {
                _isPlaced = false;
                _metric.ClearInput();
                if (_visual != null) {
                    _visual.IsPlaced = false;
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
            }
        }

        private void CommitLineStroke() {
            if (_context == null || _lineLength < 5.0) return;

            double rad = _currentAngle * Math.PI / 180.0;
            double targetX = _centerPoint.X + _lineLength * Math.Cos(rad);
            double targetY = _centerPoint.Y - _lineLength * Math.Sin(rad);

            double thickness = Math.Max(1.5, _context.CurrentSize);
            StylusPointCollection points = VectorService.CreateStrokePoints(
                _centerPoint,
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
                    _centerPoint,
                    new Point(targetX, targetY),
                    _lineLength,
                    _currentAngle,
                    _context.CurrentColor,
                    thickness
                );
                if (dimStrokes != null && dimStrokes.Count > 0) {
                    _context.AddStrokes(dimStrokes);
                }
            }
        }

        public double CurrentAngle { get { return _currentAngle; } }
        public Point CenterPoint { get { return _centerPoint; } }
        public bool IsPlaced { get { return _isPlaced; } }
        public double LineLength { get { return _lineLength; } }
    }

    /// <summary>
    /// Elemento visual que dibuja el transportador de 360° en pantalla, con marcas graduadas,
    /// números de grados, aguja indicadora y cartel con el ángulo actual.
    /// </summary>
    public class ProtractorOverlayVisual : FrameworkElement {
        private readonly ProtractorTool _tool;

        public Point Center { get; set; }
        public double Angle { get; set; }
        public double LineLength { get; set; }
        public bool IsPlaced { get; set; }
        public bool IsSnapped { get; set; }
        public Point SnapPoint { get; set; }
        public Color Color { get; set; }
        public double Size { get; set; }

        public ProtractorOverlayVisual(ProtractorTool tool) {
            _tool = tool;
            IsHitTestVisible = false;
            Center = new Point(200, 200);
            Angle = 45.0;
            LineLength = 150.0;
            Color = Colors.Red;
            Size = 3.0;
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);

            Point c = Center;
            double radius = 130.0;

            // 1. Fondo circular del transportador (oscuro semitransparente)
            Brush diskBrush = new SolidColorBrush(Color.FromArgb(195, 20, 24, 30));
            Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 0, 162, 255)), 1.5);
            dc.DrawEllipse(diskBrush, borderPen, c, radius, radius);

            // 2. Círculo concéntrico interior
            Pen innerPen = new Pen(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), 1.0);
            dc.DrawEllipse(null, innerPen, c, 40.0, 40.0);

            // 3. Ejes principales (0°-180° horizontal y 90°-270° vertical)
            Pen axisPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 0, 162, 255)), 1.0);
            dc.DrawLine(axisPen, new Point(c.X - radius, c.Y), new Point(c.X + radius, c.Y));
            dc.DrawLine(axisPen, new Point(c.X, c.Y - radius), new Point(c.X, c.Y + radius));

            // 4. Graduaciones de grados cada 5°, 10° y 30°
            Pen tickPenMajor = new Pen(Brushes.White, 1.2);
            Pen tickPenMed = new Pen(new SolidColorBrush(Color.FromArgb(180, 220, 220, 220)), 1.0);
            Pen tickPenMinor = new Pen(new SolidColorBrush(Color.FromArgb(110, 180, 180, 180)), 0.8);

            Typeface numTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

            for (int deg = 0; deg < 360; deg += 5) {
                double rad = deg * Math.PI / 180.0;
                double cos = Math.Cos(rad);
                double sin = Math.Sin(rad);

                double rIn;
                Pen currentPen;

                if (deg % 30 == 0) {
                    rIn = radius - 14.0;
                    currentPen = tickPenMajor;

                    // Dibujar número del grado
                    string degText = deg.ToString() + "°";
#pragma warning disable 618
                    FormattedText ft = new FormattedText(
                        degText,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        numTypeface,
                        9.0,
                        Brushes.White
                    );
#pragma warning restore 618

                    double textR = radius - 26.0;
                    Point textCenter = new Point(c.X + textR * cos - (ft.Width / 2.0), c.Y - textR * sin - (ft.Height / 2.0));
                    dc.DrawText(ft, textCenter);
                } else if (deg % 10 == 0) {
                    rIn = radius - 9.0;
                    currentPen = tickPenMed;
                } else {
                    rIn = radius - 5.0;
                    currentPen = tickPenMinor;
                }

                Point pOut = new Point(c.X + radius * cos, c.Y - radius * sin);
                Point pIn = new Point(c.X + rIn * cos, c.Y - rIn * sin);
                dc.DrawLine(currentPen, pIn, pOut);
            }

            // 5. Arco del ángulo seleccionado
            DrawAngleArc(dc, c, 55.0, Angle);

            // 6. Línea / Aguja orientada al ángulo exacto con la longitud métrica
            double needleRad = Angle * Math.PI / 180.0;
            double effLength = Math.Max(25.0, LineLength);
            Point needleEnd = new Point(c.X + effLength * Math.Cos(needleRad), c.Y - effLength * Math.Sin(needleRad));

            Brush needleBrush = new SolidColorBrush(Color);
            Pen needlePen = new Pen(needleBrush, Math.Max(2.5, Size * 0.8));
            dc.DrawLine(needlePen, c, needleEnd);

            // Flecha / punta en el extremo de la línea
            if (VectorService.IsVectorMode) {
                VectorService.DrawArrowHead(dc, c, needleEnd, needleBrush, Size);
            } else {
                dc.DrawEllipse(Brushes.SkyBlue, new Pen(Brushes.White, 1.2), needleEnd, 4.5, 4.5);
            }

            // 7. Cota técnica de módulo y ángulo
            VectorService.DrawDimensionPreview(dc, c, needleEnd, effLength, Angle);

            // 8. Diana / punto central
            dc.DrawEllipse(Brushes.SkyBlue, new Pen(Brushes.White, 1.0), c, 4.5, 4.5);

            // 9. Marcador visual de captura magnética en el extremo
            if (IsSnapped) {
                SnapService.DrawSnapIndicator(dc, SnapPoint, "Punto Final");
            }

            // 10. Cartel flotante HUD con información del ángulo y control métrico
            DrawInfoBadge(dc, c, radius, Angle, effLength);
        }

        private void DrawAngleArc(DrawingContext dc, Point c, double arcRadius, double angleDeg) {
            if (angleDeg <= 0.5) return;

            PathGeometry geom = new PathGeometry();
            PathFigure fig = new PathFigure { StartPoint = new Point(c.X + arcRadius, c.Y), IsClosed = false };

            int segments = Math.Max(4, (int)(angleDeg / 5.0));
            for (int i = 1; i <= segments; i++) {
                double a = (angleDeg * i / segments) * Math.PI / 180.0;
                fig.Segments.Add(new LineSegment(new Point(c.X + arcRadius * Math.Cos(a), c.Y - arcRadius * Math.Sin(a)), true));
            }
            geom.Figures.Add(fig);

            Pen arcPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 0, 162, 255)), 2.0);
            dc.DrawGeometry(null, arcPen, geom);
        }

        private void DrawArrowHead(DrawingContext dc, Point start, Point end, Brush brush) {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double angle = Math.Atan2(dy, dx);
            double arrowSize = 10.0;

            Point p1 = new Point(
                end.X - arrowSize * Math.Cos(angle - Math.PI / 6),
                end.Y - arrowSize * Math.Sin(angle - Math.PI / 6)
            );
            Point p2 = new Point(
                end.X - arrowSize * Math.Cos(angle + Math.PI / 6),
                end.Y - arrowSize * Math.Sin(angle + Math.PI / 6)
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

        private void DrawInfoBadge(DrawingContext dc, Point c, double radius, double angle, double length) {
            string prefix = VectorService.IsVectorMode ? "Vector 🏹" : "Línea";
            string dimStatus = VectorService.IsDimensionEnabled ? "[D: Cota Sí]" : "[D: Cota No]";
            string mainText = _tool.Metric.GetMainStatusText(prefix + " " + dimStatus, angle);
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

            double badgeX = c.X - badgeWidth / 2.0;
            double badgeY = c.Y + radius + 14.0;

            if (badgeY + badgeHeight > SystemParameters.VirtualScreenHeight - 40) {
                badgeY = c.Y - radius - badgeHeight - 14.0;
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

