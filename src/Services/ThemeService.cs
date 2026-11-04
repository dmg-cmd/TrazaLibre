using System;
using System.Windows.Media;

namespace Trazalibre.Services {
    public enum AppTheme {
        Dark = 0,
        Light = 1,
        CadBlue = 2
    }

    /// <summary>
    /// Servicio central de temas visuales para la interfaz de Trazalibre (Oscuro, Claro y Azul CAD).
    /// </summary>
    public static class ThemeService {
        private static AppTheme _currentTheme = AppTheme.Dark;

        public static event EventHandler ThemeChanged;

        public static AppTheme CurrentTheme {
            get { return _currentTheme; }
            set {
                if (_currentTheme != value) {
                    _currentTheme = value;
                    if (ThemeChanged != null) {
                        ThemeChanged(null, EventArgs.Empty);
                    }
                }
            }
        }

        public static void CycleTheme() {
            int next = ((int)_currentTheme + 1) % 3;
            CurrentTheme = (AppTheme)next;
        }

        public static string GetThemeDisplayName() {
            switch (_currentTheme) {
                case AppTheme.Light:
                    return "☀️ Claro";
                case AppTheme.CadBlue:
                    return "🔷 Azul CAD";
                default:
                    return "🌙 Oscuro";
            }
        }

        public static Brush GetWindowBackground() {
            switch (_currentTheme) {
                case AppTheme.Light:
                    return new LinearGradientBrush(
                        Color.FromArgb(248, 245, 247, 252),
                        Color.FromArgb(248, 232, 237, 246),
                        90.0);
                case AppTheme.CadBlue:
                    return new LinearGradientBrush(
                        Color.FromArgb(248, 14, 30, 52),
                        Color.FromArgb(248, 10, 22, 38),
                        90.0);
                default:
                    return new LinearGradientBrush(
                        Color.FromArgb(248, 30, 33, 40),
                        Color.FromArgb(248, 22, 24, 30),
                        90.0);
            }
        }

        public static Brush GetWindowBorderBrush() {
            switch (_currentTheme) {
                case AppTheme.Light:
                    return new SolidColorBrush(Color.FromArgb(200, 0, 120, 215));
                case AppTheme.CadBlue:
                    return new SolidColorBrush(Color.FromArgb(210, 0, 200, 255));
                default:
                    return new SolidColorBrush(Color.FromArgb(140, 100, 180, 255));
            }
        }

        public static Brush GetSectionBackground() {
            switch (_currentTheme) {
                case AppTheme.Light:
                    return new SolidColorBrush(Color.FromArgb(210, 255, 255, 255));
                case AppTheme.CadBlue:
                    return new SolidColorBrush(Color.FromArgb(170, 20, 44, 74));
                default:
                    return new SolidColorBrush(Color.FromArgb(150, 42, 46, 54));
            }
        }

        public static Brush GetSectionBorderBrush() {
            switch (_currentTheme) {
                case AppTheme.Light:
                    return new SolidColorBrush(Color.FromArgb(120, 160, 175, 195));
                case AppTheme.CadBlue:
                    return new SolidColorBrush(Color.FromArgb(110, 0, 190, 255));
                default:
                    return new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
            }
        }

        public static Brush GetPrimaryTextBrush() {
            return _currentTheme == AppTheme.Light
                ? new SolidColorBrush(Color.FromRgb(25, 32, 45))
                : Brushes.White;
        }

        public static Brush GetSecondaryTextBrush() {
            return _currentTheme == AppTheme.Light
                ? new SolidColorBrush(Color.FromRgb(70, 82, 100))
                : Brushes.LightGray;
        }

        public static Brush GetAccentTextBrush() {
            return _currentTheme == AppTheme.Light
                ? new SolidColorBrush(Color.FromRgb(0, 102, 184))
                : Brushes.SkyBlue;
        }

        public static Brush GetButtonIdleBackground() {
            switch (_currentTheme) {
                case AppTheme.Light:
                    return new SolidColorBrush(Color.FromArgb(230, 224, 230, 240));
                case AppTheme.CadBlue:
                    return new SolidColorBrush(Color.FromArgb(190, 24, 52, 86));
                default:
                    return new SolidColorBrush(Color.FromArgb(175, 50, 54, 62));
            }
        }

        public static Brush GetButtonIdleBorder() {
            switch (_currentTheme) {
                case AppTheme.Light:
                    return new SolidColorBrush(Color.FromArgb(160, 150, 165, 185));
                case AppTheme.CadBlue:
                    return new SolidColorBrush(Color.FromArgb(120, 80, 190, 255));
                default:
                    return new SolidColorBrush(Color.FromArgb(85, 255, 255, 255));
            }
        }

        public static Brush GetButtonActiveBackground() {
            return new SolidColorBrush(Color.FromArgb(235, 0, 122, 204));
        }

        public static Brush GetSeparatorBrush() {
            return _currentTheme == AppTheme.Light
                ? new SolidColorBrush(Color.FromArgb(90, 140, 155, 175))
                : new SolidColorBrush(Color.FromArgb(55, 255, 255, 255));
        }
    }
}
