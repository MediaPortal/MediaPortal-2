#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Extensions.OnlineLibraries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeasonSeriesRelationshipExtractor : IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { SeasonAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { SeriesAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return SeasonAspect.ROLE_SEASON; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return SeriesAspect.ROLE_SERIES; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public Guid[] MatchAspects
    {
      get { return SeriesInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(SeriesAspect.ASPECT_ID))
        return null;
      return RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_SERIES);
    }

    public ICollection<string> GetExternalIdentifiers(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(SeriesAspect.ASPECT_ID))
        return new List<string>();
      return RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_SERIES);
    }

    public async Task<bool> TryExtractRelationshipsAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> aspects, IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects)
    {
      SeasonInfo seasonInfo = new SeasonInfo();
      if (!seasonInfo.FromMetadata(aspects))
        return false;

      SeriesInfo seriesInfo = RelationshipExtractorUtils.TryCreateInfoFromLinkedAspects(extractedLinkedAspects, out List<SeriesInfo> series) ?
        series[0] : seasonInfo.CloneBasicInstance<SeriesInfo>();

      if (!SeriesMetadataExtractor.SkipOnlineSearches)
        await OnlineMatcherService.Instance.UpdateSeriesAsync(seriesInfo, false).ConfigureAwait(false);

      if (seriesInfo.Genres.Count > 0)
      {
        IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
        foreach (var genre in seriesInfo.Genres)
        {
          if (!genre.Id.HasValue && converter.GetGenreId(genre.Name, GenreCategory.Series, null, out int genreId))
          {
            genre.Id = genreId;
            seriesInfo.HasChanged = true;
          }
        }
      }

      IDictionary<Guid, IList<MediaItemAspect>> seriesAspects = seriesInfo.LinkedAspects != null ?
        seriesInfo.LinkedAspects : new Dictionary<Guid, IList<MediaItemAspect>>();
      seriesInfo.SetMetadata(seriesAspects);

      if (aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        bool episodeVirtual = true;
        if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, false, out episodeVirtual))
          MediaItemAspect.SetAttribute(seriesAspects, MediaAspect.ATTR_ISVIRTUAL, episodeVirtual);
      }

      if (!seriesAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        return false;

      if (seriesInfo.LinkedAspects == null)
        extractedLinkedAspects.Add(seriesAspects);
      return true;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      return existingAspects.ContainsKey(SeriesAspect.ASPECT_ID);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      return MediaItemAspect.TryGetAttribute(aspects, SeasonAspect.ATTR_SEASON, out index);
    }
  }
}
