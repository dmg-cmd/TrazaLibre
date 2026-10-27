using System;
using System.Collections.Generic;

namespace Trazalibre.Core {
    /// <summary>
    /// Registro central y desacoplado de herramientas.
    /// Administra el ciclo de vida de activación y cambio de herramientas dinámicamente.
    /// </summary>
    public class ToolRegistry {
        private readonly List<ITool> _tools = new List<ITool>();
        private ITool _activeTool;
        private IAnnotationContext _context;

        public event Action<ITool> ActiveToolChanged;

        public ITool ActiveTool {
            get { return _activeTool; }
        }

        public IEnumerable<ITool> Tools {
            get { return _tools.AsReadOnly(); }
        }

        public void Initialize(IAnnotationContext context) {
            _context = context;
        }

        public void RegisterTool(ITool tool) {
            if (tool == null) throw new ArgumentNullException("tool");
            if (!_tools.Contains(tool)) {
                _tools.Add(tool);
            }
        }

        public void SelectTool(string toolId) {
            ITool selected = _tools.Find(t => t.Id.Equals(toolId, StringComparison.OrdinalIgnoreCase));
            if (selected != null) {
                if (_activeTool != null && _activeTool != selected && _context != null) {
                    _activeTool.Deactivate(_context);
                }

                _activeTool = selected;

                if (_context != null) {
                    _activeTool.Activate(_context);
                }

                if (ActiveToolChanged != null) {
                    ActiveToolChanged(_activeTool);
                }
            }
        }

        public void NotifyColorChanged(System.Windows.Media.Color color) {
            if (_activeTool != null && _context != null) {
                _activeTool.OnColorChanged(_context, color);
            }
        }

        public void NotifySizeChanged(double size) {
            if (_activeTool != null && _context != null) {
                _activeTool.OnSizeChanged(_context, size);
            }
        }
    }
}

