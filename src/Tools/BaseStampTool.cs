using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using Trazalibre.Core;

namespace Trazalibre.Tools {
    /// <summary>
    /// Clase base abstracta para herramientas de sellos vectoriales.
    /// Al hacer clic sobre el lienzo, genera un trazo de sello (Stroke) con el color y tamaño activos.
    /// </summary>
    public abstract class BaseStampTool : ITool {
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public abstract string Icon { get; }

        private IAnnotationContext _context;
        private MouseButtonEventHandler _mouseHandler;

        protected abstract IEnumerable<Point> GetRelativePoints(double scale);

        public void Activate(IAnnotationContext context) {
            _context = context;
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.None;
            context.Canvas.Cursor = Cursors.Hand;

            if (_mouseHandler == null) {
                _mouseHandler = OnCanvasMouseDown;
            }
            context.Canvas.PreviewMouseDown -= _mouseHandler;
            context.Canvas.PreviewMouseDown += _mouseHandler;
        }

        public void Deactivate(IAnnotationContext context) {
            if (_mouseHandler != null && context != null && context.Canvas != null) {
                context.Canvas.PreviewMouseDown -= _mouseHandler;
            }
            _context = null;
        }

        public void OnColorChanged(IAnnotationContext context, System.Windows.Media.Color newColor) { }
        public void OnSizeChanged(IAnnotationContext context, double newSize) { }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed || _context == null) return;

            Point center = e.GetPosition(_context.Canvas);
            double scale = Math.Max(32.0, _context.CurrentSize * 8.0);
            double strokeThickness = Math.Max(3.5, _context.CurrentSize);

            StylusPointCollection points = new StylusPointCollection();
            foreach (Point rel in GetRelativePoints(scale)) {
                points.Add(new StylusPoint(center.X + rel.X, center.Y + rel.Y));
            }

            DrawingAttributes attrs = new DrawingAttributes {
                Color = _context.CurrentColor,
                Width = strokeThickness,
                Height = strokeThickness,
                FitToCurve = false,
                IsHighlighter = false
            };

            Stroke stroke = new Stroke(points, attrs);
            _context.AddStroke(stroke);

            e.Handled = true;
        }
    }
}

