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
  /// Plugin metadata class responsible for storing social metadata.
  /// </summary>
  public class PluginSocialInfo  : IPluginSocialInfo
  {
	  public double AverageRatingTotal { get; private set; }
	  public double AverageRatingCurrentVersion { get; private set; }
	  public int RatingVotesCastTotal { get; private set; }
	  public int RatingVotesCastCurrentVersion { get; private set; }
	  public int DownloadCountTotal { get; private set; }
	  public int DownloadCountCurrentVersion { get; private set; }
	  public DateTime FirstVersionReleaseDate { get; private set; }
	  public TimeSpan MeanTimeBetweenReleases { get; private set; }
	  public IDictionary<int, int> CompatibleApiVotes { get; private set; }
	  public IList<PluginReview> Reviews { get; private set; }
  }
}
