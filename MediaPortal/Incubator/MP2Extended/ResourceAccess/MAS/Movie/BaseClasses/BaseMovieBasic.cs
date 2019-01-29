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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Utilities;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie.BaseClasses
{
  class BaseMovieBasic
  {
    internal static ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID,
      VideoAspect.ASPECT_ID,
      MovieAspect.ASPECT_ID
    };

    internal static ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
      ExternalIdentifierAspect.ASPECT_ID,
      GenreAspect.ASPECT_ID
    };

    internal static WebMovieBasic MovieBasic(MediaItem item)
    {
      MediaItemAspect movieAspect = item.GetAspect(MovieAspect.Metadata);
      MediaItemAspect videoAspect = item.GetAspect(VideoAspect.Metadata);
      MediaItemAspect mediaAspect = item.GetAspect(MediaAspect.Metadata);
      MediaItemAspect importerAspect = item.GetAspect(ImporterAspect.Metadata);

      WebMovieBasic webMovieBasic = new WebMovieBasic
      {
        Title = movieAspect.GetAttributeValue<string>(MovieAspect.ATTR_MOVIE_NAME),
        Id = item.MediaItemId.ToString(),
        Type = WebMediaType.Movie,
        Path = ResourceAccessUtils.GetPaths(item),
        Year = mediaAspect.GetAttributeValue<DateTime>(MediaAspect.ATTR_RECORDINGTIME).Year,
        Runtime = movieAspect.GetAttributeValue<int>(MovieAspect.ATTR_RUNTIME_M),
        Watched = Convert.ToInt32(item.UserData.FirstOrDefault(d => d.Key == UserDataKeysKnown.KEY_PLAY_PERCENTAGE).Value ?? "0") >= 100,
        DateAdded = importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED),
        Rating = Convert.ToSingle(movieAspect.GetAttributeValue<double>(MovieAspect.ATTR_TOTAL_RATING)),
        Artwork = ResourceAccessUtils.GetWebArtwork(item),
      };

      IEnumerable<string> aspectActors = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_ACTORS);
      if (aspectActors != null)
        webMovieBasic.Actors = aspectActors.Distinct().Select(a => new WebActor(a)).ToList();

      IList<MediaItemAspect> genres;
      if (item.Aspects.TryGetValue(GenreAspect.ASPECT_ID, out genres))
        webMovieBasic.Genres = genres.Select(g => g.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)).ToList();

      string tmdbId;
      if (MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, out tmdbId) && tmdbId != null)
        webMovieBasic.ExternalId.Add(new WebExternalId { Site = "TMDB", Id = tmdbId });
      string imdbId;
      if (MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, out imdbId) && imdbId != null)
        webMovieBasic.ExternalId.Add(new WebExternalId { Site = "IMDB", Id = imdbId });

      return webMovieBasic;
    }
  }
}
