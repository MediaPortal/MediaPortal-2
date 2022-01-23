// Source from: http://madreflection.originalcoder.com/2009/12/generic-tryparse.html

#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses
{
  class BaseMusicArtistDetailed : BaseMusicArtistBasic
  {
    internal static WebMusicArtistDetailed MusicArtistDetailed(MediaItem item)
    {
      WebMusicArtistBasic webMusicArtistBasic = MusicArtistBasic(item);

      MediaItemAspect personAspect = item.GetAspect(PersonAspect.Metadata);

      return new WebMusicArtistDetailed
      {
        HasAlbums = webMusicArtistBasic.HasAlbums,
        Id = webMusicArtistBasic.Id,
        Title = webMusicArtistBasic.Title,
        Artwork = webMusicArtistBasic.Artwork,
        Biography = personAspect.GetAttributeValue<string>(PersonAspect.ATTR_BIOGRAPHY),
      };
    }
  }
}
