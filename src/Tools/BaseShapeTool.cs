using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Trazalibre.Core;
using Trazalibre.Services;

namespace Trazalibre.Tools {
    /// <summary>
    /// Clase base para herramientas interactivas de formas geométricas.
    /// Soporta arrastre con previsualización en tiempo real y generación atómica de trazo (Stroke) al soltar el ratón.
    /// </summary>
    public abstract class BaseShapeTool : ITool {
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

        protected abstract IEnumerable<Point> GeneratePoints(Point start, Point current);

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
            CleanPreview();
            if (context != null && context.Canvas != null && _mouseDownHandler != null) {
                context.Canvas.PreviewMouseDown -= _mouseDownHandler;
                context.Canvas.PreviewMouseMove -= _mouseMoveHandler;
                context.Canvas.PreviewMouseUp -= _mouseUpHandler;
            }
            _isDrawing = false;
            _context = null;
        }

        public void OnColorChanged(IAnnotationContext context, Color newColor) { }
        public void OnSizeChanged(IAnnotationContext context, double newSize) { }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed || _context == null) return;

            Point clickPos = e.GetPosition(_context.Canvas);
            SnapResult snap = SnapService.FindSnapPoint(_context.Canvas.Strokes, clickPos);
            _startPoint = snap.IsSnapped ? snap.Point : clickPos;
            _isDrawing = true;
            _previewStroke = null;
            e.Handled = true;
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e) {
            if (!_isDrawing || e.LeftButton != MouseButtonState.Pressed || _context == null) return;

            Point currentPoint = e.GetPosition(_context.Canvas);
            SnapResult endSnap = SnapService.FindSnapPoint(_context.Canvas.Strokes, currentPoint);
            Point effectiveCurrent;

            if (endSnap.IsSnapped) {
                effectiveCurrent = endSnap.Point;
            } else if ((Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) {
                // Cuantizar longitud al milímetro más cercano (5 px = 1 mm)
                double dx = currentPoint.X - _startPoint.X;
                double dy = currentPoint.Y - _startPoint.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > 4.0) {
                    double snappedDist = MetricUnits.SnapToMm(dist);
                    double ratio = snappedDist / dist;
                    effectiveCurrent = new Point(_startPoint.X + dx * ratio, _startPoint.Y + dy * ratio);
                } else {
                    effectiveCurrent = currentPoint;
                }
            } else {
                effectiveCurrent = currentPoint;
            }

            UpdatePreview(_startPoint, effectiveCurrent);
            e.Handled = true;
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e) {
            if (!_isDrawing || _context == null) return;
            _isDrawing = false;

            Point mouseUpPoint = e.GetPosition(_context.Canvas);
            SnapResult endSnap = SnapService.FindSnapPoint(_context.Canvas.Strokes, mouseUpPoint);
            Point endPoint;

            if (endSnap.IsSnapped) {
                endPoint = endSnap.Point;
            } else if ((Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) {
                double dx = mouseUpPoint.X - _startPoint.X;
                double dy = mouseUpPoint.Y - _startPoint.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > 4.0) {
                    double snappedDist = MetricUnits.SnapToMm(dist);
                    double ratio = snappedDist / dist;
                    endPoint = new Point(_startPoint.X + dx * ratio, _startPoint.Y + dy * ratio);
                } else {
                    endPoint = mouseUpPoint;
                }
            } else {
                endPoint = mouseUpPoint;
            }

            CleanPreview();

            // Ignorar clics mínimos accidentales sin arrastre
            double fdx = endPoint.X - _startPoint.X;
            double fdy = endPoint.Y - _startPoint.Y;
            if (Math.Sqrt(fdx * fdx + fdy * fdy) < 4.0) return;

            Stroke finalStroke = CreateStroke(_startPoint, endPoint);
            if (finalStroke != null) {
                _context.AddStroke(finalStroke);
            }

            e.Handled = true;
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

            if (points.Count < 2) return null;

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

