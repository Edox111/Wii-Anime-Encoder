param([Parameter(Mandatory=$true)][string]$Path)
$ErrorActionPreference='Stop'
# Generated moving color pattern plus PCM tone: no external media or codecs.
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Text;
public static class PauseFixture {
    static void Four(BinaryWriter w,string s) { w.Write(Encoding.ASCII.GetBytes(s)); }
    static long Chunk(BinaryWriter w,string s) { Four(w,s); w.Write(0); return w.BaseStream.Position; }
    static void End(BinaryWriter w,long start) { long end=w.BaseStream.Position; w.BaseStream.Position=start-4; w.Write((int)(end-start)); w.BaseStream.Position=end; if ((end&1)!=0) w.Write((byte)0); }
    public static void Write(string path) {
        const int width=320,height=240,fps=15,frames=1800,samples=48000/fps;
        using(var w=new BinaryWriter(File.Create(path))) {
            long riff=Chunk(w,"RIFF"); Four(w,"AVI ");
            long hdrl=Chunk(w,"LIST"); Four(w,"hdrl");
            long avih=Chunk(w,"avih"); w.Write(1000000/fps); w.Write(width*height*3*fps+96000); w.Write(0); w.Write(0); w.Write(frames); w.Write(0); w.Write(2); w.Write(width*height*3); w.Write(width); w.Write(height); for(int i=0;i<4;i++)w.Write(0); End(w,avih);
            long video=Chunk(w,"LIST"); Four(w,"strl");
            long sh=Chunk(w,"strh"); Four(w,"vids"); Four(w,"DIB "); w.Write(0); w.Write((short)0); w.Write((short)0); w.Write(0); w.Write(1); w.Write(fps); w.Write(0); w.Write(frames); w.Write(width*height*3); w.Write(-1); w.Write(0); w.Write((short)0); w.Write((short)0); w.Write((short)width); w.Write((short)height); End(w,sh);
            long sf=Chunk(w,"strf"); w.Write(40); w.Write(width); w.Write(height); w.Write((short)1); w.Write((short)24); w.Write(0); w.Write(width*height*3); for(int i=0;i<4;i++)w.Write(0); End(w,sf); End(w,video);
            long audio=Chunk(w,"LIST"); Four(w,"strl");
            sh=Chunk(w,"strh"); Four(w,"auds"); w.Write(0); w.Write(0); w.Write((short)0); w.Write((short)0); w.Write(0); w.Write(2); w.Write(96000); w.Write(0); w.Write(samples*frames); w.Write(samples*2); w.Write(-1); w.Write(2); for(int i=0;i<4;i++)w.Write((short)0); End(w,sh);
            sf=Chunk(w,"strf"); w.Write((short)1); w.Write((short)1); w.Write(48000); w.Write(96000); w.Write((short)2); w.Write((short)16); End(w,sf); End(w,audio); End(w,hdrl);
            long movi=Chunk(w,"LIST"); Four(w,"movi"); byte[] frame=new byte[width*height*3];
            for(int f=0;f<frames;f++) {
                for(int y=0;y<height;y++)for(int x=0;x<width;x++) { int p=(y*width+x)*3; frame[p]=(byte)(x+f*3); frame[p+1]=(byte)(y+f); frame[p+2]=(byte)(((x+f)/16+(y/16))%2*200); }
                long c=Chunk(w,"00db"); w.Write(frame); End(w,c);
                c=Chunk(w,"01wb"); for(int n=0;n<samples;n++)w.Write((short)(Math.Sin(2*Math.PI*440*(f*samples+n)/48000.0)*4000)); End(w,c);
            }
            End(w,movi); End(w,riff);
        }
    }
}
'@
[PauseFixture]::Write([IO.Path]::GetFullPath($Path))
"Created synthetic 120-second AVI with video and audio: $Path"
