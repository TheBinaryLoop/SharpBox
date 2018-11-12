using System;

namespace SharpBox.Core.Processes
{
    public class Process : IProcess
    {
        public UInt32 ProcessID { get; private set; }
        public String Executable { get; private set; }
        public Boolean HasParrent { get; private set; } = false;
        public IProcess ParrentProcess { get; private set; } = null;
        public Lazy<IProcess> ChildProcesses { get; private set; } = null;


    }
}
