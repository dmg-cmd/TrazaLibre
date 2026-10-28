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
        private ShapeSnapVisual _snapVisual;

        protected abstract IEnumerable<Point> GeneratePoints(Point start, Point current);

        public void Activate(IAnnotationContext context) {
            _context = context;
            context.SetClickThrough(false);
            context.Canvas.EditingMode = InkCanvasEditingMode.None;
            context.Canvas.Cursor = Cursors.Cross;

            if (_snapVisual == null) {
                _snapVisual = new ShapeSnapVisual();
            }
            if (!_context.Canvas.Children.Contains(_snapVisual)) {
                _context.Canvas.Children.Add(_snapVisual);
            }
            _snapVisual.ClearSnap();

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
            if (_snapVisual != null && context != null && context.Canvas != null) {
                context.Canvas.Children.Remove(_snapVisual);
            }
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

            if (_snapVisual != null) {
                _snapVisual.ClearSnap();
            }

            Point clickPos = e.GetPosition(_context.Canvas);
            SnapResult snap = SnapService.FindSnapPoint(_context.Canvas.Strokes, clickPos);
            _startPoint = snap.IsSnapped ? snap.Point : clickPos;
            _isDrawing = true;
            _previewStroke = null;
            e.Handled = true;
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e) {
            if (_context == null) return;

            if (!_isDrawing) {
                // Previsualización de captura magnética en tiempo real antes de hacer clic (para enganchar el primer punto)
                Point hoverPos = e.GetPosition(_context.Canvas);
                SnapResult hoverSnap = SnapService.FindSnapPoint(_context.Canvas.Strokes, hoverPos);
                if (hoverSnap.IsSnapped && _snapVisual != null) {
                    _snapVisual.SetSnap(hoverSnap.Point, hoverSnap.SnapType);
                } else if (_snapVisual != null) {
                    _snapVisual.ClearSnap();
                }
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed) return;

            Point currentPoint = e.GetPosition(_context.Canvas);
            SnapResult endSnap = SnapService.FindSnapPoint(_context.Canvas.Strokes, currentPoint);
            Point effectiveCurrent;

            if (endSnap.IsSnapped) {
                effectiveCurrent = endSnap.Point;
                if (_snapVisual != null) {
                    _snapVisual.SetSnap(endSnap.Point, endSnap.SnapType);
                }
            } else {
                if (_snapVisual != null) {
                    _snapVisual.ClearSnap();
                }
                if ((Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) {
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
            }

            UpdatePreview(_startPoint, effectiveCurrent);
            e.Handled = true;
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e) {
            if (!_isDrawing || _context == null) return;
            _isDrawing = false;

            if (_snapVisual != null) {
                _snapVisual.ClearSnap();
            }

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

    /// <summary>
    /// Elemento visual flotante para mostrar el indicador de captura magnética (Snap)
    /// antes y durante el trazado de figuras y vectores (permite "pegar" el punto inicial con precisión).
    /// </summary>
    internal class ShapeSnapVisual : FrameworkElement {
        public bool IsSnapped { get; set; }
        public Point SnapPoint { get; set; }
        public string SnapType { get; set; }

        public ShapeSnapVisual() {
            IsHitTestVisible = false;
        }

        public void SetSnap(Point p, string type) {
            IsSnapped = true;
            SnapPoint = p;
            SnapType = type;
            Visibility = Visibility.Visible;
            InvalidateVisual();
        }

        public void ClearSnap() {
            if (IsSnapped) {
                IsSnapped = false;
                Visibility = Visibility.Collapsed;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc) {
            base.OnRender(dc);
            if (IsSnapped) {
                SnapService.DrawSnapIndicator(dc, SnapPoint, SnapType);
            }
        }
    }
}

