using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiiAnimeEncoder
{
    public class BackgroundTable : TableLayoutPanel
    {
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var app = FindForm() as MainForm;
            if (app != null)
                app.PaintSurface(this, e);
            else
                base.OnPaintBackground(e);
        }

        public BackgroundTable()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }
}
