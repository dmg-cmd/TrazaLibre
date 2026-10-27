using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Trazalibre.Core;
using Trazalibre.Services;

namespace Trazalibre.UI {
    public class ToolbarWindow : Window {
        private class ToolComboItem {
            public string ToolId { get; set; }
            public string DisplayText { get; set; }
            public override string ToString() { return DisplayText; }
        }

        private readonly ToolRegistry _toolRegistry;
        private readonly IAnnotationContext _context;
        private readonly ISequenceTool _sequenceTool;
        private readonly Dictionary<string, Button> _mainToolButtons = new Dictionary<string, Button>();

        private ComboBox _shapeComboBox;
        private Border _shapeContainerBorder;
        private ComboBox _geometryComboBox;
        private Border _geometryContainerBorder;
        private ComboBox _stampComboBox;
        private Border _stampContainerBorder;
        private TextBox _numberTextBox;
        private Slider _sizeSlider;
        private TextBlock _sizeValueText;
        private bool _isUpdatingComboSelection = false;

        public ToolbarWindow(ToolRegistry toolRegistry, IAnnotationContext context, Window ownerWindow) {
            _toolRegistry = toolRegistry;
            _context = context;

            foreach (ITool tool in _toolRegistry.Tools) {
                ISequenceTool seq = tool as ISequenceTool;
                if (seq != null) {
                    _sequenceTool = seq;
                    break;
                }
            }

            if (ownerWindow != null) {
                Owner = ownerWindow;
            }

            Title = "Barra de Herramientas";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;

            try {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                using (System.Drawing.Icon sysIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath)) {
                    if (sysIcon != null) {
                        Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            sysIcon.Handle,
                            Int32Rect.Empty,
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()
                        );
                    }
                }
            } catch { }

            Left = SystemParameters.WorkArea.Width - 280;
            Top = 60;

            _toolRegistry.ActiveToolChanged += OnActiveToolChanged;

            if (_sequenceTool != null) {
                _sequenceTool.NumberChanged += OnNumberChanged;
            }

            Closed += (s, e) => {
                if (_sequenceTool != null) {
                    _sequenceTool.NumberChanged -= OnNumberChanged;
                }
            };

