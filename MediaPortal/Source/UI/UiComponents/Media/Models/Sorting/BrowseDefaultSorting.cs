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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public class BrowseDefaultSorting : SortByTitle
  {
    protected IComparer<MediaItem> _audioComparer = new AudioSortByAlbumTrack();
    protected IComparer<MediaItem> _videoComparer = new SortByTitle();
    protected IComparer<MediaItem> _imageComparer = new SortByTitle();

    public BrowseDefaultSorting()
    {
      _includeMias = null;
      _excludeMias = null;
    }

    public override string DisplayName
    {
      get { return Consts.RES_SORTING_BROWSE_DEFAULT; }
    }

    public override string GroupByDisplayName
    {
      get { return String.Empty; }
    }

    public override int Compare(MediaItem x, MediaItem y)
    {
      SingleMediaItemAspect aspectX;
      SingleMediaItemAspect aspectY;

      // Check audio
      if (!MediaItemAspect.TryGetAspect(x.Aspects, AudioAspect.Metadata, out aspectX))
        aspectX = null;
      if (!MediaItemAspect.TryGetAspect(y.Aspects, AudioAspect.Metadata, out aspectY))
        aspectY = null;
      if (aspectX != null && aspectY != null)
        // Both are audio items - compare to each other
        return _audioComparer.Compare(x, y);
      if (aspectX != null || aspectY != null)
        // One of them is an audio item - order that item first
        return CompareDifferentTypes(aspectX, aspectY);
      // None of them is an audio item

      // Check video
      if (!MediaItemAspect.TryGetAspect(x.Aspects, VideoAspect.Metadata, out aspectX))
        aspectX = null;
      if (!MediaItemAspect.TryGetAspect(y.Aspects, VideoAspect.Metadata, out aspectY))
        aspectY = null;
      if (aspectX != null && aspectY != null)
        // Both are vido items - compare to each other
        return _videoComparer.Compare(x, y);
      if (aspectX != null || aspectY != null)
        // One of them is a video item - order that item first
        return CompareDifferentTypes(aspectX, aspectY);
      // None of them is a video item

      // Check image
      if (!MediaItemAspect.TryGetAspect(x.Aspects, ImageAspect.Metadata, out aspectX))
        aspectX = null;
      if (!MediaItemAspect.TryGetAspect(y.Aspects, ImageAspect.Metadata, out aspectY))
        aspectY = null;
      if (aspectX != null && aspectY != null)
        // Both are image items - compare to each other
        return _imageComparer.Compare(x, y);
      if (aspectX != null || aspectY != null)
        // One of them is an image item - order that item first
        return CompareDifferentTypes(aspectX, aspectY);
      // None of them is an image item

      // Fallback
      return base.Compare(x, y);
    }

    protected int CompareDifferentTypes(MediaItemAspect aspectX, MediaItemAspect aspectY)
    {
      return aspectX != null ? 1 : -1;
    }

    public override object GetGroupByValue(MediaItem item)
    {
      return null;
    }
  }
}
