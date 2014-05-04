#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.UiComponents.Media.Views;

namespace MediaPortal.Plugins.SlimTv.Client.MediaExtensions
{
  /// <summary>
  /// View which is based on a media library query that will "stack" MediaItems by their title.
  /// </summary>
  public class StackingViewSpecification : MediaLibraryQueryViewSpecification
  {
    #region Ctor

    public StackingViewSpecification(string viewDisplayName, IFilter filter,
        IEnumerable<Guid> necessaryMIATypeIDs, IEnumerable<Guid> optionalMIATypeIDs, bool onlyOnline) :
      base(viewDisplayName, filter, necessaryMIATypeIDs, optionalMIATypeIDs, onlyOnline)
    {
      SortedSubViews = true; // Stacking view has special sorting included.
    }

    #endregion

    protected override void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications)
    {
      base.ReLoadItemsAndSubViewSpecifications(out mediaItems, out subViewSpecifications);
      // Grouped display, nothing to do here
      if (subViewSpecifications.Count > 0)
        return;

      try
      {
        var mediaItemsResults = mediaItems;
        var lookup = mediaItemsResults.ToLookup(GetBestTitle, r => r).ToLookup(r => r.Count() == 1);
        // [true] --> Single media items
        // [false]--> Multi media items
        subViewSpecifications = new List<ViewSpecification>(0);
        var groupedItems = lookup[false].OrderByDescending(g => g.Max(r => GetBestDate(r)));
        foreach (IGrouping<string, MediaItem> group in groupedItems)
        {
          StackingSubViewSpecification subViewSpecification = new StackingSubViewSpecification(group.Key, NecessaryMIATypeIds, OptionalMIATypeIds, group.ToList());
          subViewSpecifications.Add(subViewSpecification);
        }
        // Only one item per group, so first takes the only one
        mediaItems = lookup[true].Select(group => group.First()).ToList();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("StackingViewSpecification.ReLoadItemsAndSubViewSpecifications: Error requesting server", e);
        mediaItems = null;
        subViewSpecifications = null;
      }
    }

    private string GetBestTitle(MediaItem mediaItem)
    {
      string name;
      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MovieAspect.ATTR_MOVIE_NAME, out name))
        return name;
      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_SERIESNAME, out name))
        return name;
      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MediaAspect.ATTR_TITLE, out name))
        return name;
      return string.Empty;
    }

    private DateTime GetBestDate(MediaItem mediaItem)
    {
      DateTime recordingDate;
      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, RecordingAspect.ATTR_ENDTIME, out recordingDate))
        return recordingDate;
      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MediaAspect.ATTR_RECORDINGTIME, out recordingDate))
        return recordingDate;
      return DateTime.MinValue;
    }
  }
}
