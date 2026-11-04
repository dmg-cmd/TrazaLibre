using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
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

            // Mostrar inmediatamente el recuadro de carga antes de compilar los módulos pesados en JIT
            SplashWindow splash = new SplashWindow();
            splash.ContentRendered += (s, e) => {
                BeginStagedInitialization(app, splash);
            };
            splash.Show();

            app.Run();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BeginStagedInitialization(Application app, SplashWindow splash) {
            ToolRegistry registry = null;
            OverlayWindow overlay = null;
            ToolbarWindow toolbar = null;
            HotkeyService hotkeyService = null;

            int stage = 0;
            DispatcherTimer stageTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(480)
            };

            stageTimer.Tick += (s, e) => {
                stage++;
                if (stage == 1) {
                    splash.SetProgress(0.35, "⏳ Cargando herramientas e instrumentos CAD, espere...");
                    registry = CreateAndRegisterTools();
                } else if (stage == 2) {
                    splash.SetProgress(0.68, "⏳ Preparando lienzo multimonitor y cuadrículas...");
                    overlay = new OverlayWindow();
                    overlay.Show();
                    registry.Initialize(overlay);
                    registry.SelectTool("pen");
                } else if (stage == 3) {
                    splash.SetProgress(0.92, "⏳ Configurando barra de herramientas y atajos...");
                    toolbar = new ToolbarWindow(registry, overlay, overlay);
                    hotkeyService = new HotkeyService(registry, overlay);
                    hotkeyService.AttachWindow(overlay);
                    hotkeyService.AttachWindow(toolbar);

                    toolbar.Closed += (sender, args) => {
                        hotkeyService.Dispose();
                        overlay.Close();
                        app.Shutdown();
                    };
                } else if (stage == 4) {
                    splash.SetProgress(1.0, "✅ ¡Trazalibre listo! Abriendo espacio de trabajo...");
                } else {
                    stageTimer.Stop();
                    if (toolbar != null) {
                        toolbar.Show();
                    }
                    splash.Close();
                }
            };

            stageTimer.Start();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ToolRegistry CreateAndRegisterTools() {
            ToolRegistry registry = new ToolRegistry();
            registry.RegisterTool(new MouseTool());
            registry.RegisterTool(new PenTool());
            registry.RegisterTool(new HighlighterTool());
            registry.RegisterTool(new EraserTool());
            registry.RegisterTool(new TextTool());
            registry.RegisterTool(new InstructionTool());

            // Formas geométricas interactivas
            registry.RegisterTool(new ArrowShapeTool());
            registry.RegisterTool(new RectangleShapeTool());
            registry.RegisterTool(new EllipseShapeTool());
            registry.RegisterTool(new LineShapeTool());
            registry.RegisterTool(new VectorTool());

            // Módulo Geométrico (Instrumentos de dibujo técnico)
            registry.RegisterTool(new AxesTool());
            registry.RegisterTool(new ProtractorTool());
            registry.RegisterTool(new RulerTool());
            registry.RegisterTool(new Triangle45Tool());
            registry.RegisterTool(new Triangle30Tool());
            registry.RegisterTool(new CompassTool());

            // Globos de texto con flecha interactivos
            registry.RegisterTool(new RectangularCalloutTool());
            registry.RegisterTool(new OvalCalloutTool());
            registry.RegisterTool(new CloudCalloutTool());

            // Sellos vectoriales
            registry.RegisterTool(new CheckStampTool());
            registry.RegisterTool(new CrossStampTool());
            registry.RegisterTool(new StarStampTool());
            registry.RegisterTool(new ArrowStampTool());
            registry.RegisterTool(new NumberStampTool());

            return registry;
        }
    }
}

