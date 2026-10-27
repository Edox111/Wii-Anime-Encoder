using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace WiiAnimeEncoder
{
    public class ThemedButton : Button
    {
        public bool Primary = false;
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

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = new GraphicsPath())
            {
                path.AddPolygon(new Point[] { new Point(8, 1), new Point(Width - 2, 1), new Point(Width - 2, Height - 9), new Point(Width - 10, Height - 2), new Point(1, Height - 2), new Point(1, 8) });
                Color top = Primary ? Color.FromArgb(220, 35, 64) : Color.FromArgb(197, 35, 55);
                Color bottom = Primary ? Color.FromArgb(154, 13, 37) : Color.FromArgb(142, 18, 35);
                if (hover && Enabled)
                {
                    top = Color.FromArgb(240, 51, 73);
                    bottom = Color.FromArgb(186, 19, 46);
                }

                if (!Enabled)
                {
                    top = Color.FromArgb(95, 43, 49);
                    bottom = Color.FromArgb(63, 22, 30);
                }

                using (var b = new LinearGradientBrush(ClientRectangle, top, bottom, 90f))
                    g.FillPath(b, path);
                using (var pen = new Pen(Enabled ? Color.FromArgb(92, 0, 19) : Color.FromArgb(69, 47, 62)))
                    g.DrawPath(pen, path);
                OutlinedText.Draw(g, Text, Font, ClientRectangle, Enabled ? Color.White : Color.FromArgb(170, 150, 155), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                if (Focused)
                    ControlPaint.DrawFocusRectangle(g, new Rectangle(6, 5, Width - 12, Height - 10), ForeColor, bottom);
            }
        }
    }
}
