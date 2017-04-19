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
  /// <see cref="CharacterInfo"/> contains metadata information about a character item.
  /// </summary>
  public class CharacterInfo : BaseInfo, IComparable<CharacterInfo>
  {
    /// <summary>
    /// Contains the ids of the minimum aspects that need to be present in order to test the equality of instances of this item.
    /// </summary>
    public static Guid[] EQUALITY_ASPECTS = new[] { CharacterAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID };
    /// <summary>
    /// Gets or sets the character TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public int TvMazeId = 0;
    public string NameId = null;
    /// <summary>
    /// Gets or sets the character name.
    /// </summary>
    public string Name = null;
    public int? Order = null;

    /// <summary>
    /// Gets or sets the actor IMDB id.
    /// </summary>
    public string ActorImdbId = null;
    /// <summary>
    /// Gets or sets the actor TheTvDB id.
    /// </summary>
    public int ActorTvdbId = 0;
    public int ActorMovieDbId = 0;
    public int ActorTvMazeId = 0;
    public int ActorTvRageId = 0;
    public string ActorNameId = null;
    public string ActorName = null;

    //Comparisson improvers
    public string ParentMediaName = null;
    public string MediaName = null;

    public override bool IsBaseInfoPresent
    {
      get
      {
        if (string.IsNullOrEmpty(Name))
          return false;
        if (string.IsNullOrEmpty(ActorName))
          return false;

        return true;
      }
    }

    public override bool HasExternalId
    {
      get
      {
        if (ActorTvdbId > 0)
          return true;
        if (ActorMovieDbId > 0)
          return true;
        if (ActorTvMazeId > 0)
          return true;
        if (ActorTvRageId > 0)
          return true;
        if (!string.IsNullOrEmpty(ActorImdbId))
          return true;

        if (TvdbId > 0)
          return true;
        if (MovieDbId > 0)
          return true;
        if (TvMazeId > 0)
          return true;

        return false;
      }
    }

    public override void AssignNameId()
    {
      if (!string.IsNullOrEmpty(Name))
      {
        NameId = GetNameId(Name);
      }
      if (!string.IsNullOrEmpty(ActorName))
      {
        ActorNameId = GetNameId(ActorName);
      }
    }

    public CharacterInfo Clone()
    {
      return CloneProperties(this);
    }

    #region Members

    /// <summary>
    /// Copies the contained character information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public override bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(Name)) return false;

      SetMetadataChanged(aspectData);

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, GetSortTitle(Name));
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_ISVIRTUAL, false); //Always based on physical media
      MediaItemAspect.SetAttribute(aspectData, CharacterAspect.ATTR_CHARACTER_NAME, Name);
      if(!string.IsNullOrEmpty(ActorName)) MediaItemAspect.SetAttribute(aspectData, CharacterAspect.ATTR_ACTOR_NAME, ActorName);

      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_CHARACTER, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_CHARACTER, MovieDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_CHARACTER, TvMazeId.ToString());
      if (!string.IsNullOrEmpty(NameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_CHARACTER, NameId);

      if (ActorTvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_PERSON, ActorTvdbId.ToString());
      if (ActorMovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_PERSON, ActorMovieDbId.ToString());
      if (ActorTvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_PERSON, ActorTvMazeId.ToString());
      if (ActorTvRageId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_PERSON, ActorTvRageId.ToString());
      if (!string.IsNullOrEmpty(ActorImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_PERSON, ActorImdbId);
      if (!string.IsNullOrEmpty(ActorNameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_PERSON, ActorNameId);

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public override bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!aspectData.ContainsKey(CharacterAspect.ASPECT_ID))
        return false;

      GetMetadataChanged(aspectData);

      MediaItemAspect.TryGetAttribute(aspectData, CharacterAspect.ATTR_CHARACTER_NAME, out Name);
      MediaItemAspect.TryGetAttribute(aspectData, CharacterAspect.ATTR_ACTOR_NAME, out ActorName);

      string id;
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_CHARACTER, out id))
        TvdbId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_CHARACTER, out id))
        MovieDbId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_CHARACTER, out id))
        TvMazeId = Convert.ToInt32(id);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_CHARACTER, out NameId);

      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_PERSON, out id))
        ActorTvdbId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_PERSON, out id))
        ActorMovieDbId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_PERSON, out id))
        ActorTvMazeId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_PERSON, out id))
        ActorTvRageId = Convert.ToInt32(id);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_PERSON, out ActorImdbId);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_PERSON, out ActorNameId);

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

      if (otherInstance is CharacterInfo)
      {
        CharacterInfo otherCharacter = otherInstance as CharacterInfo;

        ActorImdbId = otherCharacter.ActorImdbId;
        ActorMovieDbId = otherCharacter.ActorMovieDbId;
        ActorTvdbId = otherCharacter.ActorTvdbId;
        ActorTvMazeId = otherCharacter.ActorTvMazeId;

        MovieDbId = otherCharacter.MovieDbId;
        TvdbId = otherCharacter.TvdbId;
        TvMazeId = otherCharacter.TvMazeId;
        NameId = otherCharacter.NameId;

        return true;
      }
      return false;
    }

    public override T CloneBasicInstance<T>()
    {
      if (typeof(T) == typeof(PersonInfo))
      {
        PersonInfo info = new PersonInfo();
        info.ImdbId = ActorImdbId;
        info.MovieDbId = ActorMovieDbId;
        info.TvdbId = ActorTvdbId;
        info.TvMazeId = ActorTvMazeId;
        info.TvRageId = ActorTvRageId;
        info.NameId = ActorNameId;

        info.Name = ActorName;
        info.Occupation = PersonAspect.OCCUPATION_ACTOR;
        info.LastChanged = LastChanged;
        info.DateAdded = DateAdded;
        return (T)(object)info;
      }
      else if (typeof(T) == typeof(CharacterInfo))
      {
        CharacterInfo info = new CharacterInfo();
        info.CopyIdsFrom(this);
        info.Name = Name;
        info.ActorName = ActorName;
        return (T)(object)info;
      }
      else if (typeof(T) == typeof(CharacterInfo))
      {
        CharacterInfo info = new CharacterInfo();
        info.CopyIdsFrom(this);
        info.Name = Name;
        info.ActorName = ActorName;
        return (T)(object)info;
      }
      return default(T);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return string.IsNullOrEmpty(Name) ? "[Unnamed Character]" : Name;
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      if (string.IsNullOrEmpty(NameId))
        AssignNameId();
      return string.IsNullOrEmpty(NameId) ? "[Unnamed Character]".GetHashCode() : NameId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      CharacterInfo other = obj as CharacterInfo;
      if (other == null) return false;

      if (TvdbId > 0 && other.TvdbId > 0)
        return TvdbId == other.TvdbId;
      if (MovieDbId > 0 && other.MovieDbId > 0)
        return MovieDbId == other.MovieDbId;
      if (TvMazeId > 0 && other.TvMazeId > 0)
        return TvMazeId == other.TvMazeId;

      //Name id is generated from name and can be unreliable so should only be used if matches
      if (!string.IsNullOrEmpty(NameId) && !string.IsNullOrEmpty(other.NameId) &&
        string.Equals(NameId, other.NameId, StringComparison.InvariantCultureIgnoreCase))
        return true;

      //If actor is not the same it is not the right character
      if (ActorTvdbId > 0 && other.ActorTvdbId > 0 && ActorTvdbId != other.ActorTvdbId)
        return false;
      if (ActorMovieDbId > 0 && other.ActorMovieDbId > 0 && ActorMovieDbId != other.ActorMovieDbId)
        return false;
      if (ActorTvMazeId > 0 && other.ActorTvMazeId > 0 && ActorTvMazeId != other.ActorTvMazeId)
        return false;
      if (ActorTvRageId > 0 && other.ActorTvRageId > 0 && ActorTvRageId != other.ActorTvRageId)
        return false;
      if (!string.IsNullOrEmpty(ActorImdbId) && !string.IsNullOrEmpty(other.ActorImdbId) && !string.Equals(ActorImdbId, other.ActorImdbId, StringComparison.InvariantCultureIgnoreCase))
        return false;
      if (!string.IsNullOrEmpty(ActorNameId) && !string.IsNullOrEmpty(other.ActorNameId) && !string.Equals(ActorNameId, other.ActorNameId, StringComparison.InvariantCultureIgnoreCase))
        return false;
      if (!string.IsNullOrEmpty(ActorName) && !string.IsNullOrEmpty(other.ActorName) && !MatchNames(ActorName, other.ActorName))
        return false;
      
      //More lax checking if media is the same
      if (!string.IsNullOrEmpty(ParentMediaName) && !string.IsNullOrEmpty(other.ParentMediaName) && MatchNames(ParentMediaName, other.ParentMediaName) &&
        !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && LaxMatchNames(Name, other.Name))
        return true;
      if (!string.IsNullOrEmpty(MediaName) && !string.IsNullOrEmpty(other.MediaName) && MatchNames(MediaName, other.MediaName) &&
        !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && LaxMatchNames(Name, other.Name))
        return true;

      //More lax comparison if actor is the same
      if (!string.IsNullOrEmpty(ActorName) && !string.IsNullOrEmpty(other.ActorName) &&
        MatchNames(ActorName, other.ActorName) && !string.IsNullOrEmpty(Name) &&
        !string.IsNullOrEmpty(other.Name) && LaxMatchNames(Name, other.Name))
        return true;

      if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && MatchNames(Name, other.Name))
        return true;

      return false;
    }

    public bool LaxMatchNames(string name1, string name2)
    {
      return CompareNames(name1, name2, 0.55);
    }

    public int CompareTo(CharacterInfo other)
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
