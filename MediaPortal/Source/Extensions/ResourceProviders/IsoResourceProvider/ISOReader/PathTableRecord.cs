using System.Runtime.InteropServices;

namespace ISOReader
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct PathTableRecord
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte Length;

        [MarshalAs(UnmanagedType.U1)]
        public byte ExtAttrRecLength;

        [MarshalAs(UnmanagedType.U4)]
        public uint ExtentLocation;

        [MarshalAs(UnmanagedType.U2)]
        public ushort ParentNumber;

        public static explicit operator PathTableRecordPub(PathTableRecord record)
        {
            PathTableRecordPub rec = new PathTableRecordPub();

            rec.Number = record.Length;
            rec.Extent = record.ExtAttrRecLength;
            rec.ExtentLocation = record.ExtentLocation;
            rec.ParentNumber = record.ParentNumber;

            return rec;
        }
    }
}
