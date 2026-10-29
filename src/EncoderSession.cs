using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace WiiAnimeEncoder
{
    // Used only by the isolated PowerShell worker. HandBrake's Windows CLI reads
    // p/r/q from CONIN$, not a redirected stdin pipe. Give each encoder a private
    // hidden console; never inject keys into the user's terminal or desktop.
    public sealed class EncoderSession : IDisposable
    {
        IntPtr process, input = new IntPtr(-1);
        Thread reader;
        readonly ConcurrentQueue<string> lines = new ConcurrentQueue<string>();
        volatile bool eof;
        bool attached;
        IntPtr originalInput, originalOutput, originalError;
        public bool OutputEnded { get { return eof; } }
        public bool HasExited { get { return process == IntPtr.Zero || WaitForSingleObject(process, 0) == 0; } }
        public int ExitCode { get { uint code; if (!GetExitCodeProcess(process, out code)) throw Error(); return (int)code; } }
        public string ReadLine() { string line; return lines.TryDequeue(out line) ? line : null; }
        static Exception Error() { return new Win32Exception(Marshal.GetLastWin32Error()); }
        public EncoderSession(string executable, string[] arguments)
        {
            originalInput = GetStdHandle(-10); originalOutput = GetStdHandle(-11); originalError = GetStdHandle(-12);
            IntPtr read = IntPtr.Zero, write = IntPtr.Zero;
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            try
            {
                var sa = new SECURITY_ATTRIBUTES { Length = Marshal.SizeOf(typeof(SECURITY_ATTRIBUTES)), Inherit = true };
                if (!CreatePipe(out read, out write, ref sa, 0) || !SetHandleInformation(read, 1, 0)) throw Error();
                var si = new STARTUPINFO { Size = Marshal.SizeOf(typeof(STARTUPINFO)), Flags = 0x101, ShowWindow = 0, StdOutput = write, StdError = write };
                var command = new StringBuilder(Quote(executable));
                foreach (string argument in arguments) command.Append(' ').Append(Quote(argument));
                if (!CreateProcess(executable, command, IntPtr.Zero, IntPtr.Zero, true, 0x10, IntPtr.Zero, null, ref si, out pi)) throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateProcess failed");
                process = pi.Process;
                CloseHandle(pi.Thread); pi.Thread = IntPtr.Zero;
                CloseHandle(write); write = IntPtr.Zero;
                // This class must be called from the dedicated no-window worker.
                // Refuse to detach a console that could belong to a caller.
                if (GetConsoleWindow() != IntPtr.Zero) throw new InvalidOperationException("Encoder control requires an isolated no-console worker.");
                // CREATE_NO_WINDOW can still leave an invisible console association.
                // The worker is disposable and owns no user-facing console.
                FreeConsole();
                // The console host may initialize after CreateProcess returns.
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    if (AttachConsole(pi.ProcessId)) { attached = true; break; }
                    int error = Marshal.GetLastWin32Error();
                    if (error != 6 || HasExited) throw new Win32Exception(error, "Could not attach the private encoder console.");
                    Thread.Sleep(20);
                }
                if (!attached) throw new IOException("Timed out waiting for the private encoder console.");
                input = CreateFile("CONIN$", 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
                if (input == new IntPtr(-1)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Opening private console input failed");
                RestoreWorkerHandles();
                var stream = new FileStream(new SafeFileHandle(read, true), FileAccess.Read);
                read = IntPtr.Zero;
                reader = new Thread(() => {
                    try { using (var text = new StreamReader(stream, Encoding.UTF8)) { string line; while ((line = text.ReadLine()) != null) lines.Enqueue(line); } }
                    catch (IOException ex) { lines.Enqueue("Encoder output error: " + ex.Message); }
                    finally { eof = true; }
                });
                reader.IsBackground = true;
                reader.Start();
            }
            catch { Dispose(); throw; }
            finally { if (read != IntPtr.Zero) CloseHandle(read); if (write != IntPtr.Zero) CloseHandle(write); }
        }
        public void Send(char command)
        {
            if (command != 'p' && command != 'r' && command != 'q') throw new ArgumentException("Unsupported encoder command.");
            if (HasExited) return;
            var records = new INPUT_RECORD[2];
            records[0] = new INPUT_RECORD { Type = 1, Down = 1, Repeat = 1, Key = (ushort)Char.ToUpperInvariant(command), Character = command };
            records[1] = records[0]; records[1].Down = 0;
            uint written;
            if (!WriteConsoleInput(input, records, 2, out written) || written != 2) throw Error();
        }
        public void Dispose()
        {
            if (process != IntPtr.Zero)
            {
                if (!HasExited) { try { Send('r'); Send('q'); } catch { } if (WaitForSingleObject(process, 3000) != 0) { TerminateProcess(process, 1); WaitForSingleObject(process, 3000); } }
                CloseHandle(process); process = IntPtr.Zero;
            }
            if (input != new IntPtr(-1)) { CloseHandle(input); input = new IntPtr(-1); }
            if (attached) { FreeConsole(); attached = false; }
            RestoreWorkerHandles();
            if (reader != null) { reader.Join(3000); reader = null; }
        }
        void RestoreWorkerHandles()
        {
            SetStdHandle(-10, originalInput); SetStdHandle(-11, originalOutput); SetStdHandle(-12, originalError);
        }
        public static string Quote(string value)
        {
            var result = new StringBuilder("\""); int slashes = 0;
            foreach (char c in value) { if (c == '\\') { slashes++; continue; } result.Append('\\', c == '"' ? slashes * 2 + 1 : slashes); result.Append(c); slashes = 0; }
            return result.Append('\\', slashes * 2).Append('"').ToString();
        }
        [StructLayout(LayoutKind.Sequential)] struct SECURITY_ATTRIBUTES { public int Length; public IntPtr Descriptor; [MarshalAs(UnmanagedType.Bool)] public bool Inherit; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] struct STARTUPINFO { public int Size; public string Reserved, Desktop, Title; public int X,Y,XSize,YSize,XChars,YChars,Fill,Flags; public short ShowWindow, ReservedSize; public IntPtr ReservedPtr,StdInput,StdOutput,StdError; }
        [StructLayout(LayoutKind.Sequential)] struct PROCESS_INFORMATION { public IntPtr Process,Thread; public uint ProcessId,ThreadId; }
        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode, Size = 20)] struct INPUT_RECORD { [FieldOffset(0)] public ushort Type; [FieldOffset(4)] public int Down; [FieldOffset(8)] public ushort Repeat; [FieldOffset(10)] public ushort Key; [FieldOffset(12)] public ushort Scan; [FieldOffset(14)] public char Character; [FieldOffset(16)] public uint State; }
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool CreatePipe(out IntPtr read,out IntPtr write,ref SECURITY_ATTRIBUTES attributes,int size);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool SetHandleInformation(IntPtr handle,uint mask,uint flags);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool CreateProcess(string app,StringBuilder command,IntPtr processAttributes,IntPtr threadAttributes,bool inherit,uint flags,IntPtr environment,string directory,ref STARTUPINFO startup,out PROCESS_INFORMATION processInfo);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr handle,uint timeout);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetExitCodeProcess(IntPtr handle,out uint code);
        [DllImport("kernel32.dll")] static extern bool TerminateProcess(IntPtr handle,uint code);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool AttachConsole(uint processId);
        [DllImport("kernel32.dll")] static extern bool FreeConsole();
        [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
        [DllImport("kernel32.dll")] static extern IntPtr GetStdHandle(int which);
        [DllImport("kernel32.dll")] static extern bool SetStdHandle(int which, IntPtr handle);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr CreateFile(string name,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool WriteConsoleInput(IntPtr input,INPUT_RECORD[] records,uint count,out uint written);
    }
}
