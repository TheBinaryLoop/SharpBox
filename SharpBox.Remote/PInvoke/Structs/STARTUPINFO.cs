using System;
using System.Runtime.InteropServices;

namespace SharpBox.Remote.PInvoke.Structs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFO
    {
        UInt32 cb;
        String lpReserved;
        String lpDesktop;
        String lpTitle;
        UInt32 dwX;
        UInt32 dwY;
        UInt32 dwXSize;
        UInt32 dwYSize;
        UInt32 dwXCountChars;
        UInt32 dwYCountChars;
        UInt32 dwFillAttribute;
        UInt32 dwFlags;
        UInt16 wShowWindow;
        UInt16 cbReserved2;
        Byte lpReserved2;
        IntPtr hStdInput;
        IntPtr hStdOutput;
        IntPtr hStdError;
    }
}
