using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem.BaseClasses
{
  internal class BaseFolderBasic
  {
    internal WebFolderBasic FolderBasic(DirectoryInfo folderInfo)
    {
      return new WebFolderBasic
      {
        Title = folderInfo.Name,
        Path = new List<string> { folderInfo.FullName },
        DateAdded = folderInfo.CreationTime,
        Id = Base64.Encode(folderInfo.FullName),
        LastAccessTime = folderInfo.LastAccessTime,
        LastModifiedTime = folderInfo.LastWriteTime
      };
    }
  }
}
