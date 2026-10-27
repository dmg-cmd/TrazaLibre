using System.Collections.Generic;
using System.Windows;

namespace Trazalibre.Tools {
    /// <summary>
    /// Sello de Cruz / Error (âŒ)
    /// </summary>
    public class CrossStampTool : BaseStampTool {
        public override string Id { get { return "stamp_cross"; } }
        public override string DisplayName { get { return "Cruz"; } }
        public override string Icon { get { return "❌"; } }

        protected override IEnumerable<Point> GetRelativePoints(double scale) {
            double s = scale * 0.35;
            return new Point[] {
                new Point(-s, -s),
                new Point(s, s),
                new Point(0, 0),
                new Point(-s, s),
                new Point(s, -s)
            };
        }
    }
}

