using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Trazalibre.UI {
    /// <summary>
    /// Ventana interactiva a pantalla completa para seleccionar un sector rectangular de la pantalla.
    /// Permite al usuario arrastrar el ratón para definir el área a capturar o presionar ESC para cancelar.
    /// </summary>
    public class SectorSelectionWindow : Window {
        private readonly Canvas _canvas;
        private readonly Border _selectionBorder;
        private readonly Border _badge;
        private readonly TextBlock _badgeText;
        private readonly Border _hintBanner;

        private Point _startPoint;
        private bool _isSelecting = false;

        /// <summary>
        /// Rectángulo en coordenadas físicas de pantalla seleccionado por el usuario.
        /// </summary>
        public System.Drawing.Rectangle SelectedRectangle { get; private set; }

        public SectorSelectionWindow() {
            Title = "Seleccionar Sector de Pantalla";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Topmost = true;
            ShowInTaskbar = false;

            // Cubrir toda el área virtual de pantallas (soporte multimonitor)
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            // Fondo sutilmente oscurecido para resaltar el área seleccionable
            Background = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
            Cursor = Cursors.Cross;

            _canvas = new Canvas();

            // 1. Recuadro de selección visual (borde punteado y relleno azul translúcido)
            _selectionBorder = new Border {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 162, 255)),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromArgb(40, 0, 162, 255)),
                Visibility = Visibility.Collapsed
            };
            _canvas.Children.Add(_selectionBorder);

            // 2. Etiqueta informativa con las dimensiones del sector en píxeles
            _badgeText = new TextBlock {
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11
            };

            _badge = new Border {
                Background = new SolidColorBrush(Color.FromArgb(220, 25, 25, 25)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                Child = _badgeText,
                Visibility = Visibility.Collapsed
            };
            _canvas.Children.Add(_badge);

            // 3. Banner de instrucciones en la parte superior
            TextBlock hintText = new TextBlock {
                Text = "✂️ Arrastra con el ratón para seleccionar el sector de la pantalla • Presiona [ESC] para cancelar",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            };

            _hintBanner = new Border {
                Background = new SolidColorBrush(Color.FromArgb(235, 25, 28, 35)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 0, 162, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(18, 9, 18, 9),
                Child = hintText
            };
            _canvas.Children.Add(_hintBanner);

            Content = _canvas;

            Loaded += OnLoaded;
            KeyDown += OnWindowKeyDown;
            MouseDown += OnWindowMouseDown;
            MouseMove += OnWindowMouseMove;
            MouseUp += OnWindowMouseUp;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            // Centrar el banner superior horizontalmente
            _hintBanner.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double bannerLeft = (ActualWidth - _hintBanner.DesiredSize.Width) / 2.0;
            if (bannerLeft < 20) bannerLeft = 20;
            Canvas.SetLeft(_hintBanner, bannerLeft);
            Canvas.SetTop(_hintBanner, 40);
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                // Cancelar la captura
                DialogResult = false;
                Close();
            }
        }

        private void OnWindowMouseDown(object sender, MouseButtonEventArgs e) {
            if (e.RightButton == MouseButtonState.Pressed) {
                // Clic derecho cancela la selección
                DialogResult = false;
                Close();
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed) {
                _startPoint = e.GetPosition(_canvas);
                _isSelecting = true;

                // Ocultar el banner para que no interfiera con la visualización del área
                _hintBanner.Visibility = Visibility.Collapsed;

                Canvas.SetLeft(_selectionBorder, _startPoint.X);
                Canvas.SetTop(_selectionBorder, _startPoint.Y);
                _selectionBorder.Width = 0;
                _selectionBorder.Height = 0;
                _selectionBorder.Visibility = Visibility.Visible;

                _badge.Visibility = Visibility.Collapsed;
                CaptureMouse();
            }
        }

        private void OnWindowMouseMove(object sender, MouseEventArgs e) {
            if (!_isSelecting || e.LeftButton != MouseButtonState.Pressed) return;

            Point currentPoint = e.GetPosition(_canvas);

            double x = Math.Min(_startPoint.X, currentPoint.X);
            double y = Math.Min(_startPoint.Y, currentPoint.Y);
            double w = Math.Abs(currentPoint.X - _startPoint.X);
            double h = Math.Abs(currentPoint.Y - _startPoint.Y);

            Canvas.SetLeft(_selectionBorder, x);
            Canvas.SetTop(_selectionBorder, y);
            _selectionBorder.Width = w;
            _selectionBorder.Height = h;

            // Calcular dimensiones en píxeles físicos de pantalla
            Point screenP1 = PointToScreen(_startPoint);
            Point screenP2 = PointToScreen(currentPoint);
            int pxWidth = (int)Math.Abs(screenP2.X - screenP1.X);
            int pxHeight = (int)Math.Abs(screenP2.Y - screenP1.Y);

            _badgeText.Text = string.Format("📐 {0} × {1} px", pxWidth, pxHeight);
            _badge.Visibility = Visibility.Visible;

            // Posicionar la etiqueta de dimensiones
            _badge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double badgeLeft = x;
            double badgeTop = (y + h + 8 + 30 < ActualHeight) ? (y + h + 8) : (y - 32);
            if (badgeTop < 0) badgeTop = y + 8;

            Canvas.SetLeft(_badge, badgeLeft);
            Canvas.SetTop(_badge, badgeTop);
        }

        private void OnWindowMouseUp(object sender, MouseButtonEventArgs e) {
            if (!_isSelecting) return;
            _isSelecting = false;
            ReleaseMouseCapture();

            Point endPoint = e.GetPosition(_canvas);

            // Convertir coordenadas a píxeles físicos del sistema operativo
            Point screenP1 = PointToScreen(_startPoint);
            Point screenP2 = PointToScreen(endPoint);

            int screenX = (int)Math.Min(screenP1.X, screenP2.X);
            int screenY = (int)Math.Min(screenP1.Y, screenP2.Y);
            int width = (int)Math.Abs(screenP2.X - screenP1.X);
            int height = (int)Math.Abs(screenP2.Y - screenP1.Y);

            // Umbral mínimo de 8x8 píxeles para ignorar clics accidentales
            if (width >= 8 && height >= 8) {
                SelectedRectangle = new System.Drawing.Rectangle(screenX, screenY, width, height);
                DialogResult = true;
            } else {
                DialogResult = false;
            }

            Close();
        }
    }
}

