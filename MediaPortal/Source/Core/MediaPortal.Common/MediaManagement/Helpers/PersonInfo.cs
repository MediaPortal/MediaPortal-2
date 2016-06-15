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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="PersonInfo"/> contains metadata information about a person item.
  /// </summary>
  public class PersonInfo : BaseInfo, IComparable<PersonInfo>
  {
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

    /// <summary>
    /// Gets or sets the person name.
    /// </summary>
    public string Name = null;
    public LanguageText Biography = null;
    public string Orign = null;
    public DateTime? DateOfBirth = null;
    public DateTime? DateOfDeath = null;
    public string Occupation = null;
    public bool IsGroup = false;
    public int? Order = null;

    #region Members

    /// <summary>
    /// Copies the contained person information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(Name)) return false;
      if (string.IsNullOrEmpty(Occupation)) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
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

      if (DateOfBirth.HasValue) MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_DATEOFBIRTH, DateOfBirth.Value);
      if (DateOfDeath.HasValue) MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_DATEOFDEATH, DateOfDeath.Value);
      MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_GROUP, IsGroup);

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!aspectData.ContainsKey(PersonAspect.ASPECT_ID))
        return false;

      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_PERSON_NAME, out Name);
      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_ORIGIN, out Orign);
      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_OCCUPATION, out Occupation);

      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_DATEOFBIRTH, out DateOfBirth);
      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_DATEOFDEATH, out DateOfDeath);
      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_GROUP, out IsGroup);

      string tempString;
      MediaItemAspect.TryGetAttribute(aspectData, PersonAspect.ATTR_BIOGRAPHY, out tempString);
      Biography = new LanguageText(tempString, false);

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

      byte[] data;
      if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
        Thumbnail = data;

      return true;
    }

    public bool FromString(string name)
    {
      Name = name;
      return true;
    }

    public bool CopyIdsFrom(PersonInfo otherPerson)
    {
      MovieDbId = otherPerson.MovieDbId;
      ImdbId = otherPerson.ImdbId;
      AudioDbId = otherPerson.AudioDbId;
      MusicBrainzId = otherPerson.MusicBrainzId;
      TvdbId = otherPerson.TvdbId;
      TvMazeId = otherPerson.TvMazeId;
      TvRageId = otherPerson.TvRageId;
      return true;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return Name;
    }

    public override bool Equals(object obj)
    {
      PersonInfo other = obj as PersonInfo;
      if (obj == null) return false;
      if (TvdbId > 0 && TvdbId == other.TvdbId && Occupation == other.Occupation) return true;
      if (MovieDbId > 0 && MovieDbId == other.MovieDbId && Occupation == other.Occupation) return true;
      if (AudioDbId > 0 && AudioDbId == other.AudioDbId && Occupation == other.Occupation) return true;
      if (TvMazeId > 0 && TvMazeId == other.TvMazeId && Occupation == other.Occupation) return true;
      if (TvRageId > 0 && TvRageId == other.TvRageId && Occupation == other.Occupation) return true;
      if (!string.IsNullOrEmpty(MusicBrainzId) && !string.IsNullOrEmpty(other.MusicBrainzId) &&
        string.Equals(MusicBrainzId, other.MusicBrainzId, StringComparison.InvariantCultureIgnoreCase) &&
        Occupation == other.Occupation)
        return true;
      if (!string.IsNullOrEmpty(ImdbId) && !string.IsNullOrEmpty(other.ImdbId) &&
        string.Equals(ImdbId, other.ImdbId, StringComparison.InvariantCultureIgnoreCase) && 
        Occupation == other.Occupation)
        return true;
      if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && 
        MatchNames(Name, other.Name) && Occupation == other.Occupation)
        return true;

      return false;
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
