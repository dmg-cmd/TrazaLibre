using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Trazalibre.Core;
using Trazalibre.Tools;

namespace Trazalibre.Services {
    /// <summary>
    /// Construye menús contextuales (clic derecho) unificados para la Regla, los instrumentos
    /// de Geometría, Formas y herramientas de dibujo sobre el lienzo.
    /// Se actualiza dinámicamente cada vez que se abre para reflejar la herramienta activa,
    /// sus ajustes específicos y el estado actual de grosor y color.
    /// </summary>
    public static class ToolContextMenuHelper {
        private static readonly Brush MenuBg = new SolidColorBrush(Color.FromRgb(28, 32, 40));
        private static readonly Brush MenuBorder = new SolidColorBrush(Color.FromArgb(180, 0, 162, 255));
        private static Point? _lastContextCanvasPoint;

        static ToolContextMenuHelper() {
            MenuBg.Freeze();
            MenuBorder.Freeze();
        }

        /// <summary>
        /// Crea el menú contextual unificado con acceso rápido a la Regla, elementos de Geometría
        /// y opciones contextuales de la herramienta activa.
        /// </summary>
        public static ContextMenu CreateGeometryContextMenu(IAnnotationContext context) {
            return CreateDynamicMenu(context, menu => PopulateGeometryMenu(menu, context));
        }

        /// <summary>
        /// Crea el menú contextual para la herramienta de Línea (shape_line).
        /// </summary>
        public static ContextMenu CreateLineContextMenu(IAnnotationContext context) {
            return CreateGeometryContextMenu(context);
        }

        /// <summary>
        /// Crea el menú contextual para la herramienta de Vector con Medidas (shape_vector).
        /// </summary>
        public static ContextMenu CreateVectorContextMenu(IAnnotationContext context) {
            return CreateGeometryContextMenu(context);
        }

        /// <summary>
        /// Obtiene el punto inicial de trabajo en el lienzo tras cambiar de herramienta,
        /// conservando la coordenada exacta donde se abrió el menú contextual para no perder la referencia.
        /// </summary>
        public static Point GetInitialWorkPoint(IAnnotationContext context, Point fallback) {
            if (_lastContextCanvasPoint.HasValue) {
                Point saved = _lastContextCanvasPoint.Value;
                _lastContextCanvasPoint = null;
                return saved;
            }

            if (context != null && context.Canvas != null) {
                Point current = Mouse.GetPosition(context.Canvas);
                double maxW = context.Canvas.ActualWidth > 0 ? context.Canvas.ActualWidth : SystemParameters.VirtualScreenWidth;
                double maxH = context.Canvas.ActualHeight > 0 ? context.Canvas.ActualHeight : SystemParameters.VirtualScreenHeight;
                if (current.X > 0 && current.Y > 0 && current.X <= maxW && current.Y <= maxH) {
                    return current;
                }
            }

            return fallback;
        }

        private static ContextMenu CreateDynamicMenu(IAnnotationContext context, Action<ContextMenu> populate) {
            ContextMenu menu = new ContextMenu {
                Background = MenuBg,
                BorderBrush = MenuBorder,
                BorderThickness = new Thickness(1),
                Foreground = Brushes.White
            };

            // Reconstruir ítems al abrir y capturar la coordenada actual del puntero sobre el lienzo
            menu.Opened += (s, e) => {
                if (s == menu && context != null && context.Canvas != null) {
                    _lastContextCanvasPoint = Mouse.GetPosition(context.Canvas);
                }
                menu.Items.Clear();
                populate(menu);
            };

            // Limpiar el punto guardado si el menú se cierra sin cambiar de herramienta
            menu.Closed += (s, e) => {
                if (s == menu && context != null && context.Canvas != null) {
                    context.Canvas.Dispatcher.BeginInvoke(new Action(() => {
                        _lastContextCanvasPoint = null;
                    }), DispatcherPriority.ContextIdle);
                }
            };

            populate(menu);
            return menu;
        }

