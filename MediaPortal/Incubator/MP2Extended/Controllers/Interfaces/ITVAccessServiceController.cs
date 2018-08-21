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
    Task<WebDictionary<string>> GetExternalMediaInfo(WebMediaType? type, string id);
    //Task<IList<WebTVSearchResult>> SearchResultsByRange(string text, int start, int end, WebTVSearchResultType? type = null);

    #endregion

    #region Cards

    Task<IList<WebCard>> GetCards();
    Task<IList<WebVirtualCard>> GetActiveCards();
    Task<IList<WebUser>> GetActiveUsers();
    //Task<IList<WebRtspClient>> GetStreamingClients();
    Task<IList<WebDiskSpaceInformation>> GetAllRecordingDiskInformation();
    Task<WebDiskSpaceInformation> GetRecordingDiskInformationForCard(int id);

    #endregion

    #region Schedules

    Task<WebBoolResult> StartRecordingManual(string userName, int channelId, string title);
    Task<WebBoolResult> AddSchedule(int channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType);
    Task<WebBoolResult> AddScheduleDetailed(int channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType, int preRecordInterval, int postRecordInterval, string directory, int priority);
    Task<WebIntResult> GetScheduleCount();
    Task<IList<WebScheduleBasic>> GetSchedules(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc, string filter = null);
    Task<IList<WebScheduleBasic>> GetSchedulesByRange(int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc, string filter = null);
    Task<WebScheduleBasic> GetScheduleById(int scheduleId);
    Task<WebBoolResult> CancelSchedule(int programId);
    Task<WebBoolResult> EditSchedule(int scheduleId, int? channelId = null, string title = null, DateTime? startTime = null, DateTime? endTime = null, WebScheduleType? scheduleType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null);
    Task<WebBoolResult> DeleteSchedule(int scheduleId);
    Task<WebBoolResult> StopRecording(int scheduleId);
    Task<IList<WebScheduledRecording>> GetScheduledRecordingsForDate(DateTime date, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc, string filter = null);
    Task<IList<WebScheduledRecording>> GetScheduledRecordingsForToday(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc, string filter = null);
    
    #endregion

    #region Recordings

    Task<WebIntResult> GetRecordingCount();
    Task<IList<WebRecordingBasic>> GetRecordings(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc, string filter = null);
    Task<IList<WebRecordingBasic>> GetRecordingsByRange(int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc, string filter = null);
    Task<WebRecordingBasic> GetRecordingById(Guid id);
    Task<WebBoolResult> DeleteRecording(int id);
    Task<WebRecordingFileInfo> GetRecordingFileInfo(int id);
    Task<Stream> ReadRecordingFile(int id);

    #endregion

    #region TV

    Task<WebIntResult> GetGroupCount();
    Task<IList<WebChannelGroup>> GetGroups(WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelGroup>> GetGroupsByRange(int start, int end, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebChannelGroup> GetGroupById(int groupId);
    Task<WebIntResult> GetChannelCount(int? groupId = null);
    Task<IList<WebChannelBasic>> GetChannelsBasic(int? groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelBasic>> GetChannelsBasicByRange(int start, int end, int? groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelDetailed>> GetChannelsDetailed(int? groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelDetailed>> GetChannelsDetailedByRange(int start, int end, int? groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelState>> GetAllChannelStatesForGroup(int groupId, string userName);

    #endregion

    #region Radio specific 
    
    Task<WebIntResult> GetRadioGroupCount();
    Task<IList<WebChannelGroup>> GetRadioGroups(WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelGroup>> GetRadioGroupsByRange(int start, int end, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebChannelGroup> GetRadioGroupById(int groupId);
    Task<WebIntResult> GetRadioChannelCount(int? groupId = null);
    Task<IList<WebChannelBasic>> GetRadioChannelsBasic(int? groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelBasic>> GetRadioChannelsBasicByRange(int start, int end, int? groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelDetailed>> GetRadioChannelsDetailed(int? groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelDetailed>> GetRadioChannelsDetailedByRange(int start, int end, int? groupId = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebChannelState>> GetAllRadioChannelStatesForGroup(int groupId, string userName);

    #endregion

    #region Channels

    Task<WebChannelBasic> GetChannelBasicById(int channelId);
    Task<WebChannelDetailed> GetChannelDetailedById(int channelId);
    Task<WebChannelState> GetChannelState(int channelId, string userName);

    #endregion

    #region Timeshifting
    
    Task<WebVirtualCard> SwitchTVServerToChannelAndGetVirtualCard(string userName, int channelId);
    Task<WebStringResult> SwitchTVServerToChannelAndGetStreamingUrl(string userName, int channelId);
    Task<WebStringResult> SwitchTVServerToChannelAndGetTimeshiftFilename(string userName, int channelId);
    Task<WebBoolResult> SendHeartbeat(string userName);
    Task<WebBoolResult> CancelCurrentTimeShifting(string userName);

    #endregion

    #region EPG
    
    Task<IList<WebProgramBasic>> GetProgramsBasicForChannel(int channelId, DateTime startTime, DateTime endTime);
    Task<IList<WebProgramDetailed>> GetProgramsDetailedForChannel(int channelId, DateTime startTime, DateTime endTime);
    Task<IList<WebChannelPrograms<WebProgramBasic>>> GetProgramsBasicForGroup(int groupId, DateTime startTime, DateTime endTime);
    Task<IList<WebChannelPrograms<WebProgramDetailed>>> GetProgramsDetailedForGroup(int groupId, DateTime startTime, DateTime endTime);
    Task<WebProgramDetailed> GetCurrentProgramOnChannel(int channelId);
    Task<WebProgramDetailed> GetNextProgramOnChannel(int channelId);
    Task<WebIntResult> SearchProgramsCount(string searchTerm);
    Task<IList<WebProgramBasic>> SearchProgramsBasic(string searchTerm);
    Task<IList<WebProgramBasic>> SearchProgramsBasicByRange(string searchTerm, int start, int end);
    Task<IList<WebProgramDetailed>> SearchProgramsDetailed(string searchTerm);
    Task<IList<WebProgramDetailed>> SearchProgramsDetailedByRange(string searchTerm, int start, int end);
    Task<IList<WebProgramBasic>> GetNowNextWebProgramBasicForChannel(int channelId);
    Task<IList<WebProgramDetailed>> GetNowNextWebProgramDetailedForChannel(int channelId);
    Task<WebProgramBasic> GetProgramBasicById(int programId);
    Task<WebProgramDetailed> GetProgramDetailedById(int programId);
    Task<WebBoolResult> GetProgramIsScheduledOnChannel(int channelId, int programId);
    Task<WebBoolResult> GetProgramIsScheduled(int programId);

    #endregion
  }
}
