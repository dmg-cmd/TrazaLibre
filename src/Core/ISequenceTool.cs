using System;

namespace Trazalibre.Core {
    /// <summary>
    /// Contrato desacoplado para herramientas con secuencias numéricas o contadores.
    /// Permite a la interfaz gráfica interactuar con contadores sin acoplarse a implementaciones concretas.
    /// </summary>
    public interface ISequenceTool {
        int NextNumber { get; set; }
        event Action<int> NumberChanged;
        void Reset();
    }
}

