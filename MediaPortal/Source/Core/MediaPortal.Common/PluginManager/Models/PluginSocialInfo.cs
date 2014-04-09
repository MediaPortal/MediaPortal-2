#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

namespace MediaPortal.Common.PluginManager.Models
{
  /// <summary>
  /// Plugin metadata class responsible for storing social metadata. Social metadata is gathered
  /// from the online package feed for MP2 plugins, and must be requested explicitly.
  /// </summary>
  public class PluginSocialInfo
  {
    #region Social Details
    /// <summary>
    /// Average rating for all versions.
    /// </summary>
    public double AverageRatingTotal { get; private set; }

	  /// <summary>
    /// Average rating for the current version.
    /// </summary>
	  public double AverageRatingCurrentVersion { get; private set; }
    
	  /// <summary>
    /// Total number of rating votes cast for all versions.
    /// </summary>
    public int RatingVotesCastTotal { get; private set; }

	  /// <summary>
    /// Number of rating votes cast for the current version.
    /// </summary>
	  public int RatingVotesCastCurrentVersion { get; private set; }

    /// <summary>
    /// Total number of downloads for all versions.
    /// </summary>
	  public int DownloadCountTotal { get; private set; }

    /// <summary>
    /// Number of downloads for the current version.
    /// </summary>
    public int DownloadCountCurrentVersion { get; private set; }

    /// <summary>
    /// Date when first version was released.
    /// </summary>
 	  public DateTime FirstVersionReleaseDate { get; private set; }

    /// <summary>
    /// Average duration between releases, or <see cref="TimeSpan.Zero"/> if there has been only a single release.
    /// </summary>
	  public TimeSpan MeanTimeBetweenReleases { get; private set; }

    /// <summary>
    /// Dictionary with the number of user compatibility votes (value) for every CompatibleAPI version (key).
    /// </summary>
	  public IDictionary<int, int> CompatibleApiVotes { get; private set; }

    /// <summary>
    /// List of user reviews.
    /// </summary>
	  public IList<PluginReview> Reviews { get; private set; }
    #endregion
  }
}
