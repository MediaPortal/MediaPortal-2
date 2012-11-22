using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.MediaServer.ResourceAccess;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryAlbumArt : IDirectoryAlbumArt
  {
    private MediaItem Item { get; set; }

    public MediaLibraryAlbumArt(MediaItem item)
    {
      Item = item;
    }

    public void Initialise()
    {
      var url = MediaLibraryResource.GetBaseResourceURL()
                + DlnaResourceAccessUtils.GetResourceUrl(Item.MediaItemId)
                + "?aspect=THUMBNAILSMALL";

      Uri = url;
      ProfileId = "JPEG_TN";
    }    

    public string Uri { get; set; }

    public string ProfileId { get; set; }
    
  }
}
