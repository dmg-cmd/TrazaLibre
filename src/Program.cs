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

            // 0. Mostrar pantalla de inicio e indicador de ejecución inmediata ("Por favor espere")
            SplashWindow splash = new SplashWindow();
            splash.Show();
            splash.SetProgress(0.20, "⏳ Iniciando motor vectorial CAD, por favor espere...");

            // 1. Instanciar el registro de herramientas
            ToolRegistry registry = new ToolRegistry();
            registry.RegisterTool(new MouseTool());
            registry.RegisterTool(new PenTool());
            registry.RegisterTool(new HighlighterTool());
            registry.RegisterTool(new EraserTool());
            registry.RegisterTool(new TextTool()); // Herramienta de Texto
            registry.RegisterTool(new InstructionTool()); // Herramienta de Indicación con Flecha (Recta, Curva Vertical/Horizontal)

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

            splash.SetProgress(0.55, "⏳ Preparando lienzo multimonitor y cuadrículas...");

            // 2. Crear la ventana de lienzo de anotación
            OverlayWindow overlay = new OverlayWindow();
            overlay.Show();

            // 3. Conectar el registro al lienzo
            registry.Initialize(overlay);
            registry.SelectTool("pen");

            splash.SetProgress(0.85, "⏳ Cargando barra de herramientas y atajos...");

            // 4. Crear la barra de herramientas flotante con jerarquía Owner
            ToolbarWindow toolbar = new ToolbarWindow(registry, overlay, overlay);
            toolbar.Show();

            // 5. Servicio de atajos de teclado
            HotkeyService hotkeyService = new HotkeyService(registry, overlay);
            hotkeyService.AttachWindow(overlay);
            hotkeyService.AttachWindow(toolbar);

            splash.SetProgress(1.0, "✅ ¡Trazalibre en ejecución!");
            splash.CloseAfterDelay(500);

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