        private static void PopulateGeometryMenu(ContextMenu menu, IAnnotationContext context) {
            ITool activeTool = (context != null && context.Registry != null) ? context.Registry.ActiveTool : null;
            string activeId = activeTool != null ? activeTool.Id : string.Empty;

            AddHeader(menu, GetHeaderTitle(activeTool, activeId));
            menu.Items.Add(new Separator());

            // Sección principal de cambio rápido entre Regla y elementos de Geometría
            AddHeader(menu, "📐 Instrumentos de Geometría");
            AddGeometryToolItems(menu, context, activeId);

            // Ajustes rápidos específicos del instrumento activo
            AddActiveToolOptions(menu, activeTool, activeId);

            // Submenús universales de estilo
            menu.Items.Add(new Separator());
            AddThicknessSubmenu(menu, context);
            AddColorSubmenu(menu, context);

            // Acciones rápidas inferiores
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateItem(
                "✏️ Cambiar a Lápiz Libre [P]",
                () => SelectTool(context, "pen"),
                activeId.Equals("pen", StringComparison.OrdinalIgnoreCase)));
            menu.Items.Add(CreateItem("📥 Volcar Dibujo al Documento [Ctrl+D]", () => {
                if (context != null) context.MergeIntoDocument();
            }));
            menu.Items.Add(CreateItem("↩️ Deshacer [Ctrl+Z]", () => {
                if (context != null) context.Undo();
            }));
        }

        private static string GetHeaderTitle(ITool activeTool, string activeId) {
            switch (activeId.ToLowerInvariant()) {
                case "geom_ruler": return "📏 Opciones de Regla y Geometría";
                case "geom_triangle45": return "📐 Opciones de Escuadra 45°";
                case "geom_triangle30": return "📐 Opciones de Escuadra 30° (Cartabón)";
                case "geom_protractor": return "📐 Opciones de Transportador 360°";
                case "geom_compass": return "🧭 Opciones de Compás";
                case "geom_axes": return "🌐 Opciones de Ejes Coordenados";
                case "shape_vector": return "🏹 Opciones de Vector";
                case "shape_line": return "📏 Opciones de Línea";
                case "pen": return "✏️ Opciones de Lápiz y Geometría";
                case "highlighter": return "🖍️ Opciones de Resaltador y Geometría";
                case "eraser": return "🧹 Opciones de Borrador y Geometría";
                default:
                    if (activeTool != null) {
                        return activeTool.Icon + " Opciones de " + activeTool.DisplayName;
                    }
                    return "📐 Regla y Geometría";
            }
        }

        private static void AddGeometryToolItems(ContextMenu menu, IAnnotationContext context, string activeId) {
            var geometryTools = new[] {
                new { Id = "geom_ruler", Label = "📏 Regla Graduada [M]" },
                new { Id = "geom_triangle45", Label = "📐 Escuadra 45°" },
                new { Id = "geom_triangle30", Label = "📐 Escuadra 30° / 60° (Cartabón)" },
                new { Id = "geom_protractor", Label = "📐 Transportador 360° [J]" },
                new { Id = "geom_compass", Label = "🧭 Compás de Precisión [U]" },
                new { Id = "geom_axes", Label = "🌐 Ejes Coordenados (0, 0) [G]" },
                new { Id = "shape_vector", Label = "🏹 Vector con Medidas [V]" },
                new { Id = "shape_line", Label = "📏 Línea Simple [L]" }
            };

            foreach (var tool in geometryTools) {
                string targetId = tool.Id;
                bool isCurrent = activeId.Equals(targetId, StringComparison.OrdinalIgnoreCase);
                menu.Items.Add(CreateItem(tool.Label, () => SelectTool(context, targetId), isCurrent));
            }
        }

