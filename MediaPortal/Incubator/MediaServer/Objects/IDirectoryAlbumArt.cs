using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPortal.Extensions.MediaServer.Objects
{
  public interface IDirectoryAlbumArt
  {
    [DirectoryProperty("")]
    string Uri { get; set; }

    [DirectoryProperty("@dlna:profileID", Required = false)]
    string ProfileId { get; set; }
  }
}
