using System;
using System.Runtime.InteropServices;

namespace SharpBox.Remote.PInvoke.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UNICODE_STRING : IDisposable
    {
        public UInt16 Length;
        public UInt16 MaximumLength;
        private IntPtr buffer;

        public UNICODE_STRING(String s)
        {
            Length = (UInt16)(s.Length * 2);
            MaximumLength = (UInt16)(Length + 2);
            buffer = Marshal.StringToHGlobalUni(s);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(buffer);
            buffer = IntPtr.Zero;
        }

        public override String ToString()
        {
            return Marshal.PtrToStringUni(buffer);
        }
    }
}
