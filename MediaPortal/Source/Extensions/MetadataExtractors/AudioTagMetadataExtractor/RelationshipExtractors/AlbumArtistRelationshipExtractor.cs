#region Copyright (C) 2007-2025 Team MediaPortal

/*
    Copyright (C) 2007-2025 Team MediaPortal
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
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor
{
  /// <summary>
  /// Links an album to the artists named on it.
  /// </summary>
  /// <remarks>
  /// The album's artists are the ones the track extractor put into the album's own aspect, which in
  /// turn came from the tags of the track the album was built from. This extractor exists so that the
  /// library can answer "which albums did this artist release?" without walking over the tracks.
  /// </remarks>
  internal class AlbumArtistRelationshipExtractor : IRelationshipRoleExtractor
  {
    #region Static fields

    private static readonly Guid[] ROLE_ASPECTS = { AudioAlbumAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { PersonAspect.ASPECT_ID };

    #endregion

    #region IRelationshipRoleExtractor implementation

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return AudioAlbumAspect.ROLE_ALBUM; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return PersonAspect.ROLE_ALBUMARTIST; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public Guid[] MatchAspects
    {
      get { return PersonInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(PersonAspect.ASPECT_ID))
        return null;
      return RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_PERSON);
    }

    public ICollection<string> GetExternalIdentifiers(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (!extractedAspects.ContainsKey(PersonAspect.ASPECT_ID))
        return new List<string>();
      return RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_PERSON);
    }

    public Task<bool> TryExtractRelationshipsAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> aspects, IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects)
    {
      AlbumInfo albumInfo = new AlbumInfo();
      if (!albumInfo.FromMetadata(aspects))
        return Task.FromResult(false);

      if (albumInfo.Artists.Count == 0)
        return Task.FromResult(false);

      foreach (PersonInfo person in albumInfo.Artists)
      {
        if (string.IsNullOrEmpty(person.Name))
          continue;
        person.Occupation = PersonAspect.OCCUPATION_ARTIST;
        IDictionary<Guid, IList<MediaItemAspect>> personAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        if (person.SetMetadata(personAspects) && personAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
          extractedLinkedAspects.Add(personAspects);
      }
      return Task.FromResult(extractedLinkedAspects.Count > 0);
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(PersonAspect.ASPECT_ID))
        return false;

      PersonInfo linkedPerson = new PersonInfo();
      if (!linkedPerson.FromMetadata(extractedAspects))
        return false;

      PersonInfo existingPerson = new PersonInfo();
      if (!existingPerson.FromMetadata(existingAspects))
        return false;

      return linkedPerson.Equals(existingPerson);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      if (!MediaItemAspect.TryGetAspect(linkedAspects, PersonAspect.Metadata, out SingleMediaItemAspect linkedAspect))
        return false;
      string name = linkedAspect.GetAttributeValue<string>(PersonAspect.ATTR_PERSON_NAME);

      if (!MediaItemAspect.TryGetAspect(aspects, AudioAlbumAspect.Metadata, out SingleMediaItemAspect aspect))
        return false;
      List<string> names = new List<string>(aspect.GetCollectionAttribute<string>(AudioAlbumAspect.ATTR_ARTISTS) ?? Enumerable.Empty<string>());

      index = names.FindIndex(existing => string.Equals(existing, name, StringComparison.InvariantCultureIgnoreCase));
      return index >= 0;
    }

    #endregion
  }
}
