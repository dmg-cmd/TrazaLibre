using System;
using System.Runtime.InteropServices;

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
    }
}

