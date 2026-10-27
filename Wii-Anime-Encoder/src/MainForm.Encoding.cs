using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WiiAnimeEncoder
{
    public partial class MainForm
    {
        static string Quote(string v)
        {
            return "\"" + v.Replace("\"", "\\\"") + "\"";
        }

        void SavePrefs()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(prefs));
                File.WriteAllText(prefs, serializer.Serialize(new Dictionary<string, object> { { "Video", video.Text }, { "Subtitle", subtitle.Text }, { "Output", output.Text } }));
            }
            catch
            {
            }
        }

        void LoadPrefs()
        {
            try
            {
                var d = serializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(prefs));
                subtitle.Text = d["Subtitle"];
                video.Text = d["Video"];
                output.Text = d["Output"];
            }
            catch
            {
            }
        }

        List<string> convertedFiles = new List<string>();
        string selectedFile = "", activeWork = "", previewPath = "";
        bool previewRun, cancelled, closeWhenFinished;
        int nameGeneration = 0;
        DateTime nameDue = DateTime.MaxValue;
        Process nameWorker;
        Label percent;
        string lastError = "";
        int skipped = 0;
        bool previewOpen;
        volatile bool outputClosed;
        volatile bool errorClosed;
        readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
        void ScheduleName()
        {
            if (timer != null)
                timer.Start();
            nameGeneration++;
            nameDue = DateTime.UtcNow.AddMilliseconds(350);
            if (namePreview != null)
                namePreview.Text = String.IsNullOrWhiteSpace(title.Text) ? "Detecting output name..." : title.Text.Trim() + " - EpNN - <resolution>.mp4";
        }

        Dictionary<string, object> Config(bool plan, bool clip, string work)
        {
            return new Dictionary<string, object>
            {
                {
                    "Video",
                    video.Text
                },
                {
                    "Subtitle",
                    subtitle.Text
                },
                {
                    "Output",
                    output.Text
                },
                {
                    "Title",
                    title.Text
                },
                {
                    "Quality",
                    quality.Value / 2.0
                },
                {
                    "Audio",
                    audio.Value
                },
                {
                    "First",
                    false
                },
                {
                    "Plan",
                    plan
                },
                {
                    "Preview",
                    clip
                },
                {
                    "Selected",
                    selectedFile
                },
                {
                    "Work",
                    work
                }
            };
        }

        Process MakeWorker(Dictionary<string, object> config, string file)
        {
            File.WriteAllText(file, serializer.Serialize(config));
            return new Process
            {
                StartInfo = new ProcessStartInfo("powershell.exe", "-NoLogo -NoProfile -ExecutionPolicy Bypass -File " + Quote(Path.Combine(root, "Invoke-EncodingJob.ps1")) + " -Config " + Quote(file))
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
        }

        void RefreshName()
        {
            if (nameDue > DateTime.UtcNow || (nameWorker != null && !nameWorker.HasExited))
                return;
            nameDue = DateTime.MaxValue;
            if (!Directory.Exists(video.Text))
            {
                namePreview.Text = "Choose a video folder to see its output name.";
                return;
            }

            if (nameWorker != null)
            {
                nameWorker.Dispose();
                nameWorker = null;
            }

            int generation = nameGeneration;
            string configFile = Path.Combine(root, ".name-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                nameWorker = MakeWorker(Config(true, false, ""), configFile);
                nameWorker.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        messages.Enqueue("NAMING " + generation + " " + e.Data);
                        try
                        {
                            if (IsHandleCreated && !IsDisposed)
                                BeginInvoke(new Action(() => timer.Start()));
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                };
                nameWorker.ErrorDataReceived += (s, e) =>
                {
                };
                nameWorker.EnableRaisingEvents = true;
                nameWorker.Exited += (s, e) =>
                {
                    try
                    {
                        File.Delete(configFile);
                    }
                    catch
                    {
                    }
                };
                nameWorker.Start();
                nameWorker.BeginOutputReadLine();
                nameWorker.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                namePreview.Text = "Could not update name: " + ex.Message;
                try
                {
                    File.Delete(configFile);
                }
                catch
                {
                }
            }
        }

        void Run(bool clip)
        {
            if (worker != null)
                return;
            try
            {
                if (!Directory.Exists(video.Text) || !Directory.Exists(subtitle.Text))
                    throw new Exception("Select existing video and subtitle folders.");
                if (clip)
                {
                    using (var d = new OpenFileDialog())
                    {
                        d.Title = "Choose an episode for a 10-second preview";
                        d.InitialDirectory = video.Text;
                        d.Filter = "Video files|*.mkv;*.mp4;*.avi;*.m4v;*.mov;*.mpg;*.mpeg;*.wmv;*.ts;*.m2ts";
                        if (d.ShowDialog(this) != DialogResult.OK)
                            return;
                        selectedFile = d.FileName;
                    }
                }
                else
                {
                    selectedFile = "";
                    if (String.IsNullOrWhiteSpace(output.Text))
                        throw new Exception("Choose an output folder.");
                    string dest = Path.GetFullPath(output.Text).TrimEnd(Path.DirectorySeparatorChar);
                    if (String.Equals(dest, Path.GetFullPath(video.Text).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) || String.Equals(dest, Path.GetFullPath(subtitle.Text).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Use an output folder separate from your source folders.");
                }

                BeginEncode(clip);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Cannot start", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        void BeginEncode(bool clip)
        {
            timer.Start();
            if (!File.Exists(Path.Combine(root, "HandBrakeCLI.exe")))
                throw new Exception("HandBrakeCLI.exe is missing. Run setup-handbrake.ps1 or restore the encoder beside the app.");
            activeWork = Path.Combine(root, ".work-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(activeWork);
            previewRun = clip;
            previewPath = "";
            lastError = "";
            skipped = 0;
            cancelled = false;
            percent.Visible = true;
            percent.Text = "0.0%";
            status.Text = clip ? "Preparing 10-second preview..." : "Preparing conversion...";
            SavePrefs();
            string configFile = Path.Combine(activeWork, "job.json");
            worker = MakeWorker(Config(false, clip, activeWork), configFile);
            outputClosed = false;
            errorClosed = false;
            worker.OutputDataReceived += (s, e) =>
            {
                if (e.Data == null)
                    outputClosed = true;
                else
                    messages.Enqueue(e.Data);
            };
            worker.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null)
                    errorClosed = true;
                else
                    messages.Enqueue("DIAGNOSTIC " + e.Data);
            };
            try
            {
                worker.Start();
                worker.BeginOutputReadLine();
                worker.BeginErrorReadLine();
            }
            catch
            {
                worker.Dispose();
                worker = null;
                CleanWork();
                throw;
            }

            start.Enabled = false;
            preview.Enabled = false;
            stop.Enabled = true;
        }

        void CleanWork()
        {
            if (String.IsNullOrEmpty(activeWork))
                return;
            // Only this instance's exact generated directory and its direct files.
            string dir = Path.GetFullPath(activeWork);
            if (!String.Equals(Path.GetDirectoryName(dir), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(dir).StartsWith(".work-", StringComparison.Ordinal))
                return;
            try
            {
                foreach (string file in Directory.GetFiles(dir))
                    File.Delete(file);
                Directory.Delete(dir, false);
                activeWork = "";
            }
            catch (Exception ex)
            {
                status.Text = "Temporary cleanup pending: " + ex.Message;
            }
        }

        void Drain()
        {
            if (previewOpen)
                return;
            RefreshName();
            string line;
            int count = 0;
            string pendingPercent = null;
            var budget = Stopwatch.StartNew();
            // Bound each UI turn; progress is coalesced without dropping completion events.
            while (count++ < 250 && budget.ElapsedMilliseconds < 8 && messages.TryDequeue(out line))
            {
                bool naming = false;
                if (line.StartsWith("NAMING "))
                {
                    int split = line.IndexOf(' ', 7);
                    int generation;
                    if (split < 0 || !Int32.TryParse(line.Substring(7, split - 7), out generation) || generation != nameGeneration)
                        continue;
                    line = line.Substring(split + 1);
                    naming = true;
                }

                if (!line.StartsWith("EVENT "))
                {
                    if (!naming && line.StartsWith("DIAGNOSTIC "))
                        lastError = line.Substring(11);
                    continue;
                }

                try
                {
                    var evt = serializer.Deserialize<Dictionary<string, object>>(line.Substring(6));
                    string type = (string)evt["Type"];
                    object data = evt["Data"];
                    if (naming)
                    {
                        if (type == "NAME" || type == "ERROR")
                            namePreview.Text = Convert.ToString(data);
                        continue;
                    }

                    if (type == "NAME")
                        namePreview.Text = Convert.ToString(data);
                    if (type == "PROGRESS")
                        pendingPercent = Convert.ToDouble(data, System.Globalization.CultureInfo.InvariantCulture).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%";
                    if (type == "JOB")
                    {
                        var job = (Dictionary<string, object>)data;
                        status.Text = (previewRun ? "Preview" : "Episode") + " " + job["Index"] + "/" + job["Total"] + " - " + job["Name"];
                        pendingPercent = "0.0%";
                    }

                    if (type == "ERROR")
                        lastError = Convert.ToString(data);
                    if (type == "SKIP")
                    {
                        skipped++;
                        lastError = Convert.ToString(data);
                    }

                    if (type == "CLIP")
                        previewPath = Convert.ToString(data);
                    if (type == "SAVED")
                    {
                        string path = Convert.ToString(data);
                        if (!convertedFiles.Contains(path))
                            convertedFiles.Add(path);
                    }
                }
                catch (Exception ex)
                {
                    lastError = "Could not read encoder status: " + ex.Message;
                }
            }

            if (pendingPercent != null && percent.Text != pendingPercent)
                percent.Text = pendingPercent;
            // EOF callbacks guarantee the redirected pipes are drained, without blocking the UI.
            if (worker != null && worker.HasExited && outputClosed && errorClosed && messages.IsEmpty)
            {
                int exit = worker.ExitCode;
                worker.Dispose();
                worker = null;
                start.Enabled = true;
                preview.Enabled = true;
                stop.Enabled = false;
                if (cancelled)
                    status.Text = "Cancelled.";
                else if (exit != 0)
                {
                    status.Text = "Conversion failed.";
                    MessageBox.Show(this, String.IsNullOrEmpty(lastError) ? "HandBrake could not finish this conversion." : lastError, "Conversion failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    percent.Text = "100.0%";
                    status.Text = previewRun ? "Preview ready." : "Conversion complete." + (skipped > 0 ? " Skipped: " + skipped + "." : "");
                    if (skipped > 0)
                        MessageBox.Show(this, skipped + " file(s) skipped. Last issue: " + lastError, "Skipped files");
                    if (previewRun && File.Exists(previewPath))
                    {
                        previewOpen = true;
                        try
                        {
                            using (var player = new PreviewPlayer(previewPath))
                                player.ShowDialog(this);
                        }
                        finally
                        {
                            previewOpen = false;
                        }

                        status.Text = "Preview closed.";
                    }
                }

                CleanWork();
                if (closeWhenFinished)
                {
                    BeginInvoke(new Action(() => Close()));
                    return;
                }
            }

            if (worker == null && !String.IsNullOrEmpty(activeWork))
                CleanWork();
            if (worker == null && String.IsNullOrEmpty(activeWork) && nameDue == DateTime.MaxValue && (nameWorker == null || nameWorker.HasExited) && messages.IsEmpty)
                timer.Stop();
        }

        List<string> AvailableOutputs()
        {
            var files = new List<string>();
            foreach (string file in convertedFiles)
                if (File.Exists(file))
                    files.Add(file);
            return files;
        }

        void OpenOutput()
        {
            var files = AvailableOutputs();
            if (files.Count == 0)
            {
                MessageBox.Show(this, "No completed videos from this session are available yet.", "Open output");
                return;
            }

            string selected = files[0];
            if (files.Count > 1)
            {
                using (var dialog = new Form
                {
                    Text = "Choose a converted video",
                    Size = new Size(740, 420),
                    MinimumSize = new Size(480, 300),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Color.FromArgb(196, 48, 65)
                }

                )
                {
                    var list = new ListBox
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.FromArgb(196, 48, 65),
                        ForeColor = Color.White,
                        Font = new Font("Bahnschrift", 11),
                        HorizontalScrollbar = true,
                        IntegralHeight = false
                    };
                    foreach (string file in files)
                        list.Items.Add(Path.GetFileName(file));
                    list.SelectedIndex = 0;
                    var row = new FlowLayoutPanel
                    {
                        Dock = DockStyle.Bottom,
                        Height = 48
                    };
                    var open = new ThemedButton
                    {
                        Text = "Open video",
                        Size = new Size(125, 36),
                        DialogResult = DialogResult.OK
                    };
                    var cancel = new ThemedButton
                    {
                        Text = "Cancel",
                        Size = new Size(100, 36),
                        DialogResult = DialogResult.Cancel
                    };
                    row.Controls.Add(open);
                    row.Controls.Add(cancel);
                    dialog.Controls.Add(list);
                    dialog.Controls.Add(row);
                    dialog.AcceptButton = open;
                    dialog.CancelButton = cancel;
                    list.DoubleClick += (s, e) =>
                    {
                        if (list.SelectedIndex >= 0)
                        {
                            dialog.DialogResult = DialogResult.OK;
                            dialog.Close();
                        }
                    };
                    if (dialog.ShowDialog(this) != DialogResult.OK || list.SelectedIndex < 0)
                        return;
                    selected = files[list.SelectedIndex];
                }
            }

            try
            {
                Process.Start(new ProcessStartInfo(selected) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not open the video: " + ex.Message, "Open output");
            }
        }

        void StopWorker()
        {
            if (worker == null || worker.HasExited || cancelled)
                return;
            cancelled = true;
            int processId = worker.Id;
            status.Text = "Cancelling...";
            System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
            {
                try
                {
                    using (var p = Process.Start(new ProcessStartInfo("taskkill.exe", "/PID " + processId + " /T /F") { UseShellExecute = false, CreateNoWindow = true }))
                    {
                        if (!p.WaitForExit(10000))
                            messages.Enqueue("DIAGNOSTIC Cancellation is taking longer than expected.");
                    }
                }
                catch (Exception ex)
                {
                    messages.Enqueue("DIAGNOSTIC " + ex.Message);
                }
            });
        }
    }
}
