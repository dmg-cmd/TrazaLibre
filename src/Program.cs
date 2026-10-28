using System;
using System.Windows;
using Trazalibre.Core;
using Trazalibre.Services;
using Trazalibre.Tools;
using Trazalibre.UI;

namespace Trazalibre {
    public class Program {
        [STAThread]
        public static void Main(string[] args) {
            Application app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1. Instanciar el registro de herramientas
            ToolRegistry registry = new ToolRegistry();
            registry.RegisterTool(new MouseTool());
            registry.RegisterTool(new PenTool());
            registry.RegisterTool(new HighlighterTool());
            registry.RegisterTool(new EraserTool());
            registry.RegisterTool(new TextTool()); // Herramienta de Texto

            // Registrar formas geométricas interactivas
            registry.RegisterTool(new ArrowShapeTool());
            registry.RegisterTool(new RectangleShapeTool());
            registry.RegisterTool(new EllipseShapeTool());
            registry.RegisterTool(new LineShapeTool());
            registry.RegisterTool(new VectorTool()); // Vector interactivo entre dos puntos con medidas

            // Registrar Módulo Geométrico (Instrumentos de dibujo técnico)
            registry.RegisterTool(new AxesTool()); // Ejes de coordenadas cartesianos con punto (0, 0)
            registry.RegisterTool(new ProtractorTool());
            registry.RegisterTool(new RulerTool());
            registry.RegisterTool(new Triangle45Tool());
            registry.RegisterTool(new Triangle30Tool());
            registry.RegisterTool(new CompassTool());

            // Registrar globos de texto con flecha interactivos
            registry.RegisterTool(new RectangularCalloutTool());
            registry.RegisterTool(new OvalCalloutTool());
            registry.RegisterTool(new CloudCalloutTool());

            // Registrar sellos vectoriales
            registry.RegisterTool(new CheckStampTool());
            registry.RegisterTool(new CrossStampTool());
            registry.RegisterTool(new StarStampTool());
            registry.RegisterTool(new ArrowStampTool());
            registry.RegisterTool(new NumberStampTool());

            // 2. Crear la ventana de lienzo de anotación
            OverlayWindow overlay = new OverlayWindow();
            overlay.Show();

            // 3. Conectar el registro al lienzo
            registry.Initialize(overlay);
            registry.SelectTool("pen");

            // 4. Crear la barra de herramientas flotante con jerarquía Owner
            ToolbarWindow toolbar = new ToolbarWindow(registry, overlay, overlay);
            toolbar.Show();

            // 5. Servicio de atajos de teclado
            HotkeyService hotkeyService = new HotkeyService(registry, overlay);
            hotkeyService.AttachWindow(overlay);
            hotkeyService.AttachWindow(toolbar);

            // 6. Cierre seguro
            toolbar.Closed += (s, e) => {
                hotkeyService.Dispose();
                overlay.Close();
                app.Shutdown();
            };

            app.Run();
        }
    }
}

