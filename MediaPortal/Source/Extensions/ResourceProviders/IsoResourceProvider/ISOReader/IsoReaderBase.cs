using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ISOReader
{
    /// <summary>
    /// Base class to read image files ISO9660
    /// </summary>
    public abstract class IsoReaderBase : IDisposable
    {
        /// <summary>
        /// Block size of the CDFS file system ISO9660.
        /// </summary>
        public const int ISO_SECTOR_SIZE = 2048;

        /// <summary>
        /// Dictionnary table Directories
        /// </summary>
        protected Dictionary<string, PathTableRecordPub> _tableRecords;

        /// <summary>
        /// Dictionary file entries
        /// </summary>
        protected Dictionary<string, RecordEntryInfo> _basicFilesInfo;

        private bool disposed;
        private string _discFile;
        private BinaryReader _baseBinaryReader;
        private FileStream _baseFileStream;
        private VolumeDescriptor primVolDesc;
        protected int _dataBeginSector = 0;
        protected int _sectorSize = 2048;
        private Object thisLock = new Object();             //Objet de vérouillage.       
        private AutoResetEvent _autoEvent;

        //GetFileSystemEntries.
        private List<string> _fsEntries;

        /// <summary>
        /// Gets or sets the size of a sector of the disk image
        /// Default is 2048 (ISO 9660).
        /// </summary>
        protected int SectorSize
        {
            get { return _sectorSize; }
            set { _sectorSize = value; }
        }

        /// <summary>
        /// Gets or sets the starting position of user data in sectors of the disk image
        /// Default is 0 (ISO 9660).
        /// </summary>
        protected int DataBeginSector
        {
            get { return _dataBeginSector; }
            set { _dataBeginSector = value; }
        }

        /// <summary>
        /// Obtient le chemin complet de l'image disque ouvert.
        /// </summary>
        protected string DiscFilename
        {
            get { return _discFile; }
        }

        /// <summary>
        /// Exposes a <see cref="System.IO.Stream"/> around the ISO9660 image file,
		    /// to provide access to the image stream.
        /// </summary>
        protected FileStream BaseFileStream
        {
            get { return _baseFileStream; }
            set { _baseFileStream = value; }
        }

        /// <summary>
        /// Reads primitive data types as binary values in the ISO9660 file.
        /// </summary>
        protected BinaryReader BaseBinaryReader
        {
            get { return _baseBinaryReader; }
            set { _baseBinaryReader = value; }
        }


        /// <summary>
        /// Reads the Volume Descriptor.
        /// </summary>
        /// <param name="discPath">Path of image file</param>
        /// <param name="rootDirLocation">Returns the root directory location of Volume</param>
        /// <param name="rootDirRecordLength">Returns the size of the structure of the root directory (34 bytes)</param>
        /// <returns>value <see cref=VolumeInfo/> representing the volume info ISO9660</returns>
        protected virtual VolumeInfo ReadVolumeDescriptor(string discPath,
            ref uint rootDirLocation, ref uint rootDirRecordLength)
        {
            this._discFile = discPath;

            //If file exist
            if (!File.Exists(discPath))
                throw new IOException(string.Format("{0} file not found", this._discFile));

            //Size of the structure of Volume Descriptor (2048 bytes)
            int istructsize = Marshal.SizeOf(typeof(VolumeDescriptor));
            GCHandle gch = new GCHandle();

            //Tables will contain the structure VolumeDesciptor
            byte[] bufTmp = new byte[_sectorSize];
            byte[] buf = new byte[_sectorSize];

            try
            {
                //Opens a stream on the ISO file read only
                _baseFileStream = new FileStream(discPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                _baseBinaryReader = new BinaryReader(_baseFileStream);

                //Move Primary Volume Descriptor structure
                _baseBinaryReader.BaseStream.Seek((16 * _sectorSize) + _dataBeginSector, SeekOrigin.Begin);

                //Search the latest Level Volume Descriptor and read it in buffer.
                while (bufTmp[0] != (int)VolumeType.VolumeDescriptorSetTerminator)
                {
                    _baseBinaryReader.Read(bufTmp, 0, (int)_sectorSize);

                    if (bufTmp[0] != 255)
                        Buffer.BlockCopy(bufTmp, 0, buf, 0, _sectorSize);
                }

                //Allocates a handle to the unmanaged space to transfer to VolumeDescriptor structure
                gch = GCHandle.Alloc(buf, GCHandleType.Pinned);
                //Gets its handle
                IntPtr ptrBuf = gch.AddrOfPinnedObject();

                //VolumeDescriptor reading
                primVolDesc = (VolumeDescriptor)Marshal.PtrToStructure(
                    ptrBuf, typeof(VolumeDescriptor));

                //Frees allocated to handle the transfer of the structure
                gch.Free();

                rootDirLocation = primVolDesc.RootDirectoryRecord.ExtentLocation;
                rootDirRecordLength = primVolDesc.RootDirectoryRecord.DataLength;
            }
            catch (IOException IOEx)
            {
                throw new IOException(IOEx.Message, IOEx);
            }
            catch (Exception)
            {
                throw new Exception("An error occured during reading the volume descriptor of the disc image file");
            }
            finally
            {
                if (gch != null && gch.IsAllocated)
                    gch.Free();

                CloseDiscImageFile();
            }

            return (VolumeInfo)primVolDesc;
        }

        /// <summary>
        /// Reads the directories
        /// </summary>
        /// <returns>Table <b>String"</b> table directories</returns>
        protected virtual string[] GetTable()
        {
            List<string> lTable = new List<string>();
            Dictionary<string, PathTableRecordPub> lTableRec = new Dictionary<string, PathTableRecordPub>();

            int iSizeof = Marshal.SizeOf(typeof(PathTableRecord));
            int iOffset = 0;

            //Reading sectors of the table directories
            this.BaseFileStream = new FileStream(this._discFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            this.BaseBinaryReader = new BinaryReader(this.BaseFileStream);

            // Do not care that the user data sectors of the table if the size of a sector is 2048 bytes
            byte[] bufTable = new byte[0];
            uint uiLPathTable = primVolDesc.TypeLPathTable;
            uint uiTableSize = this.primVolDesc.PathTableSize;

            if (this.SectorSize == ISO_SECTOR_SIZE)
            {
                bufTable = this.ReadSectors(uiLPathTable, uiTableSize);
            }
            else
            {
                bufTable = this.ReadSectors(uiLPathTable, Convert.ToUInt32(((uiTableSize / ISO_SECTOR_SIZE) + 1) * this.SectorSize));
                bufTable = this.CleanUserDataBuffer(bufTable, uiTableSize);
            }

            try
            {
                //Iteration on the table records
                while (iOffset > -1)
                {
                    byte[] bufRec = new byte[iSizeof];

                    Buffer.BlockCopy(bufTable, iOffset, bufRec, 0, bufRec.Length);

                    //Allocates a handle to the unmanaged space to transfer to the structure PrimaryVolumeDescriptor
                    GCHandle gch = GCHandle.Alloc(bufRec, GCHandleType.Pinned);
                    IntPtr ptrBuf = gch.AddrOfPinnedObject();

                    //Reading DirectoryRecord.
                    PathTableRecord r = (PathTableRecord)Marshal.PtrToStructure(
                        ptrBuf, typeof(PathTableRecord));

                    gch.Free();

                    PathTableRecordPub r2 = (PathTableRecordPub)r;

                    //Gets its name
                    byte[] bufName = new byte[r.Length];
                    Buffer.BlockCopy(bufTable, iOffset + 8, bufName, 0, bufName.Length);

                    if (primVolDesc.Type == 1)
                        r2.Name = ASCIIEncoding.Default.GetString(bufName);
                    else if (primVolDesc.Type == 2)
                    {
                        if ((r2.Number % 2) == 0)
                            r2.Name = ASCIIEncoding.BigEndianUnicode.GetString(bufName);
                        else
                            r2.Name = ASCIIEncoding.Default.GetString(bufName);
                    }

                    //If valid name
                    if (!string.IsNullOrEmpty(r2.Name))
                    {
                        //Then added to the list of paths
                        if (!r2.Name.Equals("\0"))
                        {
                            string szFullPath = Path.Combine(lTable[r.ParentNumber - 1], r2.Name);

                            lTable.Add(szFullPath);
                            lTableRec.Add(szFullPath, r2);
                        }
                        else
                        {
                            lTable.Add(@"\");
                            lTableRec.Add(@"\", r2);
                        }
                    }

                    iOffset += r.Length + 8;

                    if ((r2.Number % 2) > 0)
                        iOffset++;

                    if (r2.Number == 0)       //If no path leads to the following
                        iOffset++;

                    if (r2.ParentNumber == 0)     //If found = root path you pass to the next
                        iOffset++;

                    //When reading the table is finished we go out
                    if (iOffset >= uiTableSize)
                        break;
                }
            }
            catch (Exception ex)
            {
                string e = ex.Message;
            }

            _tableRecords = lTableRec;

            return lTable.ToArray();
        }

        /// <summary>
        /// Returns the names of all files and subdirectories in a specified directory
        /// </summary>
        /// <param name="path">Directory to which the names of files and subdirectories are returned</param>
        /// <param name="lba">Sector address of the entry <paramref name="path"/>.</param>
        /// <param name="length">Input size <paramref name="path"/>.</param>
        /// <param name="recursive">Specifies whether to search the current directory, or the current directory and all subdirectories</param>
        /// <param name="first"><b>True</b> for first call, <b>False</b> to call recursive</param>
        /// <returns>String array containing the names of file system entries in the directory specified</returns>
        protected virtual List<string> GetFileSystemEntries(string path, uint lba, uint length, bool recursive, bool first)
        {
            int iOffset = 0;
            uint uiExtent = 0, uiSize = 0;
            bool bFlgDir = false;
            bool bFlgHidden = false;
            int iNameLength = 0;
            string szName = "";
            List<RecordEntryInfo> lstDir = new List<RecordEntryInfo>();

            // Reset the list of inputs + set search directory.
            if (first)
            {
                _fsEntries.Clear();
                _basicFilesInfo.Clear();
            }

            try
            {
                //Total length of the entries contained in path
                uint uiDataLength = GetDataLengthDirectoryEntry(path, ref uiExtent);

                //Only keeps the user data sectors of the table if the size of a sector is not 2048 bytes
                byte[] bufData = new byte[0];
                if (_sectorSize == ISO_SECTOR_SIZE)
                {
                    bufData = this.ReadSectors(uiExtent, uiDataLength);
                }
                else
                {
                    bufData = this.ReadSectors(uiExtent, Convert.ToUInt32((uiDataLength / ISO_SECTOR_SIZE) * _sectorSize));
                    bufData = this.CleanUserDataBuffer(bufData, uiDataLength);
                }

                while (iOffset > -1)
                {
                    int iRecordLength = bufData[iOffset];      //Size of the structure

                    //If contains data
                    if (iRecordLength > 0)
                    {
                        byte[] bufRecord = new byte[4];

                        //Extent - Starting position of the file
                        Buffer.BlockCopy(bufData, iOffset + DirectoryRecord.OFFSET_EXTENT_LOC, bufRecord, 0, 4);
                        uiExtent = BitConverter.ToUInt32(bufRecord, 0);

                        bufRecord = new byte[4];

                        //Size - Filesize
                        Buffer.BlockCopy(bufData, iOffset + DirectoryRecord.OFFSET_DATA_LENGTH, bufRecord, 0, 4);
                        uiSize = BitConverter.ToUInt32(bufRecord, 0);

                        bufRecord = new byte[7];

                        //Gets the date
                        Buffer.BlockCopy(bufData, iOffset + DirectoryRecord.OFFSET_REC_DATETIME, bufRecord, 0, 7);
                        DateTime dt = new DateTime(bufRecord[0] + 1900, bufRecord[1], bufRecord[2], bufRecord[3], bufRecord[4], bufRecord[5], DateTimeKind.Utc);

                        //Flag + hidden attribute directory
                        int iFlg = bufData[iOffset + DirectoryRecord.OFFSET_FILE_FLAGS];
                        bFlgDir = Convert.ToBoolean(iFlg & (int)FileFlags.Directory);
                        bFlgHidden = Convert.ToBoolean(iFlg & (int)FileFlags.Hidden);

                        //Length of the path
                        iNameLength = bufData[iOffset + DirectoryRecord.OFFSET_FILEID_LEN];

                        //Get the name of the file path
                        if (primVolDesc.Type == 1)
                            szName = ASCIIEncoding.Default.GetString(bufData, iOffset + DirectoryRecord.OFFSET_FILEID_NAME, iNameLength);
                        else if (primVolDesc.Type == 2)
                        {
                            if ((iNameLength % 2) == 0)
                                szName = ASCIIEncoding.BigEndianUnicode.GetString(bufData, iOffset + DirectoryRecord.OFFSET_FILEID_NAME, iNameLength);
                            else
                                szName = ASCIIEncoding.Default.GetString(bufData, iOffset + DirectoryRecord.OFFSET_FILEID_NAME, iNameLength);
                        }

                        // If name is not empty and valid characters
                        if (!string.IsNullOrEmpty(szName) && (ASCIIEncoding.Default.GetBytes(szName.ToCharArray(), 0, 1)[0] > 1))
                        {
                            string szFullPath = Path.Combine(path, szName);
                            szFullPath = szFullPath.EndsWith(@";1") ? szFullPath.Remove(szFullPath.Length - 2) : szFullPath;

                            _fsEntries.Add(szFullPath);
                            _basicFilesInfo.Add(szFullPath, new RecordEntryInfo(uiExtent, uiSize, dt,
                                szName, szFullPath, bFlgHidden, bFlgDir));

                            if (recursive && bFlgDir)
                                lstDir.Add(new RecordEntryInfo(uiExtent, uiSize, dt, szName,
                                    szFullPath, bFlgHidden, bFlgDir));
                        }
                    }
                    else
                        //Goes to the next path
                        iRecordLength = 1;

                    iOffset += iRecordLength;     //Advance in the block of the table files for the following

                    //If the end of the table is reached
                    if (iOffset >= uiDataLength)
                        iOffset = -1;
                }

                bufData = null;

                if (recursive)
                {
                    // Iteration on each directory paths 
					// and read the file structures present in these paths
                    for (int j = 0; j < lstDir.Count; j++)
                    {
                        RecordEntryInfo brfiDir = lstDir[j];

                        //if (!brfiDir.Name.EndsWith(@"\"))
                        //    brfiDir.Name += @"\";

                        string sz = Path.Combine(path, brfiDir.Name);

                        GetFileSystemEntries(sz, brfiDir.Extent, brfiDir.Size, recursive, false);
                    }
                }

                return _fsEntries;
            }
            catch (Exception)
            {
                return _fsEntries;
            }
        }

        /// <summary>
        /// Reads the byte stream a specific number of bytes depending on the size of the file system disk image
        /// </summary>
        /// <param name="startOffset">Position in the iso file from which to read</param>
        /// <param name="dataLength">Number of bytes to read</param>
        /// <returns></returns>
        protected internal virtual byte[] ReadSectors(uint startOffset, uint dataLength)
        {
            this._baseBinaryReader.BaseStream.Seek(startOffset *  _sectorSize, SeekOrigin.Begin);

            byte[] buf = new byte[dataLength];
            this._baseBinaryReader.Read(buf, 0, (int)dataLength);

            return buf;
        }

      
      /*
        /// <summary>
        /// Reads and extracts a file contained in the disk image
        /// </summary>
        /// <param name="recordFileInfo">Registration information file in the disk image</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="mode">Playback Mode disk image file</param>
        /// <param name="format">Format the disk image source</param>
        /// <param name="recordIndex">Index file in the file table of the disk image source</param>
        protected void ReadFile(RecordEntryInfo recordFileInfo,
            string outputPath, DiscImageReadMode mode, ImageFileFormat? format, int? recordIndex)
        {
            lock (thisLock)
            {
                //Output file
                string szoutput = mode == DiscImageReadMode.EXTRACT_FILE ?
                    Path.Combine(outputPath, Path.GetFileName(recordFileInfo.Name)) :
                    Path.Combine(outputPath, recordFileInfo.FullPath.Substring(1));

                //Copy Info
                uint uiStart = recordFileInfo.Extent * (uint)_sectorSize;
                uint uiSize = recordFileInfo.Size;
                uint uiDelta = uiSize;

                //Mode file is opened and closes the disc image file
                //Mode disk image file is opened full and closes at the end of the reading of any files
                if (mode == DiscImageReadMode.EXTRACT_FILE)
                {
                    this._baseFileStream = new FileStream(this._discFile, FileMode.Open, FileAccess.Read, FileShare.Read, _sectorSize * 16);
                    this._baseBinaryReader = new BinaryReader(this._baseFileStream);
                }

                //Ouvre un flux en écriture sur le fichier de sortie.
                FileStream fsDst = new FileStream(szoutput, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, ISO_SECTOR_SIZE * 16);
                BinaryWriter bwDst = new BinaryWriter(fsDst);

                //Déplace le curseur au 1ier offset du fichier dans l'image.
                this._baseBinaryReader.BaseStream.Seek(uiStart, SeekOrigin.Begin);
                byte[] buf = new byte[0];

                //Lecture/écriture...
                do
                {
                    //Si l'état de l'objet d'évènement pour thread signalé on sort de la boucle.
                    //Si mode fichier oui, si mode disque cela n'est pas possible.
                    if (mode == DiscImageReadMode.EXTRACT_FILE && this._autoEvent.WaitOne(0, false))
                    {
                        if (this.Aborted != null)
                            this.Aborted(null, new Iso9660FileExtractEventArgs(recordFileInfo.FullPath, 0, 0, 0, szoutput, 0));

                        break;
                    }

                    uiDelta = uiSize - (uint)bwDst.BaseStream.Position;
                    if (uiDelta > 0)
                    {
                        //Ici, copie des données x32.
                        if (uiDelta > (ISO_SECTOR_SIZE * 16))
                        {
                            buf = new byte[_sectorSize * 16];
                            this._baseBinaryReader.Read(buf, 0, buf.Length);

                            if (_sectorSize == ISO_SECTOR_SIZE)
                                bwDst.Write(buf);
                            else
                                bwDst.Write(this.CleanUserDataBuffer(buf, ISO_SECTOR_SIZE * 16));
                        }
                        //Si données restante à écrire < 32ko c'est par ici...
                        else
                        {
                            if (_sectorSize == ISO_SECTOR_SIZE)
                            {
                                buf = new byte[uiDelta];
                                this._baseBinaryReader.Read(buf, 0, buf.Length);

                                bwDst.Write(buf);
                            }
                            else
                            {
                                buf = new byte[((uiDelta / ISO_SECTOR_SIZE) + 1) * _sectorSize];
                                this._baseBinaryReader.Read(buf, 0, buf.Length);

                                bwDst.Write(this.CleanUserDataBuffer(buf, uiDelta));
                            }
                        }

                        //Pousse les données du flux dans le fichier de sortie.
                        bwDst.Flush();

                        //Info progression.
                        if (this.Reading != null)
                        {
                            Iso9660FileExtractEventArgs dieEa = new Iso9660FileExtractEventArgs(recordFileInfo.FullPath,
                                uiStart, uiSize, uiSize - uiDelta, szoutput, recordIndex.Value + 1);
                            this.Reading(null, dieEa);
                        }
                    }
                } while (uiDelta > 0);

                //Ferme le fichier de sortie.
                bwDst.Close();
                fsDst.Close();
                fsDst.Dispose();

                //Inscrit la date d'origine sur le fichier extrait.
                File.SetLastWriteTimeUtc(szoutput, recordFileInfo.GetDate());
                //Attribut caché ?
                if (recordFileInfo.Hidden)
                    File.SetAttributes(szoutput, FileAttributes.Normal | FileAttributes.Hidden);
            }
        }
      */

        /// <summary>
        /// Ferme le fichier ouvert et les flux sous-jacents.
        /// </summary>
        protected void CloseDiscImageFile()
        {
            if (_baseBinaryReader != null)
                _baseBinaryReader.Close();

            if (_baseFileStream != null)
            {
                _baseFileStream.Close();
                _baseFileStream.Dispose();
            }
        }

        /// <summary>
        /// Initializes a new instance of the class DiscImageReader
        /// </summary>
        public IsoReaderBase()
        {
            _discFile = string.Empty;
            _fsEntries = new List<string>();
            _basicFilesInfo = new Dictionary<string, RecordEntryInfo>();
            _tableRecords = new Dictionary<string, PathTableRecordPub>();
            _autoEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Releases the resources used by <b> IsoReaderBase</b>
        /// </summary>
        ///<remarks>Call the <b>Dispose</b> method once you've finished using <b>IsoReaderBase</b>.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the resources used by <b>IsoReaderBase</b>.
        /// </summary>
        /// <param name="disposing"><b>True</b> also to release the managed resources</param>
        ///<remarks>Call the <b>Dispose</b> method once you've finished using <b>IsoReaderBase</b>.</remarks>
        public void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    //Managed Resources
                    CloseDiscImageFile();
                    _autoEvent.Close();       
                }

                //To free unmanaged resources here
            }
            disposed = true;
        }

        /// <summary>
        /// Gets the length in bytes of the paths of entries (files / directories) children of a specified directory
        /// </summary>
        /// <param name="path">Directory to which the names of files and subdirectories are parties</param>
        /// <param name="lba">Sector address of the entry <paramref name="path"/>.</param>
        /// <returns>An UInt32 integer with length of child entries</returns>
        private uint GetDataLengthDirectoryEntry(string path, ref uint lba)
        {
            string szParent = System.IO.Path.GetDirectoryName(path);
            PathTableRecordPub recParent = (path == @"\") ? _tableRecords[@"\"] : _tableRecords[szParent];
            PathTableRecordPub recPath = _tableRecords[path];
            lba = recPath.ExtentLocation;

            int iSize = Marshal.SizeOf(typeof(DirectoryRecord));
            DirectoryRecord dirRec = new DirectoryRecord();
            byte[] buf = new byte[iSize];

            this._baseBinaryReader.BaseStream.Seek(recParent.ExtentLocation * _sectorSize, SeekOrigin.Begin);
            this._baseBinaryReader.BaseStream.Seek(_dataBeginSector, SeekOrigin.Current);

            while (true)
            {
                this._baseBinaryReader.Read(buf, 0, buf.Length);

                GCHandle gch = GCHandle.Alloc(buf, GCHandleType.Pinned);
                IntPtr pBuf = gch.AddrOfPinnedObject();

                dirRec = (DirectoryRecord)Marshal.PtrToStructure(pBuf, typeof(DirectoryRecord));

                gch.Free();

                if (dirRec.ExtentLocation > 0)
                {
                    if (dirRec.ExtentLocation == recPath.ExtentLocation)
                        return dirRec.DataLength;
                    else
                        this._baseBinaryReader.BaseStream.Seek(dirRec.Length - Marshal.SizeOf(dirRec), SeekOrigin.Current);
                }
                else
                {
                    this._baseBinaryReader.BaseStream.Seek(recPath.ExtentLocation * _sectorSize, SeekOrigin.Begin);
                    this._baseBinaryReader.BaseStream.Seek(_dataBeginSector, SeekOrigin.Current);

                    buf = new byte[iSize];
                    this._baseBinaryReader.Read(buf, 0, buf.Length);

                    GCHandle gch2 = GCHandle.Alloc(buf, GCHandleType.Pinned);
                    IntPtr pBuf2 = gch2.AddrOfPinnedObject();

                    dirRec = (DirectoryRecord)Marshal.PtrToStructure(pBuf2, typeof(DirectoryRecord));

                    gch2.Free();

                    return dirRec.DataLength;
                }
            }
        }

        /// <summary>
        /// Cleans the buffer containing the data sectors to keep only their data users.
        /// </summary>
        /// <param name="source">Original buffer</param>
        /// <param name="dataLength">Size of user data</param>
        /// <returns>A Byte array containing only user data sectors</returns>
        private byte[] CleanUserDataBuffer(byte[] source, uint dataLength)
        {
            byte[] bufDst = new byte[dataLength];

            int icount = 0;
            for (int i = 0; i < dataLength; i += ISO_SECTOR_SIZE)
            {
                if ((dataLength - i) >= ISO_SECTOR_SIZE)
                {
                    Buffer.BlockCopy(source, (icount * _sectorSize) + _dataBeginSector, bufDst,
                        icount * ISO_SECTOR_SIZE, ISO_SECTOR_SIZE);

                    icount++;
                }
                else
                {
                    Buffer.BlockCopy(source, (_sectorSize * icount) + _dataBeginSector, bufDst,
                        icount * ISO_SECTOR_SIZE, (int)(dataLength - i));
                }
            }

            return bufDst;
        }
    }
}