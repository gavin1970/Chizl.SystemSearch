using System;
using System.Drawing;
using System.Windows.Forms;
using Chizl.Graphix;

namespace Chizl.Applications
{
    public class SysNotify : IDisposable
    {
        private bool disposedValue;

        private Point _startPoint = new Point(0, 0);
        private Point _currentPoint = new Point(0, 0);
        private bool _drag;
        private Form _startupForm;
        private NotifyIcon _notify;
        private Bitmap _headerImage;
        private string _headerText = string.Empty;
        private Color _headerFGColor = SystemColors.ControlText;
        private Color _headerBGColor = SystemColors.Control;
        private Padding _headerPadding = new Padding(0);

        public SysNotify(Form form, NotifyIcon notifyIcon) : this(form, notifyIcon, SysNotifyTitle.Empty) { }
        public SysNotify(Form form, NotifyIcon notifyIcon, SysNotifyTitle notifyTitle) => SetupSysNotify(form, notifyIcon, notifyTitle);
        ~SysNotify()
        {
            Dispose(disposing: false);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
            {
                return;
            }

            if (disposing)
            {
                if (_notify != null)
                {
                    _notify.Visible = false;
                    _notify.Dispose();
                }

                _headerImage = null;
            }

            disposedValue = true;
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Form_Closed(object sender, FormClosedEventArgs e)
        {
            Dispose();
        }
        private void Notify_RightClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (this.RightClick != null)
                {
                    this.RightClick?.Invoke(sender, e);
                }

                _notify.ContextMenuStrip.BringToFront();
            }
        }
        private void Notify_DblClick(object sender, EventArgs e)
        {
            if (this.DoubleClick == null)
            {
                if (!_startupForm.Visible)
                {
                    _startupForm.Visible = true;
                }

                if (_startupForm.WindowState == FormWindowState.Minimized)
                {
                    _startupForm.WindowState = FormWindowState.Normal;
                }

                _startupForm.Focus();
                _startupForm.BringToFront();
            }
            else
            {
                this.DoubleClick?.Invoke(sender, e);
            }
        }
        private void Notify_Paint(object sender, PaintEventArgs e)
        {
            int width = e.ClipRectangle.Width;
            int height = e.ClipRectangle.Height;
            int num = _headerPadding.Left + _headerPadding.Right;
            int num2 = _headerPadding.Top + _headerPadding.Bottom;
            Bitmap bitmap = new Bitmap(1, 1);
            if (_headerImage != null)
            {
                bitmap = new Bitmap(ModImg.ResizeWithRatio(_headerImage, _headerBGColor, width - num, height - num2));
            }
            else if (!string.IsNullOrWhiteSpace(_headerText))
            {
                bitmap = (Bitmap)ModImg.ResizeWithRatio(ModImg.TxtToImg(_headerText, new Font("Verdana", 12f, FontStyle.Regular, GraphicsUnit.Pixel), _headerFGColor, _headerBGColor), _headerBGColor, width - num, height - num2);
            }

            int width2 = bitmap.Width;
            int height2 = bitmap.Height;
            int x = ((width > width2) ? ((width - width2) / 2) : 0);
            int y = ((height >= height2) ? ((height - height2) / 2) : 0);
            Rectangle rect = new Rectangle(x, y, width2, height2);
            Brush brush = new SolidBrush(_headerBGColor);
            e.Graphics.FillRectangle(brush, e.ClipRectangle);
            e.Graphics.DrawImage(bitmap, rect);
        }
        private void Notify_Title_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _startPoint = e.Location;
                _drag = true;
            }
        }
        private void Notify_Title_MouseUp(object sender, MouseEventArgs e)
        {
            if (_drag)
            {
                _drag = false;
                ContextMenuStrip contextMenuStrip = _notify.ContextMenuStrip;
                contextMenuStrip.Visible = false;
                contextMenuStrip.Location = _currentPoint;
                contextMenuStrip.Show();
                contextMenuStrip.Location = _currentPoint;
            }
        }
        private void Notify_Title_MouseMove(object sender, MouseEventArgs e)
        {
            if (_drag)
            {
                ContextMenuStrip contextMenuStrip = _notify.ContextMenuStrip;
                Point p = new Point(e.X, e.Y);
                Point point = contextMenuStrip.PointToScreen(p);
                Point location = (_currentPoint = new Point(point.X - _startPoint.X, point.Y - _startPoint.Y));
                contextMenuStrip.Location = location;
            }
        }
        private void SetupSysNotify(Form form, NotifyIcon notifyIcon, SysNotifyTitle notifyTitle)
        {
            _startupForm = form ?? throw new ArgumentException($"SysNotify requires '{nameof(form)}' to be passed in.  Example: var _notify = new SysNotify(this, ...);", "form");
            _notify = notifyIcon ?? throw new ArgumentException($"SysNotify requires '{nameof(notifyIcon)}' to have a value.  Example: var _notify = new SysNotify(..., this.notifyIcon1);", "notifyIcon");

            if (string.IsNullOrWhiteSpace(notifyIcon.Text))
                _notify.Text = _startupForm.Text;
            if (_notify.Icon == null)
                _notify.Icon = _startupForm.Icon;

            if (notifyTitle != null && notifyTitle != SysNotifyTitle.Empty)
                SetImgHeader(notifyTitle);

            _notify.Visible = true;
            _notify.DoubleClick += Notify_DblClick;
            _notify.MouseDown += Notify_RightClick;
            _startupForm.FormClosed += Form_Closed;
        }

        public int ShowTipTimeout { get; set; } = 5;
        public event EventHandler DoubleClick;
        public event MouseEventHandler RightClick;

        public void SetIcon(Icon ico, string msg)
        {
            if (ico != null && _notify != null)
                _notify.Icon = new Icon(ico, ico.Size);

            _notify.Text = _startupForm.Text.Trim() + "\n" + msg?.Trim();
            _notify.Visible = true;
        }
        public void SetImgHeader(SysNotifyTitle notifyTitle)
        {
            Image headerImage = notifyTitle.HeaderImage;
            Color headerBGColor = notifyTitle.HeaderBGColor;
            Color headerFGColor = notifyTitle.HeaderFGColor;
            Padding padding = notifyTitle.Padding;
            string headerText = notifyTitle.HeaderText;
            ContextMenuStrip contextMenuStrip = _notify.ContextMenuStrip ?? new ContextMenuStrip();
            ToolStripMenuItem toolStripMenuItem = null;
            _headerText = (string.IsNullOrWhiteSpace(headerText) ? string.Empty : headerText.Trim());
            _headerImage = ((headerImage == null) ? null : new Bitmap(headerImage));
            _headerBGColor = headerBGColor;
            _headerFGColor = headerFGColor;
            _headerPadding = ((padding == Padding.Empty) ? new Padding(0) : padding);

            foreach (ToolStripMenuItem item in contextMenuStrip.Items)
            {
                if (item.Name == "mititle")
                {
                    toolStripMenuItem = item;
                    break;
                }
            }

            if ((!string.IsNullOrWhiteSpace(_headerText) || _headerImage != null) && toolStripMenuItem == null)
            {
                ToolStripMenuItem toolStripMenuItem3 = new ToolStripMenuItem("")
                {
                    Name = "mititle",
                    Enabled = true,
                    Tag = "mititle"
                };
                toolStripMenuItem3.Paint += Notify_Paint;
                toolStripMenuItem3.MouseDown += Notify_Title_MouseDown;
                toolStripMenuItem3.MouseUp += Notify_Title_MouseUp;
                toolStripMenuItem3.MouseMove += Notify_Title_MouseMove;
                contextMenuStrip.Items.Insert(0, toolStripMenuItem3);
                _notify.ContextMenuStrip = contextMenuStrip;
            }
            else if (_headerImage == null && string.IsNullOrWhiteSpace(_headerText) && toolStripMenuItem != null)
            {
                contextMenuStrip.Items.Remove(toolStripMenuItem);
                _notify.ContextMenuStrip = contextMenuStrip;
            }
        }
        public void SendMessage(string message, string title = null)
        {
            if (!string.IsNullOrWhiteSpace(title))
                _notify.BalloonTipTitle = title.Trim();

            if (!string.IsNullOrWhiteSpace(message))
            {
                _notify.BalloonTipText = message.Trim();
                _notify.ShowBalloonTip(ShowTipTimeout);
            }
        }
    }
}
