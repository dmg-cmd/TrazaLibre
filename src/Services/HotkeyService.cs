using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Trazalibre.Core;

namespace Trazalibre.Services {
    /// <summary>
    /// Servicio modular de gestión de atajos de teclado (locales y globales).
    /// </summary>
    public class HotkeyService : IDisposable {
        private const int HOTKEY_ID_TOGGLE = 9001;
        private const uint MOD_NOREPEAT = 0x4000;
        private const uint VK_F8 = 0x77; // Tecla F8 global para alternar Ratón <-> Lápiz desde cualquier app

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly ToolRegistry _registry;
        private readonly IAnnotationContext _context;
        private HwndSource _hwndSource;
        private IntPtr _hwnd;

        public HotkeyService(ToolRegistry registry, IAnnotationContext context) {
            _registry = registry;
            _context = context;
        }

        public void AttachWindow(Window window) {
            if (window == null) return;

            window.PreviewKeyDown += OnWindowPreviewKeyDown;

            window.SourceInitialized += (s, e) => {
                _hwnd = new WindowInteropHelper(window).Handle;
                _hwndSource = HwndSource.FromHwnd(_hwnd);
                if (_hwndSource != null) {
                    _hwndSource.AddHook(HwndHook);
                    RegisterHotKey(_hwnd, HOTKEY_ID_TOGGLE, MOD_NOREPEAT, VK_F8);
                }
            };
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e) {
            // Si el foco actual está en una caja de texto (TextBox), NO interceptar letras ni números
            // para permitir que el usuario escriba su texto con total normalidad
            if (Keyboard.FocusedElement is TextBox || e.OriginalSource is TextBox) {
                return;
            }

            // Tecla ESC: Salida inmediata al modo Ratón
            if (e.Key == Key.Escape) {
                _registry.SelectTool("mouse");
                e.Handled = true;
                return;
            }

            // Atajos de herramientas básicas
            if (e.Key == Key.P) {
                _registry.SelectTool("pen");
                e.Handled = true;
            } else if (e.Key == Key.H) {
                _registry.SelectTool("highlighter");
                e.Handled = true;
            } else if (e.Key == Key.E) {
                _registry.SelectTool("eraser");
                e.Handled = true;
            } else if (e.Key == Key.T) {
                _registry.SelectTool("text");
                e.Handled = true;
            }
            // Atajos de Formas Geométricas
            else if (e.Key == Key.F) {
                _registry.SelectTool("shape_arrow");
                e.Handled = true;
            } else if (e.Key == Key.Q) {
                _registry.SelectTool("shape_rect");
                e.Handled = true;
            } else if (e.Key == Key.O) {
                _registry.SelectTool("shape_ellipse");
                e.Handled = true;
            } else if (e.Key == Key.L) {
                _registry.SelectTool("shape_line");
                e.Handled = true;
            } else if (e.Key == Key.J) {
                _registry.SelectTool("shape_protractor");
                e.Handled = true;
            } else if (e.Key == Key.M) {
                _registry.SelectTool("geom_ruler");
                e.Handled = true;
            } else if (e.Key == Key.U) {
                _registry.SelectTool("geom_compass");
                e.Handled = true;
            } else if (e.Key == Key.B) {
                _registry.SelectTool("shape_callout_rect");
                e.Handled = true;
            }
            // Atajos de Sellos
            else if (e.Key == Key.K) {
                _registry.SelectTool("stamp_check");
                e.Handled = true;
            } else if (e.Key == Key.X) {
                _registry.SelectTool("stamp_cross");
                e.Handled = true;
            } else if (e.Key == Key.R) {
                _registry.SelectTool("stamp_star");
                e.Handled = true;
            } else if (e.Key == Key.A) {
                _registry.SelectTool("stamp_arrow");
                e.Handled = true;
            } else if (e.Key == Key.N) {
                _registry.SelectTool("stamp_number");
                e.Handled = true;
            }
            // Atajos de Grosores (1: Fino, 2: Medio, 3: Grueso, 4: Extra)
            else if (e.Key == Key.D1 || e.Key == Key.NumPad1) {
                SetThickness(2.0);
                e.Handled = true;
            } else if (e.Key == Key.D2 || e.Key == Key.NumPad2) {
                SetThickness(4.0);
                e.Handled = true;
            } else if (e.Key == Key.D3 || e.Key == Key.NumPad3) {
                SetThickness(8.0);
                e.Handled = true;
            } else if (e.Key == Key.D4 || e.Key == Key.NumPad4) {
                SetThickness(14.0);
                e.Handled = true;
            }
            // Acciones globales
            else if (e.Key == Key.C) {
                _context.Clear();
                e.Handled = true;
            } else if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
                _context.Undo();
                e.Handled = true;
            } else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
                _context.SaveScreenshot();
                e.Handled = true;
            }
        }

        private void SetThickness(double size) {
            _context.CurrentSize = size;
            _registry.NotifySizeChanged(size);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID_TOGGLE) {
                TogglePenAndMouse();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void TogglePenAndMouse() {
            if (_registry.ActiveTool != null && _registry.ActiveTool.Id.Equals("pen", StringComparison.OrdinalIgnoreCase)) {
                _registry.SelectTool("mouse");
            } else {
                _registry.SelectTool("pen");
            }
        }

        public void Dispose() {
            if (_hwndSource != null && _hwnd != IntPtr.Zero) {
                UnregisterHotKey(_hwnd, HOTKEY_ID_TOGGLE);
                _hwndSource.RemoveHook(HwndHook);
            }
        }
    }
}

