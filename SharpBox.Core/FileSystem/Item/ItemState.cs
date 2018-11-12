using System;

namespace SharpBox.Core.FileSystem.Item
{
    public enum ItemState : UInt32
    {
        External = 0x00000001,
        Created = 0x00000002,
        Deleted = 0x00000003,
        Blocked = 0x00000004
    }
}
