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
  /// <see cref="CompanyInfo"/> contains metadata information about a company item.
  /// </summary>
  public class CompanyInfo
  {
    /// <summary>
    /// Gets or sets the company IMDB id.
    /// </summary>
    public string ImdbId = null;
    /// <summary>
    /// Gets or sets the company TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public int TvMazeId = 0;
    public string MusicBrainzId = null;
    public long AudioDbId = 0;

    /// <summary>
    /// Gets or sets the company name.
    /// </summary>
    public string Name = null;
    public string Description = null;
    public CompanyType Type;

    #region Members

    /// <summary>
    /// Copies the contained company information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(Name)) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, CompanyAspect.ATTR_COMPANY_NAME, Name);
      if (!string.IsNullOrEmpty(Description)) MediaItemAspect.SetAttribute(aspectData, CompanyAspect.ATTR_DESCRIPTION, Description);
      MediaItemAspect.SetAttribute(aspectData, CompanyAspect.ATTR_COMPANY_TYPE, (int)Type);

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_COMAPANY, ImdbId);
      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_COMAPANY, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COMAPANY, MovieDbId.ToString());
      if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_COMAPANY, MusicBrainzId);
      if (AudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_COMAPANY, AudioDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_COMAPANY, TvMazeId.ToString());

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

      CompanyInfo other = obj as CompanyInfo;
      if (obj == null) return false;
      if (TvdbId > 0 && TvdbId == other.TvdbId && Type == other.Type) return true;
      if (MovieDbId > 0 && MovieDbId == other.MovieDbId && Type == other.Type) return true;
      if (AudioDbId > 0 && AudioDbId == other.AudioDbId && Type == other.Type) return true;
      if (!string.IsNullOrEmpty(MusicBrainzId) && !string.IsNullOrEmpty(other.MusicBrainzId) && 
        string.Equals(MusicBrainzId, other.MusicBrainzId, StringComparison.InvariantCultureIgnoreCase) && 
        Type == other.Type)
        return true;
      if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && 
        StringUtils.GetLevenshteinDistance(Name, other.Name) <= MAX_LEVENSHTEIN_DIST && 
        Type == other.Type)
        return true;

      return false;
    }

    #endregion
  }
}
