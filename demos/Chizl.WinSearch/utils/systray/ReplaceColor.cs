using System.Drawing;

namespace Chizl.Graphix
{
    public class ReplaceColor
    {
        private double _imageColorTolerance = 0.10;

        private ReplaceColor() { IsEmpty = true; }
        public ReplaceColor(Color delete, Color add, double clrTol = 0.10)
        {
            Delete = delete;
            Add = add;
            ValidateTolerance(clrTol);
        }

        public static ReplaceColor Empty => new ReplaceColor();

        public Color Delete { get; } = Color.Empty;
        public Color Add { get; } = Color.Empty;
        public double ImageColorTolerance => _imageColorTolerance;
        public bool IsEmpty { get; } = false;

        private void ValidateTolerance(double clrTol)
        {
            // if within the range of 0 to 100 percent use it, else stick with default 10%.
            if (clrTol >= 0.0f && clrTol <= 1.0f)
                _imageColorTolerance = clrTol;
        }
    }
}
