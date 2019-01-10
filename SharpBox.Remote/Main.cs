using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using EasyHook;
using SharpBox.Remote.PInvoke.Enums;
using SharpBox.Remote.PInvoke.Librarys;
using SharpBox.Remote.PInvoke.Structs;
using SharpBox.Remote.PInvoke.Types;

namespace SharpBox.Remote
{
    public class Main : IEntryPoint
    {
        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        SharpBoxInterface Interface; // TODO: Replace Interface with NamedPipe/IPC with SharpBox.SVC

        /// <summary>
        /// Message queue of all files accessed
        /// </summary>
        Queue<string> _messageQueue = new Queue<string>();

        public Main(RemoteHooking.IContext InContext, String InChannelName, String InExecutableName, String InLibraryPath_x86, String InLibraryPath_x64)
        {
            // connect to host...
            Interface = RemoteHooking.IpcConnectClient<SharpBoxInterface>(InChannelName);

            // If Ping fails then the Run method will be not be called
            Interface.Ping();

            Interface.Executable = InExecutableName;
            Interface.ChannelName = InChannelName;
            Interface.InjectionLibrary_x86 = InLibraryPath_x86;
            Interface.InjectionLibrary_x64 = InLibraryPath_x64;
        }

        public void Run(RemoteHooking.IContext InContext, String InChannelName, String InExecutableName, String InLibraryPath_x86, String InLibraryPath_x64)
        {
            Interface.IsInstalled(RemoteHooking.GetCurrentProcessId());

            // Install hooks

            #region FileSystem

            // CreateFile https://msdn.microsoft.com/en-us/library/windows/desktop/aa363858(v=vs.85).aspx
            var createFileHookA = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "CreateFileA"),
                new CreateFileA_Delegate(CreateFileA_Hook),
                this);
            var createFileHookW = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "CreateFileW"),
                new CreateFileW_Delegate(CreateFileW_Hook),
                this);

            // ReadFile https://msdn.microsoft.com/en-us/library/windows/desktop/aa365467(v=vs.85).aspx
            var readFileHook = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "ReadFile"),
                new ReadFile_Delegate(ReadFile_Hook),
                this);

            // ReadFileEx
            var readFileExHook = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "ReadFileEx"),
                new ReadFileEx_Delegate(ReadFileEx_Hook),
                this);

            // DeleteFile
            var deleteFileHookA = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "DeleteFileA"),
                new DeleteFile_Delegate(DeleteFile_Hooked),
                this);
            var deleteFileHookW = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "DeleteFileW"),
                new DeleteFile_Delegate(DeleteFile_Hooked),
                this);

            // WriteFile https://msdn.microsoft.com/en-us/library/windows/desktop/aa365747(v=vs.85).aspx
            var writeFileHook = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "WriteFile"),
                new WriteFile_Delegate(WriteFile_Hook),
                this);

            // FindFirstFile
            var findFirstFileHookA = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "FindFirstFileA"),
                new FindFirstFileA_Delegate(FindFirstFileA_Hooked),
                this);
            var findFirstFileHookW = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "FindFirstFileW"),
                new FindFirstFileW_Delegate(FindFirstFileW_Hooked),
                this);

            // FindFirstFileEx
            var findFirstFileExHookA = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "FindFirstFileExA"),
                new FindFirstFileExA_Delegate(FindFirstFileExA_Hooked),
                this);
            var findFirstFileExHookW = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "FindFirstFileExW"),
                new FindFirstFileExW_Delegate(FindFirstFileExW_Hooked),
                this);

            // FindNextFile
            var findNextFileHookA = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "FindNextFileA"),
                new FindNextFile_Delegate(FindNextFile_Hooked),
                this);
            var findNextFileHookW = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "FindNextFileW"),
                new FindNextFile_Delegate(FindNextFile_Hooked),
                this);

            // PathFileExists
            var pathFileExistsA = LocalHook.Create(
                LocalHook.GetProcAddress("shlwapi.dll", "PathFileExistsA"),
                new PathFileExists_Delegate(PathFileExists_Hooked),
                this);
            var pathFileExistsW = LocalHook.Create(
                LocalHook.GetProcAddress("shlwapi.dll", "PathFileExistsW"),
                new PathFileExists_Delegate(PathFileExists_Hooked),
                this);

            #endregion
            #region Process

            // CreateProcess
            var createProcessA = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "CreateProcessA"),
                new CreateProcess_Delegate(CreateProcess_Hooked),
                this);
            var createProcessW = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "CreateProcessW"),
                new CreateProcess_Delegate(CreateProcess_Hooked),
                this);

            // NtQuerySystemInformation
            var ntQuerySystemInformation = LocalHook.Create(
                LocalHook.GetProcAddress("ntdll.dll", "NtQuerySystemInformation"),
                new NtQuerySystemInformation_Delegate(NtQuerySystemInformation_Hook),
                this);

            #endregion
            #region Style

            // SetWindowText
            //var setWindowTextA = LocalHook.Create(
            //    LocalHook.GetProcAddress("user32.dll", "SetWindowTextA"),
            //    new SetWindowTextA_Delegate(SetWindowTextA_Hooked),
            //    this);
            //var setWindowTextW = LocalHook.Create(
            //    LocalHook.GetProcAddress("user32.dll", "SetWindowTextW"),
            //    new SetWindowTextW_Delegate(SetWindowTextW_Hooked),
            //    this);

            #endregion
            #region WinEvent

            // eventObjectNamechange = User32.SetWinEventHook(Constants.EVENT_OBJECT_NAMECHANGE, Constants.EVENT_OBJECT_NAMECHANGE, IntPtr.Zero, WinEventProc_Delegate, (UInt32)RemoteHooking.GetCurrentProcessId(), 0, Constants.WINEVENT_OUTOFCONTEXT);

            #endregion


            #region FileSystem

            // Activate hooks on all threads except the current thread
            createFileHookA.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            createFileHookW.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            deleteFileHookA.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            deleteFileHookW.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            readFileHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            readFileExHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            writeFileHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            findFirstFileHookA.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            findFirstFileHookW.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            findFirstFileExHookA.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            findFirstFileExHookW.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            findNextFileHookA.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            findNextFileHookW.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            pathFileExistsA.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            pathFileExistsW.ThreadACL.SetExclusiveACL(new Int32[] { 0 });

            #endregion
            #region Process

            createProcessA.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            createProcessW.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            ntQuerySystemInformation.ThreadACL.SetExclusiveACL(new Int32[] { 0 });

            #endregion
            #region Style

            //setWindowTextA.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            //setWindowTextW.ThreadACL.SetExclusiveACL(new Int32[] { 0 });

            #endregion

            //Interface.ReportMessage("CreateFile, CreateProcess, DeleteFile, ReadFile, WriteFile, FindFirstFile FindFirstFileEx and FindNextFile hooks installed");
            //Interface.ReportMessage("CreateFile, DeleteFile, FindFirstFile, FindFirstFileEx, FindNextFile PathFileExists, ReadFile, SetWindowText and WriteFile hooks installed");
            Interface.ReportMessage("CreateFile, CreateProcess, DeleteFile, FindFirstFile, FindFirstFileEx, FindNextFile, NtQuerySystemInformation, PathFileExists, ReadFile, ReadFileEx and WriteFile hooks installed");
            //Interface.ReportMessage("CreateFile, CreateProcess, DeleteFile, FindFirstFile, FindFirstFileEx, FindNextFile, PathFileExists, ReadFile, ReadFileEx and WriteFile hooks installed");

            // Wake up the process (required if using RemoteHooking.CreateAndInject)
            RemoteHooking.WakeUpProcess();

            try
            {
                // Loop until SharpBox closes (i.e. IPC fails)
                while (true)
                {
                    Thread.Sleep(500);

                    string[] queued = null;

                    lock (_messageQueue)
                    {
                        queued = _messageQueue.ToArray();
                        _messageQueue.Clear();
                    }

                    // Send newly monitored file accesses to FileMonitor
                    if (queued != null && queued.Length > 0)
                    {
                        Interface.ReportMessages(queued);
                    }
                    else
                    {
                        Interface.Ping();
                    }
                }
            }
            catch
            {
                // Ping() or ReportMessages() will raise an exception if host is unreachable
            }

            // Remove hooks
            #region FileSystem

            createFileHookA.Dispose();
            createFileHookW.Dispose();
            deleteFileHookA.Dispose();
            deleteFileHookW.Dispose();
            readFileHook.Dispose();
            readFileExHook.Dispose();
            writeFileHook.Dispose();
            findFirstFileHookA.Dispose();
            findFirstFileHookW.Dispose();
            findFirstFileExHookA.Dispose();
            findFirstFileExHookW.Dispose();
            findNextFileHookA.Dispose();
            findNextFileHookW.Dispose();

            #endregion
            #region Process

            createProcessA.Dispose();
            createProcessW.Dispose();
            ntQuerySystemInformation.Dispose();

            #endregion
            #region Style

            //setWindowTextA.Dispose();
            //setWindowTextW.Dispose();

            #endregion
            #region WinEvent

            //User32.UnhookWinEvent(eventObjectNamechange);

            #endregion

            // Finalise cleanup of hooks
            LocalHook.Release();
        }


        #region FileSystem

        #region CreateFile Hook

        #region CreateFileA

        /// <summary>
        /// The CreateFile delegate, this is needed to create a delegate of our hook function <see cref="CreateFile_Hook(string, FileAccess, FileShare, IntPtr, FileMode, FileAttributes, IntPtr)"/>.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="shareMode"></param>
        /// <param name="securityAttributes"></param>
        /// <param name="creationDisposition"></param>
        /// <param name="flagsAndAttributes"></param>
        /// <param name="templateFile"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall,
                    CharSet = CharSet.Ansi,
                    SetLastError = true)]
        delegate IntPtr CreateFileA_Delegate(
                    [MarshalAs(UnmanagedType.LPTStr)] string filename,
                    [MarshalAs(UnmanagedType.U4)] PInvoke.Enums.FileAccess access,
                    [MarshalAs(UnmanagedType.U4)] FileShare share,
                    IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
                    [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                    [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
                    IntPtr templateFile);

        /// <summary>
        /// The CreateFileA hook function. This will be called instead of the original CreateFileA once hooked.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="shareMode"></param>
        /// <param name="securityAttributes"></param>
        /// <param name="creationDisposition"></param>
        /// <param name="flagsAndAttributes"></param>
        /// <param name="templateFile"></param>
        /// <returns></returns>
        IntPtr CreateFileA_Hook(
            [MarshalAs(UnmanagedType.LPTStr)] string filename,
            [MarshalAs(UnmanagedType.U4)] PInvoke.Enums.FileAccess access,
            [MarshalAs(UnmanagedType.U4)] FileShare share,
            IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
            IntPtr templateFile)
        {
            bool block = false;

            try
            {
                lock (this._messageQueue)
                {
                    if (this._messageQueue.Count < 1000)
                    {
                        string mode = string.Empty;
                        switch (creationDisposition)
                        {
                            case FileMode.CreateNew:
                                mode = "CREATE_NEW";
                                break;
                            case FileMode.Create:
                                mode = "CREATE_ALWAYS";
                                break;
                            case FileMode.Open:
                                mode = "OPEN_ALWAYS";
                                break;
                            case FileMode.OpenOrCreate:
                                mode = "OPEN_OR_CREATE";
                                break;
                            case FileMode.Truncate:
                                mode = "TRUNCATE_EXISTING";
                                break;
                            case FileMode.Append:
                                mode = "APPEND_EXISTING_OR_CREATE";
                                break;
                        }

                        //block = Interface.ShouldBlock(filename, creationDisposition);
                        // Add message to send to SharpBox
                        if (Interface.ProcessCreateFileRequest(filename, access, creationDisposition, out String redirectedFilename))
                        {
                            this._messageQueue.Enqueue(
                                string.Format("[{0}:{1}]: CREATE ({2}) \"{3}\" REDIRECTED \"{4}\"",
                                RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                                , mode, filename, redirectedFilename));
                            filename = redirectedFilename;
                        }
                        else if (block)
                        {
                            this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: CREATE ({2}) \"{3}\" BLOCKED",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                            , mode, filename));
                            return INVALID_HANDLE_VALUE;
                        }
                        else
                        {
                            this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: CREATE ({2}:{4}) \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                            , mode, filename, access));
                        }
                    }
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            // now call the original API...
            return Kernel32.CreateFileA(
                filename,
                access,
                share,
                securityAttributes,
                creationDisposition,
                flagsAndAttributes,
                templateFile);
        }

        #endregion

        #region CreateFileW

        /// <summary>
        /// The CreateFile delegate, this is needed to create a delegate of our hook function <see cref="CreateFile_Hook(string, FileAccess, FileShare, IntPtr, FileMode, FileAttributes, IntPtr)"/>.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="shareMode"></param>
        /// <param name="securityAttributes"></param>
        /// <param name="creationDisposition"></param>
        /// <param name="flagsAndAttributes"></param>
        /// <param name="templateFile"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall,
                    CharSet = CharSet.Unicode,
                    SetLastError = true)]
        delegate IntPtr CreateFileW_Delegate(
                    [MarshalAs(UnmanagedType.LPTStr)] string filename,
                    [MarshalAs(UnmanagedType.U4)] PInvoke.Enums.FileAccess access,
                    [MarshalAs(UnmanagedType.U4)] FileShare share,
                    IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
                    [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                    [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
                    IntPtr templateFile);

        /// <summary>
        /// The CreateFileW hook function. This will be called instead of the original CreateFileW once hooked.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="shareMode"></param>
        /// <param name="securityAttributes"></param>
        /// <param name="creationDisposition"></param>
        /// <param name="flagsAndAttributes"></param>
        /// <param name="templateFile"></param>
        /// <returns></returns>
        IntPtr CreateFileW_Hook(
            [MarshalAs(UnmanagedType.LPTStr)] string filename,
            [MarshalAs(UnmanagedType.U4)] PInvoke.Enums.FileAccess access,
            [MarshalAs(UnmanagedType.U4)] FileShare share,
            IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
            IntPtr templateFile)
        {
            bool block = false;

            try
            {
                lock (this._messageQueue)
                {
                    if (this._messageQueue.Count < 1000)
                    {
                        string mode = string.Empty;
                        switch (creationDisposition)
                        {
                            case FileMode.CreateNew:
                                mode = "CREATE_NEW";
                                break;
                            case FileMode.Create:
                                mode = "CREATE_ALWAYS";
                                break;
                            case FileMode.Open:
                                mode = "OPEN_ALWAYS";
                                break;
                            case FileMode.OpenOrCreate:
                                mode = "OPEN_OR_CREATE";
                                break;
                            case FileMode.Truncate:
                                mode = "TRUNCATE_EXISTING";
                                break;
                            case FileMode.Append:
                                mode = "APPEND_EXISTING_OR_CREATE";
                                break;
                        }

                        //block = Interface.ShouldBlock(filename, creationDisposition);
                        // Add message to send to SharpBox
                        if (Interface.ProcessCreateFileRequest(filename, access, creationDisposition, out String redirectedFilename))
                        {
                            this._messageQueue.Enqueue(
                                string.Format("[{0}:{1}]: CREATE ({2}) \"{3}\" REDIRECTED \"{4}\"",
                                RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                                , mode, filename, redirectedFilename));
                            filename = redirectedFilename;
                        }
                        else if (block)
                        {
                            this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: CREATE ({2}) \"{3}\" BLOCKED",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                            , mode, filename));
                            return INVALID_HANDLE_VALUE;
                        }
                        else
                        {
                            this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: CREATE ({2}:{4}) \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                            , mode, filename, access));
                        }
                    }
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            // now call the original API...
            return Kernel32.CreateFileW(
                filename,
                access,
                share,
                securityAttributes,
                creationDisposition,
                flagsAndAttributes,
                templateFile);
        }

        #endregion

        #endregion

        #region DeleteFile Hook

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        delegate bool DeleteFile_Delegate(string lpFileName);

        bool DeleteFile_Hooked(string lpFileName)
        {
            try
            {
                lock (this._messageQueue)
                {
                    if (Interface.ProcessDeleteFileRequest(lpFileName, out String redirectedFilename))
                    {
                        this._messageQueue.Enqueue(
                           string.Format("[{0}:{1}]: DELETE \"{2}\" REDIRECTED \"{3}\"",
                           RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                           , lpFileName, redirectedFilename));
                    }
                    else
                    {
                        this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: DELETE \"{2}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                            , lpFileName));
                    }
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            return Kernel32.DeleteFile(lpFileName);
        }

        #endregion

        #region ReadFile Hook

        /// <summary>
        /// The ReadFile delegate, this is needed to create a delegate of our hook function <see cref="ReadFile_Hook(IntPtr, IntPtr, uint, out uint, IntPtr)"/>.
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="nNumberOfBytesToRead"></param>
        /// <param name="lpNumberOfBytesRead"></param>
        /// <param name="lpOverlapped"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate bool ReadFile_Delegate(
            IntPtr hFile,
            IntPtr lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        /// <summary>
        /// The ReadFile hook function. This will be called instead of the original ReadFile once hooked.
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="nNumberOfBytesToRead"></param>
        /// <param name="lpNumberOfBytesRead"></param>
        /// <param name="lpOverlapped"></param>
        /// <returns></returns>
        bool ReadFile_Hook(
            IntPtr hFile,
            IntPtr lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped)
        {
            bool result = false;
            lpNumberOfBytesRead = 0;

            // Call original first so we have a value for lpNumberOfBytesRead
            result = Kernel32.ReadFile(hFile, lpBuffer, nNumberOfBytesToRead, out lpNumberOfBytesRead, lpOverlapped);

            try
            {
                lock (this._messageQueue)
                {
                    if (this._messageQueue.Count < 1000)
                    {
                        // Retrieve filename from the file handle
                        StringBuilder filename = new StringBuilder(255);
                        UInt32 success = Kernel32.GetFinalPathNameByHandle(hFile, filename, 255, 0);
                        if (success == 0)
                        {
                            // Add message to send to SharpBox
                            this._messageQueue.Enqueue(
                                string.Format("[{0}:{1}]: READ ({2} bytes) GET_PATH_NAME_ERROR: {3}",
                                RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                                , lpNumberOfBytesRead, Marshal.GetLastWin32Error()));
                        }
                        else
                        {
                            // Add message to send to SharpBox
                            this._messageQueue.Enqueue(
                                string.Format("[{0}:{1}]: READ ({2} bytes) \"{3}\"",
                                RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                                , lpNumberOfBytesRead, filename));
                        }
                    }
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            return result;
        }

        #endregion

        #region ReadFileEx Hook

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        delegate bool ReadFileEx_Delegate(
            IntPtr hFile,
            out Byte[] lpBuffer,
            UInt32 nNumberOfBytesToRead,
            [In] ref NativeOverlapped lpOverlapped,
            IOCompletionCallback lpCompletionRoutine);

        bool ReadFileEx_Hook(
            IntPtr hFile,
            out Byte[] lpBuffer,
            UInt32 nNumberOfBytesToRead,
            [In] ref NativeOverlapped lpOverlapped,
            IOCompletionCallback lpCompletionRoutine)
        {
            bool result = false;

            // Call original first so we have a value for lpNumberOfBytesRead
            result = Kernel32.ReadFileEx(hFile, out lpBuffer, nNumberOfBytesToRead, ref lpOverlapped, lpCompletionRoutine);

            try
            {
                lock (this._messageQueue)
                {
                    if (this._messageQueue.Count < 1000)
                    {
                        // Retrieve filename from the file handle
                        StringBuilder filename = new StringBuilder(255);
                        UInt32 success = Kernel32.GetFinalPathNameByHandle(hFile, filename, 255, 0);
                        if (success == 0)
                        {
                            // Add message to send to SharpBox
                            this._messageQueue.Enqueue(
                                string.Format("[{0}:{1}]: READ GET_PATH_NAME_ERROR: {2}",
                                RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                                , Marshal.GetLastWin32Error()));
                        }
                        else
                        {
                            // Add message to send to SharpBox
                            this._messageQueue.Enqueue(
                                string.Format("[{0}:{1}]: READ \"{2}\"",
                                RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                                , filename));
                        }
                    }
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            return result;
        }

        #endregion

        #region WriteFile Hook

        /// <summary>
        /// The WriteFile delegate, this is needed to create a delegate of our hook function <see cref="WriteFile_Hook(IntPtr, IntPtr, uint, out uint, IntPtr)"/>.
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="nNumberOfBytesToWrite"></param>
        /// <param name="lpNumberOfBytesWritten"></param>
        /// <param name="lpOverlapped"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        delegate bool WriteFile_Delegate(
            IntPtr hFile,
            IntPtr lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        /// <summary>
        /// The WriteFile hook function. This will be called instead of the original WriteFile once hooked.
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="nNumberOfBytesToWrite"></param>
        /// <param name="lpNumberOfBytesWritten"></param>
        /// <param name="lpOverlapped"></param>
        /// <returns></returns>
        bool WriteFile_Hook(
            IntPtr hFile,
            IntPtr lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped)
        {
            bool result = false;

            // Call original first so we get lpNumberOfBytesWritten
            result = Kernel32.WriteFile(hFile, lpBuffer, nNumberOfBytesToWrite, out lpNumberOfBytesWritten, lpOverlapped);

            try
            {
                lock (this._messageQueue)
                {
                    if (this._messageQueue.Count < 1000)
                    {
                        // Retrieve filename from the file handle
                        StringBuilder filename = new StringBuilder(255);
                        UInt32 success = Kernel32.GetFinalPathNameByHandle(hFile, filename, 255, 0);
                        if (success == 0)
                        {
                            // Add message to send to SharpBox
                            this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: WRITE ({2} bytes) GET_PATH_NAME_ERROR: {3}",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                            , lpNumberOfBytesWritten, Marshal.GetLastWin32Error()));
                        }
                        else
                        {
                            // Add message to send to SharpBox
                            this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: WRITE ({2} bytes) \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                            , lpNumberOfBytesWritten, filename));
                        }
                    }
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            return result;
        }

        #endregion

        #region FindFirstFile Hook

        #region FindFirstFileA

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        delegate IntPtr FindFirstFileA_Delegate(
            String lpFileName,
            out WIN32_FIND_DATA lpFindFileData);

        IntPtr FindFirstFileA_Hooked(
            String lpFileName,
            out WIN32_FIND_DATA lpFindFileData)
        {
            IntPtr searchHandle = Kernel32.FindFirstFileA(lpFileName, out lpFindFileData);
            try
            {
                lock (this._messageQueue)
                {
                    this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: FINDFIRSTFILEA (Filename: {2}) \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId(),
                            lpFindFileData.cFileName, lpFileName));
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            return searchHandle; // If Blocked return INVALID_HANDLE
        }

        #endregion

        #region FindFirstFileW

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate IntPtr FindFirstFileW_Delegate(
            String lpFileName,
            out WIN32_FIND_DATA lpFindFileData);

        IntPtr FindFirstFileW_Hooked(
            String lpFileName,
            out WIN32_FIND_DATA lpFindFileData)
        {
            IntPtr searchHandle = Kernel32.FindFirstFileW(lpFileName, out lpFindFileData);
            try
            {
                lock (this._messageQueue)
                {
                    this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: FINDFIRSTFILEW (Filename: {2}) \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId(),
                            lpFindFileData.cFileName, lpFileName));
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            return searchHandle; // If Blocked return INVALID_HANDLE
        }

        #endregion

        #endregion

        #region FindNextFile Hook

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        delegate bool FindNextFile_Delegate(
            IntPtr hFindFile,
            out WIN32_FIND_DATA lpFindFileData);

        bool FindNextFile_Hooked(
            IntPtr hFindFile,
            out WIN32_FIND_DATA lpFindFileData)
        {
            bool result = false;

            // Call original first so we have a value for lpFindFileData
            result = Kernel32.FindNextFile(hFindFile, out lpFindFileData);

            try
            {
                //while (lpFindFileData.cFileName.Contains("Program"))
                //{
                //    lock (this._messageQueue)
                //    {
                //        if (this._messageQueue.Count < 1000)
                //        {
                //            // Retrieve filename from the file handle
                //            StringBuilder filename = new StringBuilder(255);
                //            GetFinalPathNameByHandle(hFindFile, filename, 255, 0);


                //            // Add message to send to FileMonitor
                //            this._messageQueue.Enqueue(
                //                string.Format("[{0}:{1}]: FINDNEXTFILE (Filename: {2}) \"{3}\" BLOCKED",
                //                RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                //                , lpFindFileData.cFileName, filename));

                //            // Skipp the current file
                //            result = Kernel32.FindNextFile(hFindFile, out lpFindFileData);
                //        }
                //    }
                lock (this._messageQueue)
                {
                    if (this._messageQueue.Count < 1000)
                    {
                        // Retrieve filename from the file handle
                        StringBuilder filename = new StringBuilder(255);
                        Kernel32.GetFinalPathNameByHandle(hFindFile, filename, 255, 0);

                        // Add message to send to FileMonitor
                        this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: FINDNEXTFILE (Filename: {2}) \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                            , lpFindFileData.cFileName, filename));
                    }
                }
                //}
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            return result;
        }

        #endregion

        #region FindFirstFileEx Hook

        #region FindFirstFileExA

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        delegate IntPtr FindFirstFileExA_Delegate(
            String lpFileName,
            FINDEX_INFO_LEVELS fInfoLevelId,
            out WIN32_FIND_DATA lpFindFileData,
            FINDEX_SEARCH_OPS fSearchOp,
            IntPtr lpSearchFilter,
            int dwAdditionalFlags);

        IntPtr FindFirstFileExA_Hooked(
            String lpFileName,
            FINDEX_INFO_LEVELS fInfoLevelId,
            out WIN32_FIND_DATA lpFindFileData,
            FINDEX_SEARCH_OPS fSearchOp,
            IntPtr lpSearchFilter,
            int dwAdditionalFlags)
        {
            IntPtr searchHandle = Kernel32.FindFirstFileExA(lpFileName, fInfoLevelId, out lpFindFileData, fSearchOp, lpSearchFilter, dwAdditionalFlags);
            try
            {
                lock (this._messageQueue)
                {
                    this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: FINDFIRSTFILEEXA ({2}) \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId(),
                            lpFindFileData.cFileName, lpFileName));
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            return searchHandle; // If Blocked return INVALID_HANDLE
        }

        #endregion

        #region FindFirstFileExW

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate IntPtr FindFirstFileExW_Delegate(
            String lpFileName,
            FINDEX_INFO_LEVELS fInfoLevelId,
            out WIN32_FIND_DATA lpFindFileData,
            FINDEX_SEARCH_OPS fSearchOp,
            IntPtr lpSearchFilter,
            int dwAdditionalFlags);

        IntPtr FindFirstFileExW_Hooked(
            String lpFileName,
            FINDEX_INFO_LEVELS fInfoLevelId,
            out WIN32_FIND_DATA lpFindFileData,
            FINDEX_SEARCH_OPS fSearchOp,
            IntPtr lpSearchFilter,
            int dwAdditionalFlags)
        {
            IntPtr searchHandle = Kernel32.FindFirstFileExW(lpFileName, fInfoLevelId, out lpFindFileData, fSearchOp, lpSearchFilter, dwAdditionalFlags);
            try
            {
                lock (this._messageQueue)
                {
                    this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: FINDFIRSTFILEEXW ({2}) \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId(),
                            lpFindFileData.cFileName, lpFileName));
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            return searchHandle; // If Blocked return INVALID_HANDLE
        }

        #endregion

        #endregion

        #region PathFileExists Hook

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        delegate bool PathFileExists_Delegate(
            [MarshalAs(UnmanagedType.LPTStr)]String pszPath);

        bool PathFileExists_Hooked(
            [MarshalAs(UnmanagedType.LPTStr)]String pszPath)
        {
            Boolean isDeleted = false;
            try
            {
                if (Interface.PathFileExists(pszPath, out String redirectedFilename, out isDeleted))
                {
                    this._messageQueue.Enqueue(
                                string.Format("[{0}:{1}]: PATHFILEEXISTS \"{2}\" REDIRECTED \"{3}\"",
                                RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                                , pszPath, redirectedFilename));
                    pszPath = redirectedFilename;
                }
                else
                {
                    this._messageQueue.Enqueue(
                                string.Format("[{0}:{1}]: PATHFILEEXISTS \"{2}\"",
                                RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                                , pszPath));
                }

            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }
            return isDeleted ? false : Shlwapi.PathFileExists(pszPath);
        }

        #endregion

        #endregion

        #region Process

        #region CreateProcess Hook

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        delegate bool CreateProcess_Delegate(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        bool CreateProcess_Hooked(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation)
        {
            bool result = false;

            string targetExe = lpApplicationName;
            if (String.IsNullOrEmpty(targetExe))
            {
                int index = lpCommandLine.IndexOf(".exe") + 4;
                targetExe = lpCommandLine.Substring(0, index);
            }

            if (Interface.PathFileExists(targetExe, out String redirectedFilename, out bool isDeleted))
            {
                lpApplicationName = lpApplicationName.Replace(targetExe, redirectedFilename);
                lpCommandLine = lpCommandLine.Replace(targetExe, redirectedFilename);
            }
            //result = Interface.CaptureProcess(lpApplicationName, lpCommandLine, /*ref lpProcessAttributes, ref lpThreadAttributes,*/ bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory/*, ref lpStartupInfo*/, out lpProcessInformation);
            result = Kernel32.CreateProcess(lpApplicationName, lpCommandLine, ref lpProcessAttributes, ref lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, ref lpStartupInfo, out lpProcessInformation);
            try
            {
                lock (this._messageQueue)
                {
                    this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: CREATE_PROCESS ({2}) \"{3}\" DEBUG_INFO: {4}:{5}",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId(),
                            lpCommandLine, lpApplicationName, lpProcessInformation.dwProcessId, lpProcessInformation.dwThreadId));
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            //return Kernel32.CreateProcess(lpApplicationName, lpCommandLine, ref lpProcessAttributes, ref lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, ref lpStartupInfo, out lpProcessInformation);
            return result;
        }

        #endregion

        #region NtQuerySystemInformation Hook

        [StructLayout(LayoutKind.Sequential)]
        public class SystemProcessInformation
        {
            public uint NextEntryOffset;
            public uint NumberOfThreads;
            public long SpareLi1;
            public long SpareLi2;
            public long SpareLi3;
            public long CreateTime;
            public long UserTime;
            public long KernelTime;

            public ushort NameLength;   // UNICODE_STRING   
            public ushort MaximumNameLength;
            public IntPtr NamePtr;     // This will point into the data block returned by NtQuerySystemInformation

            public int BasePriority;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
            public uint HandleCount;
            public uint SessionId;
            public UIntPtr PageDirectoryBase;
            public UIntPtr PeakVirtualSize;  // SIZE_T
            public UIntPtr VirtualSize;
            public uint PageFaultCount;

            public UIntPtr PeakWorkingSetSize;
            public UIntPtr WorkingSetSize;
            public UIntPtr QuotaPeakPagedPoolUsage;
            public UIntPtr QuotaPagedPoolUsage;
            public UIntPtr QuotaPeakNonPagedPoolUsage;
            public UIntPtr QuotaNonPagedPoolUsage;
            public UIntPtr PagefileUsage;
            public UIntPtr PeakPagefileUsage;
            public UIntPtr PrivatePageCount;

            public long ReadOperationCount;
            public long WriteOperationCount;
            public long OtherOperationCount;
            public long ReadTransferCount;
            public long WriteTransferCount;
            public long OtherTransferCount;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        delegate NTSTATUS NtQuerySystemInformation_Delegate(SYSTEM_INFORMATION_CLASS InfoClass, IntPtr Info, UInt32 Size, out UInt32 Length);

        NTSTATUS NtQuerySystemInformation_Hook(SYSTEM_INFORMATION_CLASS InfoClass, IntPtr Info, UInt32 Size, out UInt32 Length)
        {
            NTSTATUS result = Ntdll.NtQuerySystemInformation(InfoClass, Info, Size, out Length);

            if (InfoClass == SYSTEM_INFORMATION_CLASS.SystemProcessInformation && result == NTSTATUS.SUCCESS) // Hide processes
            {
                try
                {
                    long totalOffset = 0;

                    //while (true)
                    //{
                    //    IntPtr currentPtr = (IntPtr)((long)Info + totalOffset);
                    //    SystemProcessInformation pi = new SystemProcessInformation();

                    //    Marshal.PtrToStructure(currentPtr, pi);

                    //    string name = "";

                    //    if (pi.NamePtr == IntPtr.Zero)
                    //    {
                    //        if (pi.UniqueProcessId.ToInt32() == 8)
                    //        {
                    //            name = "System";
                    //        }
                    //        else if (pi.UniqueProcessId.ToInt32() == 0)
                    //        {
                    //            name = "Idle";
                    //        }
                    //        else
                    //        {
                    //            name = pi.UniqueProcessId.ToInt32().ToString();
                    //        }
                    //    }
                    //    else
                    //    {
                    //        #region GetProcessShortName

                    //        name = Marshal.PtrToStringUni(pi.NamePtr, pi.NameLength / sizeof(char));

                    //        int slash = -1;
                    //        int period = -1;

                    //        for (int i = 0; i < name.Length; i++)
                    //        {
                    //            if (name[i] == '\\')
                    //                slash = i;
                    //            else if (name[i] == '.')
                    //                period = i;
                    //        }

                    //        if (period == -1)
                    //            period = name.Length - 1; // set to end of string
                    //        else
                    //        {
                    //            // if a period was found, then see if the extension is
                    //            // .EXE, if so drop it, if not, then use end of string
                    //            // (i.e. include extension in name)
                    //            String extension = name.Substring(period);

                    //            if (String.Equals(".exe", extension, StringComparison.OrdinalIgnoreCase))
                    //                period--;                 // point to character before period
                    //            else
                    //                period = name.Length - 1; // set to end of string
                    //        }

                    //        if (slash == -1)
                    //            slash = 0;     // set to start of string
                    //        else
                    //            slash++;       // point to character next to slash

                    //        // copy characters between period (or end of string) and
                    //        // slash (or start of string) to make image name
                    //        name = name.Substring(slash, period - slash + 1);

                    //        #endregion
                    //    }

                    //    //lock (this._messageQueue)
                    //    //{
                    //    //    this._messageQueue.Enqueue(
                    //    //            string.Format("[{0}:{1}]: NtQuerySystemInformation ({2}) CurrPtr: {3}",
                    //    //            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId(),
                    //    //            name, currentPtr.ToInt64().ToString("x4")));
                    //    //}

                    //    currentPtr = (IntPtr)((long)currentPtr + Marshal.SizeOf(pi));

                    //    if (pi.NextEntryOffset == 0)
                    //    {
                    //        break;
                    //    }
                    //    totalOffset += pi.NextEntryOffset;
                    //}


                    lock (this._messageQueue)
                    {
                        this._messageQueue.Enqueue(
                                string.Format("[{0}:{1}]: NtQuerySystemInformation",
                                RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()));
                    }

                    IntPtr currentPtr = (IntPtr)((long)Info + totalOffset);

                    SystemProcessInformation pPrev = new SystemProcessInformation();
                    Marshal.PtrToStructure(currentPtr, pPrev);
                    currentPtr = (IntPtr)((long)currentPtr + Marshal.SizeOf(pPrev));
                    totalOffset += pPrev.NextEntryOffset;
                    SystemProcessInformation pCurrent = new SystemProcessInformation();
                    Marshal.PtrToStructure(currentPtr, pCurrent);

                    while (pPrev.NextEntryOffset != 0)
                    {
                        string name = "";

                        if (pCurrent.NamePtr == IntPtr.Zero)
                        {
                            if (pCurrent.UniqueProcessId.ToInt32() == 8)
                            {
                                name = "System";
                            }
                            else if (pCurrent.UniqueProcessId.ToInt32() == 0)
                            {
                                name = "Idle";
                            }
                            else
                            {
                                name = pCurrent.UniqueProcessId.ToInt32().ToString();
                            }
                        }
                        else
                        {
                            #region GetProcessShortName

                            name = Marshal.PtrToStringUni(pCurrent.NamePtr, pCurrent.NameLength / sizeof(char));

                            int slash = -1;
                            int period = -1;

                            for (int i = 0; i < name.Length; i++)
                            {
                                if (name[i] == '\\')
                                    slash = i;
                                else if (name[i] == '.')
                                    period = i;
                            }

                            if (period == -1)
                                period = name.Length - 1; // set to end of string
                            else
                            {
                                // if a period was found, then see if the extension is
                                // .EXE, if so drop it, if not, then use end of string
                                // (i.e. include extension in name)
                                String extension = name.Substring(period);

                                if (String.Equals(".exe", extension, StringComparison.OrdinalIgnoreCase))
                                    period--;                 // point to character before period
                                else
                                    period = name.Length - 1; // set to end of string
                            }

                            if (slash == -1)
                                slash = 0;     // set to start of string
                            else
                                slash++;       // point to character next to slash

                            // copy characters between period (or end of string) and
                            // slash (or start of string) to make image name
                            name = name.Substring(slash, period - slash + 1);

                            #endregion
                        }

                        lock (this._messageQueue)
                        {
                            this._messageQueue.Enqueue(
                                    string.Format("[{0}:{1}]: NtQuerySystemInformation ({2}) CurrPtr: {3}",
                                    RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId(),
                                    name, currentPtr.ToInt64().ToString("x4")));
                        }

                        if (name != "(firefox)")
                        {
                            if (pCurrent.NextEntryOffset == 0)
                            {
                                pPrev.NextEntryOffset = 0;
                            }
                            else
                            {
                                pPrev.NextEntryOffset += pCurrent.NextEntryOffset;
                            }
                            pCurrent = pPrev;
                        }
                        pPrev = pCurrent;
                        currentPtr = (IntPtr)((long)currentPtr + Marshal.SizeOf(pCurrent));
                        totalOffset += pPrev.NextEntryOffset;
                        Marshal.PtrToStructure(currentPtr, pCurrent);
                    }

                    //SYSTEM_PROCESS_INFORMATION pPrev = (SYSTEM_PROCESS_INFORMATION)Marshal.PtrToStructure(Info, typeof(SYSTEM_PROCESS_INFORMATION));
                    //SYSTEM_PROCESS_INFORMATION pCurrent;

                    //IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(pPrev));
                    //try
                    //{
                    //    Marshal.StructureToPtr(pPrev, ptr, false);
                    //    pCurrent = (SYSTEM_PROCESS_INFORMATION)Marshal.PtrToStructure(new IntPtr(ptr.ToInt64() + pPrev.NextEntryOffset), typeof(SYSTEM_PROCESS_INFORMATION));
                    //}
                    //finally
                    //{
                    //    Marshal.FreeHGlobal(ptr);
                    //}

                    //while (pPrev.NextEntryOffset != 0)
                    //{
                    //    lock (this._messageQueue)
                    //    {
                    //        this._messageQueue.Enqueue(
                    //                string.Format("[{0}:{1}]: NtQuerySystemInformation ({2}) CurrPtr: {3}",
                    //                RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId(),
                    //                pCurrent.ImageName.ToString(), ""));
                    //    }
                    //    //if (pCurrent.ImageName.ToString() != "")
                    //    //{
                    //    if (pCurrent.NextEntryOffset == 0)
                    //    {
                    //        pPrev.NextEntryOffset = 0;
                    //    }
                    //    else
                    //    {
                    //        pPrev.NextEntryOffset += pCurrent.NextEntryOffset;
                    //    }
                    //    pCurrent = pPrev;
                    //    //}
                    //    pPrev = pCurrent;
                    //    IntPtr ptrStruct = Marshal.AllocHGlobal(Marshal.SizeOf(pCurrent));
                    //    try
                    //    {
                    //        Marshal.StructureToPtr(pCurrent, ptrStruct, false);
                    //        pCurrent = (SYSTEM_PROCESS_INFORMATION)Marshal.PtrToStructure(new IntPtr(ptrStruct.ToInt64() + pCurrent.NextEntryOffset), typeof(SYSTEM_PROCESS_INFORMATION));
                    //    }
                    //    finally
                    //    {
                    //        Marshal.FreeHGlobal(ptrStruct);
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    Interface.ReportException(ex);
                }
            }
            return result;
        }

        #endregion

        #endregion

        #region Style

        #region SetWindowText Hook

        #region SetWindowTextA

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        delegate bool SetWindowTextA_Delegate(
            IntPtr hWnd,
            String lpString);

        bool SetWindowTextA_Hooked(
            IntPtr hWnd,
            String lpString)
        {

            try
            {
                //if (hWnd != IntPtr.Zero && !string.IsNullOrEmpty(lpString))
                //{
                lock (this._messageQueue)
                {
                    if (hWnd != IntPtr.Zero && !lpString.StartsWith("~SharpBoxed~"))
                    {
                        string newText = $"~SharpBoxed~ {lpString}";
                        this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: SETWINDOWTEXTA \"{2}\" CHANGED \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId(),
                            lpString, newText));
                        lpString = newText;
                    }
                    else
                    {
                        this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: SETWINDOWTEXTA \"{2}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId(),
                            lpString));
                    }
                }
                //}
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }
            return User32.SetWindowTextA(hWnd, lpString);
        }

        #endregion

        #region SetWindowTextW

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate bool SetWindowTextW_Delegate(
            IntPtr hWnd,
            String lpString);

        bool SetWindowTextW_Hooked(
            IntPtr hWnd,
            String lpString)
        {

            try
            {
                if (Process.GetCurrentProcess().MainWindowHandle != IntPtr.Zero && !Process.GetCurrentProcess().MainWindowTitle.StartsWith("~SharpBoxed~"))
                {
                    User32.SetWindowTextW(Process.GetCurrentProcess().MainWindowHandle, $"~SharpBoxed~ {Process.GetCurrentProcess().MainWindowTitle}");
                }
                //if (hWnd != IntPtr.Zero && !string.IsNullOrEmpty(lpString))
                //{
                lock (this._messageQueue)
                {
                    if (hWnd != IntPtr.Zero && !lpString.StartsWith("~SharpBoxed~"))
                    {
                        string newText = $"~SharpBoxed~ {lpString}";
                        this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: SETWINDOWTEXTW \"{2}\" CHANGED \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId(),
                            lpString, newText));
                        lpString = newText;
                    }
                    else
                    {
                        this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: SETWINDOWTEXTW \"{2}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId(),
                            lpString));
                    }
                }
                //}
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }
            return User32.SetWindowTextW(hWnd, lpString);
        }

        #endregion

        #endregion

        #endregion

        #region WinEvent

        readonly WinEventDelegate WinEventProc_Delegate = new WinEventDelegate(WinEventProc);

        static void WinEventProc(IntPtr hWinEventHook, UInt32 eventType, IntPtr hWnd, Int32 idObject, Int32 idChild, UInt32 dwEventThread, UInt32 dwmsEventTime)
        {
            SharpBoxInterface Interface = (SharpBoxInterface)HookRuntimeInfo.Callback;
            // filter out non-HWND namechanges... (eg. items within a listbox)
            if (idObject != 0 || idChild != 0)
            {
                return;
            }
            Interface.ReportMessage(string.Format("Text of hwnd changed {0:x8}", hWnd.ToInt32()));
        }

        #endregion

    }
}
