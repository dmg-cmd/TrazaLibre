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
    /// Clase base abstracta para herramientas de Globos de Texto / Llamadas de atención (Callouts) con flecha apuntadora.
    /// Soporta arrastre para definir el tamaño y orientación del globo, y despliega automáticamente un editor
    /// de texto interactivo para escribir el enunciado dentro del globo con soporte atómico para Deshacer (Undo).
    /// </summary>
    public abstract class BaseCalloutTool : ITool {
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public abstract string Icon { get; }

        private IAnnotationContext _context;
        private MouseButtonEventHandler _mouseDownHandler;
        private MouseEventHandler _mouseMoveHandler;
        private MouseButtonEventHandler _mouseUpHandler;

        private Point _startPoint;
        private bool _isDrawing = false;
        private Stroke _previewStroke = null;

        private TextBox _activeEditor = null;
        private Stroke _currentGlobeStroke = null;
        private Rect _editorBodyRect;

        /// <summary>
        /// Genera la colección de puntos del contorno cerrado del globo con su flecha conectada.
        /// </summary>
        protected abstract IEnumerable<Point> GeneratePoints(Point start, Point current);

        /// <summary>
        /// Calcula el área interna del cuerpo del globo destinada a albergar el texto.
        /// </summary>
        public abstract Rect GetBodyRect(Point start, Point current);

        public void Activate(IAnnotationContext context) {
            _context = context;
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.None;
            context.Canvas.Cursor = Cursors.Cross;

            if (_mouseDownHandler == null) {
                _mouseDownHandler = OnCanvasMouseDown;
                _mouseMoveHandler = OnCanvasMouseMove;
                _mouseUpHandler = OnCanvasMouseUp;
            }

            context.Canvas.PreviewMouseDown -= _mouseDownHandler;
            context.Canvas.PreviewMouseMove -= _mouseMoveHandler;
            context.Canvas.PreviewMouseUp -= _mouseUpHandler;

            context.Canvas.PreviewMouseDown += _mouseDownHandler;
            context.Canvas.PreviewMouseMove += _mouseMoveHandler;
            context.Canvas.PreviewMouseUp += _mouseUpHandler;
        }

        public void Deactivate(IAnnotationContext context) {
            CommitActiveText();
            CleanPreview();

            if (context != null && context.Canvas != null && _mouseDownHandler != null) {
                context.Canvas.PreviewMouseDown -= _mouseDownHandler;
                context.Canvas.PreviewMouseMove -= _mouseMoveHandler;
                context.Canvas.PreviewMouseUp -= _mouseUpHandler;
            }

            _isDrawing = false;
            _context = null;
        }

        public void OnColorChanged(IAnnotationContext context, Color newColor) {
            if (_activeEditor != null) {
                _activeEditor.Foreground = new SolidColorBrush(newColor);
                _activeEditor.CaretBrush = new SolidColorBrush(newColor);
            }
        }

        public void OnSizeChanged(IAnnotationContext context, double newSize) {
            if (_activeEditor != null) {
                _activeEditor.FontSize = CalculateFontSize(newSize);
            }
        }

        private double CalculateFontSize(double size) {
            return Math.Max(13.0, size * 3.8);
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed || _context == null) return;

            // Si hay un editor abierto y se hace clic fuera de él, confirmar el texto pendiente
            if (_activeEditor != null) {
                if (_activeEditor.IsMouseOver) return;
                CommitActiveText();
            }

            _startPoint = e.GetPosition(_context.Canvas);
            _isDrawing = true;
            _previewStroke = null;
            e.Handled = true;
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e) {
            if (!_isDrawing || e.LeftButton != MouseButtonState.Pressed || _context == null) return;

            Point currentPoint = e.GetPosition(_context.Canvas);
            UpdatePreview(_startPoint, currentPoint);
            e.Handled = true;
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e) {
            if (!_isDrawing || _context == null) return;
            _isDrawing = false;

            Point endPoint = e.GetPosition(_context.Canvas);
            CleanPreview();

            // Ignorar clics menores a 6 píxeles sin intención de arrastre
            double dx = endPoint.X - _startPoint.X;
            double dy = endPoint.Y - _startPoint.Y;
            if (Math.Sqrt(dx * dx + dy * dy) < 6.0) return;

            Stroke globeStroke = CreateStroke(_startPoint, endPoint);
            if (globeStroke != null) {
                // Mostrar temporalmente el trazo del globo en el lienzo
                _context.Canvas.Strokes.Add(globeStroke);
                _currentGlobeStroke = globeStroke;

                // Abrir inmediatamente la caja de texto dentro del cuerpo del globo
                Rect bodyRect = GetBodyRect(_startPoint, endPoint);
                OpenTextEditor(bodyRect);
            }

            e.Handled = true;
        }

        private void OpenTextEditor(Rect bodyRect) {
            if (_context == null || _context.Canvas == null) return;

            _editorBodyRect = bodyRect;
            double fontSize = CalculateFontSize(_context.CurrentSize);

            double editorWidth = Math.Max(90.0, bodyRect.Width - 16.0);
            double editorHeight = Math.Max(35.0, bodyRect.Height - 12.0);

            _activeEditor = new TextBox {
                Text = "",
                FontSize = fontSize,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(_context.CurrentColor),
                CaretBrush = new SolidColorBrush(_context.CurrentColor),
                Background = new SolidColorBrush(Color.FromArgb(210, 24, 26, 32)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(180, 0, 162, 255)),
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(6, 4, 6, 4),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Width = editorWidth,
                Height = editorHeight,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            _activeEditor.KeyDown += (s, e) => {
                if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift) {
                    CommitActiveText();
                    e.Handled = true;
                } else if (e.Key == Key.Escape) {
                    CancelActiveText();
                    e.Handled = true;
                }
            };

            _activeEditor.LostFocus += (s, e) => {
                CommitActiveText();
            };

            InkCanvas.SetLeft(_activeEditor, bodyRect.Left + (bodyRect.Width - editorWidth) / 2.0);
            InkCanvas.SetTop(_activeEditor, bodyRect.Top + (bodyRect.Height - editorHeight) / 2.0);

            _context.Canvas.Children.Add(_activeEditor);

            _activeEditor.Loaded += (s, e) => {
                _activeEditor.Focus();
                Keyboard.Focus(_activeEditor);
            };

            _activeEditor.Focus();
            Keyboard.Focus(_activeEditor);
        }

        private void CommitActiveText() {
            if (_activeEditor == null || _context == null) return;

            string content = _activeEditor.Text;
            TextBox editor = _activeEditor;
            Stroke globe = _currentGlobeStroke;
            Rect bodyRect = _editorBodyRect;

            _activeEditor = null;
            _currentGlobeStroke = null;

            if (_context.Canvas.Children.Contains(editor)) {
                _context.Canvas.Children.Remove(editor);
            }

            if (globe != null && _context.Canvas.Strokes.Contains(globe)) {
                _context.Canvas.Strokes.Remove(globe);
            }

            if (globe == null) return;

            StrokeCollection combinedBatch = new StrokeCollection();
            combinedBatch.Add(globe);

            if (!string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(content.Trim())) {
                double fontSize = editor.FontSize;
                Typeface typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

#pragma warning disable 618
                FormattedText ft = new FormattedText(
                    content.Trim(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    Brushes.Black
                );
#pragma warning restore 618

                ft.MaxTextWidth = Math.Max(50.0, bodyRect.Width - 20.0);
                ft.MaxTextHeight = Math.Max(25.0, bodyRect.Height - 12.0);

                double textX = bodyRect.Left + (bodyRect.Width - ft.Width) / 2.0;
                double textY = bodyRect.Top + (bodyRect.Height - ft.Height) / 2.0;
                if (textX < bodyRect.Left + 6) textX = bodyRect.Left + 6;
                if (textY < bodyRect.Top + 6) textY = bodyRect.Top + 6;

                Geometry textGeom = ft.BuildGeometry(new Point(textX, textY));
                PathGeometry flatGeom = textGeom.GetFlattenedPathGeometry();

                double strokeThickness = Math.Max(1.5, _context.CurrentSize * 0.4);
                DrawingAttributes textAttrs = new DrawingAttributes {
                    Color = _context.CurrentColor,
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
                    combinedBatch.Add(new Stroke(textPts, textAttrs));
                }
            }

            // Registrar globo y texto juntos atómicamente en la pila de Deshacer
            _context.AddStrokes(combinedBatch);
        }

        private void CancelActiveText() {
            if (_activeEditor == null || _context == null) return;

            TextBox editor = _activeEditor;
            Stroke globe = _currentGlobeStroke;
            _activeEditor = null;
            _currentGlobeStroke = null;

            if (_context.Canvas.Children.Contains(editor)) {
                _context.Canvas.Children.Remove(editor);
            }

            // Si cancela la escritura, conservar el trazo del globo
            if (globe != null && !_context.Canvas.Strokes.Contains(globe)) {
                _context.AddStroke(globe);
            }
        }

        private void UpdatePreview(Point start, Point current) {
            CleanPreview();
            _previewStroke = CreateStroke(start, current);
            if (_previewStroke != null && _context != null && _context.Canvas != null) {
                _context.Canvas.Strokes.Add(_previewStroke);
            }
        }

        private void CleanPreview() {
            if (_previewStroke != null && _context != null && _context.Canvas != null) {
                _context.Canvas.Strokes.Remove(_previewStroke);
                _previewStroke = null;
            }
        }

        private Stroke CreateStroke(Point start, Point current) {
            StylusPointCollection points = new StylusPointCollection();
            foreach (Point p in GeneratePoints(start, current)) {
                points.Add(new StylusPoint(p.X, p.Y));
            }

            if (points.Count < 3) return null;

            double thickness = Math.Max(1.5, _context.CurrentSize);
            DrawingAttributes attrs = new DrawingAttributes {
                Color = _context.CurrentColor,
                Width = thickness,
                Height = thickness,
                FitToCurve = false,
                IsHighlighter = false
            };

            return new Stroke(points, attrs);
        }
    }
}

