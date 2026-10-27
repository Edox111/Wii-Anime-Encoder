using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiiAnimeEncoder
{
    public class OutlinedLabel : Label
    {
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var app = FindForm() as MainForm;
            if (app != null)
                app.PaintSurface(this, e);
            else
                base.OnPaintBackground(e);
        }

        public OutlinedLabel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        string measuredText;
        Font measuredFont;
        int measuredWidth;
        Padding measuredPadding;
        Size preferredSize;
        bool hasMeasurement;
        public override Size GetPreferredSize(Size proposedSize)
        {
            int width = MaximumSize.Width > 0 ? Math.Max(1, MaximumSize.Width - Padding.Horizontal - 4) : 32767;
            if (hasMeasurement && measuredText == Text && measuredFont == Font && measuredWidth == width && measuredPadding == Padding)
                return preferredSize;
            Size measured = TextRenderer.MeasureText(Text, Font, new Size(width, 32767), TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak);
            measuredText = Text;
            measuredFont = Font;
            measuredWidth = width;
            measuredPadding = Padding;
            hasMeasurement = true;
            preferredSize = new Size(measured.Width + Padding.Horizontal + 4, measured.Height + Padding.Vertical + 2);
            return preferredSize;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rect = new Rectangle(Padding.Left, Padding.Top, Math.Max(1, Width - Padding.Horizontal), Math.Max(1, Height - Padding.Vertical));
            TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak;
            if (AutoEllipsis)
                flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            if (TextAlign == ContentAlignment.MiddleCenter)
                flags |= TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
            OutlinedText.Draw(e.Graphics, Text, Font, rect, ForeColor, flags);
        }
    }
}
