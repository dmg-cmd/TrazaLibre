using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Trazalibre.Core;

namespace Trazalibre.Tools {
    /// <summary>
    /// Herramienta interactiva de Texto en Pantalla ((🔤).
    /// Permite hacer clic en cualquier punto del escritorio, escribir con el teclado,
    /// y plasmar el texto como trazo vectorial permanente con soporte para Deshacer (Undo).
    /// </summary>
    public class TextTool : ITool {
        public string Id { get { return "text"; } }
        public string DisplayName { get { return "Texto"; } }
        public string Icon { get { return "🔤"; } }

        private IAnnotationContext _context;
        private MouseButtonEventHandler _mouseHandler;
        private TextBox _activeEditor = null;
        private Point _editorOrigin;

        public void Activate(IAnnotationContext context) {
            _context = context;
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.None;
            context.Canvas.Cursor = Cursors.IBeam;

            if (_mouseHandler == null) {
                _mouseHandler = OnCanvasMouseDown;
            }
            context.Canvas.PreviewMouseDown -= _mouseHandler;
            context.Canvas.PreviewMouseDown += _mouseHandler;
        }

        public void Deactivate(IAnnotationContext context) {
            CommitActiveText();
            if (_mouseHandler != null && context != null && context.Canvas != null) {
                context.Canvas.PreviewMouseDown -= _mouseHandler;
            }
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
            return Math.Max(14.0, size * 4.5);
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed || _context == null) return;

            // Si se hace clic fuera del editor actual, consolidar el texto anterior
            if (_activeEditor != null) {
                if (_activeEditor.IsMouseOver) return; // Si hace clic dentro del editor, continuar escribiendo
                CommitActiveText();
            }

            Point clickPos = e.GetPosition(_context.Canvas);
            CreateTextEditor(clickPos);
            e.Handled = true;
        }

        private void CreateTextEditor(Point pos) {
            _editorOrigin = pos;
            double fontSize = CalculateFontSize(_context.CurrentSize);

            _activeEditor = new TextBox {
                Text = "",
                FontSize = fontSize,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(_context.CurrentColor),
                CaretBrush = new SolidColorBrush(_context.CurrentColor),
                Background = new SolidColorBrush(Color.FromArgb(220, 20, 22, 25)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(200, 0, 122, 204)),
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(6, 4, 6, 4),
                AcceptsReturn = true,
                MinWidth = 140,
                MaxWidth = 800
            };

            // Manejo de confirmación con Enter y cancelación con Escape
            _activeEditor.KeyDown += (s, e) => {
                if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift) {
                    CommitActiveText();
                    e.Handled = true;
                } else if (e.Key == Key.Escape) {
                    CancelActiveText();
                    e.Handled = true;
                }
            };

            InkCanvas.SetLeft(_activeEditor, pos.X);
            InkCanvas.SetTop(_activeEditor, pos.Y);

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
            Point origin = _editorOrigin;
            _activeEditor = null;

            if (_context.Canvas.Children.Contains(editor)) {
                _context.Canvas.Children.Remove(editor);
            }

            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(content.Trim())) return;

            double fontSize = editor.FontSize;
            Typeface typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

#pragma warning disable 618
            FormattedText ft = new FormattedText(
                content,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black
            );
#pragma warning restore 618

            Geometry textGeom = ft.BuildGeometry(new Point(origin.X + 6, origin.Y + 4));
            PathGeometry flatGeom = textGeom.GetFlattenedPathGeometry();

            double strokeThickness = Math.Max(1.5, _context.CurrentSize * 0.4);
            DrawingAttributes textAttrs = new DrawingAttributes {
                Color = _context.CurrentColor,
                Width = strokeThickness,
                Height = strokeThickness,
                FitToCurve = false,
                IsHighlighter = false
            };

            StrokeCollection textStrokes = new StrokeCollection();
            foreach (PathFigure fig in flatGeom.Figures) {
                StylusPointCollection textPts = new StylusPointCollection();
                textPts.Add(new StylusPoint(fig.StartPoint.X, fig.StartPoint.Y));
                foreach (PathSegment seg in fig.Segments) {
                    PolyLineSegment poly = seg as PolyLineSegment;
                    if (poly != null) {
                        foreach (Point p in poly.Points) {
                            textPts.Add(new StylusPoint(p.X, p.Y));
                        }
                    } else {
                        LineSegment line = seg as LineSegment;
                        if (line != null) {
                            textPts.Add(new StylusPoint(line.Point.X, line.Point.Y));
                        }
                    }
                }
                textStrokes.Add(new Stroke(textPts, textAttrs));
            }

            if (textStrokes.Count > 0) {
                _context.AddStrokes(textStrokes);
            }
        }

        private void CancelActiveText() {
            if (_activeEditor != null && _context != null) {
                if (_context.Canvas.Children.Contains(_activeEditor)) {
                    _context.Canvas.Children.Remove(_activeEditor);
                }
                _activeEditor = null;
            }
        }
    }
}

