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
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  class EpisodeCharacterRelationshipExtractor : NfoSeriesExtractorBase, IRelationshipRoleExtractor
  {
    #region Static fields

    private static readonly Guid[] ROLE_ASPECTS = { EpisodeAspect.ASPECT_ID, VideoAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { CharacterAspect.ASPECT_ID };

    #endregion

    #region Protected methods

    /// <summary>
    /// Asynchronously tries to extract episode characters for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspects">List of MediaItemAspect dictionaries to update with metadata</param>
    /// <param name="season">Season number of the episode to update with metadata</param>
    /// <param name="episode">Episode number of the episode to update with metadata</param>
    /// <returns><c>true</c> if metadata was found and stored into the <paramref name="extractedAspects"/>, else <c>false</c></returns>
    protected async Task<bool> TryExtractEpisodeCharactersMetadataAsync(IResourceAccessor mediaItemAccessor, IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedAspects, int? season, int? episode, EpisodeInfo reimport)
    {
      NfoSeriesEpisodeReader episodeNfoReader = await TryGetNfoSeriesEpisodeReaderAsync(mediaItemAccessor, season, episode).ConfigureAwait(false);
      if (episodeNfoReader != null)
      {
        if (reimport != null && !VerifyEpisodeReimport(episodeNfoReader, reimport))
          return false;

        return episodeNfoReader.TryWriteCharacterMetadata(extractedAspects);
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
      get { return EpisodeAspect.ROLE_EPISODE; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return CharacterAspect.ROLE_CHARACTER; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public Guid[] MatchAspects
    {
      get { return CharacterInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(CharacterAspect.ASPECT_ID))
        return null;
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
        RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_CHARACTER),
        RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_PERSON));
    }

    public ICollection<string> GetExternalIdentifiers(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<string> identifiers = new List<string>();
      if (extractedAspects.ContainsKey(CharacterAspect.ASPECT_ID))
      {
        identifiers.AddRange(RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_CHARACTER));
        identifiers.AddRange(RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_PERSON));
      }
      return identifiers;
    }

    public async Task<bool> TryExtractRelationshipsAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> aspects, IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects)
    {
      EpisodeInfo episodeInfo = new EpisodeInfo();
      if (!episodeInfo.FromMetadata(aspects))
        return false;

      EpisodeInfo reimport = null;
      if (aspects.ContainsKey(ReimportAspect.ASPECT_ID))
        reimport = episodeInfo;

      int? season = episodeInfo.SeasonNumber;
      int? episode = episodeInfo.EpisodeNumbers != null && episodeInfo.EpisodeNumbers.Any() ? episodeInfo.EpisodeNumbers.First() : (int?)null;

      IList<IDictionary<Guid, IList<MediaItemAspect>>> nfoLinkedAspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();
      if (!await TryExtractEpisodeCharactersMetadataAsync(mediaItemAccessor, nfoLinkedAspects, season, episode, reimport).ConfigureAwait(false))
        return false;

      List<CharacterInfo> characters;
      if (!RelationshipExtractorUtils.TryCreateInfoFromLinkedAspects(nfoLinkedAspects, out characters))
        return false;

      characters = characters.Where(c => c != null && !string.IsNullOrEmpty(c.Name)).ToList();
      if (characters.Count == 0)
        return false;

      extractedLinkedAspects.Clear();      
      foreach (CharacterInfo character in characters)
      {
        if (character.SetLinkedMetadata() && character.LinkedAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
          extractedLinkedAspects.Add(character.LinkedAspects);
      }
      return extractedLinkedAspects.Count > 0;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(CharacterAspect.ASPECT_ID))
        return false;

      CharacterInfo linkedCharacter = new CharacterInfo();
      if (!linkedCharacter.FromMetadata(extractedAspects))
        return false;

      CharacterInfo existingCharacter = new CharacterInfo();
      if (!existingCharacter.FromMetadata(existingAspects))
        return false;

      return linkedCharacter.Equals(existingCharacter);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      SingleMediaItemAspect linkedAspect;
      if (!MediaItemAspect.TryGetAspect(linkedAspects, CharacterAspect.Metadata, out linkedAspect))
        return false;

      string name = linkedAspect.GetAttributeValue<string>(CharacterAspect.ATTR_CHARACTER_NAME);

      SingleMediaItemAspect aspect;
      if (!MediaItemAspect.TryGetAspect(aspects, VideoAspect.Metadata, out aspect))
        return false;

      IEnumerable<string> actors = aspect.GetCollectionAttribute<string>(VideoAspect.ATTR_CHARACTERS);
      List<string> nameList = new SafeList<string>(actors);

      index = nameList.IndexOf(name);
      return index >= 0;
    }

    #endregion
  }
}
