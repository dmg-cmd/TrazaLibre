using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Trazalibre.Services {
    /// <summary>
    /// Servicio centralizado para gestionar el tipo de fuente, tamaño y estilo (negrita/cursiva)
    /// en todas las herramientas de texto, indicaciones y globos de diálogo.
    /// </summary>
    public static class TextStyleService {
        public static readonly string[] AvailableFonts = new string[] {
            "Segoe UI",
            "Arial",
            "Calibri",
            "Consolas",
            "Verdana",
            "Times New Roman",
            "Georgia",
            "Comic Sans MS"
        };

        private static string _fontFamilyName = "Segoe UI";
        private static double _fontSize = 20.0;
        private static bool _isBold = true;
        private static bool _isItalic = false;

        /// <summary>
        /// Evento disparado cada vez que cambia la fuente, el tamaño o el estilo.
        /// </summary>
        public static event EventHandler StyleChanged;

        public static string FontFamilyName {
            get { return _fontFamilyName; }
            set {
                if (!string.IsNullOrEmpty(value) && _fontFamilyName != value) {
                    _fontFamilyName = value;
                    NotifyStyleChanged();
                }
            }
        }

        public static double FontSize {
            get { return _fontSize; }
            set {
                double clamped = Math.Max(10.0, Math.Min(72.0, Math.Round(value)));
                if (Math.Abs(_fontSize - clamped) > 0.01) {
                    _fontSize = clamped;
                    NotifyStyleChanged();
                }
            }
        }

        public static bool IsBold {
            get { return _isBold; }
            set {
                if (_isBold != value) {
                    _isBold = value;
                    NotifyStyleChanged();
                }
            }
        }

        public static bool IsItalic {
            get { return _isItalic; }
            set {
                if (_isItalic != value) {
                    _isItalic = value;
                    NotifyStyleChanged();
                }
            }
        }

        public static void IncreaseSize(double step = 2.0) {
            FontSize = _fontSize + step;
        }

        public static void DecreaseSize(double step = 2.0) {
            FontSize = _fontSize - step;
        }

        public static void ToggleBold() {
            IsBold = !_isBold;
        }

        public static void ToggleItalic() {
            IsItalic = !_isItalic;
        }

        public static FontFamily GetFontFamily() {
            return new FontFamily(_fontFamilyName);
        }

        public static FontWeight GetFontWeight() {
            return _isBold ? FontWeights.Bold : FontWeights.Normal;
        }

        public static FontStyle GetFontStyle() {
            return _isItalic ? FontStyles.Italic : FontStyles.Normal;
        }

        public static Typeface GetTypeface() {
            return new Typeface(GetFontFamily(), GetFontStyle(), GetFontWeight(), FontStretches.Normal);
        }

        /// <summary>
        /// Aplica las propiedades tipográficas actuales a un cuadro de edición TextBox.
        /// </summary>
        public static void ApplyToEditor(TextBox editor) {
            if (editor == null) return;
            editor.FontFamily = GetFontFamily();
            editor.FontSize = _fontSize;
            editor.FontWeight = GetFontWeight();
            editor.FontStyle = GetFontStyle();
        }

        /// <summary>
        /// Calcula un grosor de trazo proporcional al tamaño de letra para que el texto vectorizado
        /// mantenga alta legibilidad en tamaños pequeños y buen cuerpo en tamaños grandes.
        /// </summary>
        public static double GetOptimalTextStrokeThickness(double fontSize, double toolSize) {
            double byFont = fontSize * (_isBold ? 0.085 : 0.06);
            return Math.Max(1.2, Math.Min(4.0, byFont));
        }

        /// <summary>
        /// Construye una barra compacta de controles tipográficos (Fuente, A-, Tamaño, A+, Negrita, Cursiva)
        /// para incrustar en las mini-barras flotantes sobre los cuadros de texto en el lienzo.
        /// </summary>
        public static StackPanel CreateFloatingFormatBar(Action onFocusRestore) {
            StackPanel bar = new StackPanel {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Selector de familia tipográfica
            ComboBox fontCombo = new ComboBox {
                Width = 112,
                Height = 22,
                FontSize = 10.5,
                Margin = new Thickness(0, 0, 4, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                ToolTip = "Cambiar tipo de letra (Fuente)"
            };

            bool updatingUI = false;

            foreach (string f in AvailableFonts) {
                ComboBoxItem item = new ComboBoxItem {
                    Content = f,
                    FontFamily = new FontFamily(f),
                    FontSize = 11
                };
                fontCombo.Items.Add(item);
                if (f.Equals(_fontFamilyName, StringComparison.OrdinalIgnoreCase)) {
                    fontCombo.SelectedItem = item;
                }
            }

            fontCombo.SelectionChanged += (s, e) => {
                if (updatingUI) return;
                ComboBoxItem sel = fontCombo.SelectedItem as ComboBoxItem;
                if (sel != null && sel.Content != null) {
                    FontFamilyName = sel.Content.ToString();
                    if (onFocusRestore != null) onFocusRestore();
                }
            };
            bar.Children.Add(fontCombo);

            // Botón disminuir tamaño (A-)
            Button btnDec = CreateFormatButton("A-", "Reducir tamaño de letra (-2 pt)");
            btnDec.Click += (s, e) => {
                DecreaseSize(2.0);
                if (onFocusRestore != null) onFocusRestore();
            };
            bar.Children.Add(btnDec);

            // Etiqueta de tamaño actual
            TextBlock sizeText = new TextBlock {
                Text = string.Format("{0:0} pt", _fontSize),
                Foreground = Brushes.SkyBlue,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Width = 34,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0)
            };
            bar.Children.Add(sizeText);

            // Botón aumentar tamaño (A+)
            Button btnInc = CreateFormatButton("A+", "Aumentar tamaño de letra (+2 pt)");
            btnInc.Click += (s, e) => {
                IncreaseSize(2.0);
                if (onFocusRestore != null) onFocusRestore();
            };
            bar.Children.Add(btnInc);

            // Botón Negrita (B)
            Button btnBold = CreateFormatButton("B", "Alternar Negrita");
            btnBold.FontWeight = FontWeights.Bold;
            btnBold.Margin = new Thickness(4, 0, 1, 0);
            btnBold.Click += (s, e) => {
                ToggleBold();
                if (onFocusRestore != null) onFocusRestore();
            };
            bar.Children.Add(btnBold);

            // Botón Cursiva (I)
            Button btnItalic = CreateFormatButton("I", "Alternar Cursiva");
            btnItalic.FontStyle = FontStyles.Italic;
            btnItalic.Click += (s, e) => {
                ToggleItalic();
                if (onFocusRestore != null) onFocusRestore();
            };
            bar.Children.Add(btnItalic);

            Action refreshControls = () => {
                updatingUI = true;
                sizeText.Text = string.Format("{0:0} pt", _fontSize);
                for (int i = 0; i < fontCombo.Items.Count; i++) {
                    ComboBoxItem item = fontCombo.Items[i] as ComboBoxItem;
                    if (item != null && item.Content != null &&
                        item.Content.ToString().Equals(_fontFamilyName, StringComparison.OrdinalIgnoreCase)) {
                        fontCombo.SelectedIndex = i;
                        break;
                    }
                }
                ApplyToggleVisual(btnBold, _isBold);
                ApplyToggleVisual(btnItalic, _isItalic);
                updatingUI = false;
            };

            refreshControls();

            EventHandler handler = (s, e) => {
                bar.Dispatcher.Invoke(new Action(refreshControls));
            };
            StyleChanged += handler;
            bar.Unloaded += (s, e) => {
                StyleChanged -= handler;
            };

            return bar;
        }

        private static Button CreateFormatButton(string label, string tooltip) {
            return new Button {
                Content = label,
                FontSize = 10,
                MinWidth = 22,
                Height = 20,
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(1, 0, 1, 0),
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(160, 45, 48, 55)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = tooltip
            };
        }

        private static void ApplyToggleVisual(Button btn, bool active) {
            if (active) {
                btn.Background = new SolidColorBrush(Color.FromArgb(220, 0, 122, 204));
                btn.BorderBrush = Brushes.SkyBlue;
                btn.Foreground = Brushes.White;
            } else {
                btn.Background = new SolidColorBrush(Color.FromArgb(160, 45, 48, 55));
                btn.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                btn.Foreground = Brushes.LightGray;
            }
        }

        private static void NotifyStyleChanged() {
            if (StyleChanged != null) {
                StyleChanged(null, EventArgs.Empty);
            }
        }
    }
}
