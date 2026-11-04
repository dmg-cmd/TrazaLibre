using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Trazalibre.Core;
using Trazalibre.Services;
using Trazalibre.Tools;

namespace Trazalibre.UI {
    public class ToolbarWindow : Window {
        private class ToolComboItem {
            public string ToolId { get; set; }
            public string DisplayText { get; set; }
            public override string ToString() { return DisplayText; }
        }

        private class BackgroundComboItem {
            public BackgroundMode Mode { get; set; }
            public string DisplayText { get; set; }
            public override string ToString() { return DisplayText; }
        }

        private readonly ToolRegistry _toolRegistry;
        private readonly IAnnotationContext _context;
        private readonly ISequenceTool _sequenceTool;
        private readonly Dictionary<string, Button> _mainToolButtons = new Dictionary<string, Button>();
        private readonly Dictionary<BackgroundMode, Button> _bgQuickButtons = new Dictionary<BackgroundMode, Button>();

        private ComboBox _shapeComboBox;
        private Border _shapeContainerBorder;
        private ComboBox _geometryComboBox;
        private Border _geometryContainerBorder;
        private Button _axesButton;
        private Button _vectorButton;
        private ComboBox _stampComboBox;
        private Border _stampContainerBorder;
        private ComboBox _bgComboBox;
        private TextBox _numberTextBox;
        private Slider _sizeSlider;
        private TextBlock _sizeValueText;
        private bool _isUpdatingComboSelection = false;
        private bool _isUpdatingBgSelection = false;

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

            ThemeService.ThemeChanged += (s, e) => {
                Dispatcher.Invoke(new Action(() => {
                    BuildUI();
                }));
            };

            BackgroundService.BackgroundModeChanged += (s, e) => {
                Dispatcher.Invoke(new Action(SyncBackgroundControls));
            };
        }

