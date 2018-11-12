using System;

namespace SharpBox.Core.FileSystem.Item
{
    public abstract class BaseFSItem : IFSItem
    {
        public String Name { get; private set; } = String.Empty;
        public ItemType Type { get; private set; }
        public ItemState State { get; private set; }

        public BaseFSItem(String name, ItemType itemType, ItemState itemState)
        {
            Name = name;
            Type = itemType;
            State = itemState;
        }
    }
}
