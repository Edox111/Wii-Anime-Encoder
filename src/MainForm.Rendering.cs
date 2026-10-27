using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WiiAnimeEncoder
{
    public partial class MainForm
    {
        Image remilia;
        Bitmap backgroundCache;
        bool interactiveResize;
        protected override void OnResizeBegin(EventArgs e)
        {
            interactiveResize = true;
            base.OnResizeBegin(e);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            interactiveResize = false;
            ClearBackgroundCache();
            Invalidate(true);
            base.OnResizeEnd(e);
        }

        void ClearBackgroundCache()
        {
            if (backgroundCache != null)
            {
                backgroundCache.Dispose();
                backgroundCache = null;
            }
        }

        public void PaintSurface(Control control, PaintEventArgs e)
        {
            EnsureBackgroundCache();
            if (backgroundCache == null)
                return;
            Point origin = PointToClient(control.PointToScreen(Point.Empty));
            var clip = Rectangle.Intersect(control.ClientRectangle, e.ClipRectangle);
            if (clip.Width > 0 && clip.Height > 0)
                e.Graphics.DrawImage(backgroundCache, clip, new Rectangle(origin.X + clip.X, origin.Y + clip.Y, clip.Width, clip.Height), GraphicsUnit.Pixel);
            if (control.BackColor.A > 0)
                using (var brush = new SolidBrush(control.BackColor))
                    e.Graphics.FillRectangle(brush, control.ClientRectangle);
        }

        void EnsureBackgroundCache()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;
            if (backgroundCache != null && backgroundCache.Size == ClientSize)
                return;
            ClearBackgroundCache();
            backgroundCache = new Bitmap(ClientSize.Width, ClientSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(backgroundCache))
                RenderBackground(g);
        }

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int enabled = 1;
                DwmSetWindowAttribute(Handle, 20, ref enabled, 4);
            }
            catch
            {
            }
        }

        void StyleControls(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                c.ForeColor = Color.White;
                if (c is TextBox || c is ComboBox || c is NumericUpDown || c is RichTextBox)
                    c.BackColor = Color.FromArgb(196, 48, 65);
                if (c is TextBox)
                    ((TextBox)c).BorderStyle = BorderStyle.FixedSingle;
                if (c is ComboBox)
                    ((ComboBox)c).FlatStyle = FlatStyle.Flat;
                if (c is NumericUpDown)
                    ((NumericUpDown)c).BorderStyle = BorderStyle.FixedSingle;
                if (c is Label || c is CheckBox)
                    c.BackColor = Color.Transparent;
                if (c is ThemedButton)
                    c.BackColor = Color.FromArgb(196, 48, 65);
                if (c.HasChildren && !(c is NumericUpDown))
                    StyleControls(c);
            }
        }

        void ApplyScarletTheme()
        {
            Text = "Wii Anime Encoder | Scarlet Edition";
            Icon = Icon.ExtractAssociatedIcon(Path.Combine(root, "WiiAnimeEncoder.exe"));
            DoubleBuffered = true;
            BackColor = Color.FromArgb(15, 10, 18);
            string art = Path.Combine(root, "remilia.jpg");
            if (File.Exists(art))
            {
                using (var source = Image.FromFile(art))
                    remilia = new Bitmap(source);
            }

            StyleControls(this);
            ((ThemedButton)start).Primary = true;
            namePreview.ForeColor = Color.FromArgb(255, 102, 112);
            status.ForeColor = Color.White;
            status.BackColor = Color.Transparent;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                }

                hints.Dispose();
                ClearBackgroundCache();
                if (remilia != null)
                {
                    remilia.Dispose();
                    remilia = null;
                }

                if (nameWorker != null)
                {
                    nameWorker.Dispose();
                    nameWorker = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F11)
            {
                ToggleFullscreen();
                return true;
            }

            if (keyData == Keys.Escape && fullscreen)
            {
                ToggleFullscreen();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        void ToggleFullscreen()
        {
            SuspendLayout();
            if (!fullscreen)
            {
                var screen = Screen.FromControl(this).Bounds;
                savedState = WindowState;
                savedBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                savedMinimum = MinimumSize;
                fullscreen = true;
                WindowState = FormWindowState.Normal;
                MinimumSize = Size.Empty;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = screen;
            }
            else
            {
                fullscreen = false;
                FormBorderStyle = FormBorderStyle.Sizable;
                MinimumSize = savedMinimum;
                WindowState = FormWindowState.Normal;
                Bounds = savedBounds;
                WindowState = savedState;
            }

            ResumeLayout(true);
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            EnsureBackgroundCache();
            if (backgroundCache != null)
                e.Graphics.DrawImageUnscaled(backgroundCache, 0, 0);
        }

        void RenderBackground(Graphics g)
        {
            g.Clear(Color.FromArgb(13, 9, 17));
            if (remilia != null && ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                // Cover the window without distorting the image's original aspect ratio.
                float scale = Math.Max((float)ClientSize.Width / remilia.Width, (float)ClientSize.Height / remilia.Height);
                float w = remilia.Width * scale, h = remilia.Height * scale;
                g.InterpolationMode = interactiveResize ? InterpolationMode.Bilinear : InterpolationMode.HighQualityBicubic;
                g.DrawImage(remilia, new RectangleF((ClientSize.Width - w) / 2, (ClientSize.Height - h) / 2, w, h));
            }
        }
    }
}
