using System.IO;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem.BaseClasses
{
  internal class BaseFilesystemItem
  {
    internal WebFilesystemItem FilesystemItem(FileInfo fileInfo)
    {
      return FilesystemItem(new BaseFileBasic().FileBasic(fileInfo));
    }

    internal WebFilesystemItem FilesystemItem(DirectoryInfo folderInfo)
    {
      return FilesystemItem(new BaseFolderBasic().FolderBasic(folderInfo));
    }

    internal WebFilesystemItem FilesystemItem(WebFilesystemItem item)
    {
      return new WebFilesystemItem
      {
        DateAdded = item.DateAdded,
        Id = item.Id,
        LastAccessTime = item.LastAccessTime,
        LastModifiedTime = item.LastModifiedTime,
        Path = item.Path,
        PID = item.PID,
        Title = item.Title,
        Type = item.Type
      };
    }
  }
}
