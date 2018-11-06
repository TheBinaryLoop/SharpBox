using System;
using System.Runtime.InteropServices;

namespace SharpBox.Remote.PInvoke.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }
}
