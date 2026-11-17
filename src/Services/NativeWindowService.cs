using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Trazalibre.Services {
    /// <summary>
    /// Servicio de interacción con la API de Windows para control de estilos y eventos de ventana.
    /// </summary>
    public static class NativeWindowService {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_FLAGS = SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED;

        private const uint GA_ROOT = 2;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_V = 0x56;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) {
            return (IntPtr.Size == 8) ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) {
            if (IntPtr.Size == 8) {
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            } else {
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
            }
        }

        public static void SetClickThrough(IntPtr hwnd, bool clickThrough) {
            if (hwnd == IntPtr.Zero) return;

            IntPtr extendedStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            long style = extendedStyle.ToInt64();

            if (clickThrough) {
                style |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
            } else {
                style &= ~WS_EX_TRANSPARENT;
            }

            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(style));
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_FLAGS);
        }

        /// <summary>
        /// Enfoca la ventana situada bajo las coordenadas de pantalla indicadas y envía Ctrl + V
        /// para pegar el contenido del portapapeles directamente en el documento o aplicación activa.
        /// </summary>
        public static void FocusWindowAtPointAndPaste(int screenX, int screenY) {
            POINT pt = new POINT { X = screenX, Y = screenY };
            IntPtr hwnd = WindowFromPoint(pt);

            if (hwnd != IntPtr.Zero) {
                IntPtr root = GetAncestor(hwnd, GA_ROOT);
                if (root == IntPtr.Zero) {
                    root = hwnd;
                }
                SetForegroundWindow(root);
            }

            Thread.Sleep(120);

            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, 0, UIntPtr.Zero);
            Thread.Sleep(30);
            keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            Thread.Sleep(60);
        }
    }
}

