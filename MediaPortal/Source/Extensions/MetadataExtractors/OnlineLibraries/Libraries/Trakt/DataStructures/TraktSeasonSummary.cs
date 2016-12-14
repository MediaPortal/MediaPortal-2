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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSeasonSummary : TraktSeason
  {
    [DataMember(Name = "rating")]
    public double? Rating { get; set; }

    [DataMember(Name = "votes")]
    public int Votes { get; set; }

    [DataMember(Name = "episode_count")]
    public int EpisodeCount { get; set; }

    [DataMember(Name = "aired_episodes")]
    public int EpisodeAiredCount { get; set; }

    [DataMember(Name = "overview")]
    public string Overview { get; set; }

    [DataMember(Name = "images")]
    public TraktSeasonImages Images { get; set; }

    [DataMember(Name = "episodes")]
    public IEnumerable<TraktEpisodeSummary> Episodes { get; set; }
  }
}