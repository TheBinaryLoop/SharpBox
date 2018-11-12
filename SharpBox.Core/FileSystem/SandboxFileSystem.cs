using System;
using System.IO;

namespace SharpBox.Core.FileSystem
{
    public class SandboxFileSystem : BaseFileSystem
    {
        public SandboxFileSystem(String fsRootPath)
            : base(fsRootPath)
        {

        }

        public override void InitializeFS()
        {
            Directory.CreateDirectory("");
        }
    }
}
