#region Copyright (C) 2007-2012 Team MediaPortal

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
using System.Linq;
using System.Text;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryAudioItem : MediaLibraryItem, IDirectoryAudioItem
  {
    public MediaLibraryAudioItem(string baseKey, MediaItem item) : base(baseKey, item)
    {
      Genre = new List<string>();
      Publisher = new List<string>();
      Rights = new List<string>();
      AlbumArtUrls = new List<IDirectoryAlbumArt>();

      var audioAspect = item.Aspects[AudioAspect.ASPECT_ID];
      var genreObj = audioAspect.GetCollectionAttribute(AudioAspect.ATTR_GENRES);
      if (genreObj != null) Genre.Add(genreObj.ToString());

      var resource = new MediaLibraryResource(item);
      resource.Initialise();
      Resources.Add(resource);

      if (item.Aspects.ContainsKey(ThumbnailSmallAspect.ASPECT_ID))
      {
        var albumArt = new MediaLibraryAlbumArt(item);
        albumArt.Initialise();
        AlbumArtUrls.Add(albumArt);        
      }      
    }

    public override string Class
    {
      get { return "object.item.audioItem"; }
    }

    public IList<string> Genre { get; set; }

    public string Description { get; set; }

    public string LongDescription { get; set; }

    public IList<string> Publisher { get; set; }

    public string Language { get; set; }

    public string Relation { get; set; }

    public IList<string> Rights { get; set; }

    public IList<IDirectoryAlbumArt> AlbumArtUrls { get; set; }
  }
}