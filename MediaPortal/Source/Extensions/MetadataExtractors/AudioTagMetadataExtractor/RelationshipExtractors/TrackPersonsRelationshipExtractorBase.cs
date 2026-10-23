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
  /// Common code for the role extractors that turn the artist names of an audio item into linked
  /// person items.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A tag names people in two places at most - the track artists and the album artists - and both
  /// reach the library the same way: the names are read back out of the aspects the tag extractor
  /// wrote, each name becomes a <see cref="PersonInfo"/>, and the library either finds the person with
  /// that name already or creates it. The two subclasses differ only in which of the two name
  /// collections they look at and which role the link gets.
  /// </para>
  /// <para>
  /// Persons are not looked up online. A person created here is a name and, when the tags carry them,
  /// a MusicBrainz id; biography and pictures stay empty until an online library is available.
  /// </para>
  /// </remarks>
  internal abstract class TrackPersonsRelationshipExtractorBase : IRelationshipRoleExtractor
  {
    #region Private fields

    private readonly Guid _linkedRole;
    private readonly MediaItemAspectMetadata.SingleAttributeSpecification _namesAttribute;
    private readonly string _occupation;
    private readonly bool _forAlbumArtist;

    #endregion

    #region Ctor

    /// <summary>
    /// </summary>
    /// <param name="linkedRole">Person role the link gets, e.g. <see cref="PersonAspect.ROLE_ARTIST"/>.</param>
    /// <param name="namesAttribute">Name collection to read, e.g. <see cref="AudioAspect.ATTR_ARTISTS"/>.</param>
    /// <param name="occupation">Occupation to give the persons, e.g. <see cref="PersonAspect.OCCUPATION_ARTIST"/>.</param>
    /// <param name="forAlbumArtist">If <c>true</c>, read the album artist frames when looking for MusicBrainz ids.</param>
    protected TrackPersonsRelationshipExtractorBase(Guid linkedRole, MediaItemAspectMetadata.SingleAttributeSpecification namesAttribute, string occupation, bool forAlbumArtist)
    {
      _linkedRole = linkedRole;
      _namesAttribute = namesAttribute;
      _occupation = occupation;
      _forAlbumArtist = forAlbumArtist;
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Returns the persons of the given item that this extractor links.
    /// </summary>
    protected abstract IList<PersonInfo> GetPersons(IDictionary<Guid, IList<MediaItemAspect>> aspects);

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
      get { return new[] { AudioAspect.ASPECT_ID }; }
    }

    public Guid LinkedRole
    {
      get { return _linkedRole; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return new[] { PersonAspect.ASPECT_ID }; }
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
      // A virtual track (a radio stream, a recording placeholder) is not proof that a person exists.
      if (BaseInfo.IsVirtualResource(aspects))
        return Task.FromResult(false);

      IList<PersonInfo> persons = GetPersons(aspects);
      if (persons == null || persons.Count == 0)
        return Task.FromResult(false);

      // The names alone give a person nothing to be recognised by a second time; the MusicBrainz ids
      // are in the file's tag, so they are read here and attached before the person is written.
      AudioTags.TryUpdateArtists(mediaItemAccessor, persons, _forAlbumArtist);

      foreach (PersonInfo person in persons)
      {
        person.Occupation = _occupation;
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

      if (!MediaItemAspect.TryGetAspect(aspects, AudioAspect.Metadata, out SingleMediaItemAspect aspect))
        return false;
      List<string> names = new List<string>(aspect.GetCollectionAttribute<string>(_namesAttribute) ?? Enumerable.Empty<string>());

      index = names.FindIndex(existing => string.Equals(existing, name, StringComparison.InvariantCultureIgnoreCase));
      return index >= 0;
    }

    #endregion
  }
}
