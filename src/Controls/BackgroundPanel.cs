using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiiAnimeEncoder
{
    public class BackgroundPanel : Panel
    {
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var app = FindForm() as MainForm;
            if (app != null)
                app.PaintSurface(this, e);
            else
                base.OnPaintBackground(e);
        }

        public BackgroundPanel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }
}
