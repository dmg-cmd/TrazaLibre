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
    /// Herramienta interactiva de Regla Graduada de Precisión ((📏).
    /// Muestra una regla milimetrada en pantalla con marcas métricas reales (mm y cm).
    /// Permite:
    /// - Rotación precisa con la rueda del ratón (Shift: ±5°).
    /// - Ajuste de longitud de a 1 milímetro con flechas (📏).
    /// - Ingreso numérico directo de la longitud tecleando cifras y pulsando Enter.
    /// - Captura magnética a extremos de líneas previas.
    /// </summary>
    public class RulerTool : ITool {
        public string Id { get { return "geom_ruler"; } }
        public string DisplayName { get { return "Regla"; } }
        public string Icon { get { return "📏"; } }

        private IAnnotationContext _context;
        private RulerOverlayVisual _visual;

        private MouseButtonEventHandler _mouseDownHandler;
        private MouseEventHandler _mouseMoveHandler;
        private MouseButtonEventHandler _mouseUpHandler;
        private MouseWheelEventHandler _mouseWheelHandler;
        private KeyEventHandler _keyDownHandler;

        private double _angle = 0.0; // Ángulo de rotación de la regla en grados
        private Point _origin = new Point(250, 250);
        private bool _isDrawing = false;
        private double _drawLength = 250.0; // 50 mm (5.0 cm)
        private readonly MetricInputController _metric = new MetricInputController(50.0);

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
                _visual = new RulerOverlayVisual(this);
            }

            if (!_context.Canvas.Children.Contains(_visual)) {
                _context.Canvas.Children.Add(_visual);
            }

            _visual.Visibility = Visibility.Visible;
            _visual.Color = _context.CurrentColor;
            _visual.Size = _context.CurrentSize;
            _visual.Angle = _angle;
            _visual.Origin = _origin;
            _visual.IsDrawing = false;
            _drawLength = _metric.ValuePixels;
            _visual.DrawLength = _drawLength;

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
            // 1. Si se presiona Ctrl, regular la longitud milímetro a milímetro
            if (_metric.ProcessMouseWheel(e.Delta, Keyboard.Modifiers)) {
                _drawLength = _metric.ValuePixels;
                if (_visual != null) {
                    _visual.DrawLength = _drawLength;
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            // 2. Si no hay Ctrl, regular la rotación angular de la regla (Shift: pasos de 5°)
            double step = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 5.0 : 1.0;
            double dir = Math.Sign(e.Delta);

            _angle += dir * step;
            _angle = (_angle % 360.0);
            if (_angle < 0) _angle += 360.0;

            if (_visual != null) {
                _visual.Angle = _angle;
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
                _origin = snap.IsSnapped ? snap.Point : clickPos;
                _isDrawing = true;
                _drawLength = _metric.ValuePixels;

                if (_visual != null) {
                    _visual.Origin = _origin;
                    _visual.IsDrawing = true;
                    _visual.IsSnapped = false;
                    _visual.DrawLength = _drawLength;
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
                // Proyectar la distancia del ratón a lo largo del vector director de la regla
                double rad = _angle * Math.PI / 180.0;
                double cos = Math.Cos(rad);
                double sin = -Math.Sin(rad);

                double dx = mousePos.X - _origin.X;
                double dy = mousePos.Y - _origin.Y;

                double projected = (dx * cos) + (dy * sin);
                _metric.UpdateFromMouse(projected, (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt);
                _drawLength = _metric.ValuePixels;

                _visual.DrawLength = _drawLength;
                _visual.IsSnapped = false;
            } else {
                // Imantación a extremos de líneas existentes para encadenar
                SnapResult snap = SnapService.FindSnapPoint(_context.Canvas.Strokes, mousePos);
                if (snap.IsSnapped) {
                    _origin = snap.Point;
                    _visual.IsSnapped = true;
                    _visual.SnapPoint = snap.Point;
                } else {
                    _origin = mousePos;
                    _visual.IsSnapped = false;
                }
                _visual.Origin = _origin;
                _drawLength = _metric.ValuePixels;
                _visual.DrawLength = _drawLength;
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
                _drawLength = _metric.ValuePixels;
                if (committed) {
                    // Si el usuario pulsa Enter, genera el trazo con la medida exacta
                    CommitLineStroke();
                    _isDrawing = false;
                }

                if (_visual != null) {
                    _visual.DrawLength = _drawLength;
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
            if (_context == null || _drawLength < 5.0) return;

            double rad = _angle * Math.PI / 180.0;
            double targetX = _origin.X + _drawLength * Math.Cos(rad);
            double targetY = _origin.Y - _drawLength * Math.Sin(rad);
            Point targetPoint = new Point(targetX, targetY);

            double thickness = Math.Max(1.5, _context.CurrentSize);
            StylusPointCollection points = VectorService.CreateStrokePoints(
                _origin,
                targetPoint,
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

            // Si está en modo vector y la acotación está activa, estampar cota técnica de módulo y ángulo
            if (VectorService.IsVectorMode && VectorService.IsDimensionEnabled) {
                StrokeCollection dimStrokes = VectorService.CreateDimensionStrokes(
                    _origin,
                    targetPoint,
                    _drawLength,
                    _angle,
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
    /// Elemento visual que dibuja la regla graduada y la línea proyectada.
    /// </summary>
    public class RulerOverlayVisual : FrameworkElement {
        private readonly RulerTool _tool;

        public Point Origin { get; set; }
        public double Angle { get; set; }
        public double DrawLength { get; set; }
        public bool IsDrawing { get; set; }
        public bool IsSnapped { get; set; }
        public Point SnapPoint { get; set; }
        public Color Color { get; set; }
        public double Size { get; set; }

        public RulerOverlayVisual(RulerTool tool) {
            _tool = tool;
            IsHitTestVisible = false;
            Origin = new Point(250, 250);
            Angle = 0.0;
            DrawLength = 250.0;
            Color = Colors.Red;
            Size = 3.0;
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);

            Point orig = Origin;
            double rulerLen = Math.Max(320.0, DrawLength + 60.0);
            double rulerHeight = 56.0;

            dc.PushTransform(new TranslateTransform(orig.X, orig.Y));
            dc.PushTransform(new RotateTransform(-Angle));

            // 1. Cuerpo de la regla (fondo semitransparente con borde cian técnico)
            Brush rulerBrush = new SolidColorBrush(Color.FromArgb(210, 20, 24, 30));
            Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(170, 0, 162, 255)), 1.5);
            Rect rulerRect = new Rect(0, 0, rulerLen, rulerHeight);
            dc.DrawRoundedRectangle(rulerBrush, borderPen, rulerRect, 4.0, 4.0);

            // 2. Marcas graduadas en el borde superior (escala milimétrica real: 5 px = 1 mm)
            Pen markPenMajor = new Pen(Brushes.White, 1.2);
            Pen markPenMed = new Pen(new SolidColorBrush(Color.FromArgb(200, 220, 220, 220)), 1.0);
            Pen markPenMinor = new Pen(new SolidColorBrush(Color.FromArgb(120, 180, 180, 180)), 0.8);

            Typeface numTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

            for (double x = 0; x <= rulerLen - 10; x += MetricUnits.PixelsPerMm) {
                double markH;
                Pen currentPen;

                if (x % (MetricUnits.PixelsPerMm * 10.0) == 0) { // Cada 10 mm = 1.0 cm (50 px)
                    markH = 18.0;
                    currentPen = markPenMajor;

                    // Número en centímetros
                    string cmText = (x / (MetricUnits.PixelsPerMm * 10.0)).ToString("0");
#pragma warning disable 618
                    FormattedText ft = new FormattedText(
                        cmText,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        numTypeface,
                        9.5,
                        Brushes.White
                    );
#pragma warning restore 618
                    dc.DrawText(ft, new Point(x - (ft.Width / 2.0), markH + 2.0));
                } else if (x % (MetricUnits.PixelsPerMm * 5.0) == 0) { // Cada 5 mm = 0.5 cm (25 px)
                    markH = 11.0;
                    currentPen = markPenMed;
                } else { // Cada 1 mm (5 px)
                    markH = 6.0;
                    currentPen = markPenMinor;
                }

                dc.DrawLine(currentPen, new Point(x, 0), new Point(x, markH));
            }

            // 3. Línea que se está trazando sobre el borde superior de la regla
            if (DrawLength > 0) {
                Brush needleBrush = new SolidColorBrush(Color);
                Pen needlePen = new Pen(needleBrush, Math.Max(2.5, Size * 0.8));
                dc.DrawLine(needlePen, new Point(0, 0), new Point(DrawLength, 0));

                if (VectorService.IsVectorMode) {
                    VectorService.DrawArrowHead(dc, new Point(0, 0), new Point(DrawLength, 0), needleBrush, Size);
                } else {
                    dc.DrawEllipse(Brushes.SkyBlue, new Pen(Brushes.White, 1.2), new Point(DrawLength, 0), 4.5, 4.5);
                }
            }

            dc.Pop(); // Pop RotateTransform
            dc.Pop(); // Pop TranslateTransform

            // Cota técnica flotante en tiempo real (módulo y ángulo)
            double rad = Angle * Math.PI / 180.0;
            Point targetPoint = new Point(orig.X + DrawLength * Math.Cos(rad), orig.Y - DrawLength * Math.Sin(rad));
            VectorService.DrawDimensionPreview(dc, orig, targetPoint, DrawLength, Angle);

            // Marcador visual de captura magnética en el extremo
            if (IsSnapped) {
                SnapService.DrawSnapIndicator(dc, SnapPoint, "Punto Final");
            }

            // 4. Cartel HUD informativo con longitud métrica, ángulo y control de teclado
            string toolName = VectorService.IsVectorMode ? "Vector 🏹" : "Regla 📏";
            string dimStatus = VectorService.IsDimensionEnabled ? "[D: Cota Sí]" : "[D: Cota No]";
            string mainText = _tool.Metric.GetMainStatusText(toolName + " " + dimStatus, Angle);
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

            double badgeX = orig.X - badgeWidth / 2.0;
            double badgeY = orig.Y + rulerHeight + 16.0;

            if (badgeY + badgeHeight > SystemParameters.VirtualScreenHeight - 40) {
                badgeY = orig.Y - badgeHeight - 16.0;
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

