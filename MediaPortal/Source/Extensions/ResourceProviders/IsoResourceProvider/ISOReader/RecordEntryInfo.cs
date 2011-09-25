using System;

namespace ISOReader
{
    public struct RecordEntryInfo
    {
        public uint Extent;
        public uint Size;
        public DateTime Date;
        public string Name;
        public string FullPath;
        public bool Directory;
        public bool Hidden;

        public RecordEntryInfo(uint extent, uint size, DateTime date, string name, string fullPath,
            bool hidden, bool directory)
        {
            Extent = extent;
            Size = size;
            Date = date;
            Name = name;
            FullPath = fullPath;
            Hidden = hidden;
            Directory = directory;
        }
    }
}
