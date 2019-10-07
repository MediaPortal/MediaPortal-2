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

using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  internal class GetTVEpisodesDetailedForTVShow : BaseEpisodeDetailed
  {
    public static Task<IList<WebTVEpisodeDetailed>> ProcessAsync(IOwinContext context, string id, WebSortField? sort, WebSortOrder? order)
    {
      IList<MediaItem> episodes = MediaLibraryAccess.GetMediaItemsByGroup(context, EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, Guid.Parse(id), DetailedNecessaryMIATypeIds, DetailedOptionalMIATypeIds);

      var output = new List<WebTVEpisodeDetailed>();

      if (episodes.Count == 0)
        return Task.FromResult<IList<WebTVEpisodeDetailed>>(output);

      output.AddRange(episodes.Select(episode => EpisodeDetailed(episode, Guid.Parse(id))));

      // sort
      if (sort != null && order != null)
        output = output.SortWebTVEpisodeDetailed(sort, order).ToList();

      return Task.FromResult<IList<WebTVEpisodeDetailed>>(output);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
