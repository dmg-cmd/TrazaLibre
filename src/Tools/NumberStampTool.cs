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
    /// Sello Numérico Secuencial ((①).
    /// Estampa un número dentro de un círculo con el color activo y se incrementa en cada clic.
    /// Permite asignar un número de inicio personalizado y resetearlo a 1.
    /// </summary>
    public class NumberStampTool : ITool, ISequenceTool {
        public string Id { get { return "stamp_number"; } }
        public string DisplayName { get { return "Número"; } }
        public string Icon { get { return "①"; } }

        private static int _nextNumber = 1;
        public static event Action<int> NumberChanged;

        int ISequenceTool.NextNumber {
            get { return NextNumber; }
            set { NextNumber = value; }
        }

        event Action<int> ISequenceTool.NumberChanged {
            add { NumberChanged += value; }
            remove { NumberChanged -= value; }
        }

        void ISequenceTool.Reset() {
            Reset();
        }

        public static int NextNumber {
            get { return _nextNumber; }
            set { SetNumber(value); }
        }

        private IAnnotationContext _context;
        private MouseButtonEventHandler _mouseHandler;

        public static void SetNumber(int number) {
            NextNumber = Math.Max(1, number);
            if (NumberChanged != null) {
                NumberChanged(NextNumber);
            }
        }

        public static void Reset() {
            SetNumber(1);
        }

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

        public void OnColorChanged(IAnnotationContext context, Color newColor) { }
        public void OnSizeChanged(IAnnotationContext context, double newSize) { }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (_context == null) return;

            // Clic derecho reinicia el contador a 1
            if (e.RightButton == MouseButtonState.Pressed) {
                Reset();
                e.Handled = true;
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed) return;

            Point center = e.GetPosition(_context.Canvas);
            double baseRadius = Math.Max(16.0, _context.CurrentSize * 4.5);
            double strokeThickness = Math.Max(2.5, _context.CurrentSize * 0.7);

            // 1. Trazar el círculo exterior
            StylusPointCollection circlePoints = new StylusPointCollection();
            const int circleSegments = 28;
            for (int i = 0; i <= circleSegments; i++) {
                double angle = i * (2.0 * Math.PI / circleSegments);
                double x = center.X + baseRadius * Math.Cos(angle);
                double y = center.Y + baseRadius * Math.Sin(angle);
                circlePoints.Add(new StylusPoint(x, y));
            }

            DrawingAttributes circleAttrs = new DrawingAttributes {
                Color = _context.CurrentColor,
                Width = strokeThickness,
                Height = strokeThickness,
                FitToCurve = true,
                IsHighlighter = false
            };
            Stroke circleStroke = new Stroke(circlePoints, circleAttrs);

            // 2. Vectorizar el número de texto centrado
            string text = NextNumber.ToString();
            double fontSize = baseRadius * 1.25;
            Typeface typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

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

            Point textOrigin = new Point(center.X - ft.Width / 2.0, center.Y - ft.Height / 2.0);
            Geometry textGeom = ft.BuildGeometry(textOrigin);
            PathGeometry flatGeom = textGeom.GetFlattenedPathGeometry();

            DrawingAttributes textAttrs = new DrawingAttributes {
                Color = _context.CurrentColor,
                Width = Math.Max(2.0, strokeThickness * 0.8),
                Height = Math.Max(2.0, strokeThickness * 0.8),
                FitToCurve = false,
                IsHighlighter = false
            };

            StrokeCollection newStrokes = new StrokeCollection { circleStroke };

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
                newStrokes.Add(new Stroke(textPts, textAttrs));
            }

            _context.AddStrokes(newStrokes);

            // Incrementar y notificar
            SetNumber(NextNumber + 1);

            e.Handled = true;
        }
    }
}

