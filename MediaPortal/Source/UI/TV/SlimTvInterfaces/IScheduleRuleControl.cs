#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces
{
  public interface IScheduleRuleControlAsync
  {
    /// <summary>
    /// Creates a schedule rule.
    /// </summary>
    /// <param name="title">Name of the rule.</param>
    /// <param name="targets">List of target matches that are required for this rule to apply.</param>
    /// <param name="channelGroup">Channel group to record from.</param>
    /// <param name="channel">Channel to record from.</param>
    /// <param name="from">Recording time from.</param>
    /// <param name="to">Recording time to.</param>
    /// <param name="afterDay">Record on or after this day of the week.</param>
    /// <param name="beforeDay">Record on or before this day of the week.</param>
    /// <param name="recordingType">Schedule recording type.</param>
    /// <param name="preRecordInterval">Prerecording interval</param>
    /// <param name="postRecordInterval">Postrecording interval</param>
    /// <param name="priority">Schedule priority</param>
    /// <param name="keepMethod">How to keep the recording.</param>
    /// <param name="keepDate">The end date for keeping the recording if needed.</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if successful.
    /// <see cref="AsyncResult{T}.Result"/> Returns the schedule rule instance.
    /// </returns>
    Task<AsyncResult<IScheduleRule>> CreateScheduleRuleAsync(string title, IList<IScheduleRuleTarget> targets, IChannelGroup channelGroup, IChannel channel, DateTime? from, DateTime? to, DayOfWeek? afterDay, DayOfWeek? beforeDay,
      RuleRecordingType recordingType, int preRecordInterval, int postRecordInterval, int priority, KeepMethodType keepMethod, DateTime? keepDate);

    /// <summary>
    /// Creates a schedule rule.
    /// </summary>
    /// <param name="title">Name of the rule.</param>
    /// <param name="targets">List of target matches that are required for this rule to apply.</param>
    /// <param name="channelGroup">Channel group to record from.</param>
    /// <param name="channel">Channel to record from.</param>
    /// <param name="from">Recording time from.</param>
    /// <param name="to">Recording time to.</param>
    /// <param name="afterDay">Record on or after this day of the week.</param>
    /// <param name="beforeDay">Record on or before this day of the week.</param>
    /// <param name="seriesName">The name of the series.</param>
    /// <param name="seasonNumber">The season number to match if needed.</param>
    /// <param name="episodeNumber">The episode number to match if needed.</param>
    /// <param name="episodeTitle">The episode title to match if needed.</param>
    /// <param name="episodeInfoFallback">A regular expression or similar to specify where to find series information if not available in the default properties of a program.</param>
    /// <param name="episodeInfoFallbackType">The type of fallback to use for finding series information if not available in the default properties of a program </param>
    /// <param name="episodeManagementScheme">The scheme to use for managing episodes for this rule</param>
    /// <param name="recordingType">Schedule recording type.</param>
    /// <param name="preRecordInterval">Prerecording interval</param>
    /// <param name="postRecordInterval">Postrecording interval</param>
    /// <param name="priority">Schedule priority</param>
    /// <param name="keepMethod">How to keep the recording.</param>
    /// <param name="keepDate">The end date for keeping the recording if needed.</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if successful.
    /// <see cref="AsyncResult{T}.Result"/> Returns the schedule rule instance.
    /// </returns>
    Task<AsyncResult<IScheduleRule>> CreateSeriesScheduleRuleAsync(string title, IList<IScheduleRuleTarget> targets, IChannelGroup channelGroup, IChannel channel, DateTime? from, DateTime? to, DayOfWeek? afterDay, DayOfWeek? beforeDay,
      string seriesName, string seasonNumber, string episodeNumber, string episodeTitle, string episodeInfoFallback, RuleEpisodeInfoFallback episodeInfoFallbackType, EpisodeManagementScheme episodeManagementScheme,
      RuleRecordingType recordingType, int preRecordInterval, int postRecordInterval, int priority, KeepMethodType keepMethod, DateTime? keepDate);

    /// <summary>
    /// Edits a given <paramref name="scheduleRule"/>.
    /// </summary>
    /// <param name="scheduleRule">The rule to change.</param>
    /// <param name="title">Name of the rule.</param>
    /// <param name="targets">List of target matches that are required for this rule to apply.</param>
    /// <param name="channelGroup">Channel group to record from.</param>
    /// <param name="channel">Channel to record from.</param>
    /// <param name="from">Recording time from.</param>
    /// <param name="to">Recording time to.</param>
    /// <param name="afterDay">Record on or after this day of the week.</param>
    /// <param name="isSeries">Is it a series rule.</param>
    /// <param name="beforeDay">Record on or before this day of the week.</param>
    /// <param name="seriesName">The name of the series.</param>
    /// <param name="seasonNumber">The season number to match if needed.</param>
    /// <param name="episodeNumber">The episode number to match if needed.</param>
    /// <param name="episodeTitle">The episode title to match if needed.</param>
    /// <param name="episodeInfoFallback">A regular expression or similar to specify where to find series information if not available in the default properties of a program.</param>
    /// <param name="episodeInfoFallbackType">The type of fallback to use for finding series information if not available in the default properties of a program </param>
    /// <param name="episodeManagementScheme">The scheme to use for managing episodes for this rule</param>
    /// <param name="recordingType">Schedule recording type.</param>
    /// <param name="preRecordInterval">Prerecording interval</param>
    /// <param name="postRecordInterval">Postrecording interval</param>
    /// <param name="priority">Schedule priority</param>
    /// <param name="keepMethod">How to keep the recording.</param>
    /// <param name="keepDate">The end date for keeping the recording if needed.</param>
    /// <returns></returns>
    Task<bool> EditScheduleRuleAsync(IScheduleRule scheduleRule, string title, IList<IScheduleRuleTarget> targets, IChannelGroup channelGroup, IChannel channel, DateTime? from, DateTime? to, DayOfWeek? afterDay, DayOfWeek? beforeDay,
      bool? isSeries, string seriesName, string seasonNumber, string episodeNumber, string episodeTitle, string episodeInfoFallback, RuleEpisodeInfoFallback? episodeInfoFallbackType, EpisodeManagementScheme? episodeManagementScheme,
      RuleRecordingType? recordingType, int? preRecordInterval, int? postRecordInterval, int? priority, KeepMethodType? keepMethod, DateTime? keepDate);

    /// <summary>
    /// Deletes a given <paramref name="scheduleRule"/>.
    /// </summary>
    /// <param name="scheduleRule">Schedule rule to delete.</param>
    /// <returns><c>true</c> if successful.</returns>
    Task<bool> RemoveScheduleRuleAsync(IScheduleRule scheduleRule);

    /// <summary>
    /// Gets the list of all available schedule rules.
    /// </summary>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if at least one schedule rule could be found.
    /// <see cref="AsyncResult{T}.Result"/> schedule rules.
    /// </returns>
    Task<AsyncResult<IList<IScheduleRule>>> GetScheduleRulesAsync();

    /// <summary>
    /// Gets all matching programs for a schedule rule.
    /// </summary>
    /// <param name="scheduleRule">The schedule rule to get programs for.</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if successful.
    /// <see cref="AsyncResult{T}.Result"/> Returns a list programs matching the schedule rule.
    /// </returns>
    Task<AsyncResult<IList<IProgram>>> GetProgramsForScheduleRuleAsync(IScheduleRule scheduleRule);

    /// <summary>
    /// Gets all conflicting programs for a schedule rule.
    /// </summary>
    /// <param name="scheduleRule">The schedule rule to get conflicting programs for.</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if successful.
    /// <see cref="AsyncResult{T}.Result"/> Returns a list conflicting programs for the schedule rule.
    /// </returns>
    Task<AsyncResult<IList<IProgram>>> GetConflictsForScheduleRuleAsync(IScheduleRule scheduleRule);

    /// <summary>
    /// Activates/deactivates a given <paramref name="scheduleRule"/>.
    /// </summary>
    /// <param name="scheduleRule">Schedule rule to activate.</param>
    /// <param name="active">The new activated state</param>
    /// <returns><c>true</c> if successful.</returns>
    Task<bool> UpdateScheduleRuleActivationAsync(IScheduleRule scheduleRule, bool active);
  }
}
