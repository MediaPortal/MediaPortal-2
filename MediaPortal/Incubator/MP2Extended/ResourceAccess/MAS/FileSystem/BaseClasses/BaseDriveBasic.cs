using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem.BaseClasses
{
  internal class BaseDriveBasic
  {
    internal List<WebDriveBasic> DriveBasic()
    {
      return DriveInfo.GetDrives().Select(x => new WebDriveBasic()
      {
        Id = Base64.Encode(x.RootDirectory.Name),
        Title = x.Name,
        Path = new List<string>() { x.RootDirectory.FullName },
        LastAccessTime = DateTime.Now,
        LastModifiedTime = DateTime.Now
      }).ToList();
    }
  }
}
