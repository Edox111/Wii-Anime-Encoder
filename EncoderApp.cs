using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Web.Script.Serialization;
using System.Windows.Forms;

public class EncoderApp : Form {
    TextBox video, subtitle, output, title;

    ScarletSlider quality;
    ScarletSlider audio;

    Button start, preview, stop;
    Label status;
    Process worker;
    ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
    System.Windows.Forms.Timer timer;
    string root = Path.GetDirectoryName(typeof(EncoderApp).Assembly.Location);
    string prefs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WiiAnimeEncoder", "settings.json");

    [STAThread] public static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new EncoderApp());
    }
    Label LabelAt(string text, int x, int y, int w, int h) {
        Label l = new InkLabel { Text=text, Left=x, Top=y, Width=w, Height=h };
        Controls.Add(l); return l;
    }
    Button ButtonAt(string text, int x, int y, int w, EventHandler handler) {
        Button b = new ScarletButton { Text=text, Left=x, Top=y, Width=w, Height=34, FlatStyle=FlatStyle.Flat };
        b.Click += handler; Controls.Add(b); return b;
    }
    TextBox Folder(string heading, int x, int y, int width) {
        LabelAt(heading,x,y,width,24).Font = new Font("Bahnschrift",11,FontStyle.Bold);
        TextBox t = new TextBox { Left=x, Top=y+30, Width=width-100, Height=30 };
        Controls.Add(t);
        ButtonAt("Browse...",x+width-90,y+25,90,(s,e)=>{
            using (FolderBrowserDialog d = new FolderBrowserDialog()) {
                d.Description=heading;
                if (Directory.Exists(t.Text)) d.SelectedPath=t.Text;
                if(d.ShowDialog(this)==DialogResult.OK)t.Text=d.SelectedPath;
            }
        });
        return t;
    }
    public EncoderApp() : this(null) {}
    internal EncoderApp(string preferencesPath) {
        if(preferencesPath!=null) prefs=preferencesPath;
        BuildInterface();
        ApplyScarletTheme();
        LoadPrefs();
        timer=new System.Windows.Forms.Timer{Interval=250};
        timer.Tick+=(s,e)=>Drain();timer.Start();
        FormClosing+=(s,e)=>{
            if(worker!=null&&!worker.HasExited) {
                if(MessageBox.Show(this,"Stop the current encode and close?","Encoding in progress",MessageBoxButtons.YesNo)!=DialogResult.Yes){e.Cancel=true;return;}
                e.Cancel=true;closeWhenFinished=true;StopWorker();return;
            }
            if(nameWorker!=null){try{if(!nameWorker.HasExited){nameWorker.Kill();nameWorker.WaitForExit();}}catch{}}
            CleanWork();
            SavePrefs();
        };
    }
    Image remilia;
    Bitmap backgroundCache;
    bool interactiveResize;
    protected override void OnResizeBegin(EventArgs e) { interactiveResize=true; base.OnResizeBegin(e); }
    protected override void OnResizeEnd(EventArgs e) {
        interactiveResize=false; ClearBackgroundCache(); Invalidate(true); base.OnResizeEnd(e);
    }
    void ClearBackgroundCache() { if(backgroundCache!=null){backgroundCache.Dispose();backgroundCache=null;} }
    public void PaintSurface(Control control,PaintEventArgs e) {
        EnsureBackgroundCache();
        if(backgroundCache==null)return;
        Point origin=PointToClient(control.PointToScreen(Point.Empty));
        e.Graphics.DrawImage(backgroundCache,new Rectangle(0,0,control.Width,control.Height),
            new Rectangle(origin,control.Size),GraphicsUnit.Pixel);
        var ancestors=new List<Control>();
        for(Control c=control;c!=null && c!=this;c=c.Parent)ancestors.Add(c);
        for(int i=ancestors.Count-1;i>=0;i--) {
            Color color=ancestors[i].BackColor;
            if(color.A>0)using(var brush=new SolidBrush(color))e.Graphics.FillRectangle(brush,control.ClientRectangle);
        }
    }
    void EnsureBackgroundCache() {
        if(ClientSize.Width<=0||ClientSize.Height<=0)return;
        if(backgroundCache!=null && backgroundCache.Size==ClientSize)return;
        ClearBackgroundCache();
        backgroundCache=new Bitmap(ClientSize.Width,ClientSize.Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using(var g=Graphics.FromImage(backgroundCache))RenderBackground(g);
    }
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        try { int enabled=1; DwmSetWindowAttribute(Handle,20,ref enabled,4); } catch {}
    }
    string previousVideo="";
    Label namePreview;
    Panel workspace;
    Panel artRail; bool fullscreen; Rectangle savedBounds; FormWindowState savedState; Size savedMinimum;
    ToolTip hints = new ToolTip();
    Color surface=Color.Transparent;
    TableLayoutPanel Table(int columns) {
        return new GlassTable {ColumnCount=columns,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,
            Dock=DockStyle.Top,BackColor=surface,Padding=new Padding(8),Margin=new Padding(0,0,0,8)};
    }
    Label Caption(string text) {
        return new InkLabel{Text=text,AutoSize=true,Anchor=AnchorStyles.Left,
            Margin=new Padding(0,4,12,4),ForeColor=Color.White,BackColor=Color.Transparent};
    }
    Button Action(string text,EventHandler click,int width=110) {
        var b=new ScarletButton{Text=text,Size=new Size(width,36),Margin=new Padding(8,0,0,0),Anchor=AnchorStyles.Right};
        b.Click+=click;return b;
    }
    Control FolderCard(string text,out TextBox field) {
        var card=Table(1);card.Dock=DockStyle.Fill;card.Padding=Padding.Empty;card.Margin=new Padding(0,0,0,16);
        var heading=Caption(text);heading.Font=new Font(Font,FontStyle.Bold);card.Controls.Add(heading);
        var row=Table(2);row.BackColor=Color.Transparent;row.Padding=Padding.Empty;row.Margin=Padding.Empty;
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,112));
        var box=new TextBox{Dock=DockStyle.Fill,Margin=new Padding(0,5,0,4)};
        var browse=Action("Browse...",(s,e)=>{
            using(var d=new FolderBrowserDialog()){d.Description=text;
                if(Directory.Exists(box.Text))d.SelectedPath=box.Text;
                if(d.ShowDialog(this)==DialogResult.OK)box.Text=d.SelectedPath;
            }
        });
        box.TextChanged+=(s,e)=>{hints.SetToolTip(box,box.Text); if(namePreview!=null)ScheduleName();};
        row.Controls.Add(box,0,0);row.Controls.Add(browse,1,0);card.Controls.Add(row);
        field=box;return card;
    }
    void BuildInterface() {
        SuspendLayout();
        AutoScaleMode=AutoScaleMode.Dpi;
        AutoScaleDimensions=new SizeF(96,96);
        Font=new Font("Bahnschrift",10);
        ClientSize=new Size(1300,900);MinimumSize=new Size(1050,720);
        FormBorderStyle=FormBorderStyle.Sizable;MaximizeBox=true;StartPosition=FormStartPosition.CenterScreen;
        artRail=new GlassPanel{Dock=DockStyle.Left,Width=360,BackColor=Color.Transparent};
        Controls.Add(artRail);
        workspace=new GlassPanel{Dock=DockStyle.Fill,Padding=new Padding(16),BackColor=Color.Transparent};
        Controls.Add(workspace);workspace.BringToFront();
        var stack=new GlassTable{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,BackColor=Color.Transparent,Margin=Padding.Empty};
                var frame=new GlassTable{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,BackColor=Color.Transparent};
        frame.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        frame.RowStyles.Add(new RowStyle(SizeType.Absolute,430));
        frame.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        frame.RowStyles.Add(new RowStyle(SizeType.Absolute,140));
        frame.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        workspace.Controls.Add(frame);
        var settings=new GlassPanel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.Transparent,Margin=Padding.Empty};
        frame.Controls.Add(settings,0,0);
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));settings.Controls.Add(stack);
        var folders=Table(2);folders.Padding=Padding.Empty;folders.Margin=Padding.Empty;folders.BackColor=Color.Transparent;
        folders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));folders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        var inputs=Table(1);inputs.Dock=DockStyle.Top;inputs.Padding=Padding.Empty;inputs.BackColor=Color.Transparent;inputs.Margin=new Padding(0,0,16,0);
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        var outputs=Table(1);outputs.Dock=DockStyle.Top;outputs.Padding=Padding.Empty;outputs.BackColor=Color.Transparent;outputs.Margin=Padding.Empty;
        outputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        folders.Controls.Add(inputs,0,0);folders.Controls.Add(outputs,1,0);
        inputs.Controls.Add(FolderCard("1  Video folder",out video),0,0);
        inputs.Controls.Add(FolderCard("2  Subtitle folder (Default)",out subtitle),0,1);
        outputs.Controls.Add(FolderCard("3  Completed videos",out output),0,0);
        video.TextChanged+=(s,e)=>{ selectedFile="";
            if(String.IsNullOrWhiteSpace(subtitle.Text)||String.Equals(subtitle.Text,previousVideo,StringComparison.OrdinalIgnoreCase))
                subtitle.Text=video.Text;
            previousVideo=video.Text;
        };
        var names=Table(1);names.Dock=DockStyle.Fill;names.Padding=Padding.Empty;names.Margin=new Padding(0,0,0,12);

        namePreview=new InkLabel{Text="Choose a video folder to see its output name.",AutoSize=false,Height=30,Dock=DockStyle.Top,AutoEllipsis=true,Margin=Padding.Empty};
        names.Controls.Add(namePreview);
        var paths=Table(1);paths.Dock=DockStyle.Top;paths.Padding=Padding.Empty;paths.Margin=new Padding(0,0,0,8);
        paths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        paths.Controls.Add(Caption("Anime title (optional)"),0,0);
        title=new TextBox{Dock=DockStyle.Fill,Margin=new Padding(0,5,0,4)};
        paths.Controls.Add(title,0,1);
        title.TextChanged+=(s,e)=>ScheduleName();
        outputs.Controls.Add(paths,0,1);outputs.Controls.Add(names,0,2);
        stack.Controls.Add(folders);
        var options=Table(2);options.Padding=Padding.Empty;options.Margin=new Padding(0,12,0,12);
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        var rfGroup=Table(1);rfGroup.Dock=DockStyle.Fill;rfGroup.Padding=Padding.Empty;rfGroup.Margin=new Padding(0,0,16,0);rfGroup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        var rfLabel=Caption("RF quality: 19.0");rfLabel.AutoSize=false;rfLabel.Width=110;
        quality=new ScarletSlider{Minimum=36,Maximum=44,Value=38,Divisor=2,Dock=DockStyle.Fill,Margin=Padding.Empty,Height=52};
        quality.AccessibleName="RF quality in half steps";
        quality.ValueChanged+=(s,e)=>rfLabel.Text="RF quality: "+(quality.Value/2.0).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture);
        hints.SetToolTip(quality,"18-22 in 0.5 steps. Lower means higher quality and larger files. Default: 19. Use arrow keys for precise steps.");
        rfGroup.Controls.Add(rfLabel);rfGroup.Controls.Add(quality);options.Controls.Add(rfGroup,0,0);
        var audioGroup=Table(1);audioGroup.Dock=DockStyle.Fill;audioGroup.Padding=Padding.Empty;audioGroup.Margin=Padding.Empty;audioGroup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        var audioLabel=Caption("Audio track: 1");audioLabel.AutoSize=false;audioLabel.Width=110;
        audio=new ScarletSlider{Minimum=1,Maximum=8,Value=1,Dock=DockStyle.Fill,Margin=Padding.Empty,Height=52};
        audio.AccessibleName="Audio track";
        audio.ValueChanged+=(s,e)=>audioLabel.Text="Audio track: "+audio.Value;
        hints.SetToolTip(audio,"Tracks 1-8. Choose a track that exists in every selected episode; this is not an automatic track count.");
        audioGroup.Controls.Add(audioLabel);audioGroup.Controls.Add(audio);options.Controls.Add(audioGroup,1,0);        
        stack.Controls.Add(options);
        var actions=Table(9);actions.Padding=Padding.Empty;actions.Margin=new Padding(0,0,0,12);
        for(int i=0;i<9;i++)actions.ColumnStyles.Add(i%2==0?new ColumnStyle(SizeType.Percent,20):new ColumnStyle(SizeType.Absolute,12));
        preview=Action("Preview",(s,e)=>Run(true),160);preview.Margin=new Padding(0,0,10,8);
        start=Action("Start encoding",(s,e)=>Run(false),160);start.Margin=new Padding(0,0,10,8);
        stop=Action("Stop",(s,e)=>StopWorker(),85);stop.Enabled=false;stop.Margin=new Padding(0,0,10,8);
        var open=Action("Open output",(s,e)=>OpenOutput(),140);open.Margin=new Padding(0,0,0,8);
        actions.Controls.AddRange(new Control[]{preview,start,stop,open});
        var exit=Action("Exit",(s,e)=>Close(),85);exit.Margin=new Padding(10,0,0,8);actions.Controls.Add(exit);
        Control[] actionButtons={preview,start,stop,open,exit};actions.Controls.Clear();
        for(int i=0;i<actionButtons.Length;i++){Control button=actionButtons[i];button.Dock=DockStyle.Fill;button.Margin=new Padding(0,0,0,8);actions.Controls.Add(button,i*2,0);}
        exit.Margin=new Padding(0,0,0,8);
        hints.SetToolTip(preview,"Choose an episode, encode its first 10 seconds with subtitles, and watch it in a temporary preview window.");
        frame.Controls.Add(actions,0,1);
        var progress=new GlassTable{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,BackColor=surface,Padding=Padding.Empty,Margin=Padding.Empty};
        progress.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        progress.RowStyles.Add(new RowStyle(SizeType.Absolute,30));
        progress.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        status=new InkLabel{Text="",Dock=DockStyle.Fill,AutoEllipsis=true,Margin=Padding.Empty};
        progress.Controls.Add(status,0,0);
        percent=new InkLabel{Text="0.0%",Visible=false,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Font=new Font("Bahnschrift",36,FontStyle.Bold),BackColor=Color.Transparent};
        progress.Controls.Add(percent,0,1);frame.Controls.Add(progress,0,2);
        bool arranging=false;
        Action arrange=()=>{
            if(arranging)return;
            arranging=true;
            try{
                int available=Math.Max(140,frame.ClientSize.Height-actions.PreferredSize.Height-140);
                float desired=Math.Min(stack.Height+4,available);
                if(Math.Abs(frame.RowStyles[0].Height-desired)>1)frame.RowStyles[0].Height=desired;
            }finally{arranging=false;}
        };
        stack.SizeChanged+=(s,e)=>arrange();
        frame.SizeChanged+=(s,e)=>arrange();
        actions.SizeChanged+=(s,e)=>arrange();
        Shown+=(s,e)=>arrange();
        KeyPreview=true;
        Resize+=(s,e)=>{
            if(artRail!=null)artRail.Width=Math.Max(0,Math.Min(500,ClientSize.Width-950));
            Invalidate();
        };
        ResumeLayout(true);
    }
    void StyleControls(Control parent) {
        foreach(Control c in parent.Controls) {
            c.ForeColor=Color.White;
            if(c is TextBox || c is ComboBox || c is NumericUpDown || c is RichTextBox)
                c.BackColor=Color.FromArgb(196,48,65);
            if(c is TextBox)((TextBox)c).BorderStyle=BorderStyle.FixedSingle;
            if(c is ComboBox)((ComboBox)c).FlatStyle=FlatStyle.Flat;
            if(c is NumericUpDown)((NumericUpDown)c).BorderStyle=BorderStyle.FixedSingle;
            if(c is Label || c is CheckBox)c.BackColor=Color.Transparent;
            if(c is Label && (c.Text=="Wii Anime Encoder" || c.Text=="Prepare your anime for the Wii." ||
                c.Text.StartsWith("640x480 maximum"))) {
                c.BackColor=Color.Transparent;c.Padding=new Padding(4);
            }
            if(c is ScarletButton)c.BackColor=Color.FromArgb(196,48,65);
            if(c.HasChildren && !(c is NumericUpDown))StyleControls(c);
        }
    }
    void ApplyScarletTheme() {
        Text="Wii Anime Encoder | Scarlet Edition";
        Icon=Icon.ExtractAssociatedIcon(Path.Combine(root,"Wii-Anime-Scarlet.exe"));
        DoubleBuffered=true;BackColor=Color.FromArgb(15,10,18);
        string art=Path.Combine(root,"remilia.jpg");
        if(File.Exists(art)){using(var source=Image.FromFile(art))remilia=new Bitmap(source);}
        StyleControls(this);
        ((ScarletButton)start).Primary=true;
        namePreview.ForeColor=Color.FromArgb(255,102,112);

        status.ForeColor=Color.White;status.BackColor=Color.Transparent;
        FormClosed+=(s,e)=>{ClearBackgroundCache();if(remilia!=null)remilia.Dispose();timer.Stop();timer.Dispose();hints.Dispose();};
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData) {
        if(keyData==Keys.F11){ToggleFullscreen();return true;}
        if(keyData==Keys.Escape && fullscreen){ToggleFullscreen();return true;}
        return base.ProcessCmdKey(ref msg,keyData);
    }
    void ToggleFullscreen() {
        SuspendLayout();
        if(!fullscreen) {
            var screen=Screen.FromControl(this).Bounds;
            savedState=WindowState;
            savedBounds=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;
            savedMinimum=MinimumSize;
            fullscreen=true;
            WindowState=FormWindowState.Normal;
            MinimumSize=Size.Empty;
            FormBorderStyle=FormBorderStyle.None;
            Bounds=screen;

        } else {
            fullscreen=false;
            FormBorderStyle=FormBorderStyle.Sizable;
            MinimumSize=savedMinimum;
            WindowState=FormWindowState.Normal;
            Bounds=savedBounds;
            WindowState=savedState;

        }
        ResumeLayout(true);Invalidate();
    }
    protected override void OnPaintBackground(PaintEventArgs e) {
        EnsureBackgroundCache();
        if(backgroundCache!=null)e.Graphics.DrawImageUnscaled(backgroundCache,0,0);
    }
    void RenderBackground(Graphics g) {
        g.Clear(Color.FromArgb(13,9,17));
        if(remilia!=null && ClientSize.Width>0 && ClientSize.Height>0) {
            // Cover the window without distorting the image's original aspect ratio.
            float scale=Math.Max((float)ClientSize.Width/remilia.Width,(float)ClientSize.Height/remilia.Height);
            float w=remilia.Width*scale,h=remilia.Height*scale;
            g.InterpolationMode=interactiveResize?InterpolationMode.Bilinear:InterpolationMode.HighQualityBicubic;
            g.DrawImage(remilia,new RectangleF((ClientSize.Width-w)/2,(ClientSize.Height-h)/2,w,h));
        }
    }
    static string Quote(string v) { return "\"" + v.Replace("\"","\\\"") + "\""; }
    void SavePrefs(){
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(prefs));
            File.WriteAllText(prefs,new JavaScriptSerializer().Serialize(new Dictionary<string,object>{
                {"Video",video.Text},{"Subtitle",subtitle.Text},{"Output",output.Text}
            }));
        }catch{}
    }
    void LoadPrefs(){
        try{
            var d=new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(File.ReadAllText(prefs));
            subtitle.Text=d["Subtitle"];video.Text=d["Video"];output.Text=d["Output"];
        }catch{}
    }
    List<string> convertedFiles=new List<string>();
    string selectedFile="", activeWork="", previewPath="";
    bool previewRun, cancelled, closeWhenFinished;
    int nameGeneration=0;
    DateTime nameDue=DateTime.MaxValue;
    Process nameWorker;
    Label percent;
    string lastError="";
    int skipped=0;
    void ScheduleName() {
        nameGeneration++;nameDue=DateTime.UtcNow.AddMilliseconds(350);
        if(namePreview!=null)namePreview.Text=String.IsNullOrWhiteSpace(title.Text)?"Detecting output name...":title.Text.Trim()+" - EpNN - <resolution>.mp4";
    }
    Dictionary<string,object> Config(bool plan,bool clip,string work) {
        return new Dictionary<string,object>{
            {"Video",video.Text},{"Subtitle",subtitle.Text},{"Output",output.Text},
            {"Title",title.Text},{"Quality",quality.Value/2.0},{"Audio",audio.Value},
            {"First",false},{"Plan",plan},{"Preview",clip},{"Selected",selectedFile},{"Work",work}
        };
    }
    Process MakeWorker(Dictionary<string,object> config,string file) {
        File.WriteAllText(file,new JavaScriptSerializer().Serialize(config));
        return new Process{StartInfo=new ProcessStartInfo("powershell.exe",
            "-NoLogo -NoProfile -ExecutionPolicy Bypass -File "+Quote(Path.Combine(root,"runner.ps1"))+" -Config "+Quote(file)){
            UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}};
    }
    void RefreshName() {
        if(nameDue>DateTime.UtcNow || (nameWorker!=null&&!nameWorker.HasExited))return;
        nameDue=DateTime.MaxValue;
        if(!Directory.Exists(video.Text)){namePreview.Text="Choose a video folder to see its output name.";return;}
        if(nameWorker!=null){nameWorker.Dispose();nameWorker=null;}
        int generation=nameGeneration;
        string configFile=Path.Combine(root,".name-"+Guid.NewGuid().ToString("N")+".json");
        try{
            nameWorker=MakeWorker(Config(true,false,""),configFile);
            nameWorker.OutputDataReceived+=(s,e)=>{if(e.Data!=null)messages.Enqueue("NAMING "+generation+" "+e.Data);};
            nameWorker.ErrorDataReceived+=(s,e)=>{};
            nameWorker.EnableRaisingEvents=true;
            nameWorker.Exited+=(s,e)=>{try{File.Delete(configFile);}catch{}};
            nameWorker.Start();nameWorker.BeginOutputReadLine();nameWorker.BeginErrorReadLine();
        }catch(Exception ex){namePreview.Text="Could not update name: "+ex.Message;try{File.Delete(configFile);}catch{}}
    }
    void Run(bool clip) {
        if(worker!=null)return;
        try{
            if(!Directory.Exists(video.Text)||!Directory.Exists(subtitle.Text))throw new Exception("Select existing video and subtitle folders.");
            if(clip){
                using(var d=new OpenFileDialog()){
                    d.Title="Choose an episode for a 10-second preview";
                    d.InitialDirectory=video.Text;
                    d.Filter="Video files|*.mkv;*.mp4;*.avi;*.m4v;*.mov;*.mpg;*.mpeg;*.wmv;*.ts;*.m2ts";
                    if(d.ShowDialog(this)!=DialogResult.OK)return;
                    selectedFile=d.FileName;
                }
            } else {
                selectedFile="";
                if(String.IsNullOrWhiteSpace(output.Text))throw new Exception("Choose an output folder.");
                string dest=Path.GetFullPath(output.Text).TrimEnd(Path.DirectorySeparatorChar);
                if(String.Equals(dest,Path.GetFullPath(video.Text).TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase)||
                    String.Equals(dest,Path.GetFullPath(subtitle.Text).TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Use an output folder separate from your source folders.");
            }
            BeginEncode(clip);
        }catch(Exception ex){MessageBox.Show(this,ex.Message,"Cannot start",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }
    void BeginEncode(bool clip) {
        if(!File.Exists(Path.Combine(root,"HandBrakeCLI.exe")))throw new Exception("Bundled HandBrakeCLI.exe is missing. Extract all app files.");
        activeWork=Path.Combine(root,".work-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(activeWork);
        previewRun=clip;previewPath="";lastError="";skipped=0;cancelled=false;
        percent.Visible=true;percent.Text="0.0%";status.Text=clip?"Preparing 10-second preview...":"Preparing conversion...";
        SavePrefs();
        string configFile=Path.Combine(activeWork,"job.json");
        worker=MakeWorker(Config(false,clip,activeWork),configFile);
        worker.OutputDataReceived+=(s,e)=>{if(e.Data!=null)messages.Enqueue(e.Data);};
        worker.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)messages.Enqueue("DIAGNOSTIC "+e.Data);};
        try{worker.Start();worker.BeginOutputReadLine();worker.BeginErrorReadLine();}
        catch{worker.Dispose();worker=null;CleanWork();throw;}
        start.Enabled=false;preview.Enabled=false;stop.Enabled=true;
    }
    void CleanWork() {
        if(String.IsNullOrEmpty(activeWork))return;
        // Only this instance's exact generated directory and its direct files.
        string dir=Path.GetFullPath(activeWork);
        if(!String.Equals(Path.GetDirectoryName(dir),Path.GetFullPath(root),StringComparison.OrdinalIgnoreCase)||
            !Path.GetFileName(dir).StartsWith(".work-",StringComparison.Ordinal))return;
        try{
            foreach(string file in Directory.GetFiles(dir))File.Delete(file);
            Directory.Delete(dir,false);activeWork="";
        }catch(Exception ex){status.Text="Temporary cleanup pending: "+ex.Message;}
    }
    void Drain() {
        RefreshName();
        string line;int count=0;
        while(count++<500&&messages.TryDequeue(out line)){
            bool naming=false;
            if(line.StartsWith("NAMING ")){
                int split=line.IndexOf(' ',7);int generation;
                if(split<0||!Int32.TryParse(line.Substring(7,split-7),out generation)||generation!=nameGeneration)continue;
                line=line.Substring(split+1);naming=true;
            }
            if(!line.StartsWith("EVENT ")){if(!naming&&line.StartsWith("DIAGNOSTIC "))lastError=line.Substring(11);continue;}
            try{
                var evt=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(line.Substring(6));
                string type=(string)evt["Type"];object data=evt["Data"];
                if(naming){if(type=="NAME"||type=="ERROR")namePreview.Text=Convert.ToString(data);continue;}
                if(type=="NAME")namePreview.Text=Convert.ToString(data);
                if(type=="PROGRESS")percent.Text=Convert.ToDouble(data,System.Globalization.CultureInfo.InvariantCulture).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+"%";
                if(type=="JOB"){
                    var job=(Dictionary<string,object>)data;
                    status.Text=(previewRun?"Preview":"Episode")+" "+job["Index"]+"/"+job["Total"]+" - "+job["Name"];
                    percent.Text="0.0%";
                }
                if(type=="ERROR")lastError=Convert.ToString(data);
                if(type=="SKIP"){skipped++;lastError=Convert.ToString(data);}
                if(type=="CLIP")previewPath=Convert.ToString(data);
                if(type=="SAVED"){string path=Convert.ToString(data);if(!convertedFiles.Contains(path))convertedFiles.Add(path);}
            }catch(Exception ex){lastError="Could not read encoder status: "+ex.Message;}
        }
        if(worker!=null&&worker.HasExited&&messages.IsEmpty){
            worker.WaitForExit();
            if(!messages.IsEmpty)return;
            int exit=worker.ExitCode;worker.Dispose();worker=null;
            start.Enabled=true;preview.Enabled=true;stop.Enabled=false;
            if(cancelled)status.Text="Cancelled.";
            else if(exit!=0){status.Text="Conversion failed.";MessageBox.Show(this,String.IsNullOrEmpty(lastError)?"HandBrake could not finish this conversion.":lastError,"Conversion failed",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            else{
                percent.Text="100.0%";
                status.Text=previewRun?"Preview ready.":"Conversion complete."+(skipped>0?" Skipped: "+skipped+".":"");
                if(skipped>0)MessageBox.Show(this,skipped+" file(s) skipped. Last issue: "+lastError,"Skipped files");
                if(previewRun&&File.Exists(previewPath)){
                    using(var player=new ClipPlayer(previewPath))player.ShowDialog(this);
                    status.Text="Preview closed.";
                }
            }
            CleanWork();if(closeWhenFinished){BeginInvoke(new Action(()=>Close()));return;}
        }
        if(worker==null&&!String.IsNullOrEmpty(activeWork))CleanWork();
    }
    List<string> AvailableOutputs(){
        var files=new List<string>();
        foreach(string file in convertedFiles)if(File.Exists(file))files.Add(file);
        return files;
    }
    void OpenOutput(){
        var files=AvailableOutputs();
        if(files.Count==0){MessageBox.Show(this,"No completed videos from this session are available yet.","Open output");return;}
        string selected=files[0];
        if(files.Count>1){
            using(var dialog=new Form{Text="Choose a converted video",Size=new Size(740,420),MinimumSize=new Size(480,300),StartPosition=FormStartPosition.CenterParent,BackColor=Color.FromArgb(196,48,65)}){
                var list=new ListBox{Dock=DockStyle.Fill,BackColor=Color.FromArgb(196,48,65),ForeColor=Color.White,Font=new Font("Bahnschrift",11),HorizontalScrollbar=true,IntegralHeight=false};
                foreach(string file in files)list.Items.Add(Path.GetFileName(file));
                list.SelectedIndex=0;
                var row=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=48};
                var open=new ScarletButton{Text="Open video",Size=new Size(125,36),DialogResult=DialogResult.OK};
                var cancel=new ScarletButton{Text="Cancel",Size=new Size(100,36),DialogResult=DialogResult.Cancel};
                row.Controls.Add(open);row.Controls.Add(cancel);dialog.Controls.Add(list);dialog.Controls.Add(row);
                dialog.AcceptButton=open;dialog.CancelButton=cancel;
                list.DoubleClick+=(s,e)=>{if(list.SelectedIndex>=0){dialog.DialogResult=DialogResult.OK;dialog.Close();}};
                if(dialog.ShowDialog(this)!=DialogResult.OK||list.SelectedIndex<0)return;
                selected=files[list.SelectedIndex];
            }
        }
        try{Process.Start(new ProcessStartInfo(selected){UseShellExecute=true});}
        catch(Exception ex){MessageBox.Show(this,"Could not open the video: "+ex.Message,"Open output");}
    }

    void StopWorker(){
        if(worker==null||worker.HasExited||cancelled)return;
        cancelled=true;
        int processId=worker.Id;
        status.Text="Cancelling...";
        System.Threading.ThreadPool.QueueUserWorkItem(delegate(object state){
            try{
                using(var p=Process.Start(new ProcessStartInfo("taskkill.exe","/PID "+processId+" /T /F"){UseShellExecute=false,CreateNoWindow=true})){
                    if(!p.WaitForExit(10000))messages.Enqueue("DIAGNOSTIC Cancellation is taking longer than expected.");
                }
            }catch(Exception ex){messages.Enqueue("DIAGNOSTIC "+ex.Message);}
        });
    }
}
public class ScarletButton : Button {
    public bool Primary=false;
    bool hover=false;
    public ScarletButton() {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        Cursor=Cursors.Hand; FlatStyle=FlatStyle.Flat; FlatAppearance.BorderSize=0;
    }
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e) {
        var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var path=new GraphicsPath()) {
            path.AddPolygon(new Point[]{new Point(8,1),new Point(Width-2,1),new Point(Width-2,Height-9),
                new Point(Width-10,Height-2),new Point(1,Height-2),new Point(1,8)});
            Color top=Primary?Color.FromArgb(220,35,64):Color.FromArgb(197,35,55);
            Color bottom=Primary?Color.FromArgb(154,13,37):Color.FromArgb(142,18,35);
            if(hover&&Enabled){top=Color.FromArgb(240,51,73);bottom=Color.FromArgb(186,19,46);}
            if(!Enabled){top=Color.FromArgb(95,43,49);bottom=Color.FromArgb(63,22,30);}
            using(var b=new LinearGradientBrush(ClientRectangle,top,bottom,90f))g.FillPath(b,path);
            using(var pen=new Pen(Enabled?Color.FromArgb(92,0,19):Color.FromArgb(69,47,62)))g.DrawPath(pen,path);
            InkText.Draw(g,Text,Font,ClientRectangle,Enabled?Color.White:Color.FromArgb(170,150,155),
                TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(6,5,Width-12,Height-10),ForeColor,bottom);
        }
    }
}
public class GlassPanel : Panel {
    protected override void OnPaintBackground(PaintEventArgs e) {
        var app=FindForm() as EncoderApp;
        if(app!=null)app.PaintSurface(this,e);else base.OnPaintBackground(e);
    }
    public GlassPanel(){SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
}
public class GlassTable : TableLayoutPanel {
    protected override void OnPaintBackground(PaintEventArgs e) {
        var app=FindForm() as EncoderApp;
        if(app!=null)app.PaintSurface(this,e);else base.OnPaintBackground(e);
    }
    public GlassTable(){SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
}
public class GlassFlow : FlowLayoutPanel {
    protected override void OnPaintBackground(PaintEventArgs e) {
        var app=FindForm() as EncoderApp;
        if(app!=null)app.PaintSurface(this,e);else base.OnPaintBackground(e);
    }
    public GlassFlow(){SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
}
public class ScarletSlider : Control {
    public int Minimum=1, Maximum=8, Divisor=1;
    int current=1;
    public int Value {get{return current;}set{int next=Math.Max(Minimum,Math.Min(Maximum,value));if(next==current)return;current=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
    public event EventHandler ValueChanged;
    public ScarletSlider(){
        SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.Selectable|ControlStyles.SupportsTransparentBackColor,true);
        BackColor=Color.Transparent;TabStop=true;Cursor=Cursors.Hand;
    }
    protected override void OnPaintBackground(PaintEventArgs e){var app=FindForm() as EncoderApp;if(app!=null)app.PaintSurface(this,e);else base.OnPaintBackground(e);}
    int X(int v){return 12+(int)((Width-24)*(v-Minimum)/(double)Math.Max(1,Maximum-Minimum));}
    void Pick(int x){Value=Minimum+(int)Math.Round(Math.Max(0,Math.Min(1,(x-12)/(double)Math.Max(1,Width-24)))*(Maximum-Minimum));}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();Capture=true;Pick(e.X);}}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture)Pick(e.X);}
    protected override void OnMouseUp(MouseEventArgs e){Capture=false;base.OnMouseUp(e);}
    protected override bool IsInputKey(Keys k){return k==Keys.Left||k==Keys.Right||k==Keys.Up||k==Keys.Down||base.IsInputKey(k);}
    protected override void OnKeyDown(KeyEventArgs e){
        if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down){Value--;e.Handled=true;}
        if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up){Value++;e.Handled=true;}
        if(e.KeyCode==Keys.Home){Value=Minimum;e.Handled=true;}
        if(e.KeyCode==Keys.End){Value=Maximum;e.Handled=true;}
        base.OnKeyDown(e);
    }
    protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}
    protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var line=new Pen(Color.FromArgb(49,5,14),4))g.DrawLine(line,12,15,Width-12,15);
        using(var fill=new Pen(Color.FromArgb(190,13,43),4))g.DrawLine(fill,12,15,X(Value),15);
        using(var tick=new SolidBrush(Color.FromArgb(185,25,48)))
        using(var font=new Font("Bahnschrift",8)){
            for(int v=Minimum;v<=Maximum;v++){
                int x=X(v);g.FillEllipse(tick,x-2,13,4,4);
                if(Divisor==1||v%Divisor==0)InkText.Draw(g,(v/(double)Divisor).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture),font,new Rectangle(x-14,28,28,20),ForeColor,TextFormatFlags.HorizontalCenter);
            }
        }
        using(var brush=new SolidBrush(Color.FromArgb(219,24,51)))g.FillEllipse(brush,X(Value)-6,9,12,12);
        using(var pen=new Pen(Color.FromArgb(37,0,8),2))g.DrawEllipse(pen,X(Value)-6,9,12,12);
        if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(1,1,Width-2,Height-2));
    }
}public class ClipPlayer : Form {
    System.Windows.Controls.MediaElement media;
    System.Windows.Forms.Integration.ElementHost host;
    bool paused=false;
    public ClipPlayer(string file){
        Text="Preview";
        BackColor=Color.Black;FormBorderStyle=FormBorderStyle.None;
        StartPosition=FormStartPosition.Manual;KeyPreview=true;ShowInTaskbar=false;
        media=new System.Windows.Controls.MediaElement{
            LoadedBehavior=System.Windows.Controls.MediaState.Manual,
            UnloadedBehavior=System.Windows.Controls.MediaState.Manual,
            Stretch=System.Windows.Media.Stretch.Uniform,Volume=0.7
        };
        host=new System.Windows.Forms.Integration.ElementHost{Dock=DockStyle.Fill,Child=media,BackColor=Color.Black};
        Controls.Add(host);
        media.MediaFailed+=(s,e)=>{MessageBox.Show(this,"Windows could not play the preview: "+e.ErrorException.Message,"Preview playback");Close();};
        media.KeyDown+=(s,e)=>{
            if(e.Key==System.Windows.Input.Key.Escape){e.Handled=true;Close();}
            else if(e.Key==System.Windows.Input.Key.Space){e.Handled=true;TogglePause();}
            else if(e.Key==System.Windows.Input.Key.R){e.Handled=true;Replay();}
        };
        Shown+=(s,e)=>{Bounds=Screen.FromControl(Owner??this).Bounds;media.Source=new Uri(file);media.Play();};
    }
    void TogglePause(){paused=!paused;if(paused)media.Pause();else media.Play();}
    void Replay(){paused=false;media.Position=TimeSpan.Zero;media.Play();}
    protected override bool ProcessCmdKey(ref Message msg,Keys key){
        if(key==Keys.Escape){Close();return true;}
        if(key==Keys.Space){TogglePause();return true;}
        if(key==Keys.R){Replay();return true;}
        return base.ProcessCmdKey(ref msg,key);
    }
    protected override void OnFormClosing(FormClosingEventArgs e){media.Stop();media.Close();media.Source=null;host.Child=null;base.OnFormClosing(e);}
}

