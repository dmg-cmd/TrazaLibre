using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Interop;
using System.Windows.Media;
using Trazalibre.Core;
using Trazalibre.Services;

namespace Trazalibre.UI {
    public class OverlayWindow : Window, IAnnotationContext {
        private readonly InkCanvas _canvas;
        private readonly Stack<StrokeCollection> _undoStack = new Stack<StrokeCollection>();
        private IntPtr _hwnd;
        private Color _currentColor = Colors.Red;
        private double _currentSize = 4.0;
        private BackgroundMode _currentBackgroundMode = BackgroundMode.Transparent;
        private bool _isClickThrough = false;

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

            _canvas = new InkCanvas {
                Background = BackgroundService.HitTestInvisibleBrush,
                UseCustomCursor = true,
                Focusable = true,
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

            Content = _canvas;
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
            Background = _canvas.Background;
            if (!enabled) {
                _canvas.Focus();
            }
        }

        public void SetBackgroundMode(BackgroundMode mode) {
            _currentBackgroundMode = mode;
            BackgroundService.ApplyBackground(_canvas, _currentBackgroundMode, !_isClickThrough);
            Background = _canvas.Background;
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
                foreach (Stroke s in lastBatch) {
                    _canvas.Strokes.Remove(s);
                }
            } else if (_canvas.Strokes.Count > 0) {
                _canvas.Strokes.RemoveAt(_canvas.Strokes.Count - 1);
            }
        }

        public void Clear() {
            _canvas.Strokes.Clear();
            _undoStack.Clear();
        }

        public string SaveScreenshot() {
            return ScreenshotService.CaptureSector();
        }
    }
}

