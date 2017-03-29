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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  class MovieCharacterRelationshipExtractor : IMovieRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { MovieAspect.ASPECT_ID, VideoAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { CharacterAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return MovieAspect.ROLE_MOVIE; }
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
      return GetCharacterSearchFilter(extractedAspects);
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, bool importOnly, out IList<RelationshipItem> extractedLinkedAspects)
    {
      extractedLinkedAspects = null;

      if (!MovieMetadataExtractor.IncludeCharacterDetails)
        return false;

      if (importOnly)
        return false;

      if (BaseInfo.IsVirtualResource(aspects))
        return false;

      MovieInfo movieInfo = new MovieInfo();
      if (!movieInfo.FromMetadata(aspects))
        return false;

      if (CheckCacheContains(movieInfo))
        return false;
       
      int count = 0;
      if (!MovieMetadataExtractor.SkipOnlineSearches)
      {
        OnlineMatcherService.Instance.UpdateCharacters(movieInfo, importOnly);
        count = movieInfo.Characters.Where(p => p.HasExternalId).Count();
        if (!movieInfo.IsRefreshed)
          movieInfo.HasChanged = true; //Force save to update external Ids for metadata found by other MDEs
      }
      else
      {
        count = movieInfo.Characters.Where(p => !string.IsNullOrEmpty(p.Name)).Count();
      }

      if (movieInfo.Characters.Count == 0)
        return false;

      if (BaseInfo.CountRelationships(aspects, LinkedRole) < count || (BaseInfo.CountRelationships(aspects, LinkedRole) == 0 && movieInfo.Characters.Count > 0))
        movieInfo.HasChanged = true; //Force save if no relationship exists

      if (!movieInfo.HasChanged)
        return false;

      AddToCheckCache(movieInfo);

      extractedLinkedAspects = new List<RelationshipItem>();
      foreach (CharacterInfo character in movieInfo.Characters)
      {
        character.AssignNameId();
        character.HasChanged = movieInfo.HasChanged;
        IDictionary<Guid, IList<MediaItemAspect>> characterAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        character.SetMetadata(characterAspects);

        if (characterAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        {
          Guid existingId;
          if (TryGetIdFromCache(character, out existingId))
            extractedLinkedAspects.Add(new RelationshipItem(characterAspects, existingId));
          else
            extractedLinkedAspects.Add(new RelationshipItem(characterAspects, Guid.Empty));
        }
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

      IEnumerable<string> characters = aspect.GetCollectionAttribute<string>(VideoAspect.ATTR_CHARACTERS);
      List<string> nameList = new List<string>(characters);

      index = nameList.IndexOf(name);
      return index >= 0;
    }

    public void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      CharacterInfo character = new CharacterInfo();
      character.FromMetadata(extractedAspects);
      AddToCache(extractedItemId, character);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
