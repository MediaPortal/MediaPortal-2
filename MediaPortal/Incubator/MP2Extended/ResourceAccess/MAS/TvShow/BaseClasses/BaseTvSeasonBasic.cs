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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using Microsoft.Owin;
using MP2Extended.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{
  // TODO: Add more detailes
  class BaseTvSeasonBasic
  {
    internal static ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      SeasonAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      RelationshipAspect.ASPECT_ID
    };

    internal static ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
    };

    internal static WebTVSeasonBasic TVSeasonBasic(IOwinContext context, MediaItem item, Guid? showId = null)
    {
      Guid? user = ResourceAccessUtils.GetUser(context);
      ISet<Guid> necessaryMIATypespisodes = new HashSet<Guid>();
      necessaryMIATypespisodes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypespisodes.Add(EpisodeAspect.ASPECT_ID);

      IFilter unwatchedEpisodeFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And,
        new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeasonAspect.ROLE_SEASON, item.MediaItemId),
        new RelationalUserDataFilter(user.Value, UserDataKeysKnown.KEY_PLAY_PERCENTAGE, RelationalOperator.LT, UserDataKeysKnown.GetSortablePlayPercentageString(100), true));
    
      int unwatchedCount = MediaLibraryAccess.CountMediaItems(context, necessaryMIATypespisodes, unwatchedEpisodeFilter);

      GetShowId(item, ref showId);

      var mediaAspect = item.GetAspect(MediaAspect.Metadata);
      var seasonAspect = item.GetAspect(SeasonAspect.Metadata);
      var importerAspect = item.GetAspect(ImporterAspect.Metadata);

      DateTime? firstAired = mediaAspect.GetAttributeValue<DateTime?>(MediaAspect.ATTR_RECORDINGTIME);

      return new WebTVSeasonBasic
      {
        Title = mediaAspect.GetAttributeValue<string>(MediaAspect.ATTR_TITLE),
        Id = item.MediaItemId.ToString(),
        ShowId = showId.HasValue ? showId.Value.ToString() : null,
        SeasonNumber = seasonAspect.GetAttributeValue<int>(SeasonAspect.ATTR_SEASON),
        EpisodeCount = seasonAspect.GetAttributeValue<int>(SeasonAspect.ATTR_AVAILABLE_EPISODES),
        UnwatchedEpisodeCount = unwatchedCount,
        DateAdded = importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED),
        Year = firstAired.HasValue ? firstAired.Value.Year : 0,
      };
    }

    protected static void GetShowId(MediaItem item, ref Guid? showId)
    {
      if (showId.HasValue)
        return;

      IList<MediaItemAspect> relationshipAspects;
      if (!item.Aspects.TryGetValue(RelationshipAspect.ASPECT_ID, out relationshipAspects))
        return;

      showId = relationshipAspects.Where(ra =>
        ra.GetAttributeValue<Guid>(RelationshipAspect.ATTR_ROLE) == SeasonAspect.ROLE_SEASON &&
        ra.GetAttributeValue<Guid>(RelationshipAspect.ATTR_LINKED_ROLE) == SeriesAspect.ROLE_SERIES)
        .Select(ra => ra.GetAttributeValue<Guid?>(RelationshipAspect.ATTR_LINKED_ID))
        .FirstOrDefault();
    }
  }
}
