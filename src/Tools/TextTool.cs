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
    /// Herramienta interactiva de Texto en Pantalla (🔤).
    /// Permite hacer clic en cualquier punto del escritorio, escribir con el teclado,
    /// elegir fuente, tamaño y estilo en tiempo real, y plasmar el texto como trazo vectorial
    /// permanente con soporte para Deshacer (Undo).
    /// </summary>
    public class TextTool : ITool {
        public string Id { get { return "text"; } }
        public string DisplayName { get { return "Texto"; } }
        public string Icon { get { return "🔤"; } }

        private IAnnotationContext _context;
        private MouseButtonEventHandler _mouseHandler;
        private EventHandler _styleChangedHandler;
        private TextBox _activeEditor = null;
        private Border _formatPanel = null;
        private Point _editorOrigin;

        public void Activate(IAnnotationContext context) {
            _context = context;
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.None;
            context.Canvas.Cursor = Cursors.IBeam;

            if (_mouseHandler == null) {
                _mouseHandler = OnCanvasMouseDown;
                _styleChangedHandler = OnTextStyleChanged;
            }
            context.Canvas.PreviewMouseDown -= _mouseHandler;
            context.Canvas.PreviewMouseDown += _mouseHandler;
            TextStyleService.StyleChanged -= _styleChangedHandler;
            TextStyleService.StyleChanged += _styleChangedHandler;
        }

        public void Deactivate(IAnnotationContext context) {
            CommitActiveText();
            if (_mouseHandler != null && context != null && context.Canvas != null) {
                context.Canvas.PreviewMouseDown -= _mouseHandler;
            }
            if (_styleChangedHandler != null) {
                TextStyleService.StyleChanged -= _styleChangedHandler;
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
            // El tamaño de texto se controla mediante TextStyleService.FontSize
        }

        private void OnTextStyleChanged(object sender, EventArgs e) {
            if (_activeEditor != null) {
                TextStyleService.ApplyToEditor(_activeEditor);
            }
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed || _context == null) return;

            // Si se hace clic dentro del editor o de la barra de formato, continuar editando
            if (_activeEditor != null) {
                if (_activeEditor.IsMouseOver || (_formatPanel != null && _formatPanel.IsMouseOver)) return;
                CommitActiveText();
            }

            Point clickPos = e.GetPosition(_context.Canvas);
            CreateTextEditor(clickPos);
            e.Handled = true;
        }

        private void CreateTextEditor(Point pos) {
            _editorOrigin = pos;

            _activeEditor = new TextBox {
                Text = "",
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
            TextStyleService.ApplyToEditor(_activeEditor);

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

            // Mini-barra flotante de formato tipográfico
            _formatPanel = CreateFormatPanel();

            InkCanvas.SetLeft(_activeEditor, pos.X);
            InkCanvas.SetTop(_activeEditor, pos.Y);

            double panelTop = Math.Max(6.0, pos.Y - 36.0);
            InkCanvas.SetLeft(_formatPanel, pos.X);
            InkCanvas.SetTop(_formatPanel, panelTop);

            _context.Canvas.Children.Add(_activeEditor);
            _context.Canvas.Children.Add(_formatPanel);

            _activeEditor.Loaded += (s, e) => {
                _activeEditor.Focus();
                Keyboard.Focus(_activeEditor);
            };

            _activeEditor.Focus();
            Keyboard.Focus(_activeEditor);
        }

        private Border CreateFormatPanel() {
            Border border = new Border {
                Background = new SolidColorBrush(Color.FromArgb(245, 25, 28, 34)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(180, 0, 162, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5, 3, 5, 3)
            };

            StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
            StackPanel formatBar = TextStyleService.CreateFloatingFormatBar(() => {
                if (_activeEditor != null) {
                    _activeEditor.Focus();
                    Keyboard.Focus(_activeEditor);
                }
            });
            row.Children.Add(formatBar);

            Button btnCommit = new Button {
                Content = "✓ Fijar [Enter]",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(220, 0, 122, 204)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(6, 0, 2, 0),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };
            btnCommit.Click += (s, e) => CommitActiveText();
            row.Children.Add(btnCommit);

            border.Child = row;
            return border;
        }

        private void CommitActiveText() {
            if (_activeEditor == null || _context == null) return;

            string content = _activeEditor.Text;
            TextBox editor = _activeEditor;
            Border panel = _formatPanel;
            Point origin = _editorOrigin;
            _activeEditor = null;
            _formatPanel = null;

            if (_context.Canvas.Children.Contains(editor)) {
                _context.Canvas.Children.Remove(editor);
            }
            if (panel != null && _context.Canvas.Children.Contains(panel)) {
                _context.Canvas.Children.Remove(panel);
            }

            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(content.Trim())) return;

            double fontSize = TextStyleService.FontSize;
            Typeface typeface = TextStyleService.GetTypeface();

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

            double strokeThickness = TextStyleService.GetOptimalTextStrokeThickness(fontSize, _context.CurrentSize);
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
            if (_context != null) {
                if (_activeEditor != null && _context.Canvas.Children.Contains(_activeEditor)) {
                    _context.Canvas.Children.Remove(_activeEditor);
                }
                if (_formatPanel != null && _context.Canvas.Children.Contains(_formatPanel)) {
                    _context.Canvas.Children.Remove(_formatPanel);
                }
            }
            _activeEditor = null;
            _formatPanel = null;
        }
    }
}

