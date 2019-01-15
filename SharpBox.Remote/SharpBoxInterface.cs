using System;
using System.Diagnostics;
using System.IO;
using EasyHook;
using SharpBox.Remote.PInvoke.Structs;

namespace SharpBox.Remote
{
    /// <summary>
    /// Provides an interface for communicating from the client (target) to the server (injector)
    /// </summary>
    public class SharpBoxInterface : MarshalByRefObject
    {
        public String Executable = "";
        public String ChannelName = null;
        public String InjectionLibrary_x86 = null;
        public String InjectionLibrary_x64 = null;
        public readonly String SandboxRootPath = @"D:\SharpBox";
        public String SandboxName = "DefaultBox";

        public void IsInstalled(Int32 InClientPID)
        {
            Console.WriteLine($"SharpBox has injected SharpBox.Remote into process {InClientPID}.\r\n");
        }

        /// <summary>
        /// Output the message to the console.
        /// </summary>
        /// <param name="fileNames"></param>
        public void ReportMessages(string[] messages)
        {
            foreach (string message in messages)
            {
                //if (!message.Contains("SETWINDOWTEXT")) continue;
                Console.WriteLine(message);
            }
        }

        public void ReportMessage(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Report exception
        /// </summary>
        /// <param name="e"></param>
        public void ReportException(Exception e)
        {
            ConsoleColor consoleColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"The target process has reported an error:\r\n{e.ToString()}");
            Console.ForegroundColor = consoleColor;
        }

        int count = 0;
        /// <summary>
        /// Called to confirm that the IPC channel is still open / host application has not closed
        /// </summary>
        public void Ping()
        {
            //return;
            // Output token animation to visualise Ping
            var oldTop = Console.CursorTop;
            var oldLeft = Console.CursorLeft;
            Console.CursorVisible = false;

            var chars = "\\|/-";
            //Console.SetCursorPosition(Console.WindowWidth - 1, oldTop - 1);
            Console.SetCursorPosition(0, oldTop);
            Console.Write(chars[count++ % chars.Length]);

            Console.SetCursorPosition(oldLeft, oldTop);
            Console.CursorVisible = true;
        }

        #region FileSystem

