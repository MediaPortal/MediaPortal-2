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
  /// Plugin metadata interface for social (user-generated) metadata. Social metadata is fetched lazily from the package feed and only stored in memory.
  /// </summary>
  public interface IPluginSocialInfo
  {
    /// <summary>
    /// Average rating for all versions.
    /// </summary>
    double AverageRatingTotal { get; }

	  /// <summary>
    /// Average rating for the current version.
    /// </summary>
    double AverageRatingCurrentVersion { get; }
    
	  /// <summary>
    /// Total number of rating votes cast for all versions.
    /// </summary>
    int RatingVotesCastTotal { get; }

	  /// <summary>
    /// Number of rating votes cast for the current version.
    /// </summary>
    int RatingVotesCastCurrentVersion { get; }

    /// <summary>
    /// Total number of downloads for all versions.
    /// </summary>
    int DownloadCountTotal { get; }

    /// <summary>
    /// Number of downloads for the current version.
    /// </summary>
    int DownloadCountCurrentVersion { get; }

    /// <summary>
    /// Date when first version was released.
    /// </summary>
    DateTime FirstVersionReleaseDate { get; }

    /// <summary>
    /// Average duration between releases, or <see cref="TimeSpan.Zero"/> if there has been only a single release.
    /// </summary>
    TimeSpan MeanTimeBetweenReleases { get; }

    /// <summary>
    /// Dictionary with the number of user compatibility votes (value) for every CompatibleAPI version (key).
    /// </summary>
    IDictionary<int,int> CompatibleApiVotes { get; }

    /// <summary>
    /// List of user reviews.
    /// </summary>
    IList<PluginReview> Reviews { get; }
  }
}