public static class InkText {
    public static void Draw(Graphics g,string text,Font font,Rectangle rect,Color color,TextFormatFlags flags){
        foreach(Point offset in new Point[]{new Point(-1,-1),new Point(0,-1),new Point(1,-1),new Point(-1,0),new Point(1,0),new Point(-1,1),new Point(0,1),new Point(1,1)}){
            Rectangle edge=rect;edge.Offset(offset);
            TextRenderer.DrawText(g,text,font,edge,Color.Black,flags);
        }
        TextRenderer.DrawText(g,text,font,rect,color,flags);
    }
}
public class InkLabel : Label {
    public InkLabel(){SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
    public override Size GetPreferredSize(Size proposedSize){
        int width=MaximumSize.Width>0?Math.Max(1,MaximumSize.Width-Padding.Horizontal-4):32767;
        Size measured=TextRenderer.MeasureText(Text,Font,new Size(width,32767),TextFormatFlags.NoPrefix|TextFormatFlags.WordBreak);
        return new Size(measured.Width+Padding.Horizontal+4,measured.Height+Padding.Vertical+2);
    }
    protected override void OnPaint(PaintEventArgs e){
        Rectangle rect=new Rectangle(Padding.Left,Padding.Top,Math.Max(1,Width-Padding.Horizontal),Math.Max(1,Height-Padding.Vertical));
        TextFormatFlags flags=TextFormatFlags.NoPrefix|TextFormatFlags.WordBreak;
        if(AutoEllipsis)flags=TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis;
        if(TextAlign==ContentAlignment.MiddleCenter)flags|=TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter;
        InkText.Draw(e.Graphics,Text,Font,rect,ForeColor,flags);
    }
}