using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Trazalibre.Core;
using Trazalibre.Services;

namespace Trazalibre.UI {
    public class OverlayWindow : Window, IAnnotationContext {
        private const double MinZoom = 0.5;
        private const double MaxZoom = 4.0;

        private readonly Grid _rootGrid;
        private readonly InkCanvas _canvas;
        private readonly ScaleTransform _scaleTransform;
        private readonly TranslateTransform _translateTransform;
        private readonly Stack<StrokeCollection> _undoStack = new Stack<StrokeCollection>();
        private readonly HashSet<StrokeCollection> _restoreBatches = new HashSet<StrokeCollection>();
        private IntPtr _hwnd;
        private Color _currentColor = Colors.Red;
        private double _currentSize = 4.0;
        private double _zoomLevel = 1.0;
        private BackgroundMode _currentBackgroundMode = BackgroundMode.Transparent;
        private bool _isClickThrough = false;

        // Estado para desplazamiento (pan) del lienzo con zoom
        private bool _isPanning = false;
        private Point _panStartScreen;
        private Point _panStartTranslate;

        public event EventHandler<double> ZoomChanged;

        public InkCanvas Canvas {
            get { return _canvas; }
        }

        public Color CurrentColor {
            get { return _currentColor; }
            set { _currentColor = value; }
        }

        public double CurrentSize {
            get { return _currentSize; }
            set { _currentSize = value; }
        }

        public IntPtr WindowHandle {
            get { return _hwnd; }
        }

        public double ZoomLevel {
            get { return _zoomLevel; }
        }

        public ToolRegistry Registry { get; set; }

        public OverlayWindow() {
            Title = "Trazalibre - Canvas";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;

            // Soporte multimonitor: cubrir toda el área virtual de pantallas
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            _rootGrid = new Grid {
                ClipToBounds = true,
                Background = BackgroundService.HitTestInvisibleBrush
            };

            _scaleTransform = new ScaleTransform(1.0, 1.0);
            _translateTransform = new TranslateTransform(0.0, 0.0);
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(_scaleTransform);
            transformGroup.Children.Add(_translateTransform);

            _canvas = new InkCanvas {
                Background = BackgroundService.HitTestInvisibleBrush,
                UseCustomCursor = true,
                Focusable = true,
                RenderTransformOrigin = new Point(0, 0),
                RenderTransform = transformGroup,
                DefaultDrawingAttributes = new DrawingAttributes {
                    Color = _currentColor,
                    Width = _currentSize,
                    Height = _currentSize,
                    FitToCurve = true,
                    IsHighlighter = false
                }
            };

            _canvas.PreviewMouseDown += (s, e) => {
                _canvas.Focus();
            };

            // Registrar trazos manuales en la pila de deshacer
            _canvas.StrokeCollected += (s, e) => {
                _undoStack.Push(new StrokeCollection { e.Stroke });
            };

            AttachZoomAndPanHandlers();

            _rootGrid.Children.Add(_canvas);
            Content = _rootGrid;
        }

        private void AttachZoomAndPanHandlers() {
            _rootGrid.PreviewMouseWheel += (s, e) => {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
                    double step = e.Delta > 0 ? 0.25 : -0.25;
                    Point cursor = e.GetPosition(_rootGrid);
                    SetZoom(_zoomLevel + step, cursor);
                    e.Handled = true;
                }
            };

            _rootGrid.PreviewMouseDown += (s, e) => {
                bool isMiddle = e.ChangedButton == MouseButton.Middle;
                bool isSpaceDrag = e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.Space);
                if (isMiddle || isSpaceDrag) {
                    _isPanning = true;
                    _panStartScreen = e.GetPosition(_rootGrid);
                    _panStartTranslate = new Point(_translateTransform.X, _translateTransform.Y);
                    _rootGrid.CaptureMouse();
                    _rootGrid.Cursor = Cursors.SizeAll;
                    e.Handled = true;
                }
            };

            _rootGrid.PreviewMouseMove += (s, e) => {
                if (!_isPanning) return;
                Point current = e.GetPosition(_rootGrid);
                _translateTransform.X = _panStartTranslate.X + (current.X - _panStartScreen.X);
                _translateTransform.Y = _panStartTranslate.Y + (current.Y - _panStartScreen.Y);
                e.Handled = true;
            };

