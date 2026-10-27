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
    /// Herramienta interactiva de Compás de Precisión ((🧭).
    /// Muestra un compás técnico con aguja central, bisagra y brazo de trazado.
    /// Permite:
    /// - Abrir el radio paso a paso de a 1 mm con flechas (🧭).
    /// - Ingresar el radio exacto en mm mediante teclado y Enter.
    /// - Regular el arco con la rueda del ratón (desde arcos parciales hasta 360°).
    /// - Imantar el centro de la aguja a extremos de líneas existentes.
    /// </summary>
    public class CompassTool : ITool {
        public string Id { get { return "geom_compass"; } }
        public string DisplayName { get { return "Compás"; } }
        public string Icon { get { return "🧭"; } }

        private IAnnotationContext _context;
        private CompassOverlayVisual _visual;

        private MouseButtonEventHandler _mouseDownHandler;
        private MouseEventHandler _mouseMoveHandler;
        private MouseButtonEventHandler _mouseUpHandler;
        private MouseWheelEventHandler _mouseWheelHandler;
        private KeyEventHandler _keyDownHandler;

        private Point _center = new Point(250, 250);
        private double _radius = 100.0; // 20 mm iniciales
        private double _arcSpan = 360.0; // Apertura del arco en grados (15° a 360°)
        private bool _isDrawing = false;
        private readonly MetricInputController _metric = new MetricInputController(20.0);

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
                _visual = new CompassOverlayVisual(this);
            }

            if (!_context.Canvas.Children.Contains(_visual)) {
                _context.Canvas.Children.Add(_visual);
            }

            _visual.Visibility = Visibility.Visible;
            _visual.Color = _context.CurrentColor;
            _visual.Size = _context.CurrentSize;
            _visual.Center = _center;
            _radius = _metric.ValuePixels;
            _visual.Radius = _radius;
            _visual.ArcSpan = _arcSpan;
            _visual.IsDrawing = false;

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
            // 1. Con Ctrl: regular radio de apertura en mm
            if (_metric.ProcessMouseWheel(e.Delta, Keyboard.Modifiers)) {
                _radius = _metric.ValuePixels;
                if (_visual != null) {
                    _visual.Radius = _radius;
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            // 2. Sin Ctrl: regular la amplitud del arco a trazar
            double step = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 45.0 : 15.0;
            double dir = Math.Sign(e.Delta);

            _arcSpan += dir * step;
            if (_arcSpan < 15.0) _arcSpan = 15.0;
            if (_arcSpan > 360.0) _arcSpan = 360.0;

            if (_visual != null) {
                _visual.ArcSpan = _arcSpan;
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
                _center = snap.IsSnapped ? snap.Point : clickPos;
                _isDrawing = true;
                _radius = _metric.ValuePixels;

                if (_visual != null) {
                    _visual.Center = _center;
                    _visual.IsDrawing = true;
                    _visual.IsSnapped = false;
                    _visual.Radius = _radius;
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
                double dx = mousePos.X - _center.X;
                double dy = mousePos.Y - _center.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                _metric.UpdateFromMouse(dist, (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt);
                _radius = _metric.ValuePixels;

                _visual.Radius = _radius;
                _visual.IsSnapped = false;
            } else {
                SnapResult snap = SnapService.FindSnapPoint(_context.Canvas.Strokes, mousePos);
                if (snap.IsSnapped) {
                    _center = snap.Point;
                    _visual.IsSnapped = true;
                    _visual.SnapPoint = snap.Point;
                } else {
                    _center = mousePos;
                    _visual.IsSnapped = false;
                }

                _visual.Center = _center;
                _radius = _metric.ValuePixels;
                _visual.Radius = _radius;
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

            CommitCompassStroke();
            e.Handled = true;
        }

        private void OnCanvasKeyDown(object sender, KeyEventArgs e) {
            bool committed;
            if (_metric.ProcessKeyDown(e.Key, Keyboard.Modifiers, out committed)) {
                _radius = _metric.ValuePixels;
                if (committed) {
                    CommitCompassStroke();
                    _isDrawing = false;
                }

                if (_visual != null) {
                    _visual.Radius = _radius;
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

        private void CommitCompassStroke() {
            if (_context == null || _radius < 5.0) return;

            StylusPointCollection points = new StylusPointCollection();
            int steps = Math.Max(24, (int)(_arcSpan / 4.0));

            for (int i = 0; i <= steps; i++) {
                double deg = (_arcSpan * i) / steps;
                double rad = deg * Math.PI / 180.0;

                double px = _center.X + _radius * Math.Cos(rad);
                double py = _center.Y - _radius * Math.Sin(rad);
                points.Add(new StylusPoint(px, py));
            }

            if (_arcSpan >= 359.0 && points.Count > 0) {
                points.Add(points[0]);
            }

            double thickness = Math.Max(1.5, _context.CurrentSize);
            DrawingAttributes attrs = new DrawingAttributes {
                Color = _context.CurrentColor,
                Width = thickness,
                Height = thickness,
                FitToCurve = true,
                IsHighlighter = false
            };

            Stroke compassStroke = new Stroke(points, attrs);
            _context.AddStroke(compassStroke);
        }
    }

    /// <summary>
    /// Elemento visual que dibuja el compás técnico en pantalla.
    /// </summary>
    public class CompassOverlayVisual : FrameworkElement {
        private readonly CompassTool _tool;

        public Point Center { get; set; }
        public double Radius { get; set; }
        public double ArcSpan { get; set; }
        public bool IsDrawing { get; set; }
        public bool IsSnapped { get; set; }
        public Point SnapPoint { get; set; }
        public Color Color { get; set; }
        public double Size { get; set; }

        public CompassOverlayVisual(CompassTool tool) {
            _tool = tool;
            IsHitTestVisible = false;
            Center = new Point(250, 250);
            Radius = 100.0;
            ArcSpan = 360.0;
            Color = Colors.Red;
            Size = 3.0;
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);

            Point c = Center;
            double r = Radius;

            // 1. Previsualización del arco / circunferencia trazada
            PathGeometry arcGeom = new PathGeometry();
            PathFigure arcFig = new PathFigure { StartPoint = new Point(c.X + r, c.Y), IsClosed = (ArcSpan >= 359.0) };

            int steps = Math.Max(20, (int)(ArcSpan / 5.0));
            for (int i = 1; i <= steps; i++) {
                double deg = (ArcSpan * i) / steps;
                double rad = deg * Math.PI / 180.0;
                arcFig.Segments.Add(new LineSegment(new Point(c.X + r * Math.Cos(rad), c.Y - r * Math.Sin(rad)), true));
            }
            arcGeom.Figures.Add(arcFig);

            Pen arcPen = new Pen(new SolidColorBrush(Color), Math.Max(2.0, Size * 0.7));
            dc.DrawGeometry(null, arcPen, arcGeom);

            // 2. Línea de radio indicadora
            Pen radiusPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 0, 162, 255)), 1.2);
            radiusPen.DashStyle = new DashStyle(new double[] { 4, 3 }, 0);
            dc.DrawLine(radiusPen, c, new Point(c.X + r, c.Y));

            // 3. Dibujo esquemático del compás mecánico (Aguja, Bisagra, Brazo y Mina)
            double hingeY = c.Y - Math.Max(70.0, r * 0.85);
            Point hinge = new Point(c.X + (r / 2.0), hingeY);
            Point needleTip = c;
            Point pencilTip = new Point(c.X + r, c.Y);

            Pen legPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 200, 205, 215)), 3.5);
            Pen needleLegPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 0, 162, 255)), 3.5);

            dc.DrawLine(needleLegPen, hinge, needleTip);
            dc.DrawLine(legPen, hinge, pencilTip);

            dc.DrawEllipse(Brushes.White, new Pen(new SolidColorBrush(Color.FromArgb(220, 0, 162, 255)), 2.0), hinge, 6.0, 6.0);
            dc.DrawEllipse(Brushes.DeepSkyBlue, new Pen(Brushes.White, 1.2), needleTip, 4.5, 4.5);
            dc.DrawEllipse(new SolidColorBrush(Color), new Pen(Brushes.White, 1.2), pencilTip, 5.0, 5.0);

            // Marcador visual de captura magnética en el extremo
            if (IsSnapped) {
                SnapService.DrawSnapIndicator(dc, SnapPoint, "Punto Final");
            }

            // 4. Cartel HUD informativo con radio métrico y arco
            string arcText = ArcSpan >= 359.0 ? "360° (Circunferencia)" : string.Format("{0:0}° (Arco)", ArcSpan);
            string mainText = _tool.Metric.GetMainStatusText("Radio [" + arcText + "]");
            string subText = _tool.Metric.GetHelpSubText();

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
            double badgeY = c.Y + r + 18.0;

            if (badgeY + badgeHeight > SystemParameters.VirtualScreenHeight - 40) {
                badgeY = c.Y - r - badgeHeight - 18.0;
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

