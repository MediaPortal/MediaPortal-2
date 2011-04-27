using System;
using System.Runtime.InteropServices;

namespace ISOReader
{
   
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 2048)]
    internal struct VolumeDescriptor
    {
        [MarshalAs(UnmanagedType.I1)]
        public byte Type;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public char[] ID;

        [MarshalAs(UnmanagedType.I1)]
        public byte Version;

        [MarshalAs(UnmanagedType.I1)]
        public byte Unused1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] SystemID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] VolumeID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Unused2;

        [MarshalAs(UnmanagedType.U4)]
        public uint VolumeSpaceSize;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Unused3;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] Unused4;

        [MarshalAs(UnmanagedType.I2)]
        public short VolumeSetSize;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Unused5;

        [MarshalAs(UnmanagedType.I2)]
        public short VolumeSequenceNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Unused6;

        [MarshalAs(UnmanagedType.I2)]
        public short LogicalBlockSize;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Unused7;

        [MarshalAs(UnmanagedType.U4)]
        public uint PathTableSize;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Unused8;

        [MarshalAs(UnmanagedType.U4)]
        public uint TypeLPathTable;

        [MarshalAs(UnmanagedType.U4)]
        public uint OptTypeLPathTable;

        [MarshalAs(UnmanagedType.U4)]
        public uint TypeMPathTable;

        [MarshalAs(UnmanagedType.U4)]
        public uint OptTypeMPathTable;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 34)]
        public DirectoryRecord RootDirectoryRecord;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public char[] VolumeSetID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public char[] PublisherID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public char[] PreparerID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public char[] ApplicationID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 37)]
        public char[] CopyrightFileID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 37)]
        public char[] AbstractFileID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 37)]
        public char[] BibliographicFileID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public byte[] CreationDate;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public byte[] ModificationDate;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public byte[] ExpirationDate;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public byte[] EffectiveDate;

        [MarshalAs(UnmanagedType.I1)]
        public byte FileStructureVersion;

        public DateTime GetCreationDate()
        {
            return this.BytesToDateTime(CreationDate);
        }

        public DateTime GetModificationDate()
        {
            return this.BytesToDateTime(ModificationDate);
        }

        public DateTime GetExpirationDate()
        {
            return this.BytesToDateTime(ExpirationDate);
        }

        public DateTime GetEffectiveDate()
        {
            return this.BytesToDateTime(EffectiveDate);
        }

        private DateTime BytesToDateTime(byte[] date)
        {
            try
            {
                if (date != null && date.Length == 17)
                {
                    string szDateTime = System.Text.ASCIIEncoding.Default.GetString(date);

                    string szYear = szDateTime.Substring(0, 4);
                    string szMonth = szDateTime.Substring(4, 2);
                    string szDay = szDateTime.Substring(6, 2);
                    string szHour = szDateTime.Substring(8, 2);
                    string szMin = szDateTime.Substring(10, 2);
                    string szSec = szDateTime.Substring(12, 2);

                    return new DateTime(Convert.ToInt32(szYear), Convert.ToInt32(szMonth), Convert.ToInt32(szDay),
                        Convert.ToInt32(szHour), Convert.ToInt32(szMin), Convert.ToInt32(szSec));
                }
                else
                    return DateTime.MinValue;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        public static explicit operator VolumeInfo(VolumeDescriptor pvd)
        {
            char[] cTrim = new char[] { ' ' }; //, '\0' };

            string szID = new string(pvd.ID);
            string szSysID = new string(pvd.SystemID).Trim(cTrim).Replace("\0", string.Empty);
            string szVolID = new string(pvd.VolumeID).Trim(cTrim).Replace("\0", string.Empty);
            string szPubID = new string(pvd.PublisherID).Trim(cTrim).Replace("\0", string.Empty);
            string szPrepID = new string(pvd.PreparerID).Trim(cTrim).Replace("\0", string.Empty);
            string szAppID = new string(pvd.ApplicationID).Trim(cTrim).Replace("\0", string.Empty);
            string szCopyID = new string(pvd.CopyrightFileID).Trim(cTrim).Replace("\0", string.Empty);

            VolumeInfo dvi = new VolumeInfo((VolumeType)pvd.Type,
                szID, pvd.Version, szSysID, szVolID, pvd.VolumeSpaceSize,
                pvd.VolumeSequenceNumber, pvd.LogicalBlockSize, szPubID,
                szPrepID, szAppID, szCopyID, pvd.GetCreationDate(), pvd.GetModificationDate(),
                pvd.GetExpirationDate());

            return dvi;
        }
    }
}
