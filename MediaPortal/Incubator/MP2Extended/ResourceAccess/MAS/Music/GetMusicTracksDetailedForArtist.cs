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
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MP2Extended.Extensions;
using Microsoft.Owin;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  internal class GetMusicTracksDetailedForArtist : BaseMusicTrackDetailed
  {
    public static Task<IList<WebMusicTrackDetailed>> ProcessAsync(IOwinContext context, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      if (id == null)
        throw new BadRequestException("GetMusicTracksDetailedForArtist: id is null");

      IList<MediaItem> tracks = MediaLibraryAccess.GetMediaItemsByGroup(context, AudioAspect.ROLE_TRACK, PersonAspect.ROLE_ARTIST, Guid.Parse(id), BasicNecessaryMIATypeIds, BasicOptionalMIATypeIds);
      if (tracks.Count == 0)
        throw new BadRequestException("No Tracks found");

      var output = tracks.Select(t => MusicTrackDetailed(t)).ToList();

      // sort
      if (sort != null && order != null)
      {
        output = output.SortWebMusicTrackBasic(sort, order).ToList();
      }

      // assing artists
      AssignArtists(context, output);

      return Task.FromResult<IList<WebMusicTrackDetailed>>(output);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
