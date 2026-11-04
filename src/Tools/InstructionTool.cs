using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Trazalibre.Core;

namespace Trazalibre.Tools {
    /// <summary>
    /// Tipo de flecha conectora para la indicación técnica.
    /// </summary>
    public enum ArrowPointerType {
        Straight = 0,         // Flecha recta directa entre indicación y objetivo
        CurvedVertical = 1,   // Flecha curva con entrada vertical al objetivo (arriba/abajo)
        CurvedHorizontal = 2  // Flecha curva con entrada horizontal al objetivo (izquierda/derecha)
    }

    /// <summary>
    /// Herramienta interactiva de Indicación con Flecha Apuntadora (📌).
    /// Permite escribir una nota/indicación en cualquier sector y, al finalizar,
    /// desplegar una flecha dinámica hacia el punto de referencia, eligiendo entre
    /// flecha recta o curva con entrada vertical u horizontal.
    /// </summary>
    public class InstructionTool : ITool {
        public string Id { get { return "tool_instruction"; } }
        public string DisplayName { get { return "Indicación con Flecha"; } }
        public string Icon { get { return "📌"; } }

        private enum ToolState {
            Idle,
            Typing,
            Pointing
        }

        private IAnnotationContext _context;
        private ToolState _state = ToolState.Idle;
        private ArrowPointerType _selectedArrowType = ArrowPointerType.CurvedVertical;

        private MouseButtonEventHandler _canvasMouseDownHandler;
        private MouseEventHandler _canvasMouseMoveHandler;
        private KeyEventHandler _canvasKeyDownHandler;

        // Elementos de edición
        private TextBox _activeEditor = null;
        private Border _optionsPanel = null;
        private Border _pointingHud = null;
        private TextBlock _hudStatusText = null;

        // Datos de la indicación confirmada
        private Point _textOrigin;
        private string _confirmedText = "";
        private Rect _textBounds;
        private double _confirmedFontSize;

        // Trazos de previsualización en tiempo real
        private StrokeCollection _previewStrokes = new StrokeCollection();

        public void Activate(IAnnotationContext context) {
            _context = context;
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.None;
            context.Canvas.Cursor = Cursors.IBeam;
            context.Canvas.Focusable = true;
            context.Canvas.Focus();

            if (_canvasMouseDownHandler == null) {
                _canvasMouseDownHandler = OnCanvasMouseDown;
                _canvasMouseMoveHandler = OnCanvasMouseMove;
                _canvasKeyDownHandler = OnCanvasKeyDown;
            }

            AttachEvents();
            ResetState();
        }

        public void Deactivate(IAnnotationContext context) {
            CommitOrCancel(false);
            DetachEvents();
            ResetState();
            _context = null;
        }

        public void OnColorChanged(IAnnotationContext context, Color newColor) {
            if (_activeEditor != null) {
                _activeEditor.Foreground = new SolidColorBrush(newColor);
                _activeEditor.CaretBrush = new SolidColorBrush(newColor);
            }
            UpdateHudStyle();
        }

        public void OnSizeChanged(IAnnotationContext context, double newSize) {
            if (_activeEditor != null) {
                _activeEditor.FontSize = CalculateFontSize(newSize);
            }
        }

        private double CalculateFontSize(double size) {
            return Math.Max(14.0, size * 4.2);
        }

        private void AttachEvents() {
            if (_context == null || _context.Canvas == null) return;
            DetachEvents();

            _context.Canvas.PreviewMouseDown += _canvasMouseDownHandler;
            _context.Canvas.PreviewMouseMove += _canvasMouseMoveHandler;
            _context.Canvas.PreviewKeyDown += _canvasKeyDownHandler;
        }

        private void DetachEvents() {
            if (_context == null || _context.Canvas == null) return;

            _context.Canvas.PreviewMouseDown -= _canvasMouseDownHandler;
            _context.Canvas.PreviewMouseMove -= _canvasMouseMoveHandler;
            _context.Canvas.PreviewKeyDown -= _canvasKeyDownHandler;
        }

        private void ResetState() {
            CleanPreview();
            RemoveEditorAndPanels();
            _state = ToolState.Idle;
            _confirmedText = "";
            if (_context != null && _context.Canvas != null) {
                _context.Canvas.Cursor = Cursors.IBeam;
            }
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (_context == null) return;

            if (_state == ToolState.Idle) {
                if (e.LeftButton == MouseButtonState.Pressed) {
                    Point clickPos = e.GetPosition(_context.Canvas);
                    StartTyping(clickPos);
                    e.Handled = true;
                }
            } else if (_state == ToolState.Typing) {
                // Si hace clic fuera del editor y panel de opciones, pasar a apuntar
                if (_activeEditor != null && !_activeEditor.IsMouseOver &&
                    (_optionsPanel == null || !_optionsPanel.IsMouseOver)) {
                    if (!string.IsNullOrEmpty(_activeEditor.Text.Trim())) {
                        TransitionToPointing();
                    } else {
                        ResetState();
                    }
                    e.Handled = true;
                }
            } else if (_state == ToolState.Pointing) {
                if (e.LeftButton == MouseButtonState.Pressed) {
                    // Clic izquierdo: consolidar indicación + flecha al objetivo
                    Point target = e.GetPosition(_context.Canvas);
                    FinishAnnotation(target, true);
                    e.Handled = true;
                } else if (e.RightButton == MouseButtonState.Pressed) {
                    // Clic derecho: consolidar solo texto (sin flecha)
                    FinishAnnotation(new Point(0, 0), false);
                    e.Handled = true;
                }
            }
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e) {
            if (_context == null || _state != ToolState.Pointing) return;

            Point currentMouse = e.GetPosition(_context.Canvas);
            UpdatePointingPreview(currentMouse);
            e.Handled = true;
        }

        private void OnCanvasKeyDown(object sender, KeyEventArgs e) {
            if (_state == ToolState.Pointing) {
                if (e.Key == Key.D1 || e.Key == Key.NumPad1 || e.Key == Key.R) {
                    _selectedArrowType = ArrowPointerType.Straight;
                    UpdateHudText();
                    UpdatePointingPreview(Mouse.GetPosition(_context.Canvas));
                    e.Handled = true;
                } else if (e.Key == Key.D2 || e.Key == Key.NumPad2 || e.Key == Key.V) {
                    _selectedArrowType = ArrowPointerType.CurvedVertical;
                    UpdateHudText();
                    UpdatePointingPreview(Mouse.GetPosition(_context.Canvas));
                    e.Handled = true;
                } else if (e.Key == Key.D3 || e.Key == Key.NumPad3 || e.Key == Key.H) {
                    _selectedArrowType = ArrowPointerType.CurvedHorizontal;
                    UpdateHudText();
                    UpdatePointingPreview(Mouse.GetPosition(_context.Canvas));
                    e.Handled = true;
                } else if (e.Key == Key.Space) {
                    // Ciclar tipo
                    int next = ((int)_selectedArrowType + 1) % 3;
                    _selectedArrowType = (ArrowPointerType)next;
                    UpdateHudText();
                    UpdatePointingPreview(Mouse.GetPosition(_context.Canvas));
                    e.Handled = true;
                } else if (e.Key == Key.Escape) {
                    // Salir conservando solo el texto
                    FinishAnnotation(new Point(0, 0), false);
                    e.Handled = true;
                }
            }
        }

        #region Fase 1: Escritura de la Indicación

        private void StartTyping(Point pos) {
            _textOrigin = pos;
            _state = ToolState.Typing;
            double fontSize = CalculateFontSize(_context.CurrentSize);

            // 1. Editor de texto
            _activeEditor = new TextBox {
                Text = "",
                FontSize = fontSize,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(_context.CurrentColor),
                CaretBrush = new SolidColorBrush(_context.CurrentColor),
                Background = new SolidColorBrush(Color.FromArgb(235, 20, 24, 30)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(220, 0, 162, 255)),
                BorderThickness = new Thickness(1.8),
                Padding = new Thickness(8, 6, 8, 6),
                AcceptsReturn = true,
                MinWidth = 160,
                MaxWidth = 700
            };

            _activeEditor.KeyDown += (s, e) => {
                if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift) {
                    e.Handled = true;
                    if (!string.IsNullOrEmpty(_activeEditor.Text.Trim())) {
                        TransitionToPointing();
                    } else {
                        ResetState();
                    }
                } else if (e.Key == Key.Escape) {
                    e.Handled = true;
                    ResetState();
                }
            };

            // 2. Mini-barra flotante de opciones
            _optionsPanel = CreateOptionsPanel();

            InkCanvas.SetLeft(_activeEditor, pos.X);
            InkCanvas.SetTop(_activeEditor, pos.Y);

            InkCanvas.SetLeft(_optionsPanel, pos.X);
            InkCanvas.SetTop(_optionsPanel, pos.Y - 38);

            _context.Canvas.Children.Add(_activeEditor);
            _context.Canvas.Children.Add(_optionsPanel);

            _activeEditor.Loaded += (s, e) => {
                _activeEditor.Focus();
                Keyboard.Focus(_activeEditor);
            };

            _activeEditor.Focus();
            Keyboard.Focus(_activeEditor);
        }

        private Border CreateOptionsPanel() {
            Border border = new Border {
                Background = new SolidColorBrush(Color.FromArgb(245, 25, 28, 34)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(180, 0, 162, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(4, 2, 4, 2)
            };

            StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal };

            TextBlock title = new TextBlock {
                Text = "Flecha:",
                Foreground = Brushes.Silver,
                FontSize = 10.5,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 6, 0)
            };
            stack.Children.Add(title);

            // Botones de tipo
            Button btnStraight = CreateSmallButton("──▶ Recta", _selectedArrowType == ArrowPointerType.Straight);
            Button btnCurvedV = CreateSmallButton("╰▲ Curva Vert", _selectedArrowType == ArrowPointerType.CurvedVertical);
            Button btnCurvedH = CreateSmallButton("╰▶ Curva Horiz", _selectedArrowType == ArrowPointerType.CurvedHorizontal);

            Action refreshButtonStyles = () => {
                HighlightButton(btnStraight, _selectedArrowType == ArrowPointerType.Straight);
                HighlightButton(btnCurvedV, _selectedArrowType == ArrowPointerType.CurvedVertical);
                HighlightButton(btnCurvedH, _selectedArrowType == ArrowPointerType.CurvedHorizontal);
            };

            btnStraight.Click += (s, e) => {
                _selectedArrowType = ArrowPointerType.Straight;
                refreshButtonStyles();
                if (_activeEditor != null) _activeEditor.Focus();
            };

            btnCurvedV.Click += (s, e) => {
                _selectedArrowType = ArrowPointerType.CurvedVertical;
                refreshButtonStyles();
                if (_activeEditor != null) _activeEditor.Focus();
            };

            btnCurvedH.Click += (s, e) => {
                _selectedArrowType = ArrowPointerType.CurvedHorizontal;
                refreshButtonStyles();
                if (_activeEditor != null) _activeEditor.Focus();
            };

            stack.Children.Add(btnStraight);
            stack.Children.Add(btnCurvedV);
            stack.Children.Add(btnCurvedH);

            // Botón de continuar a apuntar
            Button btnProceed = new Button {
                Content = "🎯 Apuntar [Enter]",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(220, 0, 122, 204)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(6, 0, 2, 0),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };
            btnProceed.Click += (s, e) => {
                if (_activeEditor != null && !string.IsNullOrEmpty(_activeEditor.Text.Trim())) {
                    TransitionToPointing();
                }
            };
            stack.Children.Add(btnProceed);

            border.Child = stack;
            return border;
        }

        private Button CreateSmallButton(string text, bool isActive) {
            Button btn = new Button {
                Content = text,
                FontSize = 10,
                Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(1, 0, 1, 0),
                Cursor = Cursors.Hand
            };
            HighlightButton(btn, isActive);
            return btn;
        }

        private void HighlightButton(Button btn, bool isActive) {
            if (isActive) {
                btn.Background = new SolidColorBrush(Color.FromArgb(220, 0, 122, 204));
                btn.Foreground = Brushes.White;
                btn.FontWeight = FontWeights.Bold;
                btn.BorderThickness = new Thickness(1);
                btn.BorderBrush = Brushes.SkyBlue;
            } else {
                btn.Background = new SolidColorBrush(Color.FromArgb(160, 45, 48, 55));
                btn.Foreground = Brushes.LightGray;
                btn.FontWeight = FontWeights.Normal;
                btn.BorderThickness = new Thickness(1);
                btn.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
            }
        }

        #endregion

        #region Fase 2: Apuntado Interactivo con Flecha

