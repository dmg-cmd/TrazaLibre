using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Trazalibre.Core;

namespace Trazalibre.Services {
    /// <summary>
    /// Servicio que administra los estilos y fondos del lienzo de anotaciones
    /// (Transparente, Pizarra Clara, Pizarra Oscura, Cuadrícula Clara/Oscura, Plano Azul CAD y Hoja Punteada).
    /// </summary>
    public static class BackgroundService {
        private static BackgroundMode _currentMode = BackgroundMode.Transparent;
        public static event EventHandler BackgroundModeChanged;

        public static BackgroundMode CurrentMode {
            get { return _currentMode; }
        }

        private static readonly SolidColorBrush LightBoardBrush = CreateFrozenSolidBrush(Color.FromRgb(248, 250, 252));
        private static readonly SolidColorBrush DarkBoardBrush = CreateFrozenSolidBrush(Color.FromRgb(22, 25, 32));
        // Un pincel casi transparente (alfa = 1) permite al motor DWM de Windows interceptar clics para dibujar
        // sobre el escritorio sin oscurecer nada (completamente invisible para el ojo humano).
        public static readonly SolidColorBrush HitTestInvisibleBrush = CreateFrozenSolidBrush(Color.FromArgb(1, 0, 0, 0));

        private static readonly Brush LightGridBrush = CreateGridBrush(
            Color.FromRgb(248, 250, 253),
            Color.FromArgb(120, 200, 214, 230),
            Color.FromArgb(190, 165, 188, 215));

        private static readonly Brush DarkGridBrush = CreateGridBrush(
            Color.FromRgb(20, 23, 30),
            Color.FromArgb(75, 65, 80, 105),
            Color.FromArgb(135, 85, 110, 145));

        private static readonly Brush BlueprintBrush = CreateGridBrush(
            Color.FromRgb(12, 32, 58),
            Color.FromArgb(80, 0, 175, 255),
            Color.FromArgb(150, 60, 205, 255));

        private static readonly Brush DotGridBrush = CreateDotBrush(
            Color.FromRgb(247, 249, 252),
            Color.FromRgb(150, 168, 190));

        public static void ApplyBackground(InkCanvas canvas, BackgroundMode mode, bool canDraw) {
            if (canvas == null) return;

            bool changed = (_currentMode != mode);
            _currentMode = mode;

            switch (mode) {
                case BackgroundMode.Transparent:
                    canvas.Background = canDraw ? HitTestInvisibleBrush : Brushes.Transparent;
                    break;
                case BackgroundMode.Whiteboard:
                    canvas.Background = LightBoardBrush;
                    break;
                case BackgroundMode.Blackboard:
                    canvas.Background = DarkBoardBrush;
                    break;
                case BackgroundMode.LightGrid:
                    canvas.Background = LightGridBrush;
                    break;
                case BackgroundMode.DarkGrid:
                    canvas.Background = DarkGridBrush;
                    break;
                case BackgroundMode.Blueprint:
                    canvas.Background = BlueprintBrush;
                    break;
                case BackgroundMode.DotGrid:
                    canvas.Background = DotGridBrush;
                    break;
            }

            if (changed && BackgroundModeChanged != null) {
                BackgroundModeChanged(null, EventArgs.Empty);
            }
        }

        private static SolidColorBrush CreateFrozenSolidBrush(Color c) {
            SolidColorBrush b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        /// <summary>
        /// Crea un pincel en mosaico con cuadrícula técnica milimetrada (subdivisión cada 25 px = 5 mm y línea principal cada 50 px = 10 mm).
        /// </summary>
        private static Brush CreateGridBrush(Color bgColor, Color minorLineColor, Color majorLineColor) {
            double tileSize = 50.0; // 10 mm en escala Trazalibre (5 px = 1 mm)
            double halfTile = 25.0; // 5 mm

            DrawingGroup group = new DrawingGroup();

            // Fondo base
            GeometryDrawing bgDrawing = new GeometryDrawing(
                new SolidColorBrush(bgColor),
                null,
                new RectangleGeometry(new Rect(0, 0, tileSize, tileSize)));
            group.Children.Add(bgDrawing);

            // Líneas secundarias (5 mm)
            Pen minorPen = new Pen(new SolidColorBrush(minorLineColor), 1.0);
            GeometryGroup minorLines = new GeometryGroup();
            minorLines.Children.Add(new LineGeometry(new Point(halfTile, 0), new Point(halfTile, tileSize)));
            minorLines.Children.Add(new LineGeometry(new Point(0, halfTile), new Point(tileSize, halfTile)));
            group.Children.Add(new GeometryDrawing(null, minorPen, minorLines));

            // Líneas principales (10 mm / 1 cm)
            Pen majorPen = new Pen(new SolidColorBrush(majorLineColor), 1.2);
            GeometryGroup majorLines = new GeometryGroup();
            majorLines.Children.Add(new LineGeometry(new Point(0, 0), new Point(tileSize, 0)));
            majorLines.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, tileSize)));
            group.Children.Add(new GeometryDrawing(null, majorPen, majorLines));

            DrawingBrush brush = new DrawingBrush(group) {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, tileSize, tileSize),
                ViewportUnits = BrushMappingMode.Absolute
            };
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// Crea un pincel en mosaico de puntos equidistantes (cada 25 px = 5 mm).
        /// </summary>
        private static Brush CreateDotBrush(Color bgColor, Color dotColor) {
            double step = 25.0;
            DrawingGroup group = new DrawingGroup();

            GeometryDrawing bgDrawing = new GeometryDrawing(
                new SolidColorBrush(bgColor),
                null,
                new RectangleGeometry(new Rect(0, 0, step, step)));
            group.Children.Add(bgDrawing);

            GeometryDrawing dotDrawing = new GeometryDrawing(
                new SolidColorBrush(dotColor),
                null,
                new EllipseGeometry(new Point(step / 2.0, step / 2.0), 1.3, 1.3));
            group.Children.Add(dotDrawing);

            DrawingBrush brush = new DrawingBrush(group) {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, step, step),
                ViewportUnits = BrushMappingMode.Absolute
            };
            brush.Freeze();
            return brush;
        }
    }
}

