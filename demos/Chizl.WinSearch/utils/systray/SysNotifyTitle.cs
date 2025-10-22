using System.Drawing;
using System.Windows.Forms;

namespace Chizl.Applications
{
    public class SysNotifyTitle
    {
        private SysNotifyTitle() { IsEmpty = true; }

        public SysNotifyTitle(string headerText, Color headerFGColor, Color headerBGColor, Padding padding): this(headerBGColor, padding)
        {
            HeaderText = headerText;
            HeaderFGColor = headerFGColor;
        }
        public SysNotifyTitle(Image headerImage, Color headerBGColor) : this(headerImage, headerBGColor, new Padding(0)) {}
        public SysNotifyTitle(Image headerImage, Color headerBGColor, Padding padding) : this(headerBGColor, padding)
        {
            HeaderImage = headerImage;
        }
        private SysNotifyTitle(Color headerBGColor, Padding padding)
        {
            HeaderBGColor = headerBGColor;
            Padding = padding;
        }

        public bool IsEmpty { get; } = false;
        public Image HeaderImage { get; }
        public string HeaderText { get; } = string.Empty;
        public Color HeaderBGColor { get; } = SystemColors.Control;
        public Color HeaderFGColor { get; } = SystemColors.ControlText;
        public Padding Padding { get; } = new Padding(0);
        public static SysNotifyTitle Empty => new SysNotifyTitle();
    }
}
