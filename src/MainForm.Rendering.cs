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
        Image themeArtwork;
        string loadedArtwork;
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
                    c.BackColor = theme.Surface;
                if (c is TextBox)
                    ((TextBox)c).BorderStyle = BorderStyle.FixedSingle;
                if (c is ComboBox)
                    ((ComboBox)c).FlatStyle = FlatStyle.Flat;
                if (c is NumericUpDown)
                    ((NumericUpDown)c).BorderStyle = BorderStyle.FixedSingle;
                if (c is Label || c is CheckBox)
                    c.BackColor = Color.Transparent;
                if (c is ThemedButton) { c.BackColor = theme.Surface; ((ThemedButton)c).Accent = theme.Accent; ((ThemedButton)c).RichAccent = theme.Name == "Crimson" || theme.Name == "Scarlet"; }
                if (c is DiscreteSlider) ((DiscreteSlider)c).Accent = theme.Accent;
                if (c.HasChildren && !(c is NumericUpDown))
                    StyleControls(c);
            }
        }

        AppTheme theme = AppTheme.All[0];
        ComboBox themePicker;
        void InitializeThemes()
        {
            Text = "Wii Anime Encoder";
            DoubleBuffered = true;
            ApplyTheme();
        }

        void ApplyTheme()
        {
            theme = themePicker.SelectedItem as AppTheme ?? AppTheme.All[0];
            if (loadedArtwork != theme.ArtworkFile)
            {
                if (themeArtwork != null) { themeArtwork.Dispose(); themeArtwork = null; }
                loadedArtwork = theme.ArtworkFile;
                string art = Path.Combine(root, loadedArtwork);
                try { if (File.Exists(art)) using (var source = Image.FromFile(art)) themeArtwork = new Bitmap(source); }
                catch (IOException) { }
                catch (ArgumentException) { } // An invalid optional image falls back to the palette.
                catch (OutOfMemoryException) { } // GDI+ also reports invalid image data this way.
            }
            BackColor = theme.Background;
            StyleControls(this);
            ((ThemedButton)start).Primary = true;
            namePreview.ForeColor = theme.Accent;
            artRail.Width = 0;
            ClearBackgroundCache();
            Invalidate(true);
        }

        void ShowCredits()
        {
            string file = Path.Combine(root, "CREDITS-AND-LICENSES.txt");
            using (var dialog = new Form { Text = "About / Credits", Size = new Size(680, 520), StartPosition = FormStartPosition.CenterParent })
            {
                var text = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical,
                    BackColor = theme.Surface, ForeColor = Color.White, Font = Font,
                    Text = "Wii Anime Encoder\r\nTheme: " + theme.Name + "\r\n\r\n" + (File.Exists(Path.Combine(root, "themes/CREDITS.txt")) ? File.ReadAllText(Path.Combine(root, "themes/CREDITS.txt")) + "\r\n\r\n" : "") + (File.Exists(file) ? File.ReadAllText(file) : "Credits file is missing.") };
                dialog.Controls.Add(text);
                dialog.ShowDialog(this);
            }
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

                if (applicationIcon != null)
                {
                    Icon = null;
                    applicationIcon.Dispose();
                    applicationIcon = null;
                }
                hints.Dispose();
                ClearBackgroundCache();
                if (themeArtwork != null)
                {
                    themeArtwork.Dispose();
                    themeArtwork = null;
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
            g.Clear(theme.Background);
            if (themeArtwork != null && ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                // Cover the window without distorting the image's original aspect ratio.
                float scale = Math.Max((float)ClientSize.Width / themeArtwork.Width, (float)ClientSize.Height / themeArtwork.Height);
                float w = themeArtwork.Width * scale, h = themeArtwork.Height * scale;
                g.InterpolationMode = interactiveResize ? InterpolationMode.Bilinear : InterpolationMode.HighQualityBicubic;
                var target = new RectangleF((ClientSize.Width - w) / 2, (ClientSize.Height - h) / 2, w, h);
                if (theme.Name == "Crimson")
                {
                    using (var attributes = new System.Drawing.Imaging.ImageAttributes())
                    {
                        var matrix = new System.Drawing.Imaging.ColorMatrix();
                        matrix.Matrix11 = 0.48f; matrix.Matrix22 = 0.50f;
                        attributes.SetColorMatrix(matrix);
                        g.DrawImage(themeArtwork, Rectangle.Round(target), 0, 0, themeArtwork.Width, themeArtwork.Height, GraphicsUnit.Pixel, attributes);
                    }
                    // The halo follows the moon in image coordinates across window sizes.
                    float radius = h * 0.35f;
                    float cx = target.X + w * 0.5f, cy = target.Y + h * 0.5f;
                    using (var halo = new GraphicsPath())
                    {
                        halo.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
                        using (var glow = new PathGradientBrush(halo))
                        {
                            glow.CenterPoint = new PointF(cx, cy);
                            glow.InterpolationColors = new ColorBlend {
                                Colors = new[] { Color.FromArgb(0, 255, 8, 25), Color.FromArgb(115, 255, 8, 25), Color.FromArgb(20, 255, 8, 25), Color.FromArgb(0, 255, 8, 25) },
                                Positions = new[] { 0f, 0.30f, 0.48f, 1f }
                            };
                            g.FillPath(glow, halo);
                        }
                    }
                }
                else g.DrawImage(themeArtwork, target);
                using (var overlay = new SolidBrush(Color.FromArgb(theme.Name == "Crimson" ? 45 : (theme.Name == "Scarlet" ? 85 : (theme.Name == "OLED Black" ? 185 : 145)), theme.Background))) g.FillRectangle(overlay, ClientRectangle);
            }
        }
    }
}
