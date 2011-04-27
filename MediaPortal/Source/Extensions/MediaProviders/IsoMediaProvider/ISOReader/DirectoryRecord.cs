using System;
using System.Runtime.InteropServices;

namespace ISOReader
{
    /// <summary>
    /// Represents a directory record structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct DirectoryRecord
    {
        /// <summary>
        /// Length of the directory Record structure <see cref="GomuLibrary.IO.DiscImage.DirectoryRecord"/>.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public byte Length;

        /// <summary>
        /// Extended Attribute Record Length.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public byte ExtentAttrLength;

        /// <summary>
        /// Location of Extent.
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public uint ExtentLocation;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Unused1;

        /// <summary>
        /// Data Length.
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public uint DataLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Unused2;

        /// <summary>
        /// Recording Date and Time.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] DateTimeRecord;

        /// <summary>
        /// File Flags.
        /// </summary>
        /// <remarks>See flags list <see cref="GomuLibrary.IO.DiscImage.FileFlags"/></remarks>
        [MarshalAs(UnmanagedType.I1)]
        public FileFlags Flags;

        /// <summary>
        /// File Unit Size.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public byte FileUnitSize;

        /// <summary>
        /// Interleave Gap Size.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public byte InterleaveGapSize;

        /// <summary>
        /// Volume Sequence Number.
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public ushort VolumeSequenceNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Unused3;

        /// <summary>
        /// Length of File Identifier <see cref="FileId"/>
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public byte FileIdLength;

        /// <summary>
        /// File Identifier.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public char[] FileId;

        /// <summary>
        /// Directory record length offset <see cref="Length"/>.
        /// </summary>
        public const int OFFSET_DIR_REC_LENGTH = 0;

        /// <summary>
        /// Extended Attribute Record Length offset <see cref="ExtentAttrLength"/>.
        /// </summary>
        public const int OFFSET_EXT_ATTR_LENGTH = 1;

        /// <summary>
        /// Location of Extent offset <see cref="ExtentLocation"/>.
        /// </summary>
        public const int OFFSET_EXTENT_LOC = 2;

        /// <summary>
        /// Data Length offset <see cref="DataLength"/>.
        /// </summary>
        public const int OFFSET_DATA_LENGTH = 10;

        /// <summary>
        /// Recording Date and Time offset <see cref="DateTimeRecord"/>.
        /// </summary>
        public const int OFFSET_REC_DATETIME = 18;

        /// <summary>
        /// File Flags offset <see cref="Flags"/>.
        /// </summary>
        public const int OFFSET_FILE_FLAGS = 25;

        /// <summary>
        /// File Unit Size offset <see cref="FileUnitSize"/>.
        /// </summary>
        public const int OFFSET_FILE_UNIT_SIZE = 26;

        /// <summary>
        /// Interleave Gap Size <see cref="InterleaveGapSize"/>.
        /// </summary>
        public const int OFFSET_INTERLEAVE = 27;

        /// <summary>
        /// Volume Sequence Number offset <see cref="VolumeSequenceNumber"/>.
        /// </summary>
        public const int OFFSET_VOLUME_SEQUENCE_NUMBER = 28;

        /// <summary>
        /// Length of File Identifier offset <see cref="FileIdLength"/>.
        /// </summary>
        public const int OFFSET_FILEID_LEN = 32;

        /// <summary>
        /// File Identifier offset <see cref="FileId"/>.
        /// </summary>
        public const int OFFSET_FILEID_NAME = 33;
    }
}
