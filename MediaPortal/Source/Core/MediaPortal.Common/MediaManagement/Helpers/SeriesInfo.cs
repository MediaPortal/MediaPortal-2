#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Utilities;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="SeriesInfo"/> contains metadata information about a series episode item.
  /// </summary>
  /// <remarks>
  /// If all required fields are filled, the <see cref="IsCompleteMatch"/> 
  /// returns <c>true</c>. The <see cref="ToString"/> method returns a well formatted series title if <see cref="IsCompleteMatch"/> is <c>true</c>.
  /// </remarks>
  public class SeriesInfo
  {
    public static string EPISODE_FORMAT_STR = "{0} S{1}E{2} - {3}";

    /// <summary>
    /// Indicates that all required fields are filled.
    /// </summary>
    public bool IsCompleteMatch
    {
      get { return !(string.IsNullOrEmpty(Series) || string.IsNullOrEmpty(Episode) || SeasonNumber == 0 || EpisodeNumbers.Count == 0); }
    }
    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    public string Series { get; set; }

    /// <summary>
    /// Gets or sets the episode title.
    /// </summary>
    public string Episode { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int SeasonNumber;

    /// <summary>
    /// Gets a list of episode numbers.
    /// </summary>
    public IList<int> EpisodeNumbers { get; internal set; }

    #region Constructor 

    public SeriesInfo()
    {
      EpisodeNumbers = new List<int>();
    }

    #endregion

    #region Members 

    /// <summary>
    /// Copies the contained series information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, MediaItemAspect> aspectData)
    {
      if (!IsCompleteMatch)
        return false;

      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_SERIESNAME, Series);
      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_EPISODENAME, Episode);
      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_SEASON, SeasonNumber);
      MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_EPISODE, EpisodeNumbers);
      return true;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      if (IsCompleteMatch)
      {
        return string.Format(EPISODE_FORMAT_STR,
          Series,
          SeasonNumber.ToString().PadLeft(2, '0'),
          StringUtils.Join(", ", EpisodeNumbers.Select(episodeNumber => episodeNumber.ToString().PadLeft(2, '0'))),
          Episode);
      }
      return "SeriesInfo: No complete match";
    }

    #endregion
  }
}