        public bool ShouldBlock(String filename, UInt32 creationDisposition)
        {
            string validPath = Path.GetFullPath(filename.StartsWith(@"\\?\") ? filename.Remove(0, 4) : filename);
            if (validPath.EndsWith(".txt") || validPath.EndsWith(".ion")) return true;
            return false;
        }

        public bool ProcessCreateFileRequest(String filename, PInvoke.Enums.FileAccess access, FileMode creationDisposition, out String redirectedFilename)
        {
            bool redirect = false;
            string prefix = string.Empty;
            string realFilename = string.Empty;
            redirectedFilename = string.Empty;
            string redirectedRealFilename = string.Empty;

            // Check if pipe
            if (filename.StartsWith(@"\\.\pipe\")) return false;

            // Check FileAccess
            if (access == PInvoke.Enums.FileAccess.GenericWrite || access == PInvoke.Enums.FileAccess.GenericAll) redirect = true;

            // Check FileMode
            //if (creationDisposition == FileMode.Append || creationDisposition == FileMode.Create || creationDisposition == FileMode.CreateNew || creationDisposition == FileMode.OpenOrCreate || creationDisposition == FileMode.Truncate)
            if (creationDisposition != FileMode.Open) redirect = true; // FileMode.OpenOrCreate needs further checking

            // Check if we need a special prefix
            if (filename.StartsWith(@"\\?\"))
            {
                prefix = @"\\?\";
                realFilename = filename.Substring(4);
            }
            else
            {
                realFilename = filename;
            }
            try
            {
                redirectedRealFilename = Path.Combine(SandboxRootPath, SandboxName, "Drives", realFilename.Replace(":", ""));
                redirectedFilename = prefix + redirectedRealFilename;

                if (File.Exists(redirectedRealFilename)) redirect = true;

                if (redirect)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(redirectedRealFilename));
                    if (File.Exists(realFilename) && !File.Exists(redirectedRealFilename))
                    {
                        File.Copy(realFilename, redirectedRealFilename);
                    }
                    if (File.Exists(redirectedRealFilename + ".sbdeleted")) File.Delete(redirectedRealFilename + ".sbdeleted");
                    //else if (File.Exists(realFilename) && File.Exists(redirectedRealFilename))
                    //{
                    //    File.Copy(realFilename, redirectedRealFilename, File.GetLastWriteTimeUtc(realFilename) > File.GetLastWriteTimeUtc(redirectedRealFilename));
                    //}

                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
                return false;
            }
            return redirect;
        }

        public bool PathFileExists(String pszPath, out String redirectedFilename, out Boolean isDeleted)
        {
            bool redirect = false;
            isDeleted = false;
            string prefix = string.Empty;
            string realFilename = string.Empty;
            redirectedFilename = string.Empty;
            string redirectedRealFilename = string.Empty;

            if (pszPath.StartsWith(@"\\?\"))
            {
                prefix = @"\\?\";
                realFilename = pszPath.Substring(4);
            }
            else
            {
                realFilename = pszPath;
            }

            try
            {
                redirectedRealFilename = Path.Combine(SandboxRootPath, SandboxName, "Drives", realFilename.Replace(":", ""));
                redirectedFilename = prefix + redirectedRealFilename;

                if (File.Exists(redirectedRealFilename)) redirect = true;
                if (File.Exists(redirectedRealFilename + ".sbdeleted")) isDeleted = true;
            }
            catch (Exception ex)
            {
                ReportException(ex);
                return false;
            }
            return redirect;
        }

        public bool ProcessDeleteFileRequest(String lpFileName, out String redirectedFilename)
        {
            bool redirect = false;
            string prefix = string.Empty;
            string realFilename = string.Empty;
            redirectedFilename = string.Empty;
            string redirectedRealFilename = string.Empty;

            if (lpFileName.StartsWith(@"\\?\"))
            {
                prefix = @"\\?\";
                realFilename = lpFileName.Substring(4);
            }
            else
            {
                realFilename = lpFileName;
            }

            try
            {
                redirectedRealFilename = Path.Combine(SandboxRootPath, SandboxName, "Drives", realFilename.Replace(":", ""));
                redirectedFilename = prefix + redirectedRealFilename;
                if (File.Exists(redirectedRealFilename)) redirect = true;
                if (!File.Exists(redirectedRealFilename + ".sbdeleted")) File.Create(redirectedRealFilename + ".sbdeleted").Close();
            }
            catch (Exception ex)
            {
                ReportException(ex);
                return false;
            }
            return redirect;
        }

        #endregion

        #region Registry

        #endregion

        #region Network

        #endregion

        #region Process

        public bool CaptureProcess(
            string lpApplicationName,
            string lpCommandLine,
            /*ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,*/
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            /*[In] ref STARTUPINFO lpStartupInfo,*/
            out PROCESS_INFORMATION lpProcessInformation)
        {
            Console.WriteLine("Intercepting CreateProcess call");
            string targetExe = lpApplicationName;
            string args = "";
            if (String.IsNullOrEmpty(targetExe))
            {
                int index = lpCommandLine.IndexOf(".exe") + 4;
                targetExe = lpCommandLine.Substring(0, index);
                args = lpCommandLine.Substring(index).Trim();
            }

            try
            {
                Console.WriteLine("Attempting to create and inject into {0}", targetExe);

                RemoteHooking.CreateAndInject(
                    targetExe,          // executable to run
                    args,                 // command line arguments for target
                    0,                  // additional process creation flags to pass to CreateProcess
                    InjectionOptions.DoNotRequireStrongName, // allow injectionLibrary to be unsigned
                    InjectionLibrary_x86,   // 32-bit library to inject (if target is 32-bit)
                    InjectionLibrary_x64,   // 64-bit library to inject (if target is 64-bit)
                    out Int32 OutProcessId,      // retrieve the newly created process ID
                    ChannelName,         // the parameters to pass into injected library
                    targetExe,
                    InjectionLibrary_x86,
                    InjectionLibrary_x64
                );
                Process process = Process.GetProcessById(OutProcessId);
                lpProcessInformation = new PROCESS_INFORMATION
                {
                    hProcess = process.Handle,
                    dwProcessId = OutProcessId
                };
            }
            catch (Exception e)
            {
                ReportException(e);
                lpProcessInformation = new PROCESS_INFORMATION();
                return false;
            }
            return true;
        }

        #endregion

    }
}
