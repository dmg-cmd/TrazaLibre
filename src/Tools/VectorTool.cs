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

        private enum VectorState {
            Idle,
            DraggingVector,
            PlacingLabel
        }

        private IAnnotationContext _context;
        private VectorVisualOverlay _visual;

        private MouseButtonEventHandler _mouseDownHandler;
        private MouseEventHandler _mouseMoveHandler;
        private MouseButtonEventHandler _mouseUpHandler;
        private KeyEventHandler _keyDownHandler;
        private ContextMenuEventHandler _contextMenuOpeningHandler;
        private EventHandler _vectorStateChangedHandler;

        private VectorState _state = VectorState.Idle;
        private Point _startPoint;
        private Point _currentPoint;
        private Point _confirmedEndPoint;
        private Point _labelCenter;
        private double _confirmedDist;
        private double _confirmedAngleDeg;
        private bool _suppressNextContextMenu = false;

        // Estado para entrada numérica directa en coordenadas polares (módulo r y ángulo θ)
        private bool _isEnteringPolar = false;
        private bool _editingAngleField = false;
        private string _polarMagBuffer = string.Empty;
        private string _polarAngleBuffer = string.Empty;

        public bool IsPlacingLabel {
            get { return _state == VectorState.PlacingLabel; }
        }

        public bool IsActiveOrPlacing {
            get { return _state != VectorState.Idle; }
        }

        public bool IsEnteringPolar {
            get { return _isEnteringPolar; }
        }

        public bool EditingAngleField {
            get { return _editingAngleField; }
        }

        public string PolarMagBuffer {
            get { return _polarMagBuffer; }
        }

        public string PolarAngleBuffer {
            get { return _polarAngleBuffer; }
        }

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

            _state = VectorState.Idle;
            _suppressNextContextMenu = false;
            ResetPolarInput();
            _visual.Visibility = Visibility.Visible;
            _visual.Color = _context.CurrentColor;
            _visual.Size = _context.CurrentSize;
            _visual.Reset();

            context.Canvas.ContextMenu = ToolContextMenuHelper.CreateVectorContextMenu(context);

            if (_mouseDownHandler == null) {
                _mouseDownHandler = OnCanvasMouseDown;
                _mouseMoveHandler = OnCanvasMouseMove;
                _mouseUpHandler = OnCanvasMouseUp;
                _keyDownHandler = OnCanvasKeyDown;
                _contextMenuOpeningHandler = OnCanvasContextMenuOpening;
                _vectorStateChangedHandler = OnVectorStateChanged;
            }

            AttachEvents();
        }

        public void Deactivate(IAnnotationContext context) {
            if (_state == VectorState.PlacingLabel && _context != null) {
                CommitVectorWithLabel(VectorService.IsDimensionEnabled, _labelCenter);
            }

            DetachEvents();

            if (context != null && context.Canvas != null) {
                context.Canvas.ContextMenu = null;
            }

            if (_visual != null && _context != null && _context.Canvas != null) {
                _context.Canvas.Children.Remove(_visual);
            }

            _state = VectorState.Idle;
            _suppressNextContextMenu = false;
            ResetPolarInput();
            _context = null;
        }

        private void OnVectorStateChanged(object sender, EventArgs e) {
            if (_visual != null) {
                _visual.InvalidateVisual();
            }
        }

        private void ResetPolarInput() {
            _isEnteringPolar = false;
            _editingAngleField = false;
            _polarMagBuffer = string.Empty;
            _polarAngleBuffer = string.Empty;
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
            _context.Canvas.PreviewKeyDown += _keyDownHandler;
            _context.Canvas.ContextMenuOpening += _contextMenuOpeningHandler;

            if (_vectorStateChangedHandler != null) {
                VectorService.RectangularProjectionsChanged += _vectorStateChangedHandler;
                VectorService.DimensionModeChanged += _vectorStateChangedHandler;
            }
        }

        private void DetachEvents() {
            if (_context == null || _context.Canvas == null) return;

            _context.Canvas.PreviewMouseDown -= _mouseDownHandler;
            _context.Canvas.PreviewMouseMove -= _mouseMoveHandler;
            _context.Canvas.PreviewMouseUp -= _mouseUpHandler;
            _context.Canvas.PreviewKeyDown -= _keyDownHandler;
            _context.Canvas.ContextMenuOpening -= _contextMenuOpeningHandler;

            if (_vectorStateChangedHandler != null) {
                VectorService.RectangularProjectionsChanged -= _vectorStateChangedHandler;
                VectorService.DimensionModeChanged -= _vectorStateChangedHandler;
            }
        }

        private void OnCanvasContextMenuOpening(object sender, ContextMenuEventArgs e) {
            if (_suppressNextContextMenu) {
                _suppressNextContextMenu = false;
                e.Handled = true;
            }
        }

        private void CancelCurrentVector() {
            _state = VectorState.Idle;
            ResetPolarInput();
            if (_visual != null) {
                _visual.Reset();
                _visual.InvalidateVisual();
            }
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (_context == null) return;
            _context.Canvas.Focus();

            if (_state == VectorState.PlacingLabel) {
                if (e.LeftButton == MouseButtonState.Pressed) {
                    _labelCenter = e.GetPosition(_context.Canvas);
                    CommitVectorWithLabel(true, _labelCenter);
                    e.Handled = true;
                    return;
                }
                if (e.RightButton == MouseButtonState.Pressed) {
                    _suppressNextContextMenu = true;
                    CommitVectorWithLabel(false, _labelCenter);
                    e.Handled = true;
                    return;
                }
                return;
            }

            if (_state == VectorState.DraggingVector) {
                if (e.LeftButton == MouseButtonState.Pressed) {
                    Point clickEndPos = e.GetPosition(_context.Canvas);
                    SnapResult endSnap = SnapService.FindSnapPoint(_context.Canvas.Strokes, clickEndPos);
                    Point targetEnd = _isEnteringPolar ? _currentPoint : ResolveVectorEndPoint(clickEndPos, endSnap);
                    ConfirmDraggingVector(targetEnd, _isEnteringPolar ? new SnapResult() : endSnap);
                    e.Handled = true;
                    return;
                }
                if (e.RightButton == MouseButtonState.Pressed) {
                    _suppressNextContextMenu = true;
                    CancelCurrentVector();
                    e.Handled = true;
                    return;
                }
                return;
            }

            if (_state == VectorState.Idle && e.LeftButton == MouseButtonState.Pressed) {
                _suppressNextContextMenu = false;
                ResetPolarInput();
                Point clickPos = e.GetPosition(_context.Canvas);
                SnapResult snap = SnapService.FindSnapPoint(_context.Canvas.Strokes, clickPos);

                _startPoint = snap.IsSnapped ? snap.Point : clickPos;
                _currentPoint = _startPoint;
                _state = VectorState.DraggingVector;

                if (_visual != null) {
                    _visual.StartPoint = _startPoint;
                    _visual.CurrentPoint = _currentPoint;
                    _visual.IsActive = true;
                    _visual.IsPlacingLabel = false;
                    _visual.SnapStart = snap;
                    _visual.SnapEnd = new SnapResult();
                    _visual.InvalidateVisual();
                }

                e.Handled = true;
            }
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e) {
            if (_context == null) return;

            Point mousePos = e.GetPosition(_context.Canvas);

            if (_state == VectorState.Idle) {
                // Previsualización de anclaje antes de hacer clic en el punto 1
                SnapResult hoverSnap = SnapService.FindSnapPoint(_context.Canvas.Strokes, mousePos);
                if (_visual != null) {
                    _visual.HoverPoint = mousePos;
                    _visual.HoverSnap = hoverSnap;
                    _visual.IsActive = false;
                    _visual.IsPlacingLabel = false;
                    _visual.InvalidateVisual();
                }
                return;
            }

            if (_state == VectorState.PlacingLabel) {
                _labelCenter = mousePos;
                if (_visual != null) {
                    _visual.LabelCenter = _labelCenter;
                    _visual.IsPlacingLabel = true;
                    _visual.IsActive = true;
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            // Si el usuario está escribiendo coordenadas polares exactas, no sobrescribirlas con el ratón
            if (_isEnteringPolar) {
                e.Handled = true;
                return;
            }

            // Calculando punto 2 mientras se arrastra o se desplaza tras el primer clic
            SnapResult endSnap = SnapService.FindSnapPoint(_context.Canvas.Strokes, mousePos);
            Point effectivePoint = ResolveVectorEndPoint(mousePos, endSnap);
            _currentPoint = effectivePoint;

            double dx = _currentPoint.X - _startPoint.X;
            double dy = _currentPoint.Y - _startPoint.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist >= 8.0) {
                double angleDeg = ComputeCartesianAngleDeg(dx, -dy);
                _labelCenter = VectorService.FindOptimalLabelCenterForDimension(_startPoint, _currentPoint, dist, angleDeg, _context.Canvas.Strokes);
            }

            if (_visual != null) {
                _visual.CurrentPoint = _currentPoint;
                _visual.LabelCenter = _labelCenter;
                _visual.SnapEnd = endSnap;
                _visual.IsActive = true;
                _visual.IsPlacingLabel = false;
                _visual.InvalidateVisual();
            }

            e.Handled = true;
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e) {
            if (_state != VectorState.DraggingVector || _context == null) return;
            if (e.ChangedButton != MouseButton.Left) return;

            Point mouseUpPoint = e.GetPosition(_context.Canvas);
            SnapResult endSnap = SnapService.FindSnapPoint(_context.Canvas.Strokes, mouseUpPoint);
            Point finalEnd = ResolveVectorEndPoint(mouseUpPoint, endSnap);

            double fdx = finalEnd.X - _startPoint.X;
            double fdy = finalEnd.Y - _startPoint.Y;
            double fdist = Math.Sqrt(fdx * fdx + fdy * fdy);

            // Si el usuario hizo un clic simple sin arrastrar, mantener activo el vector para mover o teclear r y θ
            if (fdist < 5.0) {
                double curDx = _currentPoint.X - _startPoint.X;
                double curDy = _currentPoint.Y - _startPoint.Y;
                if (Math.Sqrt(curDx * curDx + curDy * curDy) < 5.0) {
                    double samplePx = MetricUnits.MmToPx(40.0);
                    double sampleRad = 45.0 * Math.PI / 180.0;
                    _currentPoint = new Point(
                        _startPoint.X + samplePx * Math.Cos(sampleRad),
                        _startPoint.Y - samplePx * Math.Sin(sampleRad)
                    );
                }

                double sdx = _currentPoint.X - _startPoint.X;
                double sdy = _currentPoint.Y - _startPoint.Y;
                double sdist = Math.Sqrt(sdx * sdx + sdy * sdy);
                double sAngle = ComputeCartesianAngleDeg(sdx, -sdy);
                _labelCenter = VectorService.FindOptimalLabelCenterForDimension(_startPoint, _currentPoint, sdist, sAngle, _context.Canvas.Strokes);

                if (_visual != null) {
                    _visual.StartPoint = _startPoint;
                    _visual.CurrentPoint = _currentPoint;
                    _visual.LabelCenter = _labelCenter;
                    _visual.SnapEnd = new SnapResult();
                    _visual.IsActive = true;
                    _visual.IsPlacingLabel = false;
                    _visual.InvalidateVisual();
                }

                e.Handled = true;
                return;
            }

            ConfirmDraggingVector(finalEnd, endSnap);
            e.Handled = true;
        }

        private void ConfirmDraggingVector(Point finalEnd, SnapResult endSnap) {
            if (_context == null) return;

            double fdx = finalEnd.X - _startPoint.X;
            double fdy = finalEnd.Y - _startPoint.Y;
            double fdist = Math.Sqrt(fdx * fdx + fdy * fdy);
            if (fdist < 5.0) {
                CancelCurrentVector();
                return;
            }

            double cartesianDy = -(finalEnd.Y - _startPoint.Y);
            double angleDeg = ComputeCartesianAngleDeg(fdx, cartesianDy);

            _confirmedEndPoint = finalEnd;
            _confirmedDist = fdist;
            _confirmedAngleDeg = angleDeg;
            _labelCenter = VectorService.FindOptimalLabelCenterForDimension(_startPoint, finalEnd, fdist, angleDeg, _context.Canvas.Strokes);

            if (!VectorService.IsDimensionEnabled) {
                CommitVectorWithLabel(false, _labelCenter);
            } else if (!VectorService.ManualLabelPlacement) {
                CommitVectorWithLabel(true, _labelCenter);
            } else {
                _state = VectorState.PlacingLabel;
                _context.Canvas.Focus();
                if (_visual != null) {
                    _visual.StartPoint = _startPoint;
                    _visual.CurrentPoint = _confirmedEndPoint;
                    _visual.LabelCenter = _labelCenter;
                    _visual.SnapEnd = endSnap;
                    _visual.IsActive = true;
                    _visual.IsPlacingLabel = true;
                    _visual.InvalidateVisual();
                }
            }
        }

        private void OnCanvasKeyDown(object sender, KeyEventArgs e) {
            if (_state == VectorState.PlacingLabel) {
                if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Space) {
                    CommitVectorWithLabel(true, _labelCenter);
                    e.Handled = true;
                } else if (e.Key == Key.Escape) {
                    CommitVectorWithLabel(false, _labelCenter);
                    e.Handled = true;
                }
                return;
            }

            if (_state != VectorState.DraggingVector || _context == null) return;

            ModifierKeys mods = Keyboard.Modifiers;

            // 1. Escape: cancelar el vector actual
            if (e.Key == Key.Escape) {
                CancelCurrentVector();
                e.Handled = true;
                return;
            }

            // 2. Enter: confirmar el vector con la geometría polar actual
            if (e.Key == Key.Enter || e.Key == Key.Return) {
                ConfirmDraggingVector(_currentPoint, new SnapResult());
                e.Handled = true;
                return;
            }

            // 3. Tab o Espacio: alternar entre edición de módulo r (mm) y ángulo θ (°)
            if (e.Key == Key.Tab || e.Key == Key.Space) {
                _isEnteringPolar = true;
                _editingAngleField = !_editingAngleField;
                if (_visual != null) {
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            // 4. Retroceso (Backspace)
            if (e.Key == Key.Back) {
                if (_editingAngleField) {
                    if (_polarAngleBuffer.Length > 0) {
                        _polarAngleBuffer = _polarAngleBuffer.Substring(0, _polarAngleBuffer.Length - 1);
                    } else {
                        _editingAngleField = false;
                    }
                } else if (_polarMagBuffer.Length > 0) {
                    _polarMagBuffer = _polarMagBuffer.Substring(0, _polarMagBuffer.Length - 1);
                }

                if (string.IsNullOrEmpty(_polarMagBuffer) && string.IsNullOrEmpty(_polarAngleBuffer) && !_editingAngleField) {
                    _isEnteringPolar = false;
                } else {
                    UpdateCurrentPointFromPolarInput();
                }

                if (_visual != null) {
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            // 5. Signo negativo para ángulos (ej. -45°)
            if ((e.Key == Key.Subtract || e.Key == Key.OemMinus) && _editingAngleField && string.IsNullOrEmpty(_polarAngleBuffer)) {
                _isEnteringPolar = true;
                _polarAngleBuffer = "-";
                if (_visual != null) {
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
                return;
            }

            // 6. Dígitos del 0 al 9
            int digit = -1;
            if (e.Key >= Key.D0 && e.Key <= Key.D9 && (mods & ModifierKeys.Shift) != ModifierKeys.Shift) {
                digit = e.Key - Key.D0;
            } else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) {
                digit = e.Key - Key.NumPad0;
            }

            if (digit >= 0) {
                _isEnteringPolar = true;
                if (_editingAngleField) {
                    _polarAngleBuffer += digit.ToString(CultureInfo.InvariantCulture);
                } else {
                    _polarMagBuffer += digit.ToString(CultureInfo.InvariantCulture);
                }
                UpdateCurrentPointFromPolarInput();
                e.Handled = true;
                return;
            }

            // 7. Punto o coma decimal
            if (e.Key == Key.OemPeriod || e.Key == Key.Decimal || e.Key == Key.OemComma) {
                _isEnteringPolar = true;
                if (_editingAngleField) {
                    if (!_polarAngleBuffer.Contains(".")) {
                        if (string.IsNullOrEmpty(_polarAngleBuffer) || _polarAngleBuffer == "-") {
                            _polarAngleBuffer += "0";
                        }
                        _polarAngleBuffer += ".";
                    }
                } else {
                    if (!_polarMagBuffer.Contains(".")) {
                        if (string.IsNullOrEmpty(_polarMagBuffer)) {
                            _polarMagBuffer = "0";
                        }
                        _polarMagBuffer += ".";
                    }
                }
                if (_visual != null) {
                    _visual.InvalidateVisual();
                }
                e.Handled = true;
            }
        }

        private void UpdateCurrentPointFromPolarInput() {
            double curDx = _currentPoint.X - _startPoint.X;
            double curDy = -(_currentPoint.Y - _startPoint.Y);
            double curDist = Math.Sqrt(curDx * curDx + curDy * curDy);
            double fallbackDist = curDist >= 10.0 ? curDist : MetricUnits.MmToPx(40.0);
            double fallbackAngle = curDist >= 10.0 ? ComputeCartesianAngleDeg(curDx, curDy) : 45.0;

            double rPx = fallbackDist;
            double parsedR;
            if (!string.IsNullOrEmpty(_polarMagBuffer) &&
                double.TryParse(_polarMagBuffer, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedR) &&
                parsedR > 0.0) {
                rPx = MetricUnits.MmToPx(parsedR);
            }

            double angDeg = fallbackAngle;
            double parsedAng;
            if (!string.IsNullOrEmpty(_polarAngleBuffer) &&
                _polarAngleBuffer != "-" &&
                double.TryParse(_polarAngleBuffer, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedAng)) {
                angDeg = parsedAng;
            }

            double rad = angDeg * Math.PI / 180.0;
            _currentPoint = new Point(_startPoint.X + rPx * Math.Cos(rad), _startPoint.Y - rPx * Math.Sin(rad));

            double dx = _currentPoint.X - _startPoint.X;
            double dy = _currentPoint.Y - _startPoint.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist >= 8.0) {
                double normAng = ComputeCartesianAngleDeg(dx, -dy);
                _labelCenter = VectorService.FindOptimalLabelCenterForDimension(_startPoint, _currentPoint, dist, normAng, _context.Canvas.Strokes);
            }

            if (_visual != null) {
                _visual.CurrentPoint = _currentPoint;
                _visual.LabelCenter = _labelCenter;
                _visual.SnapEnd = new SnapResult();
                _visual.IsActive = true;
                _visual.IsPlacingLabel = false;
                _visual.InvalidateVisual();
            }
        }

        private Point ResolveVectorEndPoint(Point rawPos, SnapResult endSnap) {
            if (endSnap.IsSnapped) {
                return endSnap.Point;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) {
                double dx = rawPos.X - _startPoint.X;
                double dy = rawPos.Y - _startPoint.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > 4.0) {
                    double snappedDist = MetricUnits.SnapToMm(dist);
                    double ratio = snappedDist / dist;
                    return new Point(_startPoint.X + dx * ratio, _startPoint.Y + dy * ratio);
                }
            }
            return rawPos;
        }

        private static double ComputeCartesianAngleDeg(double dx, double cartesianDy) {
            double angleRad = Math.Atan2(cartesianDy, dx);
            double angleDeg = angleRad * (180.0 / Math.PI);
            if (angleDeg < 0) angleDeg += 360.0;
            return angleDeg;
        }

        private void CommitVectorWithLabel(bool includeDimension, Point labelCenter) {
            if (_context == null) return;

            double thickness = Math.Max(2.5, _context.CurrentSize);

            // 1. Crear trazo del Vector con punta de flecha
            Point[] vectorPts = VectorService.CreateVectorPoints(_startPoint, _confirmedEndPoint, thickness);
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
            StrokeCollection batch = new StrokeCollection();

            // 1b. Agregar primero las proyecciones y marcas rectangulares sobre Eje X y Eje Y
            if (VectorService.ShowRectangularProjections) {
                StrokeCollection projStrokes = VectorService.CreateRectangularProjectionStrokes(_startPoint, _confirmedEndPoint, thickness);
                if (projStrokes != null) {
                    foreach (Stroke ps in projStrokes) {
                        batch.Add(ps);
                    }
                }
            }

            batch.Add(vectorStroke);

            // 2. Crear etiqueta técnica de medidas si corresponde
            if (includeDimension) {
                StrokeCollection dimStrokes = VectorService.CreateDimensionStrokesAt(
                    _startPoint,
                    _confirmedEndPoint,
                    labelCenter,
                    _confirmedDist,
                    _confirmedAngleDeg,
                    _context.CurrentColor,
                    thickness,
                    true
                );

                if (dimStrokes != null) {
                    foreach (Stroke ds in dimStrokes) {
                        batch.Add(ds);
                    }
                }
            }

            // 3. Estampar en lote atómico para deshacer con un solo Ctrl+Z
            _context.AddStrokes(batch);

            _state = VectorState.Idle;
            ResetPolarInput();
            if (_visual != null) {
                _visual.Reset();
                _visual.InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// Elemento visual interactivo de previsualización para el trazado de vectores con medidas.
    /// Dibuja la flecha vectorial, dianas magnéticas, la etiqueta móvil con línea guía y la placa HUD.
    /// </summary>
    public class VectorVisualOverlay : FrameworkElement {
        private readonly VectorTool _tool;

        public Point StartPoint { get; set; }
        public Point CurrentPoint { get; set; }
        public Point LabelCenter { get; set; }
        public Point HoverPoint { get; set; }
        public SnapResult HoverSnap { get; set; }
        public SnapResult SnapStart { get; set; }
        public SnapResult SnapEnd { get; set; }
        public bool IsActive { get; set; }
        public bool IsPlacingLabel { get; set; }
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
            IsPlacingLabel = false;
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

            // 1b. Previsualizar proyecciones y marcas en Eje X y Eje Y (Polar -> Rectangular)
            if (dist >= 8.0) {
                VectorService.DrawRectangularProjectionsPreview(dc, p1, p2);
            }

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

            // 5. En paso PlacingLabel: previsualizar la etiqueta flotante conectada al puntero
            if (IsPlacingLabel) {
                VectorService.DrawDimensionPreviewAt(dc, p1, p2, LabelCenter, dist, angleDeg, Color);

                string helpLine1 = string.Format("📏 Polar: {0:0.0} mm ({1:0.00} cm) ∠ {2:0.0}°  ➔  📐 Rectangular: X = {3:+0.0;-0.0;0.0} mm, Y = {4:+0.0;-0.0;0.0} mm", mm, cm, angleDeg, dxMm, dyMm);
                string helpLine2 = "🏷️ Mueve el ratón y haz clic izquierdo para colocar la medida  |  [Enter] Automático  |  [Clic derecho / Esc] Sin medida";
                DrawHudBox(dc, p1, p2, dist, dx, dy, helpLine1, helpLine2, true);
                return;
            }

            // 6. Durante el arrastre / entrada polar: mostrar placa HUD con conversión Polar -> Rectangular
            string line1 = string.Format("📏 Polar: {0:0.0} mm ({1:0.00} cm) ∠ {2:0.0}°  ➔  📐 Rectangular: X = {3:+0.0;-0.0;0.0} mm, Y = {4:+0.0;-0.0;0.0} mm", mm, cm, angleDeg, dxMm, dyMm);
            string line2;
            if (_tool != null && _tool.IsEnteringPolar) {
                string magText = string.IsNullOrEmpty(_tool.PolarMagBuffer) ? string.Format("{0:0.#}", mm) : _tool.PolarMagBuffer;
                string angText = string.IsNullOrEmpty(_tool.PolarAngleBuffer) ? string.Format("{0:0.#}", angleDeg) : _tool.PolarAngleBuffer;
                string magIndicator = !_tool.EditingAngleField ? "_" : "";
                string angIndicator = _tool.EditingAngleField ? "_" : "";
                line2 = string.Format("⌨️ Entrada Polar: Módulo r = [{0}{1}] mm  |  Ángulo θ = [{2}{3}]°  ([Tab] cambiar r/θ | [Enter] trazar | [Esc] cancelar)", magText, magIndicator, angText, angIndicator);
            } else {
                line2 = "⌨️ Teclea Módulo y [Tab] Ángulo para entrada polar exacta  |  Clic o soltar para fijar";
            }
            DrawHudBox(dc, p1, p2, dist, dx, dy, line1, line2, true);
        }

        private void DrawHudBox(DrawingContext dc, Point p1, Point p2, double dist, double dx, double dy, string line1, string line2, bool fixedTop) {
            Typeface tf = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
#pragma warning disable 618
            FormattedText ft1 = new FormattedText(line1, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 11.0, Brushes.Gold);
            FormattedText ft2 = new FormattedText(line2, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9.5, Brushes.White);
#pragma warning restore 618

            double boxWidth = Math.Max(ft1.Width, ft2.Width) + 16.0;
            double boxHeight = ft1.Height + ft2.Height + 10.0;

            Rect boxRect;
            if (fixedTop) {
                double bx = (p1.X + p2.X) / 2.0 - boxWidth / 2.0;
                double by = Math.Min(p1.Y, p2.Y) - boxHeight - 36.0;
                if (by < 14.0) {
                    by = Math.Max(p1.Y, p2.Y) + 36.0;
                }
                if (bx < 14.0) bx = 14.0;
                boxRect = new Rect(bx, by, boxWidth, boxHeight);
            } else {
                double nx = -dy / dist;
                double ny = dx / dist;
                if (ny > 0) { nx = -nx; ny = -ny; }
                double offset = 26.0;
                Point mid = new Point((p1.X + p2.X) / 2.0 + nx * offset, (p1.Y + p2.Y) / 2.0 + ny * offset);
                double bx = Math.Max(14.0, mid.X - (boxWidth / 2.0));
                double by = Math.Max(14.0, mid.Y - (boxHeight / 2.0));
                boxRect = new Rect(bx, by, boxWidth, boxHeight);
            }

            Brush bgBrush = new SolidColorBrush(Color.FromArgb(235, 18, 22, 28));
            Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 215, 0)), 1.2);

            dc.DrawRoundedRectangle(bgBrush, borderPen, boxRect, 4.0, 4.0);
            dc.DrawText(ft1, new Point(boxRect.X + 8.0, boxRect.Y + 4.0));
            dc.DrawText(ft2, new Point(boxRect.X + 8.0, boxRect.Y + 6.0 + ft1.Height));
        }
    }
}
