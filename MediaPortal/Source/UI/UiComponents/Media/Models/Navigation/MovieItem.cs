#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core.Localization;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  public class MovieItem : PlayableMediaItem
  {
    public MovieItem(MediaItem mediaItem) : base(mediaItem)
    {
      MediaItemAspect videoAspect;
      if (mediaItem.Aspects.TryGetValue(VideoAspect.ASPECT_ID, out videoAspect))
      {
        long? duration = (long?) videoAspect[VideoAspect.ATTR_DURATION];
        SimpleTitle = Title;
        Duration = duration.HasValue ? FormattingUtils.FormatMediaDuration(TimeSpan.FromSeconds((int) duration.Value)) : string.Empty;
      }
    }

    public string Duration
    {
      get { return this[Consts.KEY_DURATION]; }
      set { SetLabel(Consts.KEY_DURATION, value); }
    }
  }
}