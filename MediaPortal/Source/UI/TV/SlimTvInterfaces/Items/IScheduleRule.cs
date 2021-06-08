#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

namespace MediaPortal.Plugins.SlimTv.Interfaces.Items
{
  public enum RuleSearchMatch
  {
    Exact,
    Include,
    Exclude,
    Regex
  }

  public enum RuleSearchTarget
  {
    Titel,
    Description,
    Genre,
    StarRating
  }

  public enum RuleRecordingType
  {
    Once,
    All,
    AllOnSameChannel,
    AllOnSameChannelAndDay,
    AllOnSameDay,
  }

  public enum RuleEpisodeInfoFallback
  {
    None,
    TitleIsEpisodeName,
    DescriptionIsEpisodeName,
    TitleContainsSeasonEpisodeRegEx,
    DescriptionContainsSeasonEpisodeRegex,
    EpisodeTitleContainsSeasonEpisodeRegEx,
    TitleContainsEpisodeTitleRegEx,
    DescriptionContainsEpisodeTitleRegex,
    EpisodeTitleContainsEpisodeTitleRegEx,
  }

  public enum EpisodeManagementScheme
  {
    None,
    NewEpisodesByEpisodeNumber,
    MissingEpisodesByEpisodeNumber,
    MissingEpisodesByEpisodeName,
  }

  public interface IScheduleRule
  {
    int RuleId { get; }
    string Name { get; set; }
    bool Active { get; set; }

    IList<IScheduleRuleTarget> Targets { get; set; }

    int? ChannelGroupId { get; set; }
    int? ChannelId { get; set; }

    bool IsSeries { get; set; }
    string SeriesName { get; set; }
    string SeasonNumber { get; set; }
    string EpisodeNumber { get; set; }
    string EpisodeTitle { get; set; }
    string EpisodeInfoFallback { get; set; }
    RuleEpisodeInfoFallback EpisodeInfoFallbackType { get; set; }
    EpisodeManagementScheme EpisodeManagementScheme { get; set; }

    DateTime? StartFromTime { get; set; }
    DateTime? StartToTime { get; set; }
    DayOfWeek? StartOnOrAfterDay { get; set; }
    DayOfWeek? StartOnOrBeforeDay { get; set; }

    PriorityType Priority { get; set; }

    RuleRecordingType RecordingType { get; set; }
    TimeSpan PreRecordInterval { get; set; }
    TimeSpan PostRecordInterval { get; set; }

    KeepMethodType KeepMethod { get; set; }
    DateTime? KeepDate { get; set; }
  }
}
