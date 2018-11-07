using System;
using System.Runtime.InteropServices;

namespace SharpBox.Remote.PInvoke.Librarys
{
    public static class Shlwapi
    {
        /// <summary>
        /// Determines whether a path to a file system object such as a file or directory is valid.
        /// </summary>
        /// <param name="pszPath">A pointer to a null-terminated string of maximum length MAX_PATH that contains the full path of the object to verify.</param>
        /// <returns>Returns TRUE if the file exists, or FALSE otherwise. Call GetLastError for extended error information.</returns>
        [DllImport("shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PathFileExists([MarshalAs(UnmanagedType.LPTStr)]String pszPath);
    }
}
