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
  internal class BaseFileBasic
  {
    internal WebFileBasic FileBasic(FileInfo fileInfo)
    {
      return new WebFileBasic
      {
        Title = fileInfo.Name,
        Path = new List<string> { fileInfo.FullName },
        DateAdded = fileInfo.CreationTime,
        Id = Base64.Encode(fileInfo.FullName),
        LastAccessTime = fileInfo.LastAccessTime,
        LastModifiedTime = fileInfo.LastWriteTime,
        Size = fileInfo.Length
      };
    }
  }
}
