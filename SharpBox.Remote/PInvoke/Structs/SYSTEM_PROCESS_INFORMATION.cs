using System;

namespace SharpBox.Remote.PInvoke.Structs
{
    public struct SYSTEM_PROCESS_INFORMATION
    {
        public UInt32 NextEntryOffset;
        public UInt32 NumberOfThreads;
        public LARGE_INTEGER WorkingSetPrivateSize;
        public UInt32 HardFaultCount;
        public UInt32 NumberOfThreadsHighWatermark;
        public UInt64 CycleTime;
        public LARGE_INTEGER CreateTime;
        public LARGE_INTEGER UserTime;
        public LARGE_INTEGER KernelTime;
        public UNICODE_STRING ImageName;
        public Int32 BasePriority;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
        public UInt32 HandleCount;
        public UInt32 SessionId;
        public UIntPtr UniqueProcessKey;
        public UIntPtr PeakVirtualSize;
        public UIntPtr VirtualSize;
        public UInt32 PageFaultCount;
        public UIntPtr PeakWorkingSetSize;
        public UIntPtr WorkingSetSize;
        public UIntPtr QuotaPeakPagedPoolUsage;
        public UIntPtr QuotaPagedPoolUsage;
        public UIntPtr QuotaPeakNonPagedPoolUsage;
        public UIntPtr QuotaNonPagedPoolUsage;
        public UIntPtr PagefileUsage;
        public UIntPtr PeakPagefileUsage;
        public UIntPtr PrivatePageCount;
        public LARGE_INTEGER ReadOperationCount;
        public LARGE_INTEGER WriteOperationCount;
        public LARGE_INTEGER OtherOperationCount;
        public LARGE_INTEGER ReadTransferCount;
        public LARGE_INTEGER WriteTransferCount;
        public LARGE_INTEGER OtherTransferCount;







        //public Byte Reserved1;
        //public UNICODE_STRING ImageName;
        //public Int64 BasePriority;
        //public IntPtr UniqueProcessId;
        //public IntPtr Reserved2;
        //public UInt32 HandleCount;
        //public UInt32 SessionId;
        //public IntPtr Reserved3;
        //public UIntPtr PeakVirtualSize;
        //public UIntPtr VirtualSize;
        //public UInt32 Reserved4;
        //public UIntPtr PeakWorkingSetSize;
        //public UIntPtr WorkingSetSize;
        //public IntPtr Reserved5;
        //public UIntPtr QuotaPagedPoolUsage;
        //public IntPtr Reserved6;
        //public UIntPtr QuotaNonPagedPoolUsage;
        //public UIntPtr PagefileUsage;
        //public UIntPtr PeakPagefileUsage;
        //public UIntPtr PrivatePageCount;
        //public LARGE_INTEGER Reserved7;
    }
}
