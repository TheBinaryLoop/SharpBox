using System;
using System.IO;
using SharpBox.Core.FileSystem;

namespace SharpBox.Core
{
    public abstract class BaseSandbox : ISandbox
    {
        public String SharpBoxRoot { get; set; } = String.Empty;
        public String SandboxName { get; private set; } = String.Empty;
        public SandboxFileSystem FileSystem { get; private set; } = null;

        public BaseSandbox(String sandboxName)
        {
            SandboxName = sandboxName;
            FileSystem = new SandboxFileSystem(Path.Combine(SharpBoxRoot, SandboxName, "FS"));
        }
    }
}