            BuildUI();
        }

        private void BuildUI() {
            Border rootBorder = new Border {
                Background = new SolidColorBrush(Color.FromArgb(245, 32, 34, 37)),
                CornerRadius = new CornerRadius(10),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                MaxWidth = 270
            };

            rootBorder.MouseDown += (s, e) => {
                if (e.LeftButton == MouseButtonState.Pressed) {
                    DragMove();
                }
            };

            StackPanel mainStack = new StackPanel { Orientation = Orientation.Vertical };

            // Encabezado
            Grid headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            TextBlock titleText = new TextBlock {
                Text = "🖊️ Trazalibre",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Button closeBtn = new Button {
                Content = "✕",
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                Width = 24,
                Height = 24
            };
            closeBtn.Click += (s, e) => Application.Current.Shutdown();

            headerGrid.Children.Add(titleText);
            headerGrid.Children.Add(closeBtn);
            mainStack.Children.Add(headerGrid);

            // 1. Herramientas Principales (Botones rápidos: Ratón, Lápiz, Marca, Borra, Texto)
            WrapPanel mainToolsPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            string[] primaryToolIds = new string[] { "mouse", "pen", "highlighter", "eraser", "text" };
            foreach (string id in primaryToolIds) {
                ITool tool = FindTool(id);
                if (tool != null) {
                    ITool currentTool = tool;
                    string shortcut = GetShortcutForTool(currentTool.Id);
                    Button btn = CreateButton(currentTool.Icon + " " + currentTool.DisplayName + shortcut, () => {
                        _toolRegistry.SelectTool(currentTool.Id);
                    });
                    _mainToolButtons[currentTool.Id] = btn;
                    mainToolsPanel.Children.Add(btn);
                }
            }
            mainStack.Children.Add(mainToolsPanel);

            // 2. Desplegable de Formas Geométricas y Globos de Texto
            _shapeContainerBorder = CreateDropdownRow("🔷 Formas:", out _shapeComboBox);
            string[] shapeIds = new string[] {
                "shape_arrow",
                "shape_rect",
                "shape_ellipse",
                "shape_line",
                "shape_callout_rect",
                "shape_callout_oval",
                "shape_callout_cloud"
            };
            foreach (string sid in shapeIds) {
                ITool stool = FindTool(sid);
                if (stool != null) {
                    string sc = GetShortcutForTool(stool.Id);
                    _shapeComboBox.Items.Add(new ToolComboItem {
                        ToolId = stool.Id,
                        DisplayText = stool.Icon + " " + stool.DisplayName + sc
                    });
                }
            }
            _shapeComboBox.SelectionChanged += (s, e) => {
                if (_isUpdatingComboSelection) return;
                ToolComboItem selected = _shapeComboBox.SelectedItem as ToolComboItem;
                if (selected != null) {
                    _toolRegistry.SelectTool(selected.ToolId);
                }
            };
            mainStack.Children.Add(_shapeContainerBorder);

            // 2b. Desplegable de Módulo Geométrico (Instrumentos de dibujo técnico)
            _geometryContainerBorder = CreateDropdownRow("📐 Geometría:", out _geometryComboBox);
            string[] geometryIds = new string[] {
                "shape_protractor",
                "geom_ruler",
                "geom_triangle45",
                "geom_triangle30",
                "geom_compass"
            };
            foreach (string gid in geometryIds) {
                ITool gtool = FindTool(gid);
                if (gtool != null) {
                    string sc = GetShortcutForTool(gtool.Id);
                    _geometryComboBox.Items.Add(new ToolComboItem {
                        ToolId = gtool.Id,
                        DisplayText = gtool.Icon + " " + gtool.DisplayName + sc
                    });
                }
            }
            _geometryComboBox.SelectionChanged += (s, e) => {
                if (_isUpdatingComboSelection) return;
                ToolComboItem selected = _geometryComboBox.SelectedItem as ToolComboItem;
                if (selected != null) {
                    _toolRegistry.SelectTool(selected.ToolId);
                }
            };
            mainStack.Children.Add(_geometryContainerBorder);

            // Botón conmutador de Modo Vector / Diagramas vectoriales [V]
            Button vectorToggleBtn = new Button {
                Content = VectorService.IsVectorMode ? "🏹 Modo Vector: ACTIVO [V]" : "📏 Modo Vector: Inactivo [V]",
                Foreground = VectorService.IsVectorMode ? Brushes.Gold : Brushes.White,
                Background = new SolidColorBrush(VectorService.IsVectorMode ? Color.FromArgb(70, 255, 215, 0) : Color.FromArgb(40, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(VectorService.IsVectorMode ? Colors.Gold : Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 0, 0, 4),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };

            Action updateVectorBtn = () => {
                vectorToggleBtn.Content = VectorService.IsVectorMode ? "🏹 Modo Vector: ACTIVO [V]" : "📏 Modo Vector: Inactivo [V]";
                vectorToggleBtn.Foreground = VectorService.IsVectorMode ? Brushes.Gold : Brushes.White;
                vectorToggleBtn.Background = new SolidColorBrush(VectorService.IsVectorMode ? Color.FromArgb(70, 255, 215, 0) : Color.FromArgb(40, 255, 255, 255));
                vectorToggleBtn.BorderBrush = new SolidColorBrush(VectorService.IsVectorMode ? Colors.Gold : Color.FromArgb(80, 255, 255, 255));
            };

            vectorToggleBtn.Click += (s, e) => {
                VectorService.ToggleVectorMode();
            };

            VectorService.VectorModeChanged += (s, e) => {
                updateVectorBtn();
            };

            mainStack.Children.Add(vectorToggleBtn);

            // Botón conmutador de Cota Técnica / Módulo y Ángulo [D]
            Button dimToggleBtn = new Button {
                Content = VectorService.IsDimensionEnabled ? "🏷️ Cota (Módulo/Ángulo): SÍ [D]" : "🏷️ Cota (Módulo/Ángulo): NO [D]",
                Foreground = VectorService.IsDimensionEnabled ? Brushes.LightGreen : Brushes.White,
                Background = new SolidColorBrush(VectorService.IsDimensionEnabled ? Color.FromArgb(70, 0, 200, 100) : Color.FromArgb(40, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(VectorService.IsDimensionEnabled ? Colors.LightGreen : Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 0, 0, 8),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };

            Action updateDimBtn = () => {
                dimToggleBtn.Content = VectorService.IsDimensionEnabled ? "🏷️ Cota (Módulo/Ángulo): SÍ [D]" : "🏷️ Cota (Módulo/Ángulo): NO [D]";
                dimToggleBtn.Foreground = VectorService.IsDimensionEnabled ? Brushes.LightGreen : Brushes.White;
                dimToggleBtn.Background = new SolidColorBrush(VectorService.IsDimensionEnabled ? Color.FromArgb(70, 0, 200, 100) : Color.FromArgb(40, 255, 255, 255));
                dimToggleBtn.BorderBrush = new SolidColorBrush(VectorService.IsDimensionEnabled ? Colors.LightGreen : Color.FromArgb(80, 255, 255, 255));
            };

            dimToggleBtn.Click += (s, e) => {
                VectorService.ToggleDimensionMode();
            };

            VectorService.DimensionModeChanged += (s, e) => {
                updateDimBtn();
            };

            mainStack.Children.Add(dimToggleBtn);

            // 3. Desplegable de Sellos y Widget de Número
            _stampContainerBorder = CreateDropdownRow("⭐ Sellos:", out _stampComboBox);
            string[] stampIds = new string[] { "stamp_check", "stamp_cross", "stamp_star", "stamp_arrow", "stamp_number" };
            foreach (string sid in stampIds) {
                ITool stool = FindTool(sid);
                if (stool != null) {
                    string sc = GetShortcutForTool(stool.Id);
                    _stampComboBox.Items.Add(new ToolComboItem {
                        ToolId = stool.Id,
                        DisplayText = stool.Icon + " " + stool.DisplayName + sc
                    });
                }
            }
            _stampComboBox.SelectionChanged += (s, e) => {
                if (_isUpdatingComboSelection) return;
                ToolComboItem selected = _stampComboBox.SelectedItem as ToolComboItem;
                if (selected != null) {
                    _toolRegistry.SelectTool(selected.ToolId);
                }
            };
            mainStack.Children.Add(_stampContainerBorder);

            if (_sequenceTool != null) {
                // Fila de Control Numérico (Nº de inicio y reseteo)
                Grid numGrid = new Grid { Margin = new Thickness(2, 0, 2, 4) };
                numGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                numGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                numGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock numLabel = new TextBlock {
                    Text = "🔢 Nº inicio:",
                    Foreground = Brushes.LightGray,
                    FontSize = 10,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0)
                };
                Grid.SetColumn(numLabel, 0);
                numGrid.Children.Add(numLabel);

                _numberTextBox = new TextBox {
                    Text = _sequenceTool.NextNumber.ToString(),
                    Height = 20,
                    FontSize = 11,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromArgb(160, 40, 43, 48)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                _numberTextBox.TextChanged += (s, e) => {
                    int val;
                    if (int.TryParse(_numberTextBox.Text, out val) && val > 0) {
                        _sequenceTool.NextNumber = val;
                    }
                };
                Grid.SetColumn(_numberTextBox, 1);
                numGrid.Children.Add(_numberTextBox);

                Button resetNumBtn = new Button {
                    Content = "🔄 Reset",
                    FontSize = 10,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromArgb(160, 50, 53, 59)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(4, 0, 0, 0),
                    Padding = new Thickness(4, 1, 4, 1),
                    Cursor = Cursors.Hand
                };
                resetNumBtn.Click += (s, e) => _sequenceTool.Reset();
                Grid.SetColumn(resetNumBtn, 2);
                numGrid.Children.Add(resetNumBtn);

                mainStack.Children.Add(numGrid);
            }

            mainStack.Children.Add(CreateSeparator());

            // 4. Selector Universal de Grosor DESLIZANTE (Slider 1 a 30 px)
            Grid sizeGrid = new Grid { Margin = new Thickness(0, 3, 0, 5) };
            sizeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            sizeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sizeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock sizeLabel = new TextBlock {
                Text = "📏 Grosor:",
                Foreground = Brushes.White,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(sizeLabel, 0);
            sizeGrid.Children.Add(sizeLabel);

            _sizeSlider = new Slider {
                Minimum = 1.0,
                Maximum = 30.0,
                Value = _context.CurrentSize,
                TickFrequency = 1.0,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            _sizeSlider.ValueChanged += (s, e) => {
                _context.CurrentSize = e.NewValue;
                _toolRegistry.NotifySizeChanged(e.NewValue);
                if (_sizeValueText != null) {
                    _sizeValueText.Text = string.Format("{0:0} px", e.NewValue);
                }
            };
            Grid.SetColumn(_sizeSlider, 1);
            sizeGrid.Children.Add(_sizeSlider);

            _sizeValueText = new TextBlock {
                Text = string.Format("{0:0} px", _context.CurrentSize),
                Foreground = Brushes.SkyBlue,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 36,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(4, 0, 0, 0)
            };
            Grid.SetColumn(_sizeValueText, 2);
            sizeGrid.Children.Add(_sizeValueText);

            mainStack.Children.Add(sizeGrid);

            mainStack.Children.Add(CreateSeparator());

            // 5. Paleta de Colores
            StackPanel colorsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 6) };
            colorsPanel.Children.Add(CreateColorButton(Colors.Red));
            colorsPanel.Children.Add(CreateColorButton(Colors.Blue));
            colorsPanel.Children.Add(CreateColorButton(Colors.LimeGreen));
            colorsPanel.Children.Add(CreateColorButton(Colors.Gold));
            colorsPanel.Children.Add(CreateColorButton(Colors.White));
            colorsPanel.Children.Add(CreateColorButton(Colors.Black));
            mainStack.Children.Add(colorsPanel);

            mainStack.Children.Add(CreateSeparator());

            // 6. Fondos de Pantalla
            WrapPanel bgPanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 6) };
            bgPanel.Children.Add(CreateButton("🖥️ Pantalla", () => _context.SetBackgroundMode(BackgroundMode.Transparent)));
            bgPanel.Children.Add(CreateButton("⬜ Blanca", () => _context.SetBackgroundMode(BackgroundMode.Whiteboard)));
            bgPanel.Children.Add(CreateButton("⬛ Pizarra", () => _context.SetBackgroundMode(BackgroundMode.Blackboard)));
            mainStack.Children.Add(bgPanel);

            mainStack.Children.Add(CreateSeparator());

            // 7. Acciones Rápidas
            WrapPanel actionsPanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
            actionsPanel.Children.Add(CreateButton("↩️ Deshacer [Ctrl+Z]", () => _context.Undo()));
            actionsPanel.Children.Add(CreateButton("🗑️ Limpiar [C]", () => _context.Clear()));
            actionsPanel.Children.Add(CreateButton("📸 Capturar Sector [Ctrl+S]", () => {
                string savedPath = _context.SaveScreenshot();
                if (!string.IsNullOrEmpty(savedPath)) {
                    MessageBox.Show("Sector capturado y copiado al portapapeles con éxito:\n" + savedPath,
                        "Captura de Sector", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }));
            mainStack.Children.Add(actionsPanel);

            rootBorder.Child = mainStack;
            Content = rootBorder;

            if (_toolRegistry.ActiveTool != null) {
                HighlightActiveTool(_toolRegistry.ActiveTool.Id);
            }
        }

        private void OnNumberChanged(int newNumber) {
            Dispatcher.Invoke(new Action(() => {
                if (_numberTextBox != null && _numberTextBox.Text != newNumber.ToString()) {
                    _numberTextBox.Text = newNumber.ToString();
                }
            }));
        }

        private Border CreateDropdownRow(string labelText, out ComboBox comboBox) {
            Border border = new Border {
                Background = new SolidColorBrush(Color.FromArgb(120, 45, 48, 54)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(0, 0, 0, 4),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1)
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock label = new TextBlock {
                Text = labelText,
                Foreground = Brushes.White,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            comboBox = new ComboBox {
                Height = 23,
                FontSize = 11,
                Cursor = Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(comboBox, 1);
            grid.Children.Add(comboBox);

            border.Child = grid;
            return border;
        }

        private ITool FindTool(string id) {
            foreach (ITool t in _toolRegistry.Tools) {
                if (t.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) return t;
            }
            return null;
        }

        private string GetShortcutForTool(string toolId) {
            switch (toolId.ToLower()) {
                case "mouse": return " [Esc]";
                case "pen": return " [P]";
                case "highlighter": return " [H]";
                case "eraser": return " [E]";
                case "text": return " [T]";
                case "shape_arrow": return " [F]";
                case "shape_rect": return " [Q]";
                case "shape_ellipse": return " [O]";
                case "shape_line": return " [L]";
                case "shape_protractor": return " [J]";
                case "geom_ruler": return " [M]";
                case "geom_compass": return " [U]";
                case "shape_callout_rect": return " [B]";
                case "stamp_check": return " [K]";
                case "stamp_cross": return " [X]";
                case "stamp_star": return " [R]";
                case "stamp_arrow": return " [A]";
                case "stamp_number": return " [N]";
                default: return "";
            }
        }

        private void OnActiveToolChanged(ITool tool) {
            if (tool != null) {
                HighlightActiveTool(tool.Id);
            }
        }

        private void HighlightActiveTool(string activeId) {
            bool isGeometry = activeId.StartsWith("geom_", StringComparison.OrdinalIgnoreCase) || activeId.Equals("shape_protractor", StringComparison.OrdinalIgnoreCase);
            bool isShape = !isGeometry && activeId.StartsWith("shape_", StringComparison.OrdinalIgnoreCase);
            bool isStamp = activeId.StartsWith("stamp_", StringComparison.OrdinalIgnoreCase);

            // Actualizar botones principales
            foreach (KeyValuePair<string, Button> kv in _mainToolButtons) {
                if (kv.Key.Equals(activeId, StringComparison.OrdinalIgnoreCase)) {
                    kv.Value.Background = new SolidColorBrush(Color.FromArgb(200, 0, 122, 204));
                    kv.Value.BorderBrush = Brushes.SkyBlue;
                } else {
                    kv.Value.Background = new SolidColorBrush(Color.FromArgb(160, 50, 53, 59));
                    kv.Value.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                }
            }

            // Actualizar selector de formas
            if (isShape && _shapeComboBox != null && _shapeContainerBorder != null) {
                _shapeContainerBorder.BorderBrush = Brushes.SkyBlue;
                _shapeContainerBorder.Background = new SolidColorBrush(Color.FromArgb(180, 0, 90, 150));

                _isUpdatingComboSelection = true;
                for (int i = 0; i < _shapeComboBox.Items.Count; i++) {
                    ToolComboItem item = _shapeComboBox.Items[i] as ToolComboItem;
                    if (item != null && item.ToolId.Equals(activeId, StringComparison.OrdinalIgnoreCase)) {
                        _shapeComboBox.SelectedIndex = i;
                        break;
                    }
                }
                _isUpdatingComboSelection = false;
            } else if (_shapeContainerBorder != null) {
                _shapeContainerBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                _shapeContainerBorder.Background = new SolidColorBrush(Color.FromArgb(120, 45, 48, 54));
            }

            // Actualizar selector de geometría
            if (isGeometry && _geometryComboBox != null && _geometryContainerBorder != null) {
                _geometryContainerBorder.BorderBrush = Brushes.SkyBlue;
                _geometryContainerBorder.Background = new SolidColorBrush(Color.FromArgb(180, 0, 90, 150));

                _isUpdatingComboSelection = true;
                for (int i = 0; i < _geometryComboBox.Items.Count; i++) {
                    ToolComboItem item = _geometryComboBox.Items[i] as ToolComboItem;
                    if (item != null && item.ToolId.Equals(activeId, StringComparison.OrdinalIgnoreCase)) {
                        _geometryComboBox.SelectedIndex = i;
                        break;
                    }
                }
                _isUpdatingComboSelection = false;
            } else if (_geometryContainerBorder != null) {
                _geometryContainerBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                _geometryContainerBorder.Background = new SolidColorBrush(Color.FromArgb(120, 45, 48, 54));
            }

            // Actualizar selector de sellos
            if (isStamp && _stampComboBox != null && _stampContainerBorder != null) {
                _stampContainerBorder.BorderBrush = Brushes.SkyBlue;
                _stampContainerBorder.Background = new SolidColorBrush(Color.FromArgb(180, 0, 90, 150));

                _isUpdatingComboSelection = true;
                for (int i = 0; i < _stampComboBox.Items.Count; i++) {
                    ToolComboItem item = _stampComboBox.Items[i] as ToolComboItem;
                    if (item != null && item.ToolId.Equals(activeId, StringComparison.OrdinalIgnoreCase)) {
                        _stampComboBox.SelectedIndex = i;
                        break;
                    }
                }
                _isUpdatingComboSelection = false;
            } else if (_stampContainerBorder != null) {
                _stampContainerBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                _stampContainerBorder.Background = new SolidColorBrush(Color.FromArgb(120, 45, 48, 54));
            }
        }

        private Button CreateButton(string text, Action onClick) {
            Button btn = new Button {
                Content = text,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(160, 50, 53, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2),
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 11,
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private UIElement CreateColorButton(Color color) {
            Button btn = new Button {
                Width = 22,
                Height = 22,
                Margin = new Thickness(3, 0, 3, 0),
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(color),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1)
            };
            btn.Click += (s, e) => {
                _context.CurrentColor = color;
                _toolRegistry.NotifyColorChanged(color);
            };
            return btn;
        }

        private UIElement CreateSeparator() {
            return new Rectangle {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Margin = new Thickness(0, 2, 0, 4)
            };
        }
    }
}

