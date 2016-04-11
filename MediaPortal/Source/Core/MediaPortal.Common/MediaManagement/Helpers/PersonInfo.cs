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
using MediaPortal.Utilities;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="PersonInfo"/> contains metadata information about a person item.
  /// </summary>
  public class PersonInfo
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

    /// <summary>
    /// Gets or sets the person name.
    /// </summary>
    public string Name = null;
    public string Biography = null;
    public string Orign = null;
    public DateTime? DateOfBirth = null;
    public DateTime? DateOfDeath = null;
    public PersonOccupation Occupation;
    public bool IsGroup = false;

    #region Members

    /// <summary>
    /// Copies the contained person information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(Name)) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_PERSON_NAME, Name);
      MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_BIOGRAPHY, Biography);
      MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_ORIGIN, Orign);
      MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_OCCUPATION, (int)Occupation);

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_PERSON, ImdbId);
      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_PERSON, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_PERSON, MovieDbId.ToString());
      if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_PERSON, MusicBrainzId);
      if (AudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_PERSON, AudioDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_PERSON, TvMazeId.ToString());

      if (DateOfBirth.HasValue) MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_DATEOFBIRTH, DateOfBirth.Value);
      if (DateOfDeath.HasValue) MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_DATEOFDEATH, DateOfDeath.Value);
      MediaItemAspect.SetAttribute(aspectData, PersonAspect.ATTR_GROUP, IsGroup);

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
      const int MAX_LEVENSHTEIN_DIST = 4;

      PersonInfo other = obj as PersonInfo;
      if (obj == null) return false;
      if (TvdbId > 0 && TvdbId == other.TvdbId && Occupation == other.Occupation) return true;
      if (MovieDbId > 0 && MovieDbId == other.MovieDbId && Occupation == other.Occupation) return true;
      if (!string.IsNullOrEmpty(ImdbId) && !string.IsNullOrEmpty(other.ImdbId) &&
        string.Equals(ImdbId, other.ImdbId, StringComparison.InvariantCultureIgnoreCase) && 
        Occupation == other.Occupation)
        return true;
      if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && 
        StringUtils.GetLevenshteinDistance(Name, other.Name) <= MAX_LEVENSHTEIN_DIST &&
         Occupation == other.Occupation)
        return true;

      return false;
    }

    #endregion
  }
}