        private static void AddActiveToolOptions(ContextMenu menu, ITool activeTool, string activeId) {
            bool isLinearOrAngular =
                activeId.Equals("geom_ruler", StringComparison.OrdinalIgnoreCase) ||
                activeId.Equals("geom_triangle45", StringComparison.OrdinalIgnoreCase) ||
                activeId.Equals("geom_triangle30", StringComparison.OrdinalIgnoreCase) ||
                activeId.Equals("geom_protractor", StringComparison.OrdinalIgnoreCase) ||
                activeId.Equals("shape_vector", StringComparison.OrdinalIgnoreCase) ||
                activeId.Equals("shape_line", StringComparison.OrdinalIgnoreCase);

            if (isLinearOrAngular) {
                menu.Items.Add(new Separator());
                menu.Items.Add(CreateItem(
                    "🏹 Punta de Flecha (Modo Vector) [V]",
                    () => { VectorService.ToggleVectorMode(); },
                    VectorService.IsVectorMode));
                menu.Items.Add(CreateItem(
                    "🏷️ Mostrar Cota (Módulo / Ángulo) [D]",
                    () => { VectorService.ToggleDimensionMode(); },
                    VectorService.IsDimensionEnabled));
                menu.Items.Add(CreateItem(
                    "📐 Marcar en Eje X y Eje Y (Polar ➔ Rect)",
                    () => { VectorService.ToggleRectangularProjections(); },
                    VectorService.ShowRectangularProjections));
                menu.Items.Add(CreateItem(
                    "📍 Ubicar Medida con el Ratón (Manual)",
                    () => { VectorService.ToggleManualLabelPlacement(); },
                    VectorService.ManualLabelPlacement));
            }

            RulerTool ruler = activeTool as RulerTool;
            if (ruler != null) {
                AddAngleSubmenu(menu, ruler.Angle, angle => ruler.SetAngle(angle));
                return;
            }

            Triangle45Tool tri45 = activeTool as Triangle45Tool;
            if (tri45 != null) {
                AddAngleSubmenu(menu, tri45.BaseAngle, angle => tri45.SetBaseAngle(angle));
                return;
            }

            Triangle30Tool tri30 = activeTool as Triangle30Tool;
            if (tri30 != null) {
                AddAngleSubmenu(menu, tri30.BaseAngle, angle => tri30.SetBaseAngle(angle));
                return;
            }

            ProtractorTool protractor = activeTool as ProtractorTool;
            if (protractor != null) {
                AddAngleSubmenu(menu, protractor.CurrentAngle, angle => protractor.SetAngle(angle));
                return;
            }

            CompassTool compass = activeTool as CompassTool;
            if (compass != null) {
                menu.Items.Add(new Separator());
                AddArcSpanSubmenu(menu, compass);
                return;
            }

            AxesTool axes = activeTool as AxesTool;
            if (axes != null) {
                menu.Items.Add(new Separator());
                AddAxesOptions(menu, axes);
            }
        }

        private static void AddAngleSubmenu(ContextMenu menu, double currentAngle, Action<double> setAngle) {
            double normalizedCurrent = currentAngle % 360.0;
            if (normalizedCurrent < 0) normalizedCurrent += 360.0;

            MenuItem root = new MenuItem {
                Header = "🔄 Ángulo Rápido",
                Foreground = Brushes.White,
                Background = MenuBg
            };

            var presets = new[] {
                new { Label = "0° (Horizontal)", Angle = 0.0 },
                new { Label = "30°", Angle = 30.0 },
                new { Label = "45°", Angle = 45.0 },
                new { Label = "60°", Angle = 60.0 },
                new { Label = "90° (Vertical)", Angle = 90.0 },
                new { Label = "135°", Angle = 135.0 },
                new { Label = "180°", Angle = 180.0 },
                new { Label = "270°", Angle = 270.0 }
            };

            foreach (var p in presets) {
                double targetAngle = p.Angle;
                bool isCurrent = Math.Abs(normalizedCurrent - targetAngle) < 0.5 ||
                                 (targetAngle == 0.0 && Math.Abs(normalizedCurrent - 360.0) < 0.5);
                root.Items.Add(CreateItem(p.Label, () => setAngle(targetAngle), isCurrent));
            }

            menu.Items.Add(root);
        }

        private static void AddArcSpanSubmenu(ContextMenu menu, CompassTool compass) {
            MenuItem root = new MenuItem {
                Header = "🔄 Amplitud de Arco",
                Foreground = Brushes.White,
                Background = MenuBg
            };

            var presets = new[] {
                new { Label = "360° (Circunferencia)", Span = 360.0 },
                new { Label = "270°", Span = 270.0 },
                new { Label = "180° (Semicírculo)", Span = 180.0 },
                new { Label = "90° (Cuarto)", Span = 90.0 },
                new { Label = "60°", Span = 60.0 },
                new { Label = "45°", Span = 45.0 }
            };

            foreach (var p in presets) {
                double targetSpan = p.Span;
                bool isCurrent = Math.Abs(compass.ArcSpan - targetSpan) < 0.5;
                root.Items.Add(CreateItem(p.Label, () => compass.SetArcSpan(targetSpan), isCurrent));
            }

            menu.Items.Add(root);
        }

        private static void AddAxesOptions(ContextMenu menu, AxesTool axes) {
            menu.Items.Add(CreateItem(
                "📏 Grilla Milimetrada en Ejes",
                () => {
                    AxesTool.SetMillimeterGrid(!AxesTool.ShowMillimeterGrid);
                    axes.InvalidatePreview();
                },
                AxesTool.ShowMillimeterGrid));

            menu.Items.Add(CreateItem("➕ Incrementar Ejes (+10 mm)", () => {
                axes.IncreaseSize(10.0);
            }));

            menu.Items.Add(CreateItem("➖ Reducir Ejes (-10 mm)", () => {
                axes.DecreaseSize(10.0);
            }));
        }

        private static void SelectTool(IAnnotationContext context, string toolId) {
            if (context != null && context.Registry != null) {
                context.Registry.SelectTool(toolId);
            }
        }

        private static void AddHeader(ContextMenu menu, string title) {
            MenuItem header = new MenuItem {
                Header = title,
                IsEnabled = false,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.LightCyan
            };
            menu.Items.Add(header);
        }

        private static void AddThicknessSubmenu(ContextMenu menu, IAnnotationContext context) {
            MenuItem root = new MenuItem {
                Header = "✏️ Grosor de trazo",
                Foreground = Brushes.White,
                Background = MenuBg
            };

            var presets = new[] {
                new { Label = "Fino (2 px)", Size = 2.0 },
                new { Label = "Medio (4 px)", Size = 4.0 },
                new { Label = "Grueso (8 px)", Size = 8.0 },
                new { Label = "Extra Grueso (14 px)", Size = 14.0 }
            };

            foreach (var p in presets) {
                double targetSize = p.Size;
                bool isCurrent = context != null && Math.Abs(context.CurrentSize - targetSize) < 0.5;
                root.Items.Add(CreateItem(p.Label, () => {
                    if (context != null) {
                        context.CurrentSize = targetSize;
                        if (context.Registry != null) {
                            context.Registry.NotifySizeChanged(targetSize);
                        }
                    }
                }, isCurrent));
            }

            menu.Items.Add(root);
        }

        private static void AddColorSubmenu(ContextMenu menu, IAnnotationContext context) {
            MenuItem root = new MenuItem {
                Header = "🎨 Color",
                Foreground = Brushes.White,
                Background = MenuBg
            };

            var colors = new[] {
                new { Label = "🔴 Rojo", Color = Colors.Red },
                new { Label = "🔵 Azul", Color = Color.FromRgb(0, 140, 255) },
                new { Label = "🟢 Verde", Color = Color.FromRgb(0, 210, 110) },
                new { Label = "🟡 Amarillo", Color = Colors.Gold },
                new { Label = "⚪ Blanco", Color = Colors.White },
                new { Label = "⚫ Negro", Color = Colors.Black }
            };

            foreach (var c in colors) {
                Color targetColor = c.Color;
                bool isCurrent = context != null && context.CurrentColor == targetColor;
                root.Items.Add(CreateItem(c.Label, () => {
                    if (context != null) {
                        context.CurrentColor = targetColor;
                        if (context.Registry != null) {
                            context.Registry.NotifyColorChanged(targetColor);
                        }
                    }
                }, isCurrent));
            }

            menu.Items.Add(root);
        }

        private static MenuItem CreateItem(string header, Action onClick, bool isChecked = false) {
            MenuItem item = new MenuItem {
                Header = header,
                IsChecked = isChecked,
                Foreground = isChecked ? Brushes.Gold : Brushes.White,
                Background = MenuBg
            };
            item.Click += (s, e) => {
                if (onClick != null) onClick();
            };
            return item;
        }
    }
}
