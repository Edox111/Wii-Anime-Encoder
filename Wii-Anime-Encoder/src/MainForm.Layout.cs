using System.IO;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiiAnimeEncoder
{
    public partial class MainForm
    {
        string previousVideo = "";
        Label namePreview;
        Panel workspace;
        Panel artRail;
        bool fullscreen;
        Rectangle savedBounds;
        FormWindowState savedState;
        Size savedMinimum;
        ToolTip hints = new ToolTip();
        Color surface = Color.Transparent;
        TableLayoutPanel Table(int columns)
        {
            return new BackgroundTable
            {
                ColumnCount = columns,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                BackColor = surface,
                Padding = new Padding(8),
                Margin = new Padding(0, 0, 0, 8)
            };
        }

        Label Caption(string text)
        {
            return new OutlinedLabel
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 4, 12, 4),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
        }

        Button Action(string text, EventHandler click, int width = 110)
        {
            var b = new ThemedButton
            {
                Text = text,
                Size = new Size(width, 36),
                Margin = new Padding(8, 0, 0, 0),
                Anchor = AnchorStyles.Right
            };
            b.Click += click;
            return b;
        }

        Control FolderCard(string text, out TextBox field)
        {
            var card = Table(1);
            card.Dock = DockStyle.Fill;
            card.Padding = Padding.Empty;
            card.Margin = new Padding(0, 0, 0, 16);
            var heading = Caption(text);
            heading.Font = new Font(Font, FontStyle.Bold);
            card.Controls.Add(heading);
            var row = Table(2);
            row.BackColor = Color.Transparent;
            row.Padding = Padding.Empty;
            row.Margin = Padding.Empty;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            var box = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 4)
            };
            var browse = Action("Browse...", (s, e) =>
            {
                using (var d = new FolderBrowserDialog())
                {
                    d.Description = text;
                    if (Directory.Exists(box.Text))
                        d.SelectedPath = box.Text;
                    if (d.ShowDialog(this) == DialogResult.OK)
                        box.Text = d.SelectedPath;
                }
            });
            box.TextChanged += (s, e) =>
            {
                hints.SetToolTip(box, box.Text);
            };
            row.Controls.Add(box, 0, 0);
            row.Controls.Add(browse, 1, 0);
            card.Controls.Add(row);
            field = box;
            return card;
        }

        void BuildInterface()
        {
            SuspendLayout();
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            Font = new Font("Bahnschrift", 10);
            ClientSize = new Size(1300, 900);
            MinimumSize = new Size(1050, 720);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            artRail = new BackgroundPanel
            {
                Dock = DockStyle.Left,
                Width = 360,
                BackColor = Color.Transparent
            };
            Controls.Add(artRail);
            workspace = new BackgroundPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                BackColor = Color.Transparent
            };
            Controls.Add(workspace);
            workspace.BringToFront();
            var stack = new BackgroundTable
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            var frame = new BackgroundTable
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            frame.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            frame.RowStyles.Add(new RowStyle(SizeType.Absolute, 430));
            frame.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            frame.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
            frame.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            workspace.Controls.Add(frame);
            var settings = new BackgroundPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            frame.Controls.Add(settings, 0, 0);
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            settings.Controls.Add(stack);
            var folders = Table(2);
            folders.Padding = Padding.Empty;
            folders.Margin = Padding.Empty;
            folders.BackColor = Color.Transparent;
            folders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            folders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var inputs = Table(1);
            inputs.Dock = DockStyle.Top;
            inputs.Padding = Padding.Empty;
            inputs.BackColor = Color.Transparent;
            inputs.Margin = new Padding(0, 0, 16, 0);
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var outputs = Table(1);
            outputs.Dock = DockStyle.Top;
            outputs.Padding = Padding.Empty;
            outputs.BackColor = Color.Transparent;
            outputs.Margin = Padding.Empty;
            outputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            folders.Controls.Add(inputs, 0, 0);
            folders.Controls.Add(outputs, 1, 0);
            inputs.Controls.Add(FolderCard("1  Video folder", out video), 0, 0);
            inputs.Controls.Add(FolderCard("2  Subtitle folder (Default)", out subtitle), 0, 1);
            outputs.Controls.Add(FolderCard("3  Completed videos", out output), 0, 0);
            video.TextChanged += (s, e) =>
            {
                selectedFile = "";
                ScheduleName();
                if (String.IsNullOrWhiteSpace(subtitle.Text) || String.Equals(subtitle.Text, previousVideo, StringComparison.OrdinalIgnoreCase))
                    subtitle.Text = video.Text;
                previousVideo = video.Text;
            };
            var names = Table(1);
            names.Dock = DockStyle.Fill;
            names.Padding = Padding.Empty;
            names.Margin = new Padding(0, 0, 0, 12);
            namePreview = new OutlinedLabel
            {
                Text = "Choose a video folder to see its output name.",
                AutoSize = false,
                Height = 30,
                Dock = DockStyle.Top,
                AutoEllipsis = true,
                Margin = Padding.Empty
            };
            names.Controls.Add(namePreview);
            var paths = Table(1);
            paths.Dock = DockStyle.Top;
            paths.Padding = Padding.Empty;
            paths.Margin = new Padding(0, 0, 0, 8);
            paths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            paths.Controls.Add(Caption("Anime title (optional)"), 0, 0);
            title = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 4)
            };
            paths.Controls.Add(title, 0, 1);
            title.TextChanged += (s, e) => ScheduleName();
            outputs.Controls.Add(paths, 0, 1);
            outputs.Controls.Add(names, 0, 2);
            stack.Controls.Add(folders);
            var options = Table(2);
            options.Padding = Padding.Empty;
            options.Margin = new Padding(0, 12, 0, 12);
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var rfGroup = Table(1);
            rfGroup.Dock = DockStyle.Fill;
            rfGroup.Padding = Padding.Empty;
            rfGroup.Margin = new Padding(0, 0, 16, 0);
            rfGroup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var rfLabel = Caption("RF quality: 19.0");
            rfLabel.AutoSize = false;
            rfLabel.Dock = DockStyle.Top;
            rfLabel.Height = 28;
            rfLabel.AutoEllipsis = true;
            quality = new DiscreteSlider
            {
                Minimum = 36,
                Maximum = 44,
                Value = 38,
                Divisor = 2,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Height = 52
            };
            quality.AccessibleName = "RF quality in half steps";
            quality.ValueChanged += (s, e) => rfLabel.Text = "RF quality: " + (quality.Value / 2.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            hints.SetToolTip(quality, "18-22 in 0.5 steps. Lower means higher quality and larger files. Default: 19. Use arrow keys for precise steps.");
            rfGroup.Controls.Add(rfLabel);
            rfGroup.Controls.Add(quality);
            options.Controls.Add(rfGroup, 0, 0);
            var audioGroup = Table(1);
            audioGroup.Dock = DockStyle.Fill;
            audioGroup.Padding = Padding.Empty;
            audioGroup.Margin = Padding.Empty;
            audioGroup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var audioLabel = Caption("Audio track: 1");
            audioLabel.AutoSize = false;
            audioLabel.Dock = DockStyle.Top;
            audioLabel.Height = 28;
            audioLabel.AutoEllipsis = true;
            audio = new DiscreteSlider
            {
                Minimum = 1,
                Maximum = 8,
                Value = 1,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Height = 52
            };
            audio.AccessibleName = "Audio track";
            audio.ValueChanged += (s, e) => audioLabel.Text = "Audio track: " + audio.Value;
            hints.SetToolTip(audio, "Tracks 1-8. Choose a track that exists in every selected episode; this is not an automatic track count.");
            audioGroup.Controls.Add(audioLabel);
            audioGroup.Controls.Add(audio);
            options.Controls.Add(audioGroup, 1, 0);
            stack.Controls.Add(options);
            var actions = Table(9);
            actions.Padding = Padding.Empty;
            actions.Margin = new Padding(0, 0, 0, 12);
            for (int i = 0; i < 9; i++)
                actions.ColumnStyles.Add(i % 2 == 0 ? new ColumnStyle(SizeType.Percent, 20) : new ColumnStyle(SizeType.Absolute, 12));
            preview = Action("Preview", (s, e) => Run(true), 160);
            preview.Margin = new Padding(0, 0, 10, 8);
            start = Action("Start encoding", (s, e) => Run(false), 160);
            start.Margin = new Padding(0, 0, 10, 8);
            stop = Action("Stop", (s, e) => StopWorker(), 85);
            stop.Enabled = false;
            stop.Margin = new Padding(0, 0, 10, 8);
            var open = Action("Open output", (s, e) => OpenOutput(), 140);
            open.Margin = new Padding(0, 0, 0, 8);
            actions.Controls.AddRange(new Control[] { preview, start, stop, open });
            var exit = Action("Exit", (s, e) => Close(), 85);
            exit.Margin = new Padding(10, 0, 0, 8);
            actions.Controls.Add(exit);
            Control[] actionButtons =
            {
                preview,
                start,
                stop,
                open,
                exit
            };
            actions.Controls.Clear();
            for (int i = 0; i < actionButtons.Length; i++)
            {
                Control button = actionButtons[i];
                button.Dock = DockStyle.Fill;
                button.Margin = new Padding(0, 0, 0, 8);
                actions.Controls.Add(button, i * 2, 0);
            }

            exit.Margin = new Padding(0, 0, 0, 8);
            hints.SetToolTip(preview, "Choose an episode, encode its first 10 seconds with subtitles, and watch it in a temporary preview window.");
            frame.Controls.Add(actions, 0, 1);
            var progress = new BackgroundTable
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = surface,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            progress.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            progress.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            progress.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            status = new OutlinedLabel
            {
                Text = "",
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Margin = Padding.Empty
            };
            progress.Controls.Add(status, 0, 0);
            percent = new OutlinedLabel
            {
                Text = "0.0%",
                Visible = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Bahnschrift", 36, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            progress.Controls.Add(percent, 0, 1);
            frame.Controls.Add(progress, 0, 2);
            bool arranging = false;
            Action arrange = () =>
            {
                if (arranging)
                    return;
                arranging = true;
                try
                {
                    int available = Math.Max(140, frame.ClientSize.Height - actions.PreferredSize.Height - 140);
                    float desired = Math.Min(stack.Height + 4, available);
                    if (Math.Abs(frame.RowStyles[0].Height - desired) > 1)
                        frame.RowStyles[0].Height = desired;
                }
                finally
                {
                    arranging = false;
                }
            };
            stack.SizeChanged += (s, e) => arrange();
            frame.SizeChanged += (s, e) => arrange();
            actions.SizeChanged += (s, e) => arrange();
            Shown += (s, e) => arrange();
            KeyPreview = true;
            Resize += (s, e) =>
            {
                if (artRail != null)
                    artRail.Width = Math.Max(0, Math.Min(500, ClientSize.Width - 950));
                Invalidate();
            };
            ResumeLayout(true);
        }
    }
}
