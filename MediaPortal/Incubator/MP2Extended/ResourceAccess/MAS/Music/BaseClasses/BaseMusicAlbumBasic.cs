#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.Music;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses
{
  internal class BaseMusicAlbumBasic
  {
    internal WebMusicAlbumBasic MusicAlbumBasic(MediaItem item)
    {
      MediaItemAspect albumAspect = MediaItemAspect.GetAspect(item.Aspects, AudioAlbumAspect.Metadata);
      var artists = (HashSet<object>)albumAspect[AudioAlbumAspect.ATTR_ARTISTS];

      IList<MultipleMediaItemAspect> genres;
      if (!MediaItemAspect.TryGetAspects(item.Aspects, GenreAspect.Metadata, out genres))
        genres = new List<MultipleMediaItemAspect>();

      //var composers = (HashSet<object>)albumAspect[AudioAlbumAspect.ATTR_COMPOSERS];

      return new WebMusicAlbumBasic
      {
        PID = 0,
        Id = item.MediaItemId.ToString(),
        Artists = artists.Cast<string>().ToList(),
        Genres = genres.Select(a => a.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)).ToList(),
        //Composer = composers.Cast<string>().ToList()
      };



    }
  }
}
