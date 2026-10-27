using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiiAnimeEncoder
{
    public class PreviewPlayer : Form
    {
        System.Windows.Controls.MediaElement media;
        System.Windows.Forms.Integration.ElementHost host;
        bool paused = false;
        public PreviewPlayer(string file)
        {
            Text = "Preview";
            BackColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;
            ShowInTaskbar = false;
            media = new System.Windows.Controls.MediaElement
            {
                LoadedBehavior = System.Windows.Controls.MediaState.Manual,
                UnloadedBehavior = System.Windows.Controls.MediaState.Manual,
                Stretch = System.Windows.Media.Stretch.Uniform,
                Volume = 0.7
            };
            host = new System.Windows.Forms.Integration.ElementHost
            {
                Dock = DockStyle.Fill,
                Child = media,
                BackColor = Color.Black
            };
            Controls.Add(host);
            media.MediaFailed += (s, e) =>
            {
                MessageBox.Show(this, "Windows could not play the preview: " + e.ErrorException.Message, "Preview playback");
                Close();
            };
            media.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    e.Handled = true;
                    Close();
                }
                else if (e.Key == System.Windows.Input.Key.Space)
                {
                    e.Handled = true;
                    TogglePause();
                }
                else if (e.Key == System.Windows.Input.Key.R)
                {
                    e.Handled = true;
                    Replay();
                }
            };
            Shown += (s, e) =>
            {
                Bounds = Screen.FromControl(Owner ?? this).Bounds;
                media.Source = new Uri(file);
                media.Play();
            };
        }

        void TogglePause()
        {
            paused = !paused;
            if (paused)
                media.Pause();
            else
                media.Play();
        }

        void Replay()
        {
            paused = false;
            media.Position = TimeSpan.Zero;
            media.Play();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys key)
        {
            if (key == Keys.Escape)
            {
                Close();
                return true;
            }

            if (key == Keys.Space)
            {
                TogglePause();
                return true;
            }

            if (key == Keys.R)
            {
                Replay();
                return true;
            }

            return base.ProcessCmdKey(ref msg, key);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            media.Stop();
            media.Close();
            media.Source = null;
            host.Child = null;
            base.OnFormClosing(e);
        }
    }
}