        private void BuildUI() {
            _mainToolButtons.Clear();
            _bgQuickButtons.Clear();

            Border rootBorder = new Border {
                Background = ThemeService.GetWindowBackground(),
                CornerRadius = new CornerRadius(11),
                BorderBrush = ThemeService.GetWindowBorderBrush(),
                BorderThickness = new Thickness(1.4),
                Padding = new Thickness(10),
                MaxWidth = 278
            };

            rootBorder.MouseDown += (s, e) => {
                if (e.LeftButton == MouseButtonState.Pressed) {
                    DragMove();
                }
            };

            StackPanel mainStack = new StackPanel { Orientation = Orientation.Vertical };

            // Encabezado con Icono Oficial, Indicador de Ejecución y Conmutador de Aspecto
            Grid headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel brandStack = new StackPanel {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            ImageSource appIcon = SplashWindow.TryLoadAppIcon();
            if (appIcon != null) {
                Image iconImg = new Image {
                    Source = appIcon,
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(iconImg, BitmapScalingMode.HighQuality);
                brandStack.Children.Add(iconImg);
            }

            TextBlock titleText = new TextBlock {
                Text = "Trazalibre",
                Foreground = ThemeService.GetPrimaryTextBrush(),
                FontWeight = FontWeights.Bold,
                FontSize = 13.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            brandStack.Children.Add(titleText);

            Border statusBadge = new Border {
                Background = new SolidColorBrush(Color.FromArgb(65, 0, 210, 110)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(170, 0, 220, 120)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Trazalibre está activo y en ejecución"
            };
            statusBadge.Child = new TextBlock {
                Text = "● Activo",
                Foreground = ThemeService.CurrentTheme == AppTheme.Light
                    ? new SolidColorBrush(Color.FromRgb(0, 135, 65))
                    : Brushes.LightGreen,
                FontSize = 9,
                FontWeight = FontWeights.Bold
            };
            brandStack.Children.Add(statusBadge);

            Grid.SetColumn(brandStack, 0);
            headerGrid.Children.Add(brandStack);

            // Botón para cambiar el aspecto visual (Oscuro / Claro / Azul CAD)
            Button themeBtn = new Button {
                Content = ThemeService.GetThemeDisplayName(),
                Foreground = ThemeService.GetPrimaryTextBrush(),
                Background = ThemeService.GetButtonIdleBackground(),
                BorderBrush = ThemeService.GetButtonIdleBorder(),
                BorderThickness = new Thickness(1),
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(4, 0, 4, 0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Cambiar aspecto visual de la barra (Oscuro / Claro / Azul CAD)"
            };
            themeBtn.Click += (s, e) => ThemeService.CycleTheme();
            Grid.SetColumn(themeBtn, 1);
            headerGrid.Children.Add(themeBtn);

            Button closeBtn = new Button {
                Content = "✕",
                Foreground = ThemeService.GetPrimaryTextBrush(),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                Width = 24,
                Height = 24,
                ToolTip = "Cerrar Trazalibre"
            };
            closeBtn.Click += (s, e) => Application.Current.Shutdown();
            Grid.SetColumn(closeBtn, 2);

            headerGrid.Children.Add(closeBtn);
            mainStack.Children.Add(headerGrid);

            // 1. Herramientas Principales (Botones rápidos: Ratón, Lápiz, Marca, Borra, Texto, Indicación)
            WrapPanel mainToolsPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            string[] primaryToolIds = new string[] { "mouse", "pen", "highlighter", "eraser", "text", "tool_instruction" };
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
                "shape_vector",
                "shape_arrow",
                "tool_instruction",
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
                "geom_axes",
                "shape_vector",
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

            // Botón dedicado de Ejes Coordenados (0, 0) [G]
            _axesButton = new Button {
                Content = "🌐 Ejes Coordenados (0, 0) [G]",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(160, 45, 50, 60)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 0, 162, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 0, 0, 4),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            _axesButton.Click += (s, e) => {
                _toolRegistry.SelectTool("geom_axes");
            };
            mainStack.Children.Add(_axesButton);

            // Fila de control de incremento para los Ejes de Coordenadas
            Grid axesSizeGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            axesSizeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            axesSizeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            axesSizeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Button btnAxesDec = new Button {
                Content = "➖",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(140, 50, 53, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Width = 28,
                Height = 22,
                Cursor = Cursors.Hand,
                ToolTip = "Reducir tamaño de ejes (-10 mm)"
            };

            TextBlock txtAxesSize = new TextBlock {
                Text = string.Format("Ejes: {0:0} mm", MetricUnits.PxToMm(AxesTool.SharedAxisHalfLength)),
                Foreground = Brushes.LightCyan,
                FontSize = 10.0,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Button btnAxesInc = new Button {
                Content = "➕",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(140, 50, 53, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Width = 28,
                Height = 22,
                Cursor = Cursors.Hand,
                ToolTip = "Incrementar tamaño de ejes (+10 mm)"
            };

            Action updateAxesSizeLabel = () => {
                txtAxesSize.Text = string.Format("Ejes: {0:0} mm", MetricUnits.PxToMm(AxesTool.SharedAxisHalfLength));
            };

            btnAxesDec.Click += (s, e) => {
                AxesTool tool = FindTool("geom_axes") as AxesTool;
                if (tool != null) tool.DecreaseSize(10.0);
                else AxesTool.SharedAxisHalfLength = Math.Max(50.0, AxesTool.SharedAxisHalfLength - MetricUnits.MmToPx(10.0));
                updateAxesSizeLabel();
            };

            btnAxesInc.Click += (s, e) => {
                AxesTool tool = FindTool("geom_axes") as AxesTool;
                if (tool != null) tool.IncreaseSize(10.0);
                else AxesTool.SharedAxisHalfLength = Math.Min(900.0, AxesTool.SharedAxisHalfLength + MetricUnits.MmToPx(10.0));
                updateAxesSizeLabel();
            };

            AxesTool.AxisLengthChanged += (s, newLen) => {
                updateAxesSizeLabel();
            };

            Grid.SetColumn(btnAxesDec, 0);
            Grid.SetColumn(txtAxesSize, 1);
            Grid.SetColumn(btnAxesInc, 2);

            axesSizeGrid.Children.Add(btnAxesDec);
            axesSizeGrid.Children.Add(txtAxesSize);
            axesSizeGrid.Children.Add(btnAxesInc);

            mainStack.Children.Add(axesSizeGrid);

            // Botón dedicado de Vector (2 Puntos) con Medidas [V]
            _vectorButton = new Button {
                Content = "🏹 Vector con Medidas [V]",
                Foreground = Brushes.Gold,
                Background = new SolidColorBrush(Color.FromArgb(160, 45, 50, 60)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 215, 0)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 0, 0, 4),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            _vectorButton.Click += (s, e) => {
                _toolRegistry.SelectTool("shape_vector");
            };
            mainStack.Children.Add(_vectorButton);

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
                Foreground = ThemeService.GetPrimaryTextBrush(),
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
                Foreground = ThemeService.GetAccentTextBrush(),
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

            // 4b. Selector de Fuente, Tamaño y Estilo de Texto
            Border fontContainer = new Border {
                Background = ThemeService.GetSectionBackground(),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 2, 0, 4),
                BorderBrush = ThemeService.GetSectionBorderBrush(),
                BorderThickness = new Thickness(1)
            };

            StackPanel fontStack = new StackPanel { Orientation = Orientation.Vertical };

            // Fila 1: Selector de Familia de Fuente
            Grid fontFamilyRow = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            fontFamilyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            fontFamilyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock fontLabel = new TextBlock {
                Text = "🔤 Fuente:",
                Foreground = ThemeService.GetPrimaryTextBrush(),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(fontLabel, 0);
            fontFamilyRow.Children.Add(fontLabel);

            ComboBox fontCombo = new ComboBox {
                Height = 22,
                FontSize = 11,
                Cursor = Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            bool updatingFontUI = false;
            foreach (string fName in TextStyleService.AvailableFonts) {
                ComboBoxItem fItem = new ComboBoxItem {
                    Content = fName,
                    FontFamily = new FontFamily(fName),
                    FontSize = 11
                };
                fontCombo.Items.Add(fItem);
                if (fName.Equals(TextStyleService.FontFamilyName, StringComparison.OrdinalIgnoreCase)) {
                    fontCombo.SelectedItem = fItem;
                }
            }
            fontCombo.SelectionChanged += (s, e) => {
                if (updatingFontUI) return;
                ComboBoxItem sel = fontCombo.SelectedItem as ComboBoxItem;
                if (sel != null && sel.Content != null) {
                    TextStyleService.FontFamilyName = sel.Content.ToString();
                }
            };
            Grid.SetColumn(fontCombo, 1);
            fontFamilyRow.Children.Add(fontCombo);
            fontStack.Children.Add(fontFamilyRow);

            // Fila 2: Tamaño de letra (A- / pt / A+) y Estilos (Negrita B / Cursiva I)
            Grid fontSizeRow = new Grid();
            fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock txtSizeTitle = new TextBlock {
                Text = "Tamaño:",
                Foreground = Brushes.LightGray,
                FontSize = 10.5,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(txtSizeTitle, 0);
            fontSizeRow.Children.Add(txtSizeTitle);

            Button btnFontDec = new Button {
                Content = "A-",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(160, 50, 53, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Width = 28,
                Height = 21,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                ToolTip = "Reducir tamaño de texto (-2 pt)"
            };
            btnFontDec.Click += (s, e) => TextStyleService.DecreaseSize(2.0);
            Grid.SetColumn(btnFontDec, 1);
            fontSizeRow.Children.Add(btnFontDec);

            TextBlock txtFontSizeVal = new TextBlock {
                Text = string.Format("{0:0} pt", TextStyleService.FontSize),
                Foreground = Brushes.SkyBlue,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtFontSizeVal, 2);
            fontSizeRow.Children.Add(txtFontSizeVal);

            Button btnFontInc = new Button {
                Content = "A+",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(160, 50, 53, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Width = 28,
                Height = 21,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                ToolTip = "Aumentar tamaño de texto (+2 pt)"
            };
            btnFontInc.Click += (s, e) => TextStyleService.IncreaseSize(2.0);
            Grid.SetColumn(btnFontInc, 3);
            fontSizeRow.Children.Add(btnFontInc);

            Button btnBold = new Button {
                Content = "B",
                Width = 24,
                Height = 21,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(6, 0, 2, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Alternar texto en Negrita"
            };
            btnBold.Click += (s, e) => TextStyleService.ToggleBold();
            Grid.SetColumn(btnBold, 4);
            fontSizeRow.Children.Add(btnBold);

            Button btnItalic = new Button {
                Content = "I",
                Width = 24,
                Height = 21,
                FontSize = 10.5,
                FontStyle = FontStyles.Italic,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                ToolTip = "Alternar texto en Cursiva"
            };
            btnItalic.Click += (s, e) => TextStyleService.ToggleItalic();
            Grid.SetColumn(btnItalic, 5);
            fontSizeRow.Children.Add(btnItalic);

            Action syncFontToolbarUI = () => {
                updatingFontUI = true;
                txtFontSizeVal.Text = string.Format("{0:0} pt", TextStyleService.FontSize);
                for (int i = 0; i < fontCombo.Items.Count; i++) {
                    ComboBoxItem item = fontCombo.Items[i] as ComboBoxItem;
                    if (item != null && item.Content != null &&
                        item.Content.ToString().Equals(TextStyleService.FontFamilyName, StringComparison.OrdinalIgnoreCase)) {
                        fontCombo.SelectedIndex = i;
                        break;
                    }
                }
                btnBold.Background = TextStyleService.IsBold
                    ? new SolidColorBrush(Color.FromArgb(220, 0, 122, 204))
                    : new SolidColorBrush(Color.FromArgb(160, 50, 53, 59));
                btnBold.BorderBrush = TextStyleService.IsBold
                    ? Brushes.SkyBlue
                    : new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                btnBold.Foreground = TextStyleService.IsBold ? Brushes.White : Brushes.LightGray;

                btnItalic.Background = TextStyleService.IsItalic
                    ? new SolidColorBrush(Color.FromArgb(220, 0, 122, 204))
                    : new SolidColorBrush(Color.FromArgb(160, 50, 53, 59));
                btnItalic.BorderBrush = TextStyleService.IsItalic
                    ? Brushes.SkyBlue
                    : new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                btnItalic.Foreground = TextStyleService.IsItalic ? Brushes.White : Brushes.LightGray;
                updatingFontUI = false;
            };

            syncFontToolbarUI();
            TextStyleService.StyleChanged += (s, e) => {
                Dispatcher.Invoke(new Action(syncFontToolbarUI));
            };

            fontStack.Children.Add(fontSizeRow);
            fontContainer.Child = fontStack;
            mainStack.Children.Add(fontContainer);

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

            // 6. Fondos de Pantalla y Lienzos Técnicos
            Border bgContainer = CreateDropdownRow("🖼️ Fondo:", out _bgComboBox);
            BackgroundComboItem[] bgItems = new BackgroundComboItem[] {
                new BackgroundComboItem { Mode = BackgroundMode.Transparent, DisplayText = "🖥️ Escritorio (Transparente)" },
                new BackgroundComboItem { Mode = BackgroundMode.Whiteboard,  DisplayText = "⬜ Pizarra Clara (Blanca)" },
                new BackgroundComboItem { Mode = BackgroundMode.Blackboard,  DisplayText = "⬛ Pizarra Oscura (Grafito)" },
                new BackgroundComboItem { Mode = BackgroundMode.LightGrid,   DisplayText = "📐 Cuadrícula Clara (Milimetrada)" },
                new BackgroundComboItem { Mode = BackgroundMode.DarkGrid,    DisplayText = "🌐 Cuadrícula Oscura (CAD)" },
                new BackgroundComboItem { Mode = BackgroundMode.Blueprint,   DisplayText = "🟦 Plano Azul Ingeniería (Blueprint)" },
                new BackgroundComboItem { Mode = BackgroundMode.DotGrid,     DisplayText = "📄 Hoja Punteada (Dot Grid)" }
            };
            foreach (BackgroundComboItem bItem in bgItems) {
                _bgComboBox.Items.Add(bItem);
            }
            _bgComboBox.SelectionChanged += (s, e) => {
                if (_isUpdatingBgSelection) return;
                BackgroundComboItem sel = _bgComboBox.SelectedItem as BackgroundComboItem;
                if (sel != null) {
                    _context.SetBackgroundMode(sel.Mode);
                    SyncBackgroundControls();
                }
            };
            mainStack.Children.Add(bgContainer);

            WrapPanel bgPanel = new WrapPanel { Margin = new Thickness(0, 2, 0, 6) };
            AddQuickBgButton(bgPanel, "🖥️ Escritorio", BackgroundMode.Transparent);
            AddQuickBgButton(bgPanel, "⬜ Claro", BackgroundMode.Whiteboard);
            AddQuickBgButton(bgPanel, "⬛ Oscuro", BackgroundMode.Blackboard);
            AddQuickBgButton(bgPanel, "📐 Cuadrícula", BackgroundMode.LightGrid);
            AddQuickBgButton(bgPanel, "🟦 Plano CAD", BackgroundMode.Blueprint);
            mainStack.Children.Add(bgPanel);
            SyncBackgroundControls();

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
                Background = ThemeService.GetSectionBackground(),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(0, 0, 0, 4),
                BorderBrush = ThemeService.GetSectionBorderBrush(),
                BorderThickness = new Thickness(1)
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock label = new TextBlock {
                Text = labelText,
                Foreground = ThemeService.GetPrimaryTextBrush(),
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

        private void AddQuickBgButton(WrapPanel panel, string label, BackgroundMode mode) {
            Button btn = CreateButton(label, () => {
                _context.SetBackgroundMode(mode);
                SyncBackgroundControls();
            });
            _bgQuickButtons[mode] = btn;
            panel.Children.Add(btn);
        }

        private void SyncBackgroundControls() {
            BackgroundMode current = BackgroundService.CurrentMode;

            if (_bgComboBox != null) {
                _isUpdatingBgSelection = true;
                for (int i = 0; i < _bgComboBox.Items.Count; i++) {
                    BackgroundComboItem item = _bgComboBox.Items[i] as BackgroundComboItem;
                    if (item != null && item.Mode == current) {
                        _bgComboBox.SelectedIndex = i;
                        break;
                    }
                }
                _isUpdatingBgSelection = false;
            }

            foreach (KeyValuePair<BackgroundMode, Button> kv in _bgQuickButtons) {
                if (kv.Key == current) {
                    kv.Value.Background = ThemeService.GetButtonActiveBackground();
                    kv.Value.BorderBrush = Brushes.SkyBlue;
                    kv.Value.Foreground = Brushes.White;
                    kv.Value.FontWeight = FontWeights.SemiBold;
                } else {
                    kv.Value.Background = ThemeService.GetButtonIdleBackground();
                    kv.Value.BorderBrush = ThemeService.GetButtonIdleBorder();
                    kv.Value.Foreground = ThemeService.GetPrimaryTextBrush();
                    kv.Value.FontWeight = FontWeights.Normal;
                }
            }
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
                case "tool_instruction": return " [I]";
                case "shape_arrow": return " [F]";
                case "shape_rect": return " [Q]";
                case "shape_ellipse": return " [O]";
                case "shape_line": return " [L]";
                case "shape_vector": return " [V]";
                case "geom_axes": return " [G]";
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
            bool isShape = !isGeometry && (activeId.StartsWith("shape_", StringComparison.OrdinalIgnoreCase) || activeId.Equals("tool_instruction", StringComparison.OrdinalIgnoreCase));
            bool isStamp = activeId.StartsWith("stamp_", StringComparison.OrdinalIgnoreCase);

            // Actualizar botones principales
            foreach (KeyValuePair<string, Button> kv in _mainToolButtons) {
                if (kv.Key.Equals(activeId, StringComparison.OrdinalIgnoreCase)) {
                    kv.Value.Background = ThemeService.GetButtonActiveBackground();
                    kv.Value.BorderBrush = Brushes.SkyBlue;
                    kv.Value.Foreground = Brushes.White;
                } else {
                    kv.Value.Background = ThemeService.GetButtonIdleBackground();
                    kv.Value.BorderBrush = ThemeService.GetButtonIdleBorder();
                    kv.Value.Foreground = ThemeService.GetPrimaryTextBrush();
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
                _shapeContainerBorder.BorderBrush = ThemeService.GetSectionBorderBrush();
                _shapeContainerBorder.Background = ThemeService.GetSectionBackground();
            }

            // Actualizar botón dedicado de ejes de coordenadas
            if (_axesButton != null) {
                bool isAxes = activeId.Equals("geom_axes", StringComparison.OrdinalIgnoreCase);
                _axesButton.Background = isAxes ? ThemeService.GetButtonActiveBackground() : ThemeService.GetButtonIdleBackground();
                _axesButton.BorderBrush = isAxes ? Brushes.Gold : new SolidColorBrush(Color.FromArgb(140, 0, 162, 255));
                _axesButton.Foreground = isAxes ? Brushes.Gold : ThemeService.GetPrimaryTextBrush();
            }

            // Actualizar botón dedicado de vector con medidas
            if (_vectorButton != null) {
                bool isVector = activeId.Equals("shape_vector", StringComparison.OrdinalIgnoreCase);
                _vectorButton.Background = isVector ? ThemeService.GetButtonActiveBackground() : ThemeService.GetButtonIdleBackground();
                _vectorButton.BorderBrush = isVector ? Brushes.Gold : new SolidColorBrush(Color.FromArgb(140, 255, 215, 0));
                _vectorButton.Foreground = isVector
                    ? Brushes.Gold
                    : (ThemeService.CurrentTheme == AppTheme.Light ? new SolidColorBrush(Color.FromRgb(160, 95, 0)) : Brushes.Gold);
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
                _geometryContainerBorder.BorderBrush = ThemeService.GetSectionBorderBrush();
                _geometryContainerBorder.Background = ThemeService.GetSectionBackground();
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
                _stampContainerBorder.BorderBrush = ThemeService.GetSectionBorderBrush();
                _stampContainerBorder.Background = ThemeService.GetSectionBackground();
            }
        }

        private Button CreateButton(string text, Action onClick) {
            Button btn = new Button {
                Content = text,
                Foreground = ThemeService.GetPrimaryTextBrush(),
                Background = ThemeService.GetButtonIdleBackground(),
                BorderBrush = ThemeService.GetButtonIdleBorder(),
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
                BorderBrush = ThemeService.CurrentTheme == AppTheme.Light ? Brushes.DimGray : Brushes.White,
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
                Fill = ThemeService.GetSeparatorBrush(),
                Margin = new Thickness(0, 2, 0, 4)
            };
        }
    }
}

