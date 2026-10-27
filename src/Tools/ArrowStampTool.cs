using System.Collections.Generic;
using System.Windows;

namespace Trazalibre.Tools {
    /// <summary>
    /// Sello de Flecha direccional apuntando hacia la derecha (âž”)
    /// </summary>
    public class ArrowStampTool : BaseStampTool {
        public override string Id { get { return "stamp_arrow"; } }
        public override string DisplayName { get { return "Flecha"; } }
        public override string Icon { get { return "➔"; } }

        protected override IEnumerable<Point> GetRelativePoints(double scale) {
            double length = scale * 0.45;
            double headX = scale * 0.15;
            double headY = scale * 0.3;

            return new Point[] {
                new Point(-length, 0),
                new Point(length, 0),
                new Point(headX, -headY),
                new Point(length, 0),
                new Point(headX, headY)
            };
        }
    }
}

