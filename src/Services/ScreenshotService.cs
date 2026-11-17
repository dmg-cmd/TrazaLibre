using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Trazalibre.Core;
using Trazalibre.UI;

namespace Trazalibre.Services {
    /// <summary>
    /// Servicio modular encargado de la selección y captura de sectores de pantalla.
    /// Soporta selección interactiva mediante rectángulos de arrastre, exportación a PNG y copiado al Portapapeles.
    /// </summary>
    public static class ScreenshotService {
        /// <summary>
        /// Inicia el modo de selección de sector de pantalla. Permite al usuario arrastrar para elegir el área deseada.
        /// Captura la pantalla real del sistema junto con cualquier anotación dibujada.
        /// </summary>
        /// <returns>La ruta absoluta del archivo PNG guardado, o null si la operación fue cancelada.</returns>
        public static string CaptureSector() {
            List<Window> hiddenToolbars = new List<Window>();

            try {
                // 1. Ocultar temporalmente las barras de herramientas para no entorpecer la selección ni la captura
                if (Application.Current != null) {
                    foreach (Window win in Application.Current.Windows) {
                        if (win is ToolbarWindow && win.IsVisible) {
                            hiddenToolbars.Add(win);
                            win.Visibility = Visibility.Hidden;
                        }
                    }
                }

                // 2. Abrir la ventana de selección interactiva a pantalla completa
                SectorSelectionWindow selector = new SectorSelectionWindow();
                bool? dialogResult = selector.ShowDialog();

                if (dialogResult != true || selector.SelectedRectangle.Width <= 0 || selector.SelectedRectangle.Height <= 0) {
                    return null; // El usuario canceló o no arrastró un área válida
                }

                System.Drawing.Rectangle area = selector.SelectedRectangle;

                // 3. Pausa breve (100 ms) para asegurar que el escritorio termine de repintarse sin la ventana de selección
                Thread.Sleep(100);

                // 4. Capturar el sector de la pantalla mediante GDI CopyFromScreen
                using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(area.Width, area.Height)) {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp)) {
                        g.CopyFromScreen(
                            area.X,
                            area.Y,
                            0,
                            0,
                            new System.Drawing.Size(area.Width, area.Height),
                            System.Drawing.CopyPixelOperation.SourceCopy
                        );
                    }

                    // 5. Crear la carpeta de destino en Imágenes\Trazalibre
                    string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    string folder = Path.Combine(picturesPath, "Trazalibre");
                    if (!Directory.Exists(folder)) {
                        Directory.CreateDirectory(folder);
                    }

                    string filename = string.Format("Captura_Sector_{0:yyyyMMdd_HHmmss}.png", DateTime.Now);
                    string fullPath = Path.Combine(folder, filename);

                    // 6. Guardar la imagen en disco
                    bmp.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);

                    // 7. Copiar la imagen capturada al Portapapeles de Windows
                    BitmapSource bitmapSource = ToBitmapSource(bmp);
                    Clipboard.SetImage(bitmapSource);

                    return fullPath;
                }
            } finally {
                // Restaurar la visibilidad de las barras de herramientas
                foreach (Window win in hiddenToolbars) {
                    win.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Renderiza limpiamente los trazos actuales del lienzo (recortados a su área exacta sobre fondo blanco),
        /// los copia al portapapeles, guarda un respaldo PNG, limpia el lienzo conservando el historial Undo
        /// y enfoca la aplicación subyacente para pegar el dibujo directamente (Ctrl + V).
        /// </summary>
        public static bool MergeDrawingIntoApplication(IAnnotationContext context) {
            if (context == null || context.Canvas == null || context.Canvas.Strokes.Count == 0) {
                MessageBox.Show(
                    "No hay ningún dibujo en el lienzo para volcar al documento.",
                    "Volcar a Documento",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return false;
            }

            Rect strokeBounds = context.Canvas.Strokes.GetBounds();
            if (strokeBounds.IsEmpty || strokeBounds.Width < 2.0 || strokeBounds.Height < 2.0) {
                return false;
            }

            double padding = 16.0;
            Rect cropRect = Rect.Inflate(strokeBounds, padding, padding);
            int pixelWidth = Math.Max(16, (int)Math.Ceiling(cropRect.Width));
            int pixelHeight = Math.Max(16, (int)Math.Ceiling(cropRect.Height));

            // Calcular el punto de pantalla antes de ocultar la ventana del lienzo
            int targetX;
            int targetY;
            try {
                Point screenCenter = context.Canvas.PointToScreen(
                    new Point(cropRect.Left + cropRect.Width / 2.0, cropRect.Top + cropRect.Height / 2.0));
                targetX = (int)Math.Round(screenCenter.X);
                targetY = (int)Math.Round(screenCenter.Y);
            } catch {
                targetX = (int)Math.Round(SystemParameters.VirtualScreenLeft + cropRect.Left + cropRect.Width / 2.0);
                targetY = (int)Math.Round(SystemParameters.VirtualScreenTop + cropRect.Top + cropRect.Height / 2.0);
            }

            // Clonar trazos y oscurecer trazos blancos puros para garantizar visibilidad sobre fondo blanco
            StrokeCollection strokesToRender = context.Canvas.Strokes.Clone();
            foreach (Stroke s in strokesToRender) {
                if (s != null && s.DrawingAttributes.Color == Colors.White) {
                    s.DrawingAttributes.Color = Color.FromRgb(30, 35, 45);
                }
            }

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen()) {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, pixelWidth, pixelHeight));
                dc.PushTransform(new TranslateTransform(-cropRect.Left, -cropRect.Top));
                strokesToRender.Draw(dc);
                dc.Pop();
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, 96.0, 96.0, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();

            // Guardar copia de respaldo en Mis Imágenes\Trazalibre
            string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string folder = Path.Combine(picturesPath, "Trazalibre");
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }
            string filename = string.Format("Volcado_Documento_{0:yyyyMMdd_HHmmss}.png", DateTime.Now);
            string fullPath = Path.Combine(folder, filename);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write)) {
                encoder.Save(fs);
            }

            Clipboard.SetImage(rtb);

            // Archivar trazos para poder recuperarlos con Ctrl+Z, limpiar el lienzo y pasar a Modo Ratón
            OverlayWindow overlay = context as OverlayWindow;
            if (overlay != null) {
                overlay.ArchiveAndClearStrokes();
            } else {
                context.Clear();
            }

            if (context.Registry != null) {
                context.Registry.SelectTool("mouse");
            }

            List<Window> hiddenWindows = new List<Window>();
            try {
                if (Application.Current != null) {
                    foreach (Window win in Application.Current.Windows) {
                        if ((win is OverlayWindow || win is ToolbarWindow) && win.IsVisible) {
                            hiddenWindows.Add(win);
                            win.Visibility = Visibility.Hidden;
                        }
                    }
                }

                Thread.Sleep(40);
                NativeWindowService.FocusWindowAtPointAndPaste(targetX, targetY);
                return true;
            } finally {
                foreach (Window win in hiddenWindows) {
                    win.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Compatibilidad hacia atrás: captura el sector seleccionado por el usuario.
        /// </summary>
        public static string CaptureAndSave(FrameworkElement visual, double width, double height) {
            return CaptureSector();
        }

        /// <summary>
        /// Convierte un Bitmap de System.Drawing en un BitmapSource de WPF para el Portapapeles.
        /// </summary>
        private static BitmapSource ToBitmapSource(System.Drawing.Bitmap bitmap) {
            using (MemoryStream ms = new MemoryStream()) {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();

                return image;
            }
        }
    }
}

