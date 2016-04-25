using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using MediaPortal.Common.Services.Dokan;
using MediaPortal.Common.Services.Dokan.Native;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Dokan
{
    public class Proxy
    {

    public delegate NtStatus ZwCreateFileDelegate(
          [MarshalAs(UnmanagedType.LPWStr)] string rawFileName, IntPtr SecurityContext, uint rawDesiredAccess, uint rawFileAttributes,
          uint rawShareAccess, uint rawCreateDisposition, uint rawCreateOptions,
          [MarshalAs(UnmanagedType.LPStruct), In, Out] DokanFileInfo dokanFileInfo);
    // private DokanOperations operations_;
    //private ArrayList array_;
    //private Dictionary<ulong, DokanFileInfo> infoTable_;
    //private ulong infoId_ = 0;
    //private object infoTableLock_ = new object();
    //private DokanOptions options_;
    private readonly IDokanOperations operations;

    private readonly uint serialNumber;

    public Proxy(IDokanOperations operations)
        {
            this.operations = operations;
            serialNumber = (uint)this.operations.GetHashCode();
        }

    public NtStatus ZwCreateFileProxy(string rawFileName, IntPtr SecurityContext, uint rawDesiredAccess, uint rawFileAttributes, uint rawShareAccess, uint rawCreateDisposition, uint rawCreateOptions, DokanFileInfo dokanFileInfo)
    {
      try
      {
        FileOptions fileOptions = 0;
        FileAttributes fileAttributes = 0;
        int FileAttributesAndFlags = 0;
        int CreationDisposition = 0;
        NativeMethods.DokanMapKernelToUserCreateFileFlags(rawFileAttributes, rawCreateOptions, rawCreateDisposition, ref FileAttributesAndFlags, ref CreationDisposition);

        foreach (FileOptions fileOption in Enum.GetValues(typeof(FileOptions)))
        {
          if (((FileOptions)(FileAttributesAndFlags & 0xffffc000) & fileOption) == fileOption)
            fileOptions |= fileOption;
        }

        foreach (FileAttributes fileAttribute in Enum.GetValues(typeof(FileAttributes)))
        {
          if (((FileAttributes)(FileAttributesAndFlags & 0x3fff) & fileAttribute) == fileAttribute)
            fileAttributes |= fileAttribute;
        }

        NtStatus result = operations.CreateFile(rawFileName, (FileAccess)rawDesiredAccess, (FileShare)rawShareAccess, (FileMode)CreationDisposition, fileOptions, fileAttributes, dokanFileInfo);


        return result;
      }
      catch (Exception e)
      {
        Console.Error.WriteLine(e.ToString());
        return DokanResult.Unsuccessful;
      }
    }

   
    //private void ConvertFileInfo(ref DOKAN_FILE_INFO rawInfo, DokanFileInfo info)
    //{
    //    info.IsDirectory = rawInfo.IsDirectory == 1;
    //    info.ProcessId = rawInfo.ProcessId;
    //    info.PagingIo = rawInfo.PagingIo == 1;
    //    info.DeleteOnClose = rawInfo.DeleteOnClose == 1;
    //    info.SynchronousIo = rawInfo.SynchronousIo == 1;
    //    info.Nocache = rawInfo.Nocache == 1;
    //    info.WriteToEndOfFile = rawInfo.WriteToEndOfFile == 1;
    //}

    //private DokanFileInfo GetNewFileInfo(ref DOKAN_FILE_INFO rawFileInfo)
    //{
    //    DokanFileInfo fileInfo = new DokanFileInfo(rawFileInfo.DokanContext);

    //    lock (infoTableLock_)
    //    {
    //        fileInfo.InfoId = ++infoId_;

    //        rawFileInfo.Context = fileInfo.InfoId;
    //        ConvertFileInfo(ref rawFileInfo, fileInfo);
    //        // to avoid GC
    //        infoTable_[fileInfo.InfoId] = fileInfo;
    //    }
    //    return fileInfo;
    //}

    //private DokanFileInfo GetFileInfo(ref DOKAN_FILE_INFO rawFileInfo)
    //{
    //    DokanFileInfo fileInfo = null;
    //    lock (infoTableLock_)
    //    {
    //        if (rawFileInfo.Context != 0)
    //        {
    //            infoTable_.TryGetValue(rawFileInfo.Context, out fileInfo);
    //        }

    //        if (fileInfo == null)
    //        {
    //            // bug?
    //            fileInfo = new DokanFileInfo(rawFileInfo.DokanContext);
    //        }
    //        ConvertFileInfo(ref rawFileInfo, fileInfo);
    //    }
    //    return fileInfo;
    //}

    //private string GetFileName(IntPtr fileName)
    //{
    //    return Marshal.PtrToStringUni(fileName);
    //}


    //private const uint GENERIC_READ = 0x80000000;
    //private const uint GENERIC_WRITE = 0x40000000;
    //private const uint GENERIC_EXECUTE = 0x20000000;

    //private const uint FILE_READ_DATA = 0x0001;
    //private const uint FILE_READ_ATTRIBUTES = 0x0080;
    //private const uint FILE_READ_EA = 0x0008;
    //private const uint FILE_WRITE_DATA = 0x0002;
    //private const uint FILE_WRITE_ATTRIBUTES = 0x0100;
    //private const uint FILE_WRITE_EA = 0x0010;

    //private const uint FILE_SHARE_READ = 0x00000001;
    //private const uint FILE_SHARE_WRITE = 0x00000002;
    //private const uint FILE_SHARE_DELETE = 0x00000004;

    //private const uint CREATE_NEW = 1;
    //private const uint CREATE_ALWAYS = 2;
    //private const uint OPEN_EXISTING = 3;
    //private const uint OPEN_ALWAYS = 4;
    //private const uint TRUNCATE_EXISTING = 5;

    //private const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
    //private const uint FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
    //private const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
    //private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
    //private const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000;
    //private const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
    //private const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
    //private const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
    //private const uint FILE_ATTRIBUTE_TEMPORARY = 0x00000100;

    //public delegate long CreateFileDelegate(
    //    IntPtr rawFilName,
    //    uint rawAccessMode,
    //    uint rawShare,
    //    uint rawCreationDisposition,
    //    uint rawFlagsAndAttributes,
    //    ref DokanFileInfo dokanFileInfo);



    //internal ZwCreateFileDelegate ZwCreateFileProxy()
    //{
    //  throw new NotImplementedException();
    //}





    ////

    //public delegate long OpenDirectoryDelegate(
    //        IntPtr FileName,
    //        ref DOKAN_FILE_INFO FileInfo);

    //    public long OpenDirectoryProxy(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);

    //            DokanFileInfo info = GetNewFileInfo(ref rawFileInfo);
    //            return operations_.OpenDirectory(file, info);

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    ////

    //    public delegate long CreateDirectoryDelegate(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long CreateDirectoryProxy(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);

    //            DokanFileInfo info = GetNewFileInfo(ref rawFileInfo);
    //            return operations_.CreateDirectory(file, info);

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    ////

    //    public delegate long CleanupDelegate(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long CleanupProxy(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);
    //            return operations_.Cleanup(file, GetFileInfo(ref rawFileInfo));

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    ////

    //    public delegate long CloseFileDelegate(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long CloseFileProxy(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);
    //            DokanFileInfo info = GetFileInfo(ref rawFileInfo);

    //            long ret = operations_.CloseFile(file, info);

    //            rawFileInfo.Context = 0;

    //            lock (infoTableLock_)
    //            {
    //                infoTable_.Remove(info.InfoId);
    //            }
    //            return ret;

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    ////

    //    public delegate long ReadFileDelegate(
    //        IntPtr rawFileName,
    //        IntPtr rawBuffer,
    //        uint rawBufferLength,
    //        ref uint rawReadLength,
    //        long rawOffset,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long ReadFileProxy(
    //        IntPtr rawFileName,
    //        IntPtr rawBuffer,
    //        uint rawBufferLength,
    //        ref uint rawReadLength,
    //        long rawOffset,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);

    //            byte[] buf = new Byte[rawBufferLength];

    //            uint readLength = 0;
    //            long ret = operations_.ReadFile(
    //                file, buf, ref readLength, rawOffset, GetFileInfo(ref rawFileInfo));
    //            if (ret == 0)
    //            {
    //                rawReadLength = readLength;
    //                Marshal.Copy(buf, 0, rawBuffer, (int)rawBufferLength);
    //            }
    //            return ret;

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    ////

    //    public delegate long WriteFileDelegate(
    //        IntPtr rawFileName,
    //        IntPtr rawBuffer,
    //        uint rawNumberOfBytesToWrite,
    //        ref uint rawNumberOfBytesWritten,
    //        long rawOffset,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long WriteFileProxy(
    //        IntPtr rawFileName,
    //        IntPtr rawBuffer,
    //        uint rawNumberOfBytesToWrite,
    //        ref uint rawNumberOfBytesWritten,
    //        long rawOffset,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);

    //            Byte[] buf = new Byte[rawNumberOfBytesToWrite];
    //            Marshal.Copy(rawBuffer, buf, 0, (int)rawNumberOfBytesToWrite);

    //            uint bytesWritten = 0;
    //            long ret = operations_.WriteFile(
    //                file, buf, ref bytesWritten, rawOffset, GetFileInfo(ref rawFileInfo));
    //            if (ret == 0)
    //                rawNumberOfBytesWritten = bytesWritten;
    //            return ret;

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    ////

    //    public delegate long FlushFileBuffersDelegate(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long FlushFileBuffersProxy(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);
    //            long ret = operations_.FlushFileBuffers(file, GetFileInfo(ref rawFileInfo));
    //            return ret;

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    ////

    //    public delegate long GetFileInformationDelegate(
    //        IntPtr FileName,
    //        ref BY_HANDLE_FILE_INFORMATION HandleFileInfo,
    //        ref DOKAN_FILE_INFO FileInfo);

    //    public long GetFileInformationProxy(
    //        IntPtr rawFileName,
    //        ref BY_HANDLE_FILE_INFORMATION rawHandleFileInformation,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);

    //            FileInformation fi = new FileInformation();

    //            long ret = operations_.GetFileInformation(file, fi, GetFileInfo(ref rawFileInfo));

    //            if (ret == 0)
    //            {
    //                rawHandleFileInformation.dwFileAttributes = (uint)fi.Attributes;

    //                rawHandleFileInformation.ftCreationTime.dwHighDateTime =
    //                    (int)(fi.CreationTime.ToFileTime() >> 32);
    //                rawHandleFileInformation.ftCreationTime.dwLowDateTime =
    //                    (int)(fi.CreationTime.ToFileTime() & 0xffffffff);

    //                rawHandleFileInformation.ftLastAccessTime.dwHighDateTime =
    //                    (int)(fi.LastAccessTime.ToFileTime() >> 32);
    //                rawHandleFileInformation.ftLastAccessTime.dwLowDateTime =
    //                    (int)(fi.LastAccessTime.ToFileTime() & 0xffffffff);

    //                rawHandleFileInformation.ftLastWriteTime.dwHighDateTime =
    //                    (int)(fi.LastWriteTime.ToFileTime() >> 32);
    //                rawHandleFileInformation.ftLastWriteTime.dwLowDateTime =
    //                    (int)(fi.LastWriteTime.ToFileTime() & 0xffffffff);

    //                rawHandleFileInformation.nFileSizeLow =
    //                    (uint)(fi.Length & 0xffffffff);
    //                rawHandleFileInformation.nFileSizeHigh =
    //                    (uint)(fi.Length >> 32);
    //            }

    //            return ret;

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }

    //    }

    //    ////



    //    private delegate int FILL_FIND_DATA(
    //        ref WIN32_FIND_DATA rawFindData,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public delegate long FindFilesDelegate(
    //        IntPtr rawFileName,
    //        IntPtr rawFillFindData, // function pointer
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long FindFilesProxy(
    //        IntPtr rawFileName,
    //        IntPtr rawFillFindData, // function pointer
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);

    //            ArrayList files = new ArrayList();
    //            long ret = operations_.FindFiles(file, files, GetFileInfo(ref rawFileInfo));

    //            FILL_FIND_DATA fill = (FILL_FIND_DATA)Marshal.GetDelegateForFunctionPointer(
    //                rawFillFindData, typeof(FILL_FIND_DATA));

    //            if (ret == 0)
    //            {
    //                IEnumerator entry = files.GetEnumerator();
    //                while (entry.MoveNext())
    //                {
    //                    FileInformation fi = (FileInformation)(entry.Current);
    //                    WIN32_FIND_DATA data = new WIN32_FIND_DATA();
    //                    //ZeroMemory(&data, sizeof(WIN32_FIND_DATAW));

    //                    data.dwFileAttributes = fi.Attributes;

    //                    data.ftCreationTime.dwHighDateTime =
    //                        (int)(fi.CreationTime.ToFileTime() >> 32);
    //                    data.ftCreationTime.dwLowDateTime =
    //                        (int)(fi.CreationTime.ToFileTime() & 0xffffffff);

    //                    data.ftLastAccessTime.dwHighDateTime =
    //                        (int)(fi.LastAccessTime.ToFileTime() >> 32);
    //                    data.ftLastAccessTime.dwLowDateTime =
    //                        (int)(fi.LastAccessTime.ToFileTime() & 0xffffffff);

    //                    data.ftLastWriteTime.dwHighDateTime =
    //                        (int)(fi.LastWriteTime.ToFileTime() >> 32);
    //                    data.ftLastWriteTime.dwLowDateTime =
    //                        (int)(fi.LastWriteTime.ToFileTime() & 0xffffffff);

    //                    data.nFileSizeLow =
    //                        (uint)(fi.Length & 0xffffffff);
    //                    data.nFileSizeHigh =
    //                        (uint)(fi.Length >> 32);

    //                    data.cFileName = fi.FileName;

    //                    fill(ref data, ref rawFileInfo);
    //                }

    //            }
    //            return ret;

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }

    //    }

    //    ////

    //    public delegate long SetEndOfFileDelegate(
    //        IntPtr rawFileName,
    //        long rawByteOffset,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long SetEndOfFileProxy(
    //        IntPtr rawFileName,
    //        long rawByteOffset,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);

    //            return operations_.SetEndOfFile(file, rawByteOffset, GetFileInfo(ref rawFileInfo));

    //        }
    //        catch (Exception e)
    //        {

    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }


    //    public delegate long SetAllocationSizeDelegate(
    //        IntPtr rawFileName,
    //        long rawLength,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long SetAllocationSizeProxy(
    //        IntPtr rawFileName,
    //        long rawLength,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);

    //            return operations_.SetAllocationSize(file, rawLength, GetFileInfo(ref rawFileInfo));

    //        }
    //        catch (Exception e)
    //        {

    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }


    //  ////

    //    public delegate long SetFileAttributesDelegate(
    //        IntPtr rawFileName,
    //        uint rawAttributes,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long SetFileAttributesProxy(
    //        IntPtr rawFileName,
    //        uint rawAttributes,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);

    //            FileAttributes attr = (FileAttributes)rawAttributes;
    //            return operations_.SetFileAttributes(file, attr, GetFileInfo(ref rawFileInfo));

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    ////

    //    public delegate long SetFileTimeDelegate(
    //        IntPtr rawFileName,
    //        ref ComTypes.FILETIME rawCreationTime,
    //        ref ComTypes.FILETIME rawLastAccessTime,
    //        ref ComTypes.FILETIME rawLastWriteTime,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long SetFileTimeProxy(
    //        IntPtr rawFileName,
    //        ref ComTypes.FILETIME rawCreationTime,
    //        ref ComTypes.FILETIME rawLastAccessTime,
    //        ref ComTypes.FILETIME rawLastWriteTime,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);

    //            long time;

    //            time = ((long)rawCreationTime.dwHighDateTime << 32) + (uint)rawCreationTime.dwLowDateTime;
    //            DateTime ctime = DateTime.FromFileTime(time);

    //            if (time == 0)
    //                ctime = DateTime.MinValue;

    //            time = ((long)rawLastAccessTime.dwHighDateTime << 32) + (uint)rawLastAccessTime.dwLowDateTime;
    //            DateTime atime = DateTime.FromFileTime(time);

    //            if (time == 0)
    //                atime = DateTime.MinValue;

    //            time = ((long)rawLastWriteTime.dwHighDateTime << 32) + (uint)rawLastWriteTime.dwLowDateTime;
    //            DateTime mtime = DateTime.FromFileTime(time);

    //            if (time == 0)
    //                mtime = DateTime.MinValue;

    //            return operations_.SetFileTime(
    //                file, ctime, atime, mtime, GetFileInfo(ref rawFileInfo));

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    ////

    //    public delegate long DeleteFileDelegate(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long DeleteFileProxy(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);

    //            return operations_.DeleteFile(file, GetFileInfo(ref rawFileInfo));

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    ////

    //    public delegate long DeleteDirectoryDelegate(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long DeleteDirectoryProxy(
    //        IntPtr rawFileName,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);
    //            return operations_.DeleteDirectory(file, GetFileInfo(ref rawFileInfo));

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //   ////

    //    public delegate long MoveFileDelegate(
    //        IntPtr rawFileName,
    //        IntPtr rawNewFileName,
    //        int rawReplaceIfExisting,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long MoveFileProxy(
    //        IntPtr rawFileName,
    //        IntPtr rawNewFileName,
    //        int rawReplaceIfExisting,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);
    //            string newfile = GetFileName(rawNewFileName);

    //            return operations_.MoveFile(
    //                file, newfile, rawReplaceIfExisting != 0 ? true : false,
    //                GetFileInfo(ref rawFileInfo));

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    ////

    //    public delegate long LockFileDelegate(
    //        IntPtr rawFileName,
    //        long rawByteOffset,
    //        long rawLength,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long LockFileProxy(
    //        IntPtr rawFileName,
    //        long rawByteOffset,
    //        long rawLength,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);
    //            return operations_.LockFile(
    //                file, rawByteOffset, rawLength, GetFileInfo(ref rawFileInfo));

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //   ////

    //    public delegate long UnlockFileDelegate(
    //        IntPtr rawFileName,
    //        long rawByteOffset,
    //        long rawLength,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long UnlockFileProxy(
    //        IntPtr rawFileName,
    //        long rawByteOffset,
    //        long rawLength,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            string file = GetFileName(rawFileName);
    //            return operations_.UnlockFile(
    //                file, rawByteOffset, rawLength, GetFileInfo(ref rawFileInfo));

    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    ////

    //    public delegate long GetDiskFreeSpaceDelegate(
    //        ref long rawFreeBytesAvailable,
    //        ref long rawTotalNumberOfBytes,
    //        ref long rawTotalNumberOfFreeBytes,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long GetDiskFreeSpaceProxy(
    //        ref long rawFreeBytesAvailable,
    //        ref long rawTotalNumberOfBytes,
    //        ref long rawTotalNumberOfFreeBytes,
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            return operations_.GetDiskFreeSpace(
    //                ref rawFreeBytesAvailable,
    //                ref rawTotalNumberOfBytes,
    //                ref rawTotalNumberOfFreeBytes,
    //                GetFileInfo(ref rawFileInfo));
    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    public delegate long GetVolumeInformationDelegate(
    //        IntPtr rawVolumeNameBuffer,
    //        uint rawVolumeNameSize,
    //        ref uint rawVolumeSerialNumber,
    //        ref uint rawMaximumComponentLength,
    //        ref uint rawFileSystemFlags,
    //        IntPtr rawFileSystemNameBuffer,
    //        uint rawFileSystemNameSize,
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long GetVolumeInformationProxy(
    //        IntPtr rawVolumeNameBuffer,
    //        uint rawVolumeNameSize,
    //        ref uint rawVolumeSerialNumber,
    //        ref uint rawMaximumComponentLength,
    //        ref uint rawFileSystemFlags,
    //        IntPtr rawFileSystemNameBuffer,
    //        uint rawFileSystemNameSize,
    //        ref DOKAN_FILE_INFO FileInfo)
    //    {
    //        byte[] volume = System.Text.Encoding.Unicode.GetBytes(options_.VolumeLabel);
    //        Marshal.Copy(volume, 0, rawVolumeNameBuffer, Math.Min((int)rawVolumeNameSize, volume.Length));
    //        rawVolumeSerialNumber = 0x19831116;
    //        rawMaximumComponentLength = 256;

    //        // FILE_CASE_SENSITIVE_SEARCH | 
    //        // FILE_CASE_PRESERVED_NAMES |
    //        // FILE_UNICODE_ON_DISK
    //        rawFileSystemFlags = 7;

    //        byte[] sys = System.Text.Encoding.Unicode.GetBytes("DOKAN");
    //        Marshal.Copy(sys, 0, rawFileSystemNameBuffer, Math.Min((int)rawFileSystemNameSize, sys.Length));
    //        return 0;
    //    }


    //    public delegate long UnmountDelegate(
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long UnmountProxy(
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //            return operations_.Unmount(GetFileInfo(ref rawFileInfo));
    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.ToString());
    //            return -1;
    //        }
    //    }

    //    //public delegate long GetFileSecurityDelegate(
    //    //    IntPtr rawFileName,
    //    //    ref SECURITY_INFORMATION rawRequestedInformation,
    //    //    ref SECURITY_DESCRIPTOR rawSecurityDescriptor,
    //    //    uint rawSecurityDescriptorLength,
    //    //    ref uint rawSecurityDescriptorLengthNeeded,
    //    //    ref DOKAN_FILE_INFO rawFileInfo);

    //    //public long GetFileSecurity(
    //    //    IntPtr rawFileName,
    //    //    ref SECURITY_INFORMATION rawRequestedInformation,
    //    //    ref SECURITY_DESCRIPTOR rawSecurityDescriptor,
    //    //    uint rawSecurityDescriptorLength,
    //    //    ref uint rawSecurityDescriptorLengthNeeded,
    //    //    ref DOKAN_FILE_INFO rawFileInfo)
    //    //{
    //    //  return -1;
    //    //}

    //    //public delegate long SetFileSecurityDelegate(
    //    //    IntPtr rawFileName,
    //    //    ref SECURITY_INFORMATION rawSecurityInformation,
    //    //    ref SECURITY_DESCRIPTOR rawSecurityDescriptor,
    //    //    uint rawSecurityDescriptorLength,
    //    //    ref DOKAN_FILE_INFO rawFileInfo);

    //    //public long SetFileSecurity(
    //    //    IntPtr rawFileName,
    //    //    ref SECURITY_INFORMATION rawSecurityInformation,
    //    //    ref SECURITY_DESCRIPTOR rawSecurityDescriptor,
    //    //    ref uint rawSecurityDescriptorLengthNeeded,
    //    //    ref DOKAN_FILE_INFO rawFileInfo)
    //    //{
    //    //  return -1;
    //    //}


    //    public delegate long UnmountedDelegate(
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long UnmountedProxy(
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //          return operations_.Unmounted(GetFileInfo(ref rawFileInfo));
    //        }
    //        catch (Exception e)
    //        {
    //              Console.Error.WriteLine((e.ToString()));
    //              return -1;
    //        }
    //    }

    //    public delegate long MountedDelegate(
    //        ref DOKAN_FILE_INFO rawFileInfo);

    //    public long MountedProxy(
    //        ref DOKAN_FILE_INFO rawFileInfo)
    //    {
    //        try
    //        {
    //          return operations_.Mounted(GetFileInfo(ref rawFileInfo));
    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine((e.ToString()));
    //            return -1;
    //        }
    //    }


    }
}
