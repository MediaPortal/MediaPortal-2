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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.UiComponents.Media.Models.Sorting;
using MediaPortal.UiComponents.Media.Views;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.Utilities;

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
      base(viewDisplayName, filter, necessaryMIATypeIDs, optionalMIATypeIDs, onlyOnline, null)
    {
      SortedSubViews = true; // Stacking view has special sorting included.
      CustomItemsListSorting = SortByRecordingDate;
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
        var lookup = mediaItemsResults.ToLookup(SortByRecordingDateDesc.GetBestTitle, r => r).ToLookup(r => r.Count() == 1);
        // [true] --> Single media items
        // [false]--> Multi media items
        subViewSpecifications = new List<ViewSpecification>(0);
        var groupedItems = lookup[false].OrderByDescending(g => g.Max(r => SortByRecordingDateDesc.GetBestDate(r)));
        foreach (IGrouping<string, MediaItem> group in groupedItems)
        {
          StackingSubViewSpecification subViewSpecification = new StackingSubViewSpecification(group.Key, NecessaryMIATypeIds, OptionalMIATypeIds, group.OrderByDescending(SortByRecordingDateDesc.GetBestDate).ToList());
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

    public void SortByRecordingDate(ItemsList items, Sorting sorting)
    {
      var sorted = items.ToList();
      sorted.Sort((item1, item2) =>
      {
        PlayableMediaItem pmi1 = item1 as PlayableMediaItem;
        ViewItem vi1 = item1 as ViewItem;
        PlayableMediaItem pmi2 = item2 as PlayableMediaItem;
        ViewItem vi2 = item2 as ViewItem;

        MediaItem mi1 = pmi1 != null ? pmi1.MediaItem : (vi1 != null ? vi1.FirstMediaItem : null);
        MediaItem mi2 = pmi2 != null ? pmi2.MediaItem : (vi2 != null ? vi2.FirstMediaItem : null);
        return sorting.Compare(mi1, mi2);
      });
      items.Clear();
      CollectionUtils.AddAll(items, sorted);
    }
  }
}