        private void TransitionToPointing() {
            if (_activeEditor == null || string.IsNullOrEmpty(_activeEditor.Text.Trim())) {
                ResetState();
                return;
            }

            _confirmedText = _activeEditor.Text.Trim();
            _confirmedFontSize = _activeEditor.FontSize;

            // Medir dimensiones del texto
            Typeface typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
#pragma warning disable 618
            FormattedText ft = new FormattedText(
                _confirmedText,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                _confirmedFontSize,
                Brushes.Black
            );
#pragma warning restore 618

            double boxPadding = 8.0;
            double boxWidth = ft.Width + (boxPadding * 2.0);
            double boxHeight = ft.Height + (boxPadding * 2.0);
            _textBounds = new Rect(_textOrigin.X, _textOrigin.Y, boxWidth, boxHeight);

            // Retirar el editor de la pantalla
            RemoveEditorAndPanels();

            _state = ToolState.Pointing;
            _context.Canvas.Cursor = Cursors.Cross;

            // Crear el HUD de instrucciones y control
            CreatePointingHud();

            // Dibujar la previsualización inicial en la posición del ratón
            Point mousePos = Mouse.GetPosition(_context.Canvas);
            UpdatePointingPreview(mousePos);

            _context.Canvas.Focus();
        }

        private void CreatePointingHud() {
            _pointingHud = new Border {
                Background = new SolidColorBrush(Color.FromArgb(240, 20, 22, 28)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(200, 255, 215, 0)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 5, 8, 5)
            };

            StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal };

            _hudStatusText = new TextBlock {
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            UpdateHudText();
            stack.Children.Add(_hudStatusText);

            // Botones interactivos en el propio HUD para cambiar de tipo al hacer clic
            Button b1 = CreateHudArrowButton("[1] Recta", ArrowPointerType.Straight);
            Button b2 = CreateHudArrowButton("[2] Curva Vert", ArrowPointerType.CurvedVertical);
            Button b3 = CreateHudArrowButton("[3] Curva Horiz", ArrowPointerType.CurvedHorizontal);

            stack.Children.Add(b1);
            stack.Children.Add(b2);
            stack.Children.Add(b3);

            Button btnNoArrow = new Button {
                Content = "✕ Sin Flecha [Esc]",
                Foreground = Brushes.LightPink,
                Background = new SolidColorBrush(Color.FromArgb(140, 60, 30, 30)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 100, 100)),
                FontSize = 9.5,
                Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(6, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnNoArrow.Click += (s, e) => {
                FinishAnnotation(new Point(0, 0), false);
            };
            stack.Children.Add(btnNoArrow);

            _pointingHud.Child = stack;

            double hudY = _textBounds.Top - 34.0;
            if (hudY < 10) hudY = _textBounds.Bottom + 10.0;
            InkCanvas.SetLeft(_pointingHud, Math.Max(10.0, _textBounds.Left));
            InkCanvas.SetTop(_pointingHud, hudY);

            _context.Canvas.Children.Add(_pointingHud);
        }

        private Button CreateHudArrowButton(string label, ArrowPointerType type) {
            Button btn = new Button {
                Content = label,
                FontSize = 9.5,
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(3, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            Action update = () => {
                if (_selectedArrowType == type) {
                    btn.Background = new SolidColorBrush(Color.FromArgb(220, 0, 122, 204));
                    btn.Foreground = Brushes.White;
                    btn.FontWeight = FontWeights.Bold;
                    btn.BorderBrush = Brushes.Cyan;
                } else {
                    btn.Background = new SolidColorBrush(Color.FromArgb(140, 45, 50, 60));
                    btn.Foreground = Brushes.Silver;
                    btn.FontWeight = FontWeights.Normal;
                    btn.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                }
            };
            update();

            btn.Click += (s, e) => {
                _selectedArrowType = type;
                UpdateHudText();
                UpdatePointingPreview(Mouse.GetPosition(_context.Canvas));
                _context.Canvas.Focus();
            };

            return btn;
        }

        private void UpdateHudText() {
            if (_hudStatusText == null) return;
            string typeName = "Recta";
            if (_selectedArrowType == ArrowPointerType.CurvedVertical) typeName = "Curva Vertical";
            else if (_selectedArrowType == ArrowPointerType.CurvedHorizontal) typeName = "Curva Horizontal";

            _hudStatusText.Text = string.Format("🎯 Apunta y haz clic | Flecha: {0} ", typeName);
        }

        private void UpdateHudStyle() {
            // Refrescar si es necesario
        }

        private void UpdatePointingPreview(Point target) {
            CleanPreview();
            if (_context == null || _context.Canvas == null) return;

            // Generar lote de previsualización
            StrokeCollection batch = GenerateStrokes(target, true);
            foreach (Stroke s in batch) {
                _context.Canvas.Strokes.Add(s);
                _previewStrokes.Add(s);
            }
        }

        private void CleanPreview() {
            if (_context != null && _context.Canvas != null && _previewStrokes.Count > 0) {
                foreach (Stroke s in _previewStrokes) {
                    _context.Canvas.Strokes.Remove(s);
                }
                _previewStrokes.Clear();
            }
        }

        #endregion

        #region Fase 3: Generación Geométrica Vectorial y Consolidación

        private void FinishAnnotation(Point target, bool includeArrow) {
            CleanPreview();
            RemoveEditorAndPanels();

            if (string.IsNullOrEmpty(_confirmedText)) {
                ResetState();
                return;
            }

            // Crear el lote final atómico
            StrokeCollection finalBatch = GenerateStrokes(target, includeArrow);
            if (finalBatch != null && finalBatch.Count > 0) {
                _context.AddStrokes(finalBatch);
            }

            ResetState();
        }

        private void CommitOrCancel(bool commit) {
            if (_state == ToolState.Typing && _activeEditor != null && !string.IsNullOrEmpty(_activeEditor.Text.Trim())) {
                _confirmedText = _activeEditor.Text.Trim();
                _confirmedFontSize = _activeEditor.FontSize;
                Typeface typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
#pragma warning disable 618
                FormattedText ft = new FormattedText(_confirmedText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, _confirmedFontSize, Brushes.Black);
#pragma warning restore 618
                double pad = 8.0;
                _textBounds = new Rect(_textOrigin.X, _textOrigin.Y, ft.Width + pad * 2, ft.Height + pad * 2);
                FinishAnnotation(new Point(0, 0), false);
            } else if (_state == ToolState.Pointing && !string.IsNullOrEmpty(_confirmedText)) {
                FinishAnnotation(new Point(0, 0), false);
            } else {
                ResetState();
            }
        }

        private StrokeCollection GenerateStrokes(Point target, bool includeArrow) {
            StrokeCollection batch = new StrokeCollection();
            double thickness = Math.Max(1.5, _context.CurrentSize);
            Color mainColor = _context.CurrentColor;

            // 1. Marco rectangular con esquinas redondeadas para la indicación
            Stroke frameStroke = CreateRoundedRectStroke(_textBounds, mainColor, thickness);
            if (frameStroke != null) {
                batch.Add(frameStroke);
            }

            // 2. Trazos de texto vectorizado
            StrokeCollection textStrokes = CreateTextStrokes(_confirmedText, _textBounds, _confirmedFontSize, mainColor, thickness);
            if (textStrokes != null) {
                foreach (Stroke ts in textStrokes) batch.Add(ts);
            }

            // 3. Flecha conectora hacia el objetivo
            if (includeArrow) {
                Point anchor = GetAnchorPoint(_textBounds, target);
                double dist = Math.Sqrt(Math.Pow(target.X - anchor.X, 2) + Math.Pow(target.Y - anchor.Y, 2));

                if (dist >= 10.0) {
                    Stroke arrowStroke = CreateArrowStroke(anchor, target, _selectedArrowType, mainColor, thickness);
                    if (arrowStroke != null) {
                        batch.Add(arrowStroke);
                    }
                }
            }

            return batch;
        }

        private Stroke CreateRoundedRectStroke(Rect rect, Color color, double thickness) {
            double radius = 5.0;
            StylusPointCollection pts = new StylusPointCollection();

            pts.Add(new StylusPoint(rect.Left + radius, rect.Top));
            pts.Add(new StylusPoint(rect.Right - radius, rect.Top));
            pts.Add(new StylusPoint(rect.Right, rect.Top + radius));
            pts.Add(new StylusPoint(rect.Right, rect.Bottom - radius));
            pts.Add(new StylusPoint(rect.Right - radius, rect.Bottom));
            pts.Add(new StylusPoint(rect.Left + radius, rect.Bottom));
            pts.Add(new StylusPoint(rect.Left, rect.Bottom - radius));
            pts.Add(new StylusPoint(rect.Left, rect.Top + radius));
            pts.Add(new StylusPoint(rect.Left + radius, rect.Top));

            DrawingAttributes attrs = new DrawingAttributes {
                Color = color,
                Width = thickness,
                Height = thickness,
                FitToCurve = false,
                IsHighlighter = false
            };

            return new Stroke(pts, attrs);
        }

        private StrokeCollection CreateTextStrokes(string text, Rect bounds, double fontSize, Color color, double thickness) {
            StrokeCollection strokes = new StrokeCollection();
            Typeface typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

#pragma warning disable 618
            FormattedText ft = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black
            );
#pragma warning restore 618

            // Centrar texto dentro del marco
            double textX = bounds.Left + (bounds.Width - ft.Width) / 2.0;
            double textY = bounds.Top + (bounds.Height - ft.Height) / 2.0;

            Geometry textGeom = ft.BuildGeometry(new Point(textX, textY));
            PathGeometry flatGeom = textGeom.GetFlattenedPathGeometry();

            double strokeThickness = Math.Max(1.4, thickness * 0.45);
            DrawingAttributes textAttrs = new DrawingAttributes {
                Color = color,
                Width = strokeThickness,
                Height = strokeThickness,
                FitToCurve = false,
                IsHighlighter = false
            };

            foreach (PathFigure fig in flatGeom.Figures) {
                StylusPointCollection textPts = new StylusPointCollection();
                textPts.Add(new StylusPoint(fig.StartPoint.X, fig.StartPoint.Y));
                foreach (PathSegment seg in fig.Segments) {
                    PolyLineSegment poly = seg as PolyLineSegment;
                    if (poly != null) {
                        foreach (Point p in poly.Points) textPts.Add(new StylusPoint(p.X, p.Y));
                    } else {
                        LineSegment line = seg as LineSegment;
                        if (line != null) textPts.Add(new StylusPoint(line.Point.X, line.Point.Y));
                    }
                }
                strokes.Add(new Stroke(textPts, textAttrs));
            }

            return strokes;
        }

        /// <summary>
        /// Calcula el punto exacto de anclaje de salida sobre el contorno del marco de la indicación
        /// en dirección al punto de destino.
        /// </summary>
        private Point GetAnchorPoint(Rect rect, Point target) {
            Point center = new Point(rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0);
            double dx = target.X - center.X;
            double dy = target.Y - center.Y;

            if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001) {
                return new Point(rect.Right, center.Y);
            }

            double halfW = rect.Width / 2.0;
            double halfH = rect.Height / 2.0;

            double tanTarget = Math.Abs(dy / (dx != 0 ? dx : 0.0001));
            double tanBox = halfH / halfW;

            if (tanTarget <= tanBox) {
                // Intersecta con lateral izquierdo o derecho
                if (dx >= 0) {
                    double y = center.Y + halfW * (dy / dx);
                    return new Point(rect.Right, Clamp(y, rect.Top + 3, rect.Bottom - 3));
                } else {
                    double y = center.Y - halfW * (dy / dx);
                    return new Point(rect.Left, Clamp(y, rect.Top + 3, rect.Bottom - 3));
                }
            } else {
                // Intersecta con borde superior o inferior
                if (dy >= 0) {
                    double x = center.X + halfH * (dx / dy);
                    return new Point(Clamp(x, rect.Left + 3, rect.Right - 3), rect.Bottom);
                } else {
                    double x = center.X - halfH * (dx / dy);
                    return new Point(Clamp(x, rect.Left + 3, rect.Right - 3), rect.Top);
                }
            }
        }

        private double Clamp(double val, double min, double max) {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        /// <summary>
        /// Construye el trazo completo de la flecha con su cuerpo (recto o curva Bézier) y su punta orientada.
        /// </summary>
        private Stroke CreateArrowStroke(Point p0, Point p3, ArrowPointerType arrowType, Color color, double thickness) {
            StylusPointCollection points = new StylusPointCollection();
            double arrowAngle = 0.0;

            if (arrowType == ArrowPointerType.Straight) {
                // 1. Flecha Recta
                points.Add(new StylusPoint(p0.X, p0.Y));
                points.Add(new StylusPoint(p3.X, p3.Y));
                arrowAngle = Math.Atan2(p3.Y - p0.Y, p3.X - p0.X);
            } else if (arrowType == ArrowPointerType.CurvedVertical) {
                // 2. Curva con entrada estrictamente Vertical al objetivo
                double vertSign = (p3.Y >= p0.Y) ? 1.0 : -1.0;
                double lead = Math.Max(25.0, Math.Abs(p3.Y - p0.Y) * 0.55);

                Point p1 = new Point(p0.X + (p3.X - p0.X) * 0.5, p0.Y);
                Point p2 = new Point(p3.X, p3.Y - vertSign * lead);

                List<Point> curve = GenerateCubicBezier(p0, p1, p2, p3, 28);
                foreach (Point pt in curve) {
                    points.Add(new StylusPoint(pt.X, pt.Y));
                }

                // La punta apunta verticalmente pura (hacia abajo o hacia arriba)
                arrowAngle = (vertSign > 0) ? (Math.PI / 2.0) : (-Math.PI / 2.0);
            } else {
                // 3. Curva con entrada estrictamente Horizontal al objetivo
                double horizSign = (p3.X >= p0.X) ? 1.0 : -1.0;
                double lead = Math.Max(25.0, Math.Abs(p3.X - p0.X) * 0.55);

                Point p1 = new Point(p0.X, p0.Y + (p3.Y - p0.Y) * 0.5);
                Point p2 = new Point(p3.X - horizSign * lead, p3.Y);

                List<Point> curve = GenerateCubicBezier(p0, p1, p2, p3, 28);
                foreach (Point pt in curve) {
                    points.Add(new StylusPoint(pt.X, pt.Y));
                }

                // La punta apunta horizontalmente pura (hacia la derecha o hacia la izquierda)
                arrowAngle = (horizSign > 0) ? 0.0 : Math.PI;
            }

            // Construir alas de la cabeza de la flecha en p3
            double headLength = Math.Max(13.0, thickness * 3.6);
            double headAngle = Math.PI / 6.0; // 30 grados

            Point wing1 = new Point(
                p3.X - headLength * Math.Cos(arrowAngle - headAngle),
                p3.Y - headLength * Math.Sin(arrowAngle - headAngle)
            );
            Point wing2 = new Point(
                p3.X - headLength * Math.Cos(arrowAngle + headAngle),
                p3.Y - headLength * Math.Sin(arrowAngle + headAngle)
            );

            // Continuación de trazo: Destino -> Ala 1 -> Destino -> Ala 2
            points.Add(new StylusPoint(wing1.X, wing1.Y));
            points.Add(new StylusPoint(p3.X, p3.Y));
            points.Add(new StylusPoint(wing2.X, wing2.Y));

            DrawingAttributes attrs = new DrawingAttributes {
                Color = color,
                Width = thickness,
                Height = thickness,
                FitToCurve = false,
                IsHighlighter = false
            };

            return new Stroke(points, attrs);
        }

        private List<Point> GenerateCubicBezier(Point p0, Point p1, Point p2, Point p3, int steps) {
            List<Point> points = new List<Point>(steps + 1);
            for (int i = 0; i <= steps; i++) {
                double t = (double)i / steps;
                double u = 1.0 - t;
                double tt = t * t;
                double uu = u * u;
                double uuu = uu * u;
                double ttt = tt * t;

                double x = uuu * p0.X + 3.0 * uu * t * p1.X + 3.0 * u * tt * p2.X + ttt * p3.X;
                double y = uuu * p0.Y + 3.0 * uu * t * p1.Y + 3.0 * u * tt * p2.Y + ttt * p3.Y;
                points.Add(new Point(x, y));
            }
            return points;
        }

        private void RemoveEditorAndPanels() {
            if (_context == null || _context.Canvas == null) return;

            if (_activeEditor != null && _context.Canvas.Children.Contains(_activeEditor)) {
                _context.Canvas.Children.Remove(_activeEditor);
            }
            _activeEditor = null;

            if (_optionsPanel != null && _context.Canvas.Children.Contains(_optionsPanel)) {
                _context.Canvas.Children.Remove(_optionsPanel);
            }
            _optionsPanel = null;

            if (_pointingHud != null && _context.Canvas.Children.Contains(_pointingHud)) {
                _context.Canvas.Children.Remove(_pointingHud);
            }
            _pointingHud = null;
        }

        #endregion
    }
}
