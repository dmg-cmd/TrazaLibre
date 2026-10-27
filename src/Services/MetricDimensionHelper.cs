using System;
using System.Globalization;
using System.Windows.Input;

namespace Trazalibre.Services {
    /// <summary>
    /// Constantes y conversiones métricas universales para instrumentos graduados.
    /// Define una escala métrica calibrada de pantalla: 5 píxeles = 1 milímetro (50 píxeles = 1 centímetro).
    /// </summary>
    public static class MetricUnits {
        /// <summary>
        /// Factor de conversión estándar: 5.0 píxeles equivalen a 1 milímetro.
        /// (10 mm = 50 píxeles = 1 centímetro).
        /// </summary>
        public const double PixelsPerMm = 5.0;

        /// <summary>
        /// Convierte una distancia en píxeles a milímetros.
        /// </summary>
        public static double PxToMm(double px) {
            return px / PixelsPerMm;
        }

        /// <summary>
        /// Convierte una distancia en milímetros a píxeles.
        /// </summary>
        public static double MmToPx(double mm) {
            return mm * PixelsPerMm;
        }

        /// <summary>
        /// Cuantiza o ajusta una medida en píxeles al milímetro entero más cercano.
        /// </summary>
        public static double SnapToMm(double px) {
            double mm = Math.Round(px / PixelsPerMm);
            return Math.Max(PixelsPerMm, mm * PixelsPerMm);
        }

        /// <summary>
        /// Cuantiza una medida en píxeles a pasos de medio milímetro (0.5 mm = 2.5 px).
        /// </summary>
        public static double SnapToHalfMm(double px) {
            double halfMms = Math.Round(px / (PixelsPerMm * 0.5));
            return Math.Max(PixelsPerMm * 0.5, halfMms * (PixelsPerMm * 0.5));
        }
    }

    /// <summary>
    /// Controlador interactivo de dimensión métrica y entrada numérica de precisión (tipo CAD).
    /// Permite:
    /// 1. Ingresar el valor numérico exacto directamente tecleando números y pulsando Enter.
    /// 2. Avanzar o retroceder paso a paso de a 1 milímetro con las flechas del teclado o Ctrl+Rueda (Shift para 10 mm = 1 cm).
    /// 3. Ajustar el cursor al milímetro exacto durante el arrastre con el ratón.
    /// </summary>
    public class MetricInputController {
        private double _valueMm;
        private string _inputBuffer;

        public MetricInputController(double defaultMm = 50.0) {
            _valueMm = Math.Max(1.0, defaultMm);
            _inputBuffer = string.Empty;
        }

        /// <summary>
        /// Indica si el usuario está actualmente tecleando una cifra numérica.
        /// </summary>
        public bool IsEntering {
            get { return !string.IsNullOrEmpty(_inputBuffer); }
        }

        /// <summary>
        /// Texto del búfer numérico tecleado por el usuario.
        /// </summary>
        public string InputBuffer {
            get { return _inputBuffer; }
        }

        /// <summary>
        /// Valor actual de la longitud en milímetros.
        /// </summary>
        public double ValueMm {
            get { return _valueMm; }
            set {
                _valueMm = Math.Max(1.0, value);
            }
        }

        /// <summary>
        /// Valor actual de la longitud en píxeles de pantalla.
        /// </summary>
        public double ValuePixels {
            get { return _valueMm * MetricUnits.PixelsPerMm; }
            set {
                _valueMm = Math.Max(1.0, value / MetricUnits.PixelsPerMm);
            }
        }

        /// <summary>
        /// Cancela la entrada numérica pendiente y limpia el búfer.
        /// </summary>
        public void ClearInput() {
            _inputBuffer = string.Empty;
        }

        /// <summary>
        /// Actualiza la longitud desde la posición del ratón. Si holdAlt es falso, cuantiza al milímetro más cercano.
        /// </summary>
        public void UpdateFromMouse(double mousePixels, bool holdAlt = false) {
            if (IsEntering) return; // Si está tecleando, respetar la cifra en proceso

            if (holdAlt) {
                ValuePixels = Math.Max(MetricUnits.PixelsPerMm, mousePixels);
            } else {
                ValuePixels = MetricUnits.SnapToMm(Math.Max(MetricUnits.PixelsPerMm, mousePixels));
            }
        }

        /// <summary>
        /// Procesa eventos de teclado para entrada numérica directa o ajuste de a 1 mm.
        /// Devuelve true si la tecla fue consumida.
        /// El parámetro valueCommitted indica si se presionó Enter para confirmar/trazar.
        /// </summary>
        public bool ProcessKeyDown(Key key, ModifierKeys modifiers, out bool valueCommitted) {
            valueCommitted = false;

            // 1. Detección de dígitos del 0 al 9
            int digit = -1;
            if (key >= Key.D0 && key <= Key.D9 && (modifiers & ModifierKeys.Shift) != ModifierKeys.Shift) {
                digit = key - Key.D0;
            } else if (key >= Key.NumPad0 && key <= Key.NumPad9) {
                digit = key - Key.NumPad0;
            }

            if (digit >= 0) {
                _inputBuffer += digit.ToString(CultureInfo.InvariantCulture);
                double parsed;
                if (double.TryParse(_inputBuffer, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)) {
                    _valueMm = Math.Max(1.0, parsed);
                }
                return true;
            }

            // 2. Punto o coma decimal
            if (key == Key.OemPeriod || key == Key.Decimal || key == Key.OemComma) {
                if (!_inputBuffer.Contains(".")) {
                    if (string.IsNullOrEmpty(_inputBuffer)) {
                        _inputBuffer = "0";
                    }
                    _inputBuffer += ".";
                }
                return true;
            }

            // 3. Retroceso (Backspace)
            if (key == Key.Back) {
                if (_inputBuffer.Length > 0) {
                    _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                    if (_inputBuffer.Length > 0 && _inputBuffer != "0") {
                        double parsed;
                        if (double.TryParse(_inputBuffer, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)) {
                            _valueMm = Math.Max(1.0, parsed);
                        }
                    }
                    return true;
                }
                return false;
            }

            // 4. Escape (Cancelar entrada en búfer)
            if (key == Key.Escape) {
                if (IsEntering) {
                    _inputBuffer = string.Empty;
                    return true;
                }
                return false;
            }

            // 5. Enter / Return (Fijar valor o confirmar trazo)
            if (key == Key.Enter || key == Key.Return) {
                if (IsEntering) {
                    double parsed;
                    if (double.TryParse(_inputBuffer, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)) {
                        _valueMm = Math.Max(1.0, parsed);
                    }
                    _inputBuffer = string.Empty;
                }
                valueCommitted = true;
                return true;
            }

            // 6. Flechas y teclas de suma / resta (+ / -) para avanzar o retroceder de a 1 mm (o 10 mm con Shift)
            double step = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 10.0 : 1.0;

            if (key == Key.Up || key == Key.Right || key == Key.Add || key == Key.OemPlus) {
                _valueMm = Math.Round(_valueMm + step, 1);
                _inputBuffer = string.Empty;
                return true;
            }

            if (key == Key.Down || key == Key.Left || key == Key.Subtract || key == Key.OemMinus) {
                _valueMm = Math.Max(1.0, Math.Round(_valueMm - step, 1));
                _inputBuffer = string.Empty;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Procesa la rueda del ratón con tecla Ctrl presionada para avanzar de a 1 milímetro.
        /// Devuelve true si el evento fue consumido.
        /// </summary>
        public bool ProcessMouseWheel(int delta, ModifierKeys modifiers) {
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
                double step = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 10.0 : 1.0;
                int dir = Math.Sign(delta);
                _valueMm = Math.Max(1.0, Math.Round(_valueMm + (dir * step), 1));
                _inputBuffer = string.Empty;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Genera el texto de estado principal formateado en milímetros y centímetros para los badges de la interfaz.
        /// </summary>
        public string GetMainStatusText(string label, double angleDeg = double.NaN) {
            if (IsEntering) {
                return string.Format("⌨️ Ingresando {0}: [ {1} ] mm  •  [Enter] para fijar o trazar", label, _inputBuffer);
            }

            string anglePart = !double.IsNaN(angleDeg) ? string.Format("  |  📐 Ángulo: {0:0.#}°", angleDeg) : string.Empty;
            return string.Format("📏 {0}: {1:0.#} mm ({2:0.0#} cm | {3:0} px){4}",
                label,
                _valueMm,
                _valueMm / 10.0,
                ValuePixels,
                anglePart);
        }

        /// <summary>
        /// Genera el texto de ayuda contextual para el usuario.
        /// </summary>
        public string GetHelpSubText() {
            if (IsEntering) {
                return "💡 Escribe la cifra en milímetros y pulsa Enter para aplicar  •  [Esc] cancelar";
            }
            return "⌨️ Escribe números directos  •  Flechas ↑/↓ o Ctrl+Rueda: ±1 mm (Shift: ±10 mm)  •  [Enter]: trazar";
        }
    }
}

