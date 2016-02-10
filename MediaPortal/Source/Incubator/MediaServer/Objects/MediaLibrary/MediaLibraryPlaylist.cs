#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MediaServer.Profiles;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryPlaylist : MediaLibraryContainer, IDirectoryPlaylistItem
  {
    public MediaLibraryPlaylist(MediaItem item, EndPointSettings client)
      : base(item, null, null, null, client)
    {
    }

    public IList<MediaItem> GetItems()
    {
      throw new NotImplementedException("Playlists don't work");
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
