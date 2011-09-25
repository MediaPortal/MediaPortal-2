using System;

namespace ISOReader
{
    /// <summary>
    /// File flags enumeration. Used in the <see cref="DirectoryRecord"/>.
    /// Flag can be computed.
    /// </summary>
    [Flags()]
    internal enum FileFlags : byte
    {
        /// <summary>
        /// File is Hidden.
        /// </summary>
        Hidden = 1,

        /// <summary>
        /// Entry is a Directory.
        /// </summary>
        Directory = 2,

        /// <summary>
        /// Information is structured according to the extended attribute record if this bit is 1.
        /// </summary>
        AssociatedFile = 3,

        /// <summary>
        /// Owner, group and permissions are specified in the extended attribute record if this bit is 1.
        /// </summary>
        Record = 4,

        /// <summary>
        /// File is protected.
        /// </summary>
        Protection = 5,

        /// <summary>
        /// Reserved.
        /// </summary>
        Reserved = 6,

        /// <summary>
        /// Reserved.
        /// </summary>
        Reserved2 = 7,

        /// <summary>
        /// File has more than one directory record if this bit is 1.
        /// </summary>
        MultiExtent = 8
    }
}
