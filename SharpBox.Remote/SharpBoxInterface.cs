using System;
using System.IO;

namespace SharpBox.Remote
{
    /// <summary>
    /// Provides an interface for communicating from the client (target) to the server (injector)
    /// </summary>
    public class SharpBoxInterface : MarshalByRefObject
    {
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
            for (int i = 0; i < messages.Length; i++)
            {
                Console.WriteLine(messages[i]);
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
            Console.WriteLine($"The target process has reported an error:\r\n{e.ToString()}");
        }

        int count = 0;
        /// <summary>
        /// Called to confirm that the IPC channel is still open / host application has not closed
        /// </summary>
        public void Ping()
        {
            // Output token animation to visualise Ping
            var oldTop = Console.CursorTop;
            var oldLeft = Console.CursorLeft;
            Console.CursorVisible = false;

            var chars = "\\|/-";
            Console.SetCursorPosition(Console.WindowWidth - 1, oldTop - 1);
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

        #endregion

        #region Registry

        #endregion

        #region Network

        #endregion

    }
}
