using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SharpBox.Remote.PInvoke.Enums;
using SharpBox.Remote.PInvoke.Structs;

namespace SharpBox.Remote.PInvoke.Librarys
{
    public static class Kernel32
    {

        //[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        //public static extern IntPtr CreateFile(
        //    [MarshalAs(UnmanagedType.LPTStr)] String filename,
        //    [MarshalAs(UnmanagedType.U4)] Enums.FileAccess access,
        //    [MarshalAs(UnmanagedType.U4)] FileShare share,
        //    IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
        //    [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
        //    [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
        //    IntPtr templateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr CreateFileA(
             [MarshalAs(UnmanagedType.LPStr)] String filename,
             [MarshalAs(UnmanagedType.U4)] Enums.FileAccess access,
             [MarshalAs(UnmanagedType.U4)] FileShare share,
             IntPtr securityAttributes,
             [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
             [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
             IntPtr templateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateFileW(
             [MarshalAs(UnmanagedType.LPWStr)] String filename,
             [MarshalAs(UnmanagedType.U4)] Enums.FileAccess access,
             [MarshalAs(UnmanagedType.U4)] FileShare share,
             IntPtr securityAttributes,
             [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
             [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
             IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern Boolean CreateProcess(
            String lpApplicationName,
            String lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            Boolean bInheritHandles,
            UInt32 dwCreationFlags,
            IntPtr lpEnvironment,
            String lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean DeleteFile(String lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean ReadFile(
            IntPtr hFile,
            IntPtr lpBuffer,
            UInt32 nNumberOfBytesToRead,
            out UInt32 lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern Boolean ReadFileEx(
            IntPtr hFile,
            out Byte[] lpBuffer,
            UInt32 nNumberOfBytesToRead,
            [In] ref NativeOverlapped lpOverlapped,
            IOCompletionCallback lpCompletionRoutine);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean WriteFile(
            IntPtr hFile,
            IntPtr lpBuffer,
            UInt32 nNumberOfBytesToWrite,
            out UInt32 lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr FindFirstFileA(
            String lpFileName,
            out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFileW(
            String lpFileName,
            out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr FindFirstFileExA(
            String lpFileName,
            FINDEX_INFO_LEVELS fInfoLevelId,
            out WIN32_FIND_DATA lpFindFileData,
            FINDEX_SEARCH_OPS fSearchOp,
            IntPtr lpSearchFilter,
            Int32 dwAdditionalFlags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFileExW(
            String lpFileName,
            FINDEX_INFO_LEVELS fInfoLevelId,
            out WIN32_FIND_DATA lpFindFileData,
            FINDEX_SEARCH_OPS fSearchOp,
            IntPtr lpSearchFilter,
            Int32 dwAdditionalFlags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern Boolean FindNextFile(
            IntPtr hFindFile,
            out WIN32_FIND_DATA lpFindFileData);

        /// <summary>
        /// P/Invoke to determine the filename from a file handle
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa364962(v=vs.85).aspx
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpszFilePath"></param>
        /// <param name="cchFilePath"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern UInt32 GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, UInt32 cchFilePath, UInt32 dwFlags);

    }
}
