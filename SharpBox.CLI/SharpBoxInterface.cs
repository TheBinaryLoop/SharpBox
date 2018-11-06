using System;

namespace SharpBox.CLI
{
    public class SharpBoxInterface : MarshalByRefObject
    {
        public void IsInstalled(Int32 InClientPID)
        {
            Console.WriteLine($"SharpBox has been installed intarget {InClientPID}.\r\n");
        }

        public void ReportException(Exception InInfo)
        {
            Console.WriteLine($"The target process has reported an error:\r\n{InInfo.ToString()}");
        }

        public void Ping() { } // TODO: Implement Ping

        #region FileSystem

        public void OnCreateFile(Int32 InClientPID, String[] InFileNames)
        {
            foreach (string filename in InFileNames)
            {
                Console.WriteLine(filename);
            }
        }

        #endregion

        #region Registry

        #endregion

        #region Network

        #endregion

    }
}
