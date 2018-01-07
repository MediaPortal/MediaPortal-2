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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="PersonInfo"/> contains metadata information about a person item.
  /// </summary>
  public class PersonInfo : BaseInfo, IComparable<PersonInfo>
  {
    /// <summary>
    /// Contains the ids of the minimum aspects that need to be present in order to test the equality of instances of this item.
    /// </summary>
    public static Guid[] EQUALITY_ASPECTS = new[] { PersonAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID };
    /// <summary>
    /// Gets or sets the person IMDB id.
    /// </summary>
    public string ImdbId = null;
    /// <summary>
    /// Gets or sets the person TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public string MusicBrainzId = null;
    public long AudioDbId = 0;
    public int TvMazeId = 0;
    public int TvRageId = 0;
    public string NameId = null;

    /// <summary>
    /// Gets or sets the person name.
    /// </summary>
    public string Name = null;
    public string AlternateName = null;
    public SimpleTitle Biography = null;
    public string Orign = null;
    public DateTime? DateOfBirth = null;
    public DateTime? DateOfDeath = null;
    public string Occupation = null;
    public bool IsGroup = false;
    public int? Order = null;

    //Comparisson improvers
    public string ParentMediaName = null;
    public string MediaName = null;
  
    public override bool IsBaseInfoPresent
    {
      get
      {
        if (string.IsNullOrEmpty(Name))
          return false;
        if (string.IsNullOrEmpty(Occupation))
          return false;

        return true;
      }
    }

    public override bool HasExternalId
    {
      get
      {
        if (TvdbId > 0)
          return true;
        if (MovieDbId > 0)
          return true;
        if (AudioDbId > 0)
          return true;
        if (TvMazeId > 0)
          return true;
        if (TvRageId > 0)
          return true;
        if (!string.IsNullOrEmpty(MusicBrainzId))
          return true;
        if (!string.IsNullOrEmpty(ImdbId))
          return true;

        return false;
      }
    }

    public override void AssignNameId()
    {
      if (!string.IsNullOrEmpty(Name))
      {
        //Give the person a fallback Id so it will always be created
        NameId = GetNameId(Name);
      }
    }

    public PersonInfo Clone()
    {
      return CloneProperties(this);
    }

    #region Members

    /// <summary>
    /// Copies the contained person information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public override bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(Name)) return false;
      if (string.IsNullOrEmpty(Occupation)) return false;

      SetMetadataChanged(aspectData);

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, GetSortTitle(Name));
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_ISVIRTUAL, false); //Always based on physical media
      MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_PERSON_NAME, Name);
      if (!Biography.IsEmpty) MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_BIOGRAPHY, CleanString(Biography.Text));
      if (!string.IsNullOrEmpty(Orign)) MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_ORIGIN, Orign);
      MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_OCCUPATION, Occupation);

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_PERSON, ImdbId);
      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_PERSON, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_PERSON, MovieDbId.ToString());
      if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_PERSON, MusicBrainzId);
      if (AudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_PERSON, AudioDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_PERSON, TvMazeId.ToString());
      if (TvRageId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_PERSON, TvRageId.ToString());
      if (!string.IsNullOrEmpty(NameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_PERSON, NameId);

      if (DateOfBirth.HasValue) MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_DATEOFBIRTH, DateOfBirth.Value);
      if (DateOfDeath.HasValue) MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_DATEOFDEATH, DateOfDeath.Value);
      MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_GROUP, IsGroup);

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public override bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!aspectData.ContainsKey(PersonAspect.ASPECT_ID))
        return false;

      GetMetadataChanged(aspectData);

      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_PERSON_NAME, out Name);
      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_ORIGIN, out Orign);
      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_OCCUPATION, out Occupation);

      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_DATEOFBIRTH, out DateOfBirth);
      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_DATEOFDEATH, out DateOfDeath);
      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_GROUP, out IsGroup);

      string tempString;
      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_BIOGRAPHY, out tempString);
      Biography = new SimpleTitle(tempString, false);

      string id;
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_PERSON, out id))
        MovieDbId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_PERSON, out id))
        TvdbId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_PERSON, out id))
        AudioDbId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_PERSON, out id))
        TvMazeId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_PERSON, out id))
        TvRageId = Convert.ToInt32(id);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_PERSON, out ImdbId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_PERSON, out MusicBrainzId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_PERSON, out NameId);

      byte[] data;
      if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
        HasThumbnail = true;

      return true;
    }

    public override bool FromString(string name)
    {
      Name = name;
      return true;
    }

    public override bool CopyIdsFrom<T>(T otherInstance)
    {
      if (otherInstance == null)
        return false;

      if (otherInstance is PersonInfo)
      {
        PersonInfo otherPerson = otherInstance as PersonInfo;
        MovieDbId = otherPerson.MovieDbId;
        ImdbId = otherPerson.ImdbId;
        AudioDbId = otherPerson.AudioDbId;
        MusicBrainzId = otherPerson.MusicBrainzId;
        TvdbId = otherPerson.TvdbId;
        TvMazeId = otherPerson.TvMazeId;
        TvRageId = otherPerson.TvRageId;
        NameId = otherPerson.NameId;
        return true;
      }
      return false;
    }

    public override T CloneBasicInstance<T>()
    {
      if (typeof(T) == typeof(PersonInfo))
      {
        PersonInfo info = new PersonInfo();
        info.CopyIdsFrom(this);
        info.Name = Name;
        info.Occupation = Occupation;
        return (T)(object)info;
      }
      return default(T);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return string.IsNullOrEmpty(Name) ? "[Unnamed Person]" : Name;
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      if (string.IsNullOrEmpty(NameId))
        AssignNameId();
      return string.IsNullOrEmpty(NameId) ? "[Unnamed Person]".GetHashCode() : NameId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      PersonInfo other = obj as PersonInfo;
      if (other == null) return false;

      if (TvdbId > 0 && other.TvdbId > 0 && Occupation == other.Occupation)
        return TvdbId == other.TvdbId;
      if (MovieDbId > 0 && other.MovieDbId > 0 && Occupation == other.Occupation)
        return MovieDbId == other.MovieDbId;
      if (AudioDbId > 0 && other.AudioDbId > 0 && Occupation == other.Occupation)
        return AudioDbId == other.AudioDbId;
      if (TvMazeId > 0 && other.TvMazeId > 0 && Occupation == other.Occupation)
        return TvMazeId == other.TvMazeId;
      if (TvRageId > 0 && other.TvRageId > 0 && Occupation == other.Occupation)
        return TvRageId == other.TvRageId;
      if (!string.IsNullOrEmpty(MusicBrainzId) && !string.IsNullOrEmpty(other.MusicBrainzId) && Occupation == other.Occupation)
        return string.Equals(MusicBrainzId, other.MusicBrainzId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(ImdbId) && !string.IsNullOrEmpty(other.ImdbId) && Occupation == other.Occupation)
        return string.Equals(ImdbId, other.ImdbId, StringComparison.InvariantCultureIgnoreCase);

      //Name id is generated from name and can be unreliable so should only be used if matches
      if (!string.IsNullOrEmpty(NameId) && !string.IsNullOrEmpty(other.NameId) &&
        string.Equals(NameId, other.NameId, StringComparison.InvariantCultureIgnoreCase))
        return true;

      if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && StrictMatchNames(Name, other.Name) && Occupation == other.Occupation)
        return true;
      if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.AlternateName) && StrictMatchNames(Name, other.AlternateName) && Occupation == other.Occupation)
        return true;

      //More lax checking if media is the same
      if (!string.IsNullOrEmpty(ParentMediaName) && !string.IsNullOrEmpty(other.ParentMediaName) && MatchNames(ParentMediaName, other.ParentMediaName) &&
        !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && MatchNames(Name, other.Name) && Occupation == other.Occupation)
        return true;
      if (!string.IsNullOrEmpty(MediaName) && !string.IsNullOrEmpty(other.MediaName) && MatchNames(MediaName, other.MediaName) &&
        !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && MatchNames(Name, other.Name) && Occupation == other.Occupation)
        return true;
      if (!string.IsNullOrEmpty(ParentMediaName) && !string.IsNullOrEmpty(other.ParentMediaName) && MatchNames(ParentMediaName, other.ParentMediaName) &&
        !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.AlternateName) && MatchNames(Name, other.AlternateName) && Occupation == other.Occupation)
        return true;
      if (!string.IsNullOrEmpty(MediaName) && !string.IsNullOrEmpty(other.MediaName) && MatchNames(MediaName, other.MediaName) &&
        !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.AlternateName) && MatchNames(Name, other.AlternateName) && Occupation == other.Occupation)
        return true;

      return false;
    }

    public bool StrictMatchNames(string name1, string name2)
    {
      //Artist and composer names can consist of multiple names which can cause false positives so matching should be more strict
      if (Occupation == PersonAspect.OCCUPATION_ARTIST || Occupation == PersonAspect.OCCUPATION_COMPOSER)
        return CompareNames(name1, name2, 0.8);

      return CompareNames(name1, name2);
    }

    public int CompareTo(PersonInfo other)
    {
      if (Order != other.Order)
      {
        if (!Order.HasValue) return 1;
        if (!other.Order.HasValue) return -1;
        return Order.Value.CompareTo(other.Order.Value);
      }

      return Name.CompareTo(other.Name);
    }

    #endregion
  }
}
