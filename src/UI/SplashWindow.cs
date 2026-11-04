using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Trazalibre.UI {
    /// <summary>
    /// Ventana de bienvenida e indicador de carga inicial ("Por favor espere")
    /// optimizada para renderizado instantáneo sin efectos pesados en el arranque en frío.
    /// </summary>
    public class SplashWindow : Window {
        private TextBlock _statusText;
        private Border _progressFill;
        private DispatcherTimer _animTimer;
        private double _currentRatio = 0.08;
        private double _targetRatio = 0.15;
        private const double TotalBarWidth = 290.0;

        public SplashWindow() {
            Title = "Iniciando Trazalibre...";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;
            Cursor = Cursors.Wait;

            BuildUI();
            StartSmoothProgressTimer();
        }

        private void BuildUI() {
            // Sombra ligera por borde multicapa (sin DropShadowEffect para apertura instantánea)
            Border outerFrame = new Border {
                Background = new SolidColorBrush(Color.FromArgb(110, 0, 0, 0)),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(4)
            };

            Border card = new Border {
                Width = 350,
                Background = new LinearGradientBrush(
                    Color.FromArgb(252, 18, 22, 30),
                    Color.FromArgb(252, 28, 35, 48),
                    45.0),
                CornerRadius = new CornerRadius(14),
                BorderBrush = new SolidColorBrush(Color.FromArgb(230, 0, 174, 255)),
                BorderThickness = new Thickness(2.0),
                Padding = new Thickness(26, 22, 26, 22)
            };

            StackPanel stack = new StackPanel {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // 1. Contenedor del Icono Oficial
            Border iconBadge = new Border {
                Width = 72,
                Height = 72,
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromArgb(95, 0, 140, 220)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(220, 80, 200, 255)),
                BorderThickness = new Thickness(1.6),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };

            ImageSource appIcon = TryLoadAppIcon();
            if (appIcon != null) {
                Icon = appIcon;
                Image img = new Image {
                    Source = appIcon,
                    Width = 50,
                    Height = 50,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                iconBadge.Child = img;
            } else {
                TextBlock fallbackIcon = new TextBlock {
                    Text = "🖊️",
                    FontSize = 36,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                iconBadge.Child = fallbackIcon;
            }
            stack.Children.Add(iconBadge);

            // 2. Título y Versión
            StackPanel titleRow = new StackPanel {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };

            TextBlock titleText = new TextBlock {
                Text = "Trazalibre",
                Foreground = Brushes.White,
                FontSize = 23,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleRow.Children.Add(titleText);

            string verString = "v1.3.0";
            try {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                if (v != null) {
                    verString = string.Format("v{0}.{1}.{2}", v.Major, v.Minor, v.Build);
                }
            } catch { }

            Border verBadge = new Border {
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 122, 204)),
                BorderBrush = Brushes.SkyBlue,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            verBadge.Child = new TextBlock {
                Text = verString,
                Foreground = Brushes.White,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold
            };
            titleRow.Children.Add(verBadge);
            stack.Children.Add(titleRow);

            // 3. Subtítulo descriptivo
            TextBlock subtitleText = new TextBlock {
                Text = "Pizarra Interactiva e Instrumental Geométrico CAD",
                Foreground = new SolidColorBrush(Color.FromRgb(175, 190, 210)),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            stack.Children.Add(subtitleText);

            // 4. Barra de progreso
            Border progressTrack = new Border {
                Width = TotalBarWidth,
                Height = 9,
                CornerRadius = new CornerRadius(4.5),
                Background = new SolidColorBrush(Color.FromArgb(200, 10, 14, 20)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _progressFill = new Border {
                Width = 24,
                Height = 7,
                CornerRadius = new CornerRadius(3.5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new LinearGradientBrush(
                    Color.FromRgb(0, 162, 255),
                    Color.FromRgb(0, 230, 180),
                    0.0)
            };
            progressTrack.Child = _progressFill;
            stack.Children.Add(progressTrack);

            // 5. Mensaje de estado ("En ejecución, por favor espere...")
            _statusText = new TextBlock {
                Text = "⏳ Iniciando Trazalibre, por favor espere...",
                Foreground = Brushes.Gold,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(_statusText);

            card.Child = stack;
            outerFrame.Child = card;
            Content = outerFrame;
        }

        private void StartSmoothProgressTimer() {
            _animTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(25)
            };
            _animTimer.Tick += (s, e) => {
                if (_currentRatio < _targetRatio) {
                    _currentRatio = Math.Min(_targetRatio, _currentRatio + 0.025);
                    if (_progressFill != null) {
                        _progressFill.Width = (TotalBarWidth - 2.0) * _currentRatio;
                    }
                }
            };
            _animTimer.Start();

            Closed += (s, e) => {
                if (_animTimer != null) _animTimer.Stop();
            };
        }

        public static ImageSource TryLoadAppIcon() {
            try {
                string exePath = Assembly.GetExecutingAssembly().Location;
                using (System.Drawing.Icon sysIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath)) {
                    if (sysIcon != null) {
                        return Imaging.CreateBitmapSourceFromHIcon(
                            sysIcon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions()
                        );
                    }
                }
            } catch { }
            return null;
        }

        public void SetProgress(double ratio, string message) {
            _targetRatio = Math.Max(0.08, Math.Min(1.0, ratio));
            if (_statusText != null && !string.IsNullOrEmpty(message)) {
                _statusText.Text = message;
            }
        }
    }
}
