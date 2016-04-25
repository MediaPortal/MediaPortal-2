using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;
using MediaPortal.Common.Services.Dokan;

namespace Dokan
{
    public interface IDokanOperations
    {
        NtStatus CreateFile(
                string filename,
                FileAccess access,
                FileShare share,
                FileMode mode,
                FileOptions options,
                FileAttributes attributes,
                DokanFileInfo info);

        long OpenDirectory(
                string filename,
                DokanFileInfo info);

        long CreateDirectory(
                string filename,
                DokanFileInfo info);

        long Cleanup(
                string filename,
                DokanFileInfo info);

        long CloseFile(
                string filename,
                DokanFileInfo info);

        long ReadFile(
                string filename,
                byte[] buffer,
                ref uint readBytes,
                long offset,
                DokanFileInfo info);
        
        long WriteFile(
                string filename,
                byte[] buffer,
                ref uint writtenBytes,
                long offset,
                DokanFileInfo info);

        long FlushFileBuffers(
                string filename,
                DokanFileInfo info);

        long GetFileInformation(
                string filename,
                FileInformation fileinfo,
                DokanFileInfo info);

        long FindFiles(
                string filename,
                ArrayList files,
                DokanFileInfo info);

        long SetFileAttributes(
                string filename,
                FileAttributes attr,
                DokanFileInfo info);

        long SetFileTime(
                string filename,
                DateTime ctime,
                DateTime atime,
                DateTime mtime,
                DokanFileInfo info);

        long DeleteFile(
                string filename,
                DokanFileInfo info);

        long DeleteDirectory(
                string filename,
                DokanFileInfo info);

        long MoveFile(
                string filename,
                string newname,
                bool replace,
                DokanFileInfo info);

        long SetEndOfFile(
                string filename,
                long length,
                DokanFileInfo info);

        long SetAllocationSize(
                string filename,
                long length,
                DokanFileInfo info);

        long LockFile(
                string filename,
                long offset,
                long length,
                DokanFileInfo info);

        long UnlockFile(
                string filename,
                long offset,
                long length,
                DokanFileInfo info);

        long GetDiskFreeSpace(
                ref long freeBytesAvailable,
                ref long totalBytes,
                ref long totalFreeBytes,
                DokanFileInfo info);

        long Unmount(
                DokanFileInfo info);

        long Mounted(
                DokanFileInfo info);

        long Unmounted(
                DokanFileInfo info);

    }
}
