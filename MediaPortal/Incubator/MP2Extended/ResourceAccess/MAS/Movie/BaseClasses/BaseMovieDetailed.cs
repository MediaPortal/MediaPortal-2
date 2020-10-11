// Source from: http://madreflection.originalcoder.com/2009/12/generic-tryparse.html

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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.TranscodingService.Interfaces.MetaData;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie.BaseClasses
{
  class BaseMovieDetailed : BaseMovieBasic
  {
    internal static WebMovieDetailed MovieDetailed(MediaItem item)
    {
      WebMovieBasic webMovieBasic = MovieBasic(item);

      MediaItemAspect movieAspect = MediaItemAspectExtensions.GetAspect(item, (MediaItemAspectMetadata)MovieAspect.Metadata);
      MediaItemAspect videoAspect = MediaItemAspectExtensions.GetAspect(item, (MediaItemAspectMetadata)VideoAspect.Metadata);
      IList<MultipleMediaItemAspect> audioAspects;
      List<string> languages = new List<string>();
      if (MediaItemAspect.TryGetAspects(item.Aspects, VideoAudioStreamAspect.Metadata, out audioAspects))
      {
        foreach (MultipleMediaItemAspect audioAspect in audioAspects)
        {
          string language = audioAspect.GetAttributeValue<string>(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE);
          if (!string.IsNullOrEmpty(language) && !languages.Contains(language))
          {
            languages.Add(language);
          }
        }
      }

      WebMovieDetailed webMovieDetailed = new WebMovieDetailed
      {
        IsProtected = webMovieBasic.IsProtected,
        Type = webMovieBasic.Type,
        Watched = webMovieBasic.Watched,
        Runtime = webMovieBasic.Runtime,
        DateAdded = webMovieBasic.DateAdded,
        Id = webMovieBasic.Id,
        PID = webMovieBasic.PID,
        Title = webMovieBasic.Title,
        ExternalId = webMovieBasic.ExternalId,
        Rating = webMovieBasic.Rating,
        Year = webMovieBasic.Year,
        Actors = webMovieBasic.Actors,
        Genres = webMovieBasic.Genres,
        Path = webMovieBasic.Path,
        Artwork = webMovieBasic.Artwork,
        Tagline = movieAspect.GetAttributeValue<string>(MovieAspect.ATTR_TAGLINE) ?? string.Empty,
        Summary = videoAspect.GetAttributeValue<string>(VideoAspect.ATTR_STORYPLOT) ?? string.Empty,
        Language = string.Join(", ", languages.ToArray())
      };

      IEnumerable<string> aspectWriters = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_WRITERS);
      if (aspectWriters != null)
        webMovieDetailed.Writers = aspectWriters.Distinct().ToList();

      IEnumerable<string> aspectDirectors = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_DIRECTORS);
      if (aspectDirectors != null)
        webMovieDetailed.Directors = aspectDirectors.Distinct().ToList();

      return webMovieDetailed;
    }
  }
}
