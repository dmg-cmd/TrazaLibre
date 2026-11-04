using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Trazalibre.UI {
    /// <summary>
    /// Ventana de bienvenida e indicador de carga inicial ("Por favor espere")
    /// que muestra el icono oficial de Trazalibre y el progreso de inicialización.
    /// </summary>
    public class SplashWindow : Window {
        private TextBlock _statusText;
        private Border _progressFill;
        private const double TotalBarWidth = 280.0;

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
        }

        private void BuildUI() {
            Border card = new Border {
                Width = 340,
                Background = new LinearGradientBrush(
                    Color.FromArgb(250, 18, 22, 30),
                    Color.FromArgb(250, 28, 35, 48),
                    45.0),
                CornerRadius = new CornerRadius(14),
                BorderBrush = new SolidColorBrush(Color.FromArgb(210, 0, 174, 255)),
                BorderThickness = new Thickness(1.8),
                Padding = new Thickness(24, 20, 24, 20),
                Margin = new Thickness(14),
                Effect = new DropShadowEffect {
                    Color = Colors.Black,
                    BlurRadius = 18,
                    ShadowDepth = 4,
                    Opacity = 0.65
                }
            };

            StackPanel stack = new StackPanel {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // 1. Contenedor del Icono Oficial con halo luminoso
            Border iconBadge = new Border {
                Width = 68,
                Height = 68,
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromArgb(90, 0, 140, 220)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(200, 80, 200, 255)),
                BorderThickness = new Thickness(1.5),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };

            ImageSource appIcon = TryLoadAppIcon();
            if (appIcon != null) {
                Icon = appIcon;
                Image img = new Image {
                    Source = appIcon,
                    Width = 48,
                    Height = 48,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                iconBadge.Child = img;
            } else {
                TextBlock fallbackIcon = new TextBlock {
                    Text = "🖊️",
                    FontSize = 34,
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
                FontSize = 22,
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
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 122, 204)),
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
                Foreground = new SolidColorBrush(Color.FromRgb(170, 185, 205)),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            stack.Children.Add(subtitleText);

            // 4. Barra de progreso
            Border progressTrack = new Border {
                Width = TotalBarWidth,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.FromArgb(180, 12, 15, 22)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _progressFill = new Border {
                Width = 30,
                Height = 6,
                CornerRadius = new CornerRadius(3),
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
                Text = "⏳ Ejecutando Trazalibre, por favor espere...",
                Foreground = Brushes.Gold,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(_statusText);

            card.Child = stack;
            Content = card;
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
            double clamped = Math.Max(0.08, Math.Min(1.0, ratio));
            if (_progressFill != null) {
                _progressFill.Width = (TotalBarWidth - 2.0) * clamped;
            }
            if (_statusText != null && !string.IsNullOrEmpty(message)) {
                _statusText.Text = message;
            }
            ForceRender();
        }

        public void ForceRender() {
            try {
                Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            } catch { }
        }

        public void CloseAfterDelay(int delayMs = 450) {
            DispatcherTimer timer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(delayMs)
            };
            timer.Tick += (s, e) => {
                timer.Stop();
                Close();
            };
            timer.Start();
        }
    }
}
