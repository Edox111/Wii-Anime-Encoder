using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace WiiAnimeEncoder
{
    public class ThemedButton : Button
    {
        public bool Primary = false;
        public bool RichAccent = false;
        public Color Accent = Color.FromArgb(113, 133, 181);
        bool pressed;
        bool hover = false;
        public ThemedButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e) { pressed = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnMouseCaptureChanged(EventArgs e) { pressed = false; Invalidate(); base.OnMouseCaptureChanged(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) { pressed = true; Invalidate(); } base.OnKeyDown(e); }
        protected override void OnKeyUp(KeyEventArgs e) { pressed = false; Invalidate(); base.OnKeyUp(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = new GraphicsPath())
            {
                path.AddPolygon(new Point[] { new Point(8, 1), new Point(Width - 2, 1), new Point(Width - 2, Height - 9), new Point(Width - 10, Height - 2), new Point(1, Height - 2), new Point(1, 8) });
                Color top = AppTheme.Mix(BackColor, Accent, RichAccent ? (Primary ? 0.95 : 0.68) : (Primary ? 0.72 : 0.30));
                if (hover && Enabled) top = AppTheme.Mix(top, Accent, 0.22);
                if (pressed && Enabled) top = AppTheme.Mix(top, Color.Black, 0.25);
                if (!Enabled) top = AppTheme.Mix(BackColor, Color.Gray, 0.14);
                top = AppTheme.ForWhiteText(top);
                Color bottom = AppTheme.Mix(top, Color.Black, RichAccent ? 0.48 : 0.16);
                using (var b = new LinearGradientBrush(ClientRectangle, top, bottom, 90f))
                    g.FillPath(b, path);
                using (var pen = new Pen(Enabled ? AppTheme.Mix(BackColor, Accent, 0.6) : Color.FromArgb(65, 65, 70)))
                    g.DrawPath(pen, path);
                if (RichAccent && Enabled)
                {
                    using (var edge = new Pen(Color.FromArgb(hover ? 210 : 135, Accent), 2f))
                        g.DrawLine(edge, 10, 3, Width - 4, 3);
                }
                OutlinedText.Draw(g, Text, Font, ClientRectangle, Enabled ? Color.White : Color.FromArgb(145, 145, 150), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                if (Focused)
                    ControlPaint.DrawFocusRectangle(g, new Rectangle(6, 5, Width - 12, Height - 10), ForeColor, bottom);
            }
        }
    }
}
