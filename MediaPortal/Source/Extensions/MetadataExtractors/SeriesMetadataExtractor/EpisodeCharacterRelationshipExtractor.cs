#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class EpisodeCharacterRelationshipExtractor : IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { VideoAspect.ASPECT_ID, EpisodeAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { CharacterAspect.ASPECT_ID };

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

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects, bool forceQuickMode)
    {
      extractedLinkedAspects = null;

      SingleMediaItemAspect videoAspect;
      if (!MediaItemAspect.TryGetAspect(aspects, VideoAspect.Metadata, out videoAspect))
        return false;

      IEnumerable<string> characters = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_CHARACTERS);

      // Build the character MI

      List<CharacterInfo> characterInfos = new List<CharacterInfo>();
      if (characters != null) foreach (string character in characters) characterInfos.Add(new CharacterInfo() { Name = character });

      EpisodeInfo episodeInfo;
      if (!SeriesRelationshipExtractor.GetBaseInfo(aspects, out episodeInfo))
        return false;

      SeriesTheMovieDbMatcher.Instance.UpdateEpisodeCharacters(episodeInfo, characterInfos);
      SeriesTvMazeMatcher.Instance.UpdateEpisodeCharacters(episodeInfo, characterInfos);
      SeriesTvDbMatcher.Instance.UpdateEpisodeCharacters(episodeInfo, characterInfos);

      if (characterInfos.Count == 0)
        return false;

      extractedLinkedAspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();

      foreach (CharacterInfo character in characterInfos)
      {
        IDictionary<Guid, IList<MediaItemAspect>> characterAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        extractedLinkedAspects.Add(characterAspects);
        character.SetMetadata(characterAspects);
      }
      return true;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(CharacterAspect.ASPECT_ID))
        return false;

      string linkedName;
      if (!MediaItemAspect.TryGetAttribute(linkedAspects, CharacterAspect.ATTR_CHARACTER_NAME, out linkedName))
        return false;

      string existingName;
      if (!MediaItemAspect.TryGetAttribute(existingAspects, CharacterAspect.ATTR_CHARACTER_NAME, out existingName))
        return false;

      return linkedName == existingName;
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, out int index)
    {
      return MediaItemAspect.TryGetAttribute(aspects, VideoAspect.ATTR_CHARACTERS, out index);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
