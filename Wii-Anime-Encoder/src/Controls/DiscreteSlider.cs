using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace WiiAnimeEncoder
{
    public class DiscreteSlider : Control
    {
        public int Minimum = 1, Maximum = 8, Divisor = 1;
        readonly Font tickFont = new Font("Bahnschrift", 8);
        int current = 1;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                tickFont.Dispose();
            base.Dispose(disposing);
        }

        public int Value
        {
            get
            {
                return current;
            }

            set
            {
                int next = Math.Max(Minimum, Math.Min(Maximum, value));
                if (next == current)
                    return;
                current = next;
                Invalidate();
                if (ValueChanged != null)
                    ValueChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler ValueChanged;
        public DiscreteSlider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var app = FindForm() as MainForm;
            if (app != null)
                app.PaintSurface(this, e);
            else
                base.OnPaintBackground(e);
        }

        int X(int v)
        {
            return 12 + (int)((Width - 24) * (v - Minimum) / (double)Math.Max(1, Maximum - Minimum));
        }

        void Pick(int x)
        {
            Value = Minimum + (int)Math.Round(Math.Max(0, Math.Min(1, (x - 12) / (double)Math.Max(1, Width - 24))) * (Maximum - Minimum));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                Focus();
                Capture = true;
                Pick(e.X);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Capture)
                Pick(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            Capture = false;
            base.OnMouseUp(e);
        }

        protected override bool IsInputKey(Keys k)
        {
            return k == Keys.Left || k == Keys.Right || k == Keys.Up || k == Keys.Down || base.IsInputKey(k);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down)
            {
                Value--;
                e.Handled = true;
            }

            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up)
            {
                Value++;
                e.Handled = true;
            }

            if (e.KeyCode == Keys.Home)
            {
                Value = Minimum;
                e.Handled = true;
            }

            if (e.KeyCode == Keys.End)
            {
                Value = Maximum;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var line = new Pen(Color.FromArgb(49, 5, 14), 4))
                g.DrawLine(line, 12, 15, Width - 12, 15);
            using (var fill = new Pen(Color.FromArgb(190, 13, 43), 4))
                g.DrawLine(fill, 12, 15, X(Value), 15);
            using (var tick = new SolidBrush(Color.FromArgb(185, 25, 48)))
            {
                for (int v = Minimum; v <= Maximum; v++)
                {
                    int x = X(v);
                    g.FillEllipse(tick, x - 2, 13, 4, 4);
                    if (Divisor == 1 || v % Divisor == 0)
                        OutlinedText.Draw(g, (v / (double)Divisor).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture), tickFont, new Rectangle(x - 14, 28, 28, 20), ForeColor, TextFormatFlags.HorizontalCenter);
                }
            }

            using (var brush = new SolidBrush(Color.FromArgb(219, 24, 51)))
                g.FillEllipse(brush, X(Value) - 6, 9, 12, 12);
            using (var pen = new Pen(Color.FromArgb(37, 0, 8), 2))
                g.DrawEllipse(pen, X(Value) - 6, 9, 12, 12);
            // Indicate keyboard focus on the thumb without boxing in the entire slider.
            if (Focused && ShowFocusCues && !Capture)
                using (var focus = new Pen(Color.White, 1))
                    g.DrawEllipse(focus, X(Value) - 8, 7, 16, 16);
        }
    }
}
