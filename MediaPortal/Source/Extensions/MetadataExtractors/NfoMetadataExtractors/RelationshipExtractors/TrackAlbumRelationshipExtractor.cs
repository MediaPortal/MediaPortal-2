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
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  class TrackAlbumRelationshipExtractor : NfoAudioExtractorBase, IRelationshipRoleExtractor
  {
    #region Static fields

    private static readonly Guid[] ROLE_ASPECTS = { AudioAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { AudioAlbumAspect.ASPECT_ID };

    #endregion

    #region Protected methods

    /// <summary>
    /// Asynchronously tries to extract series metadata for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspectData">Dictionary of MediaItemAspect to update with metadata</param>
    /// <param name="reimport">During reimport only allow if nfo is for same media as this</param>
    /// <returns><c>true</c> if metadata was found and stored into the <paramref name="extractedAspectData"/>, else <c>false</c></returns>
    protected async Task<bool> TryExtractAlbumMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, AlbumInfo reimport)
    {
      NfoAlbumReader albumNfoReader = await TryGetNfoAlbumReaderAsync(mediaItemAccessor).ConfigureAwait(false);
      if(albumNfoReader != null)
      {
        if (reimport != null && !VerifyAlbumReimport(albumNfoReader, reimport))
          return false;

        return albumNfoReader.TryWriteMetadata(extractedAspectData);
      }
      return false;
    }

    #endregion

    #region IRelationshipRoleExtractor implementation

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return AudioAspect.ROLE_TRACK; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return AudioAlbumAspect.ROLE_ALBUM; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public Guid[] MatchAspects
    {
      get { return AlbumInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
        return null;
      return RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_ALBUM);
    }

    public ICollection<string> GetExternalIdentifiers(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
        return new List<string>();
      return RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_ALBUM);
    }

    public async Task<bool> TryExtractRelationshipsAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> aspects, IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects)
    {
      TrackInfo trackInfo = new TrackInfo();
      if (!trackInfo.FromMetadata(aspects))
        return false;

      AlbumInfo reimport = null;
      if (aspects.ContainsKey(ReimportAspect.ASPECT_ID))
        reimport = trackInfo.CloneBasicInstance<AlbumInfo>();

      IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData = extractedLinkedAspects.Count > 0 ?
        extractedLinkedAspects[0] : new Dictionary<Guid, IList<MediaItemAspect>>();
      if (!await TryExtractAlbumMetadataAsync(mediaItemAccessor, extractedAspectData, reimport).ConfigureAwait(false))
        return false;

      AlbumInfo albumInfo = new AlbumInfo();
      if (!albumInfo.FromMetadata(extractedAspectData))
        return false;

      IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
      foreach (var genre in albumInfo.Genres)
      {
        if (!genre.Id.HasValue && converter.GetGenreId(genre.Name, GenreCategory.Music, null, out int genreId))
          genre.Id = genreId;
      }
      albumInfo.SetMetadata(extractedAspectData);

      if (!extractedAspectData.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        return false;

      bool trackVirtual;
      if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, false, out trackVirtual))
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, trackVirtual);

      extractedLinkedAspects.Clear();
      extractedLinkedAspects.Add(extractedAspectData);
      return true;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (existingAspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
      {
        AlbumInfo extracted = new AlbumInfo();
        extracted.FromMetadata(extractedAspects);
        AlbumInfo existing = new AlbumInfo();
        existing.FromMetadata(existingAspects);

        return existing.Equals(extracted);
      }
      return false;
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      SingleMediaItemAspect aspect;
      if (!MediaItemAspect.TryGetAspect(aspects, AudioAspect.Metadata, out aspect))
        return false;

      int disc = aspect.GetAttributeValue<int>(AudioAspect.ATTR_DISCID);
      int track = aspect.GetAttributeValue<int>(AudioAspect.ATTR_TRACK);
      if (disc <= 0)
        disc = 1;

      index = disc * 1000 + track;
      return true;
    }

    #endregion
  }
}
