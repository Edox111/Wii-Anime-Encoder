using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiiAnimeEncoder
{
    public static class OutlinedText
    {
        static readonly Point[] OutlineOffsets =
        {
            new Point(-1, -1),
            new Point(0, -1),
            new Point(1, -1),
            new Point(-1, 0),
            new Point(1, 0),
            new Point(-1, 1),
            new Point(0, 1),
            new Point(1, 1)
        };
        public static void Draw(Graphics g, string text, Font font, Rectangle rect, Color color, TextFormatFlags flags)
        {
            foreach (Point offset in OutlineOffsets)
            {
                Rectangle edge = rect;
                edge.Offset(offset);
                TextRenderer.DrawText(g, text, font, edge, Color.Black, flags);
            }

            TextRenderer.DrawText(g, text, font, rect, color, flags);
        }
    }
}
