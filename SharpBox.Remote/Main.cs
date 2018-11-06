using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using EasyHook;

namespace SharpBox.Remote
{
    public class Main : IEntryPoint
    {
        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        SharpBoxInterface Interface;

        /// <summary>
        /// Message queue of all files accessed
        /// </summary>
        Queue<string> _messageQueue = new Queue<string>();


        public Main(RemoteHooking.IContext InContext, String InChannelName)
        {
            // connect to host...
            Interface = RemoteHooking.IpcConnectClient<SharpBoxInterface>(InChannelName);

            // If Ping fails then the Run method will be not be called
            Interface.Ping();
        }

        public void Run(RemoteHooking.IContext InContext, String InChannelName)
        {
            Interface.IsInstalled(RemoteHooking.GetCurrentProcessId());

            // Install hooks

            // CreateFile https://msdn.microsoft.com/en-us/library/windows/desktop/aa363858(v=vs.85).aspx
            var createFileHook = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "CreateFileW"),
                new CreateFile_Delegate(CreateFile_Hook),
                this);

            // ReadFile https://msdn.microsoft.com/en-us/library/windows/desktop/aa365467(v=vs.85).aspx
            var readFileHook = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "ReadFile"),
                new ReadFile_Delegate(ReadFile_Hook),
                this);

            // WriteFile https://msdn.microsoft.com/en-us/library/windows/desktop/aa365747(v=vs.85).aspx
            var writeFileHook = LocalHook.Create(
                LocalHook.GetProcAddress("kernel32.dll", "WriteFile"),
                new WriteFile_Delegate(WriteFile_Hook),
                this);

            // Activate hooks on all threads except the current thread
            createFileHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            readFileHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            writeFileHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });

            Interface.ReportMessage("CreateFile, ReadFile and WriteFile hooks installed");

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
            createFileHook.Dispose();
            readFileHook.Dispose();
            writeFileHook.Dispose();

            // Finalise cleanup of hooks
            LocalHook.Release();
        }

        /// <summary>
        /// P/Invoke to determine the filename from a file handle
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa364962(v=vs.85).aspx
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpszFilePath"></param>
        /// <param name="cchFilePath"></param>
        /// <param name="dwFlags"></param>
        /// <returns></returns>
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

        #region FileSystem

        #region CreateFileW Hook

        /// <summary>
        /// The CreateFile delegate, this is needed to create a delegate of our hook function <see cref="CreateFile_Hook(string, uint, uint, IntPtr, uint, uint, IntPtr)"/>.
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
        delegate IntPtr CreateFile_Delegate(
                    String filename,
                    UInt32 desiredAccess,
                    UInt32 shareMode,
                    IntPtr securityAttributes,
                    UInt32 creationDisposition,
                    UInt32 flagsAndAttributes,
                    IntPtr templateFile);

        /// <summary>
        /// Using P/Invoke to call original method.
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa363858(v=vs.85).aspx
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="shareMode"></param>
        /// <param name="securityAttributes"></param>
        /// <param name="creationDisposition"></param>
        /// <param name="flagsAndAttributes"></param>
        /// <param name="templateFile"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll",
            CharSet = CharSet.Unicode,
            SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern IntPtr CreateFileW(
            String filename,
            UInt32 desiredAccess,
            UInt32 shareMode,
            IntPtr securityAttributes,
            UInt32 creationDisposition,
            UInt32 flagsAndAttributes,
            IntPtr templateFile);

        /// <summary>
        /// The CreateFile hook function. This will be called instead of the original CreateFile once hooked.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="desiredAccess"></param>
        /// <param name="shareMode"></param>
        /// <param name="securityAttributes"></param>
        /// <param name="creationDisposition"></param>
        /// <param name="flagsAndAttributes"></param>
        /// <param name="templateFile"></param>
        /// <returns></returns>
        IntPtr CreateFile_Hook(
            String filename,
            UInt32 desiredAccess,
            UInt32 shareMode,
            IntPtr securityAttributes,
            UInt32 creationDisposition,
            UInt32 flagsAndAttributes,
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
                            case 1:
                                mode = "CREATE_NEW";
                                break;
                            case 2:
                                mode = "CREATE_ALWAYS";
                                break;
                            case 3:
                                mode = "OPEN_ALWAYS";
                                break;
                            case 4:
                                mode = "OPEN_EXISTING";
                                break;
                            case 5:
                                mode = "TRUNCATE_EXISTING";
                                break;
                        }

                        //block = Interface.ShouldBlock(filename, creationDisposition);

                        // Add message to send to SharpBox
                        if (block)
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
                            string.Format("[{0}:{1}]: CREATE ({2}) \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                            , mode, filename));
                        }
                    }
                }
            }
            catch
            {
                // swallow exceptions so that any issues caused by this code do not crash target process
            }

            // now call the original API...
            return CreateFileW(
                filename,
                desiredAccess,
                shareMode,
                securityAttributes,
                creationDisposition,
                flagsAndAttributes,
                templateFile);
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
        /// Using P/Invoke to call the orginal function
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="nNumberOfBytesToRead"></param>
        /// <param name="lpNumberOfBytesRead"></param>
        /// <param name="lpOverlapped"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool ReadFile(
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
            result = ReadFile(hFile, lpBuffer, nNumberOfBytesToRead, out lpNumberOfBytesRead, lpOverlapped);

            try
            {
                lock (this._messageQueue)
                {
                    if (this._messageQueue.Count < 1000)
                    {
                        // Retrieve filename from the file handle
                        StringBuilder filename = new StringBuilder(255);
                        GetFinalPathNameByHandle(hFile, filename, 255, 0);

                        // Add message to send to FileMonitor
                        this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: READ ({2} bytes) \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                            , lpNumberOfBytesRead, filename));
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
        /// Using P/Invoke to call original WriteFile method
        /// </summary>
        /// <param name="hFile"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="nNumberOfBytesToWrite"></param>
        /// <param name="lpNumberOfBytesWritten"></param>
        /// <param name="lpOverlapped"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool WriteFile(
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
            result = WriteFile(hFile, lpBuffer, nNumberOfBytesToWrite, out lpNumberOfBytesWritten, lpOverlapped);

            try
            {
                lock (this._messageQueue)
                {
                    if (this._messageQueue.Count < 1000)
                    {
                        // Retrieve filename from the file handle
                        StringBuilder filename = new StringBuilder(255);
                        GetFinalPathNameByHandle(hFile, filename, 255, 0);

                        // Add message to send to FileMonitor
                        this._messageQueue.Enqueue(
                            string.Format("[{0}:{1}]: WRITE ({2} bytes) \"{3}\"",
                            RemoteHooking.GetCurrentProcessId(), RemoteHooking.GetCurrentThreadId()
                            , lpNumberOfBytesWritten, filename));
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

        #endregion
    }
}
