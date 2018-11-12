using System;
using System.Collections.Generic;
using System.IO;
using SharpBox.Core.FileSystem;
using SharpBox.Core.Processes;
using SharpBox.Core.Sessions;

namespace SharpBox.Core
{
    public abstract class BaseSandbox : ISandbox
    {
        public String SharpBoxRoot { get; set; } = String.Empty;
        public String SandboxName { get; private set; } = String.Empty;
        public SandboxFileSystem FileSystem { get; private set; } = null;
        public List<ISession> Sessions { get; private set; } = new List<ISession>();

        public BaseSandbox(String sandboxName)
        {
            SandboxName = sandboxName;
            FileSystem = new SandboxFileSystem(Path.Combine(SharpBoxRoot, SandboxName, "FS"));
        }

        public void Test()
        {
            Session tmpSession = new Session();
            tmpSession.Processes.Add(new Process());
            Sessions.Add(tmpSession);
        }
    }
}
