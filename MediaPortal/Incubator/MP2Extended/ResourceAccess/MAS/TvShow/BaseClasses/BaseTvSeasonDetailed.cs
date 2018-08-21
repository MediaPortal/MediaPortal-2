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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using Microsoft.Owin;
using System;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{
  // TODO: Add more detailes
  class BaseTvSeasonDetailed : BaseTvSeasonBasic
  {
    internal WebTVSeasonDetailed TVSeasonDetailed(IOwinContext context, MediaItem item, Guid? showId)
    {
      WebTVSeasonBasic basic = TVSeasonBasic(context, item, showId);

      return new WebTVSeasonDetailed
      {
        Title = basic.Title,
        Id = basic.Id,
        ShowId = basic.ShowId,
        SeasonNumber = basic.SeasonNumber,
        EpisodeCount = basic.EpisodeCount,
        UnwatchedEpisodeCount = basic.UnwatchedEpisodeCount,
        DateAdded = basic.DateAdded,
        Year = basic.Year,
        IsProtected = basic.IsProtected,
        PID = basic.PID
      };
    }
  }
}
