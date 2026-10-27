using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Windows.Forms;

namespace WiiAnimeEncoder
{
    public partial class MainForm : Form
    {
        TextBox video, subtitle, output, title;
        DiscreteSlider quality;
        DiscreteSlider audio;
        Button start, preview, stop;
        Label status;
        Process worker;
        ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        System.Windows.Forms.Timer timer;
        string root = Path.GetDirectoryName(typeof(MainForm).Assembly.Location);
        string prefs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WiiAnimeEncoder", "settings.json");
        public MainForm() : this(null)
        {
        }

        internal MainForm(string preferencesPath)
        {
            if (preferencesPath != null)
                prefs = preferencesPath;
            BuildInterface();
            ApplyScarletTheme();
            LoadPrefs();
            timer = new System.Windows.Forms.Timer
            {
                Interval = 250
            };
            timer.Tick += (s, e) => Drain();
            if (nameDue != DateTime.MaxValue)
                timer.Start();
            FormClosing += (s, e) =>
            {
                if (worker != null && !worker.HasExited)
                {
                    if (MessageBox.Show(this, "Stop the current encode and close?", "Encoding in progress", MessageBoxButtons.YesNo) != DialogResult.Yes)
                    {
                        e.Cancel = true;
                        return;
                    }

                    e.Cancel = true;
                    closeWhenFinished = true;
                    StopWorker();
                    return;
                }

                if (nameWorker != null)
                {
                    try
                    {
                        if (!nameWorker.HasExited)
                        {
                            nameWorker.Kill();
                        }
                    }
                    catch
                    {
                    }
                }

                CleanWork();
                SavePrefs();
            };
        }
    }
}
