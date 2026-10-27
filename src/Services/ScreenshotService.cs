using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
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

