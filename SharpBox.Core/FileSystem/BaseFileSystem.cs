using System;

namespace SharpBox.Core.FileSystem
{
    public abstract class BaseFileSystem : IFileSystem
    {
        public String RootDirectory { get; private set; } = String.Empty;

        public BaseFileSystem(String fsRootPath)
        {
            RootDirectory = fsRootPath;
        }

        public abstract void InitializeFS();
    }
}
