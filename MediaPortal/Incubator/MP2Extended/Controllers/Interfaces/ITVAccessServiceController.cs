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
using System.IO;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Misc;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;

namespace MediaPortal.Plugins.MP2Extended.Controllers.Interfaces
{
  public interface ITVAccessServiceController
  {
    Task<WebTVServiceDescription> GetServiceDescription();

    #region TV Server

    Task<WebBoolResult> TestConnectionToTVService();
    Task<WebStringResult> ReadSettingFromDatabase(string tagName);
    Task<WebBoolResult> WriteSettingToDatabase(string tagName, string value);
    Task<IList<WebDiskSpaceInformation>> GetLocalDiskInformation();
    //Task<IList<WebTVSearchResult>> Search(string text, WebTVSearchResultType? type = null);
    //Task<WebDictionary<string>> GetExternalMediaInfo(WebMediaType? type, string id);
    //Task<IList<WebTVSearchResult>> SearchResultsByRange(string text, int start, int end, WebTVSearchResultType? type = null);

    #endregion

    #region Cards

    Task<IList<WebCard>> GetCards();
    Task<IList<WebVirtualCard>> GetActiveCards();
    Task<IList<WebUser>> GetActiveUsers();
    //Task<IList<WebRtspClient>> GetStreamingClients();
    Task<IList<WebDiskSpaceInformation>> GetAllRecordingDiskInformation();
    Task<WebDiskSpaceInformation> GetRecordingDiskInformationForCard(string id);

    #endregion

    #region Schedules

    Task<WebBoolResult> StartRecordingManual(string userName, string channelId, string title);
    Task<WebBoolResult> AddSchedule(string channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType);
    Task<WebBoolResult> AddScheduleDetailed(string channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType, int preRecordInterval, int postRecordInterval, string directory, int priority);
    Task<WebIntResult> GetScheduleCount();
    Task<IList<WebScheduleBasic>> GetSchedules(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc, string filter = null);
    Task<IList<WebScheduleBasic>> GetSchedulesByRange(int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc, string filter = null);
    Task<WebScheduleBasic> GetScheduleById(string scheduleId);
    Task<WebBoolResult> CancelSchedule(string programId);
    Task<WebBoolResult> EditSchedule(string scheduleId, string channelId = null, string title = null, DateTime? startTime = null, DateTime? endTime = null, WebScheduleType? scheduleType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null);
    Task<WebBoolResult> DeleteSchedule(string scheduleId);
    Task<WebBoolResult> StopRecording(string scheduleId);
    Task<IList<WebScheduledRecording>> GetScheduledRecordingsForDate(DateTime date, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc, string filter = null);
    Task<IList<WebScheduledRecording>> GetScheduledRecordingsForToday(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc, string filter = null);
    
    #endregion

    #region Recordings

    Task<WebIntResult> GetRecordingCount();
    Task<IList<WebRecordingBasic>> GetRecordings(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc, string filter = null);
    Task<IList<WebRecordingBasic>> GetRecordingsByRange(int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc, string filter = null);
    Task<WebRecordingBasic> GetRecordingById(string id);
    Task<WebBoolResult> DeleteRecording(string id);
    Task<WebRecordingFileInfo> GetRecordingFileInfo(string id);
    Task<Stream> ReadRecordingFile(string id);

    #endregion

    #region TV

    Task<WebIntResult> GetGroupCount();
    Task<IList<WebChannelGroup>> GetGroups(WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelGroup>> GetGroupsByRange(int start, int end, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebChannelGroup> GetGroupById(string groupId);
    Task<WebIntResult> GetChannelCount(string groupId = null);
    Task<IList<WebChannelBasic>> GetChannelsBasic(string groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelBasic>> GetChannelsBasicByRange(int start, int end, string groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelDetailed>> GetChannelsDetailed(string groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelDetailed>> GetChannelsDetailedByRange(int start, int end, string groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelState>> GetAllChannelStatesForGroup(string groupId, string userName);

    #endregion

    #region Radio specific 
    
    Task<WebIntResult> GetRadioGroupCount();
    Task<IList<WebChannelGroup>> GetRadioGroups(WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelGroup>> GetRadioGroupsByRange(int start, int end, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebChannelGroup> GetRadioGroupById(string groupId);
    Task<WebIntResult> GetRadioChannelCount(string groupId = null);
    Task<IList<WebChannelBasic>> GetRadioChannelsBasic(string groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelBasic>> GetRadioChannelsBasicByRange(int start, int end, string groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelDetailed>> GetRadioChannelsDetailed(string groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelDetailed>> GetRadioChannelsDetailedByRange(int start, int end, string groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelState>> GetAllRadioChannelStatesForGroup(string groupId, string userName);

    #endregion

    #region Channels

    Task<WebChannelBasic> GetChannelBasicById(string channelId);
    Task<WebChannelDetailed> GetChannelDetailedById(string channelId);
    Task<WebChannelState> GetChannelState(string channelId, string userName);

    #endregion

    #region Timeshifting

    //Task<WebVirtualCard> SwitchTVServerToChannelAndGetVirtualCard(string userName, string channelId);
    Task<WebStringResult> SwitchTVServerToChannelAndGetStreamingUrl(string userName, string channelId);
    Task<WebStringResult> SwitchTVServerToChannelAndGetTimeshiftFilename(string userName, string channelId);
    Task<WebBoolResult> SendHeartbeat(string userName);
    Task<WebBoolResult> CancelCurrentTimeShifting(string userName);

    #endregion

    #region EPG
    
    Task<IList<WebProgramBasic>> GetProgramsBasicForChannel(string channelId, DateTime startTime, DateTime endTime);
    Task<IList<WebProgramDetailed>> GetProgramsDetailedForChannel(string channelId, DateTime startTime, DateTime endTime);
    Task<IList<WebChannelPrograms<WebProgramBasic>>> GetProgramsBasicForGroup(string groupId, DateTime startTime, DateTime endTime);
    Task<IList<WebChannelPrograms<WebProgramDetailed>>> GetProgramsDetailedForGroup(string groupId, DateTime startTime, DateTime endTime);
    Task<WebProgramDetailed> GetCurrentProgramOnChannel(string channelId);
    Task<WebProgramDetailed> GetNextProgramOnChannel(string channelId);
    Task<WebIntResult> SearchProgramsCount(string searchTerm);
    Task<IList<WebProgramBasic>> SearchProgramsBasic(string searchTerm);
    Task<IList<WebProgramBasic>> SearchProgramsBasicByRange(string searchTerm, int start, int end);
    Task<IList<WebProgramDetailed>> SearchProgramsDetailed(string searchTerm);
    Task<IList<WebProgramDetailed>> SearchProgramsDetailedByRange(string searchTerm, int start, int end);
    Task<IList<WebProgramBasic>> GetNowNextWebProgramBasicForChannel(string channelId);
    Task<IList<WebProgramDetailed>> GetNowNextWebProgramDetailedForChannel(string channelId);
    Task<WebProgramBasic> GetProgramBasicById(string programId);
    Task<WebProgramDetailed> GetProgramDetailedById(string programId);
    Task<WebBoolResult> GetProgramIsScheduledOnChannel(string channelId, string programId);
    Task<WebBoolResult> GetProgramIsScheduled(string programId);

    #endregion
  }
}
