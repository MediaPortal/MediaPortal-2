using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MediaServer.Profiles;
using System;
using System.Collections.Generic;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryPlaylist : MediaLibraryContainer, IDirectoryPlaylistItem
  {
    public MediaLibraryPlaylist(string key, MediaItem item, EndPointSettings client)
      : base(key, item, client)
    {
    }

    public IList<string> Artist
    {
      get { throw new NotImplementedException(); }
      set { throw new NotImplementedException(); }
    }

    public IList<string> Genre
    {
      get { throw new NotImplementedException(); }
      set { throw new NotImplementedException(); }
    }

    public string LongDescription
    {
      get { throw new NotImplementedException(); }
      set { throw new NotImplementedException(); }
    }

    public string StorageMedium
    {
      get { throw new NotImplementedException(); }
      set { throw new NotImplementedException(); }
    }

    public string Description
    {
      get { throw new NotImplementedException(); }
      set { throw new NotImplementedException(); }
    }

    public string Date
    {
      get { throw new NotImplementedException(); }
      set { throw new NotImplementedException(); }
    }

    public string Language
    {
      get { throw new NotImplementedException(); }
      set { throw new NotImplementedException(); }
    }
  }
}
