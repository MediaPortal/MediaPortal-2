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
  public class MediaLibraryPlaylistItem : MediaLibraryContainer, IDirectoryPlaylistItem
  {
    public MediaLibraryPlaylistItem(MediaItem item, EndPointSettings client)
      : base(item, null, null, null, client)
    {
    }

    public override string Class
    {
      get { return "object.item.playlistItem"; }
    }

    public IList<MediaItem> GetItems()
    {
      throw new NotImplementedException("Playlists don't work");
    }

    public IList<string> Artist { get; set; }

    public IList<string> Genre { get; set; }

    public string LongDescription { get; set; }

    public string StorageMedium { get; set; }

    public string Description { get; set; }

    public string Date { get; set; }

    public string Language { get; set; }

    public string RefId { get; set; }
  }
}
