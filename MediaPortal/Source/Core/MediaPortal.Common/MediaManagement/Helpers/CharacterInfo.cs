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
  /// <see cref="CharacterInfo"/> contains metadata information about a character item.
  /// </summary>
  public class CharacterInfo
  {
    /// <summary>
    /// Gets or sets the character TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public int TvMazeId = 0;
    /// <summary>
    /// Gets or sets the character name.
    /// </summary>
    public string Name = null;

    public string MediaImdbId = null;
    /// <summary>
    /// Gets or sets the media TheTvDB id.
    /// </summary>
    public int MediaTvdbId = 0;
    public int MediaMovieDbId = 0;
    public int MediaTvMazeId = 0;
    public string MediaTitle = null;
    public bool MediaIsMovie = false;

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
    public string ActorName = null;
   
    #region Members

    /// <summary>
    /// Copies the contained character information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(Name)) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, CharacterAspect.ATTR_CHARACTER_NAME, Name);
      if(!string.IsNullOrEmpty(ActorName)) MediaItemAspect.SetAttribute(aspectData, CharacterAspect.ATTR_ACTOR_NAME, ActorName);
      if (!string.IsNullOrEmpty(MediaTitle)) MediaItemAspect.SetAttribute(aspectData, CharacterAspect.ATTR_MEDIA_TITLE, MediaTitle);

      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_CHARACTER, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_CHARACTER, MovieDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_CHARACTER, TvMazeId.ToString());

      if (ActorTvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_PERSON, ActorTvdbId.ToString());
      if (ActorMovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_PERSON, ActorMovieDbId.ToString());
      if (ActorTvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_PERSON, ActorTvMazeId.ToString());
      if (!string.IsNullOrEmpty(ActorImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_PERSON, ActorImdbId);

      if (MediaIsMovie)
      {
        if (MediaMovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, MediaMovieDbId.ToString());
        if (!string.IsNullOrEmpty(MediaImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, MediaImdbId);
      }
      else
      {
        if (MediaTvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, MediaTvdbId.ToString());
        if (MediaMovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, MediaMovieDbId.ToString());
        if (MediaTvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, MediaTvMazeId.ToString());
        if (!string.IsNullOrEmpty(MediaImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, MediaImdbId);
      }

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

      CharacterInfo other = obj as CharacterInfo;
      if (obj == null) return false;
      if (string.IsNullOrEmpty(ActorName) || string.IsNullOrEmpty(other.ActorName) || StringUtils.GetLevenshteinDistance(ActorName, other.ActorName) > MAX_LEVENSHTEIN_DIST)
        return false;
      if (string.IsNullOrEmpty(MediaTitle) || string.IsNullOrEmpty(other.MediaTitle) || StringUtils.GetLevenshteinDistance(MediaTitle, other.MediaTitle) > MAX_LEVENSHTEIN_DIST)
        return false;
      if (TvdbId > 0 && TvdbId == other.TvdbId) return true;
      if (MovieDbId > 0 && MovieDbId == other.MovieDbId) return true;
      if (TvMazeId > 0 && TvMazeId == other.TvMazeId) return true;
      if (MovieDbId > 0 && MovieDbId == other.MovieDbId) return true;
      if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && StringUtils.GetLevenshteinDistance(Name, other.Name) <= MAX_LEVENSHTEIN_DIST)
        return true;

      return false;
    }

    #endregion
  }
}
