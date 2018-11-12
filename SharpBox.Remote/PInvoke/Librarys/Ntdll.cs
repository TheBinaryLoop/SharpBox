using System;
using System.Runtime.InteropServices;
using SharpBox.Remote.PInvoke.Enums;

namespace SharpBox.Remote.PInvoke.Librarys
{
    public static class Ntdll
    {
        /// <summary>Retrieves the specified system information.</summary>
        /// <param name="InfoClass">indicate the kind of system information to be retrieved</param>
        /// <param name="Info">a buffer that receives the requested information</param>
        /// <param name="Size">The allocation size of the buffer pointed to by Info</param>
        /// <param name="Length">If null, ignored.  Otherwise tells you the size of the information returned by the kernel.</param>
        /// <returns>Status Information</returns>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms724509%28v=vs.85%29.aspx
        [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern NTSTATUS NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS InfoClass, IntPtr Info, UInt32 Size, out UInt32 Length);
    }
}
