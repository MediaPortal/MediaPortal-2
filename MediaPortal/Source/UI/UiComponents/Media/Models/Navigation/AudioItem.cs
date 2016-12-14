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

using System;
using System.Collections.Generic;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Utilities;
using MediaPortal.Common.MediaManagement.Helpers;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  public class AudioItem : PlayableMediaItem
  {
    public AudioItem(MediaItem mediaItem)
      : base(mediaItem)
    {
    }

    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);
      TrackInfo trackInfo = new TrackInfo();
      if (!trackInfo.FromMetadata(mediaItem.Aspects))
        return;

      Album = trackInfo.Album;
      string artists = StringUtils.Join(", ", trackInfo.Artists.Select(a => a.Name));
      SimpleTitle = Title + (string.IsNullOrEmpty(artists) ? string.Empty : (" (" + artists + ")"));
      
      FireChange();
    }

    public string Album
    {
      get { return this[Consts.KEY_ALBUM]; }
      set { SetLabel(Consts.KEY_ALBUM, value); }
    }
  }
}
