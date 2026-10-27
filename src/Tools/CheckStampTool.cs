using System.Collections.Generic;
using System.Windows;

namespace Trazalibre.Tools {
    /// <summary>
    /// Sello de Verificación / Checkmark ((✔️)
    /// </summary>
    public class CheckStampTool : BaseStampTool {
        public override string Id { get { return "stamp_check"; } }
        public override string DisplayName { get { return "Check"; } }
        public override string Icon { get { return "✔️"; } }

        protected override IEnumerable<Point> GetRelativePoints(double scale) {
            return new Point[] {
                new Point(-scale * 0.35, 0),
                new Point(-scale * 0.1, scale * 0.35),
                new Point(scale * 0.4, -scale * 0.35)
            };
        }
    }
}

