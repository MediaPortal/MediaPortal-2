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

using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  class EpisodeSeriesRelationshipExtractor : NfoSeriesExtractorBase, IRelationshipRoleExtractor
  {
    #region Static members

    private static readonly Guid[] ROLE_ASPECTS = { EpisodeAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { SeriesAspect.ASPECT_ID };

    #endregion

    #region Protected methods

    /// <summary>
    /// Asynchronously tries to extract series metadata for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspectData">Dictionary of MediaItemAspect to update with metadata</param>
    /// <returns><c>true</c> if metadata was found and stored into the <paramref name="extractedAspectData"/>, else <c>false</c></returns>
    protected async Task<bool> TryExtractSeriesMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      NfoSeriesReader seriesNfoReader = await TryGetNfoSeriesReaderAsync(mediaItemAccessor).ConfigureAwait(false);
      if (seriesNfoReader != null)
        return seriesNfoReader.TryWriteMetadata(extractedAspectData);
      return false;
    }

    #endregion

    #region IRelationshipRoleExtractor implementations

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return EpisodeAspect.ROLE_EPISODE; }
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
      IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData = extractedLinkedAspects.Count > 0 ?
        extractedLinkedAspects[0] : new Dictionary<Guid, IList<MediaItemAspect>>();

      if (!await TryExtractSeriesMetadataAsync(mediaItemAccessor, extractedAspectData).ConfigureAwait(false))
        return false;

      SeriesInfo seriesInfo = new SeriesInfo();
      if (!seriesInfo.FromMetadata(extractedAspectData))
        return false;

      IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
      foreach (var genre in seriesInfo.Genres)
      {
        if (!genre.Id.HasValue && converter.GetGenreId(genre.Name, GenreCategory.Series, null, out int genreId))
          genre.Id = genreId;
      }
      seriesInfo.SetMetadata(extractedAspectData);
      if (!extractedAspectData.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        return false;

      bool episodeVirtual;
      if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, false, out episodeVirtual))
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, episodeVirtual);

      extractedLinkedAspects.Clear();
      extractedLinkedAspects.Add(extractedAspectData);
      return true;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      return existingAspects.ContainsKey(SeriesAspect.ASPECT_ID);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      SingleMediaItemAspect linkedAspect;
      if (!MediaItemAspect.TryGetAspect(aspects, EpisodeAspect.Metadata, out linkedAspect))
        return false;

      int? season = linkedAspect.GetAttributeValue<int?>(EpisodeAspect.ATTR_SEASON);
      if (!season.HasValue)
        return false;

      IEnumerable<int> episodes = linkedAspect.GetCollectionAttribute<int>(EpisodeAspect.ATTR_EPISODE);
      List<int> episodeList = new SafeList<int>(episodes);

      index = season.Value * 1000 + episodeList.First();
      return index >= 0;
    }

    #endregion
  }
}