            _rootGrid.PreviewMouseUp += (s, e) => {
                if (!_isPanning) return;
                if (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Left) {
                    _isPanning = false;
                    _rootGrid.ReleaseMouseCapture();
                    _rootGrid.ClearValue(CursorProperty);
                    e.Handled = true;
                }
            };
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            _hwnd = new WindowInteropHelper(this).Handle;
            SetClickThrough(false);
        }

        public void SetClickThrough(bool enabled) {
            _isClickThrough = enabled;
            NativeWindowService.SetClickThrough(_hwnd, enabled);
            BackgroundService.ApplyBackground(_canvas, _currentBackgroundMode, !enabled);
            _rootGrid.Background = _canvas.Background;
            Background = _canvas.Background;
            if (!enabled) {
                _canvas.Focus();
            }
        }

        public void SetBackgroundMode(BackgroundMode mode) {
            _currentBackgroundMode = mode;
            BackgroundService.ApplyBackground(_canvas, _currentBackgroundMode, !_isClickThrough);
            _rootGrid.Background = _canvas.Background;
            Background = _canvas.Background;
        }

        public void SetZoom(double zoom, Point? center = null) {
            double clamped = Math.Max(MinZoom, Math.Min(MaxZoom, Math.Round(zoom, 2)));
            if (Math.Abs(clamped - _zoomLevel) < 0.001) return;

            Point anchor = center.HasValue
                ? center.Value
                : new Point(ActualWidth / 2.0, ActualHeight / 2.0);

            // Mantener el punto bajo el ancla en la misma posición de pantalla y relación 1:1 (ScaleX == ScaleY)
            double canvasX = (anchor.X - _translateTransform.X) / _zoomLevel;
            double canvasY = (anchor.Y - _translateTransform.Y) / _zoomLevel;

            _zoomLevel = clamped;
            _scaleTransform.ScaleX = clamped;
            _scaleTransform.ScaleY = clamped;
            _translateTransform.X = anchor.X - canvasX * clamped;
            _translateTransform.Y = anchor.Y - canvasY * clamped;

            EventHandler<double> handler = ZoomChanged;
            if (handler != null) {
                handler(this, _zoomLevel);
            }
        }

        public void ResetZoom() {
            _zoomLevel = 1.0;
            _scaleTransform.ScaleX = 1.0;
            _scaleTransform.ScaleY = 1.0;
            _translateTransform.X = 0.0;
            _translateTransform.Y = 0.0;

            EventHandler<double> handler = ZoomChanged;
            if (handler != null) {
                handler(this, _zoomLevel);
            }
        }

        public void AddStroke(Stroke stroke) {
            if (stroke != null) {
                _canvas.Strokes.Add(stroke);
                _undoStack.Push(new StrokeCollection { stroke });
            }
        }

        public void AddStrokes(StrokeCollection strokes) {
            if (strokes != null && strokes.Count > 0) {
                _canvas.Strokes.Add(strokes);
                _undoStack.Push(strokes);
            }
        }

        public void Undo() {
            if (_undoStack.Count > 0) {
                StrokeCollection lastBatch = _undoStack.Pop();
                if (_restoreBatches.Remove(lastBatch)) {
                    foreach (Stroke s in lastBatch) {
                        if (!_canvas.Strokes.Contains(s)) {
                            _canvas.Strokes.Add(s);
                        }
                    }
                } else {
                    foreach (Stroke s in lastBatch) {
                        _canvas.Strokes.Remove(s);
                    }
                }
            } else if (_canvas.Strokes.Count > 0) {
                _canvas.Strokes.RemoveAt(_canvas.Strokes.Count - 1);
            }
        }

        public void ArchiveAndClearStrokes() {
            if (_canvas.Strokes.Count > 0) {
                StrokeCollection snapshot = new StrokeCollection(_canvas.Strokes);
                _restoreBatches.Add(snapshot);
                _undoStack.Push(snapshot);
                _canvas.Strokes.Clear();
            }
        }

        public void Clear() {
            _canvas.Strokes.Clear();
            _undoStack.Clear();
            _restoreBatches.Clear();
        }

        public string SaveScreenshot() {
            return ScreenshotService.CaptureSector();
        }

        public bool MergeIntoDocument() {
            return ScreenshotService.MergeDrawingIntoApplication(this);
        }
    }
}

