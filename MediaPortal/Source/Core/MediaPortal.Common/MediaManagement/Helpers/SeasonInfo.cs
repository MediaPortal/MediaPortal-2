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
  /// <see cref="SeasonInfo"/> contains information about a series season. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class SeasonInfo
  {
    /// <summary>
    /// Returns the index for "Series" used in <see cref="FormatString"/>.
    /// </summary>
    public static int SERIES_INDEX = 0;
    /// <summary>
    /// Returns the index for "Season" used in <see cref="FormatString"/>.
    /// </summary>
    public static int SEASON_INDEX = 1;
    /// <summary>
    /// Format string that holds series name and season number.
    /// </summary>
    public static string SEASON_FORMAT_STR = "{0} S{1:00}";
    /// <summary>
    /// Short format string that holds season number. Used for browsing seasons by series name.
    /// </summary>
    public static string SHORT_FORMAT_STR = "S{1:00}";

        /// <summary>
    /// Gets or sets the series IMDB id.
    /// </summary>
    public string ImdbId = null;
    /// <summary>
    /// Gets or sets the series TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public int TvMazeId = 0;

    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    public string Series = null;
    /// <summary>
    /// Gets or sets the season number. A "0" value will be treated as valid season number.
    /// </summary>
    public int? SeasonNumber = null;
    /// <summary>
    /// Gets or sets the first aired date of season.
    /// </summary>
    public DateTime? FirstAired = null;
    /// <summary>
    /// Gets or sets the season description.
    /// </summary>
    public string Description = null;

    #region Members

    /// <summary>
    /// Copies the contained series information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_SERIES_NAME, Series);
      if (!string.IsNullOrEmpty(Description)) MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_DESCRIPTION, Description);
      if (SeasonNumber.HasValue) MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_SEASON, SeasonNumber.Value);
      if (FirstAired.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, FirstAired.Value);

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, ImdbId);
      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, MovieDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, TvMazeId.ToString());

      // Construct a "Series Season" string, which will be used for filtering and season banner retrieval.
      MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_SERIES_SEASON, ToString());

      return true;
    }

    public string ToShortString()
    {
      return string.Format(SHORT_FORMAT_STR, Series, SeasonNumber ?? 0);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return string.Format(SEASON_FORMAT_STR, Series, SeasonNumber ?? 0);
    }

    #endregion
  }
}
