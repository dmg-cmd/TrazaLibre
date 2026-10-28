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
    /// Herramienta interactiva de Vector entre Dos Puntos con Medidas ((🏹).
    /// Permite anclar el primer punto (por ejemplo en el origen 0,0 de los ejes o en cualquier extremo)
    /// y arrastrar hacia el segundo punto con visor en tiempo real de módulo en mm, ángulo trigonométrico
    /// y componentes cartesianas (ΔX, ΔY). Al soltar, estampa el vector con su cota de medidas.
    /// </summary>
    public class VectorTool : ITool {
        public string Id { get { return "shape_vector"; } }
        public string DisplayName { get { return "Vector con Medidas"; } }
        public string Icon { get { return "🏹"; } }

        private IAnnotationContext _context;
        private VectorVisualOverlay _visual;

        private MouseButtonEventHandler _mouseDownHandler;
        private MouseEventHandler _mouseMoveHandler;
        private MouseButtonEventHandler _mouseUpHandler;

        private Point _startPoint;
        private Point _currentPoint;
        private bool _isDrawing = false;

        public void Activate(IAnnotationContext context) {
            _context = context;
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.None;
            context.Canvas.Cursor = Cursors.Cross;
            context.Canvas.Focusable = true;
            context.Canvas.Focus();

            if (_visual == null) {
                _visual = new VectorVisualOverlay(this);
            }

            if (!_context.Canvas.Children.Contains(_visual)) {
                _context.Canvas.Children.Add(_visual);
            }

            _visual.Visibility = Visibility.Visible;
            _visual.Color = _context.CurrentColor;
            _visual.Size = _context.CurrentSize;
            _visual.Reset();

            if (_mouseDownHandler == null) {
                _mouseDownHandler = OnCanvasMouseDown;
                _mouseMoveHandler = OnCanvasMouseMove;
                _mouseUpHandler = OnCanvasMouseUp;
            }

            AttachEvents();
        }

        public void Deactivate(IAnnotationContext context) {
            DetachEvents();

            if (_visual != null && _context != null && _context.Canvas != null) {
                _context.Canvas.Children.Remove(_visual);
            }

            _isDrawing = false;
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
        }

        private void DetachEvents() {
            if (_context == null || _context.Canvas == null) return;

            _context.Canvas.PreviewMouseDown -= _mouseDownHandler;
            _context.Canvas.PreviewMouseMove -= _mouseMoveHandler;
            _context.Canvas.PreviewMouseUp -= _mouseUpHandler;
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed || _context == null) return;

            Point clickPos = e.GetPosition(_context.Canvas);
            SnapResult snap = SnapService.FindSnapPoint(_context.Canvas.Strokes, clickPos);

            _startPoint = snap.IsSnapped ? snap.Point : clickPos;
            _currentPoint = _startPoint;
            _isDrawing = true;

            if (_visual != null) {
                _visual.StartPoint = _startPoint;
                _visual.CurrentPoint = _currentPoint;
                _visual.IsActive = true;
                _visual.SnapStart = snap;
                _visual.InvalidateVisual();
            }

            e.Handled = true;
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e) {
            if (_context == null) return;

            Point mousePos = e.GetPosition(_context.Canvas);

            if (!_isDrawing) {
                // Previsualización de anclaje antes de hacer clic en el punto 1
                SnapResult hoverSnap = SnapService.FindSnapPoint(_context.Canvas.Strokes, mousePos);
                if (_visual != null) {
                    _visual.HoverPoint = mousePos;
                    _visual.HoverSnap = hoverSnap;
                    _visual.IsActive = false;
                    _visual.InvalidateVisual();
                }
                return;
            }

            // Calculando punto 2 mientras se arrastra
            SnapResult endSnap = SnapService.FindSnapPoint(_context.Canvas.Strokes, mousePos);
            Point effectivePoint;

            if (endSnap.IsSnapped) {
                effectivePoint = endSnap.Point;
            } else if ((Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) {
                // Cuantización métrica milimétrica (5 px = 1 mm)
                double dx = mousePos.X - _startPoint.X;
                double dy = mousePos.Y - _startPoint.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > 4.0) {
                    double snappedDist = MetricUnits.SnapToMm(dist);
                    double ratio = snappedDist / dist;
                    effectivePoint = new Point(_startPoint.X + dx * ratio, _startPoint.Y + dy * ratio);
                } else {
                    effectivePoint = mousePos;
                }
            } else {
                effectivePoint = mousePos;
            }

            _currentPoint = effectivePoint;

            if (_visual != null) {
                _visual.CurrentPoint = _currentPoint;
                _visual.SnapEnd = endSnap;
                _visual.IsActive = true;
                _visual.InvalidateVisual();
            }

            e.Handled = true;
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e) {
            if (!_isDrawing || _context == null) return;
            _isDrawing = false;

            Point mouseUpPoint = e.GetPosition(_context.Canvas);
            SnapResult endSnap = SnapService.FindSnapPoint(_context.Canvas.Strokes, mouseUpPoint);
            Point finalEnd;

            if (endSnap.IsSnapped) {
                finalEnd = endSnap.Point;
            } else if ((Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) {
                double dx = mouseUpPoint.X - _startPoint.X;
                double dy = mouseUpPoint.Y - _startPoint.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > 4.0) {
                    double snappedDist = MetricUnits.SnapToMm(dist);
                    double ratio = snappedDist / dist;
                    finalEnd = new Point(_startPoint.X + dx * ratio, _startPoint.Y + dy * ratio);
                } else {
                    finalEnd = mouseUpPoint;
                }
            } else {
                finalEnd = mouseUpPoint;
            }

            if (_visual != null) {
                _visual.Reset();
                _visual.InvalidateVisual();
            }

            // Ignorar clics accidentales de menos de 5 píxeles
            double fdx = finalEnd.X - _startPoint.X;
            double fdy = finalEnd.Y - _startPoint.Y;
            double fdist = Math.Sqrt(fdx * fdx + fdy * fdy);
            if (fdist < 5.0) return;

            // Calcular medidas matemáticas del vector
            double cartesianDy = -(finalEnd.Y - _startPoint.Y); // Y cartesiano hacia arriba
            double angleRad = Math.Atan2(cartesianDy, fdx);
            double angleDeg = angleRad * (180.0 / Math.PI);
            if (angleDeg < 0) {
                angleDeg += 360.0;
            }

            double thickness = Math.Max(2.5, _context.CurrentSize);

            // 1. Crear trazo del Vector con punta de flecha
            Point[] vectorPts = VectorService.CreateVectorPoints(_startPoint, finalEnd, thickness);
            StylusPointCollection points = new StylusPointCollection();
            foreach (Point p in vectorPts) {
                points.Add(new StylusPoint(p.X, p.Y));
            }

            DrawingAttributes attrs = new DrawingAttributes {
                Color = _context.CurrentColor,
                Width = thickness,
                Height = thickness,
                FitToCurve = false,
                IsHighlighter = false
            };

            Stroke vectorStroke = new Stroke(points, attrs);
            StrokeCollection batch = new StrokeCollection { vectorStroke };

            // 2. Crear trazo de la cota técnica con las medidas permanentes (módulo y ángulo)
            StrokeCollection dimStrokes = VectorService.CreateDimensionStrokes(
                _startPoint,
                finalEnd,
                fdist,
                angleDeg,
                _context.CurrentColor,
                thickness,
                true // Forzar creación de medidas
            );

            if (dimStrokes != null) {
                foreach (Stroke ds in dimStrokes) {
                    batch.Add(ds);
                }
            }

            // 3. Estampar en lote atómico para deshacer con un solo Ctrl+Z
            _context.AddStrokes(batch);

            e.Handled = true;
        }
    }

    /// <summary>
    /// Elemento visual interactivo de previsualización para el trazado de vectores con medidas.
    /// Dibuja la flecha vectorial, dianas magnéticas y la placa de medidas HUD con componentes ΔX y ΔY.
    /// </summary>
    public class VectorVisualOverlay : FrameworkElement {
        private readonly VectorTool _tool;

        public Point StartPoint { get; set; }
        public Point CurrentPoint { get; set; }
        public Point HoverPoint { get; set; }
        public SnapResult HoverSnap { get; set; }
        public SnapResult SnapStart { get; set; }
        public SnapResult SnapEnd { get; set; }
        public bool IsActive { get; set; }
        public Color Color { get; set; }
        public double Size { get; set; }

        public VectorVisualOverlay(VectorTool tool) {
            _tool = tool;
            IsHitTestVisible = false;
            Color = Colors.Red;
            Size = 3.0;
            Reset();
        }

        public void Reset() {
            IsActive = false;
            HoverSnap = new SnapResult();
            SnapStart = new SnapResult();
            SnapEnd = new SnapResult();
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);

            // 1. Estado previo al primer clic: mostrar hover snap si lo hay
            if (!IsActive) {
                if (HoverSnap.IsSnapped) {
                    SnapService.DrawSnapIndicator(dc, HoverSnap.Point, HoverSnap.SnapType);
                }
                return;
            }

            Point p1 = StartPoint;
            Point p2 = CurrentPoint;

            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // 2. Dibujar flecha del vector en tiempo real
            Brush mainBrush = new SolidColorBrush(Color);
            Pen linePen = new Pen(mainBrush, Math.Max(2.0, Size));
            dc.DrawLine(linePen, p1, p2);

            if (dist >= 8.0) {
                VectorService.DrawArrowHead(dc, p1, p2, mainBrush, Size);
            }

            // 3. Indicadores de anclaje inicial y final
            if (SnapStart.IsSnapped) {
                SnapService.DrawSnapIndicator(dc, p1, SnapStart.SnapType);
            }
            if (SnapEnd.IsSnapped) {
                SnapService.DrawSnapIndicator(dc, p2, SnapEnd.SnapType);
            }

            if (dist < 10.0) return;

            // 4. Calcular medidas para la placa HUD
            double mm = MetricUnits.PxToMm(dist);
            double cm = mm / 10.0;
            double cartesianDy = -(p2.Y - p1.Y);
            double angleRad = Math.Atan2(cartesianDy, dx);
            double angleDeg = angleRad * (180.0 / Math.PI);
            if (angleDeg < 0) angleDeg += 360.0;

            double dxMm = MetricUnits.PxToMm(Math.Abs(dx)) * Math.Sign(dx);
            double dyMm = MetricUnits.PxToMm(Math.Abs(cartesianDy)) * Math.Sign(cartesianDy);

            // 5. Placa flotante de medidas (Módulo, Ángulo y Componentes)
            string line1 = string.Format("📏 Módulo: {0:0.0} mm ({1:0.00} cm)  |  ∠ {2:0.0}°", mm, cm, angleDeg);
            string line2 = string.Format("📐 ΔX: {0:+0.0;-0.0;0.0} mm  |  ΔY: {1:+0.0;-0.0;0.0} mm", dxMm, dyMm);

            Typeface tf = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
#pragma warning disable 618
            FormattedText ft1 = new FormattedText(line1, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 11.0, Brushes.Gold);
            FormattedText ft2 = new FormattedText(line2, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9.5, Brushes.White);
#pragma warning restore 618

            double boxWidth = Math.Max(ft1.Width, ft2.Width) + 16.0;
            double boxHeight = ft1.Height + ft2.Height + 10.0;

            // Ubicar la placa cerca del punto medio con desplazamiento perpendicular
            double nx = -dy / dist;
            double ny = dx / dist;
            if (ny > 0) { nx = -nx; ny = -ny; }
            double offset = 26.0;

            Point mid = new Point((p1.X + p2.X) / 2.0 + nx * offset, (p1.Y + p2.Y) / 2.0 + ny * offset);
            Rect boxRect = new Rect(mid.X - (boxWidth / 2.0), mid.Y - (boxHeight / 2.0), boxWidth, boxHeight);

            Brush bgBrush = new SolidColorBrush(Color.FromArgb(235, 18, 22, 28));
            Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 215, 0)), 1.2);

            dc.DrawRoundedRectangle(bgBrush, borderPen, boxRect, 4.0, 4.0);
            dc.DrawText(ft1, new Point(boxRect.X + 8.0, boxRect.Y + 4.0));
            dc.DrawText(ft2, new Point(boxRect.X + 8.0, boxRect.Y + 6.0 + ft1.Height));
        }
    }
}
