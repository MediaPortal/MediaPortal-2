#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;

namespace MediaPortal.Common.Services.Dokan
{
    public interface IDokanOperations
    {
      NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info);

      void Cleanup(string fileName, DokanFileInfo info);

      void CloseFile(string fileName, DokanFileInfo info);

      NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info);

      NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten,long offset, DokanFileInfo info);

      NtStatus FlushFileBuffers(string fileName, DokanFileInfo info);

      NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info);

      NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info);

      NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, DokanFileInfo info);

      NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info);

      NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info);

      NtStatus DeleteFile(string fileName, DokanFileInfo info);

      NtStatus DeleteDirectory(string fileName, DokanFileInfo info);

      NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info);

      NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info);

      NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info);

      NtStatus LockFile(string fileName, long offset, long length, DokanFileInfo info);

      NtStatus UnlockFile(string fileName, long offset, long length, DokanFileInfo info);

      NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, DokanFileInfo info);

      NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, DokanFileInfo info);

      NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info);

      NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info);

      NtStatus Mounted(DokanFileInfo info);

      NtStatus Unmounted(DokanFileInfo info);

      NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info);
  }
}
