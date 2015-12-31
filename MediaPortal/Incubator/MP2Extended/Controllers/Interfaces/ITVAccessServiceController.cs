using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
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
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebTVServiceDescription GetServiceDescription();

    #region TV Server
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult TestConnectionToTVService();

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebStringResult ReadSettingFromDatabase(string tagName);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult WriteSettingToDatabase(string tagName, string value);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebDiskSpaceInformation> GetLocalDiskInformation(string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebTVSearchResult> Search(string text, WebTVSearchResultType? type = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebDictionary<string> GetExternalMediaInfo(WebMediaType? type, string id);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebTVSearchResult> SearchResultsByRange(string text, int start, int end, WebTVSearchResultType? type = null);
    #endregion

    #region Cards
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebCard> GetCards(string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebVirtualCard> GetActiveCards(string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebUser> GetActiveUsers(string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebRtspClient> GetStreamingClients(string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebDiskSpaceInformation> GetAllRecordingDiskInformation(string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebDiskSpaceInformation GetRecordingDiskInformationForCard(int id);
    #endregion

    #region Schedules
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult StartRecordingManual(string userName, int channelId, string title);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult AddSchedule(int channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult AddScheduleDetailed(int channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType, int preRecordInterval, int postRecordInterval, string directory, int priority);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebIntResult GetScheduleCount(string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebScheduleBasic> GetSchedules(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebScheduleBasic> GetSchedulesByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebScheduleBasic GetScheduleById(int scheduleId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult CancelSchedule(int programId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult EditSchedule(int scheduleId, int? channelId = null, string title = null, DateTime? startTime = null, DateTime? endTime = null, WebScheduleType? scheduleType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult DeleteSchedule(int scheduleId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult StopRecording(int scheduleId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebScheduledRecording> GetScheduledRecordingsForDate(DateTime date, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebScheduledRecording> GetScheduledRecordingsForToday(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    #endregion

    #region Recordings
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebIntResult GetRecordingCount(string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebRecordingBasic> GetRecordings(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebRecordingBasic> GetRecordingsByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebRecordingBasic GetRecordingById(int id);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult DeleteRecording(int id);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebRecordingFileInfo GetRecordingFileInfo(int id);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    Stream ReadRecordingFile(int id);
    #endregion

    #region TV
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebIntResult GetGroupCount(string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelGroup> GetGroups(string filter = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelGroup> GetGroupsByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebChannelGroup GetGroupById(int groupId);


    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebIntResult GetChannelCount(int? groupId = null, string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelBasic> GetChannelsBasic(int? groupId = null, string filter = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelBasic> GetChannelsBasicByRange(int start, int end, int? groupId = null, string filter = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelDetailed> GetChannelsDetailed(int? groupId = null, string filter = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelDetailed> GetChannelsDetailedByRange(int start, int end, int? groupId = null, string filter = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelState> GetAllChannelStatesForGroup(int groupId, string userName, string filter = null);
    #endregion

    #region Radio specific
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebIntResult GetRadioGroupCount(string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelGroup> GetRadioGroups(string filter = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelGroup> GetRadioGroupsByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebChannelGroup GetRadioGroupById(int groupId);


    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebIntResult GetRadioChannelCount(int? groupId = null, string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelBasic> GetRadioChannelsBasic(int? groupId = null, string filter = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelBasic> GetRadioChannelsBasicByRange(int start, int end, int? groupId = null, string filter = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelDetailed> GetRadioChannelsDetailed(int? groupId = null, string filter = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelDetailed> GetRadioChannelsDetailedByRange(int start, int end, int? groupId = null, string filter = null, WebSortField? sort = WebSortField.User, WebSortOrder? order = WebSortOrder.Asc);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelState> GetAllRadioChannelStatesForGroup(int groupId, string userName, string filter = null);
    #endregion

    #region Channels

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebChannelBasic GetChannelBasicById(int channelId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebChannelDetailed GetChannelDetailedById(int channelId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebChannelState GetChannelState(int channelId, string userName);
    #endregion

    #region Timeshifting
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebVirtualCard SwitchTVServerToChannelAndGetVirtualCard(string userName, int channelId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebStringResult SwitchTVServerToChannelAndGetStreamingUrl(string userName, int channelId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebStringResult SwitchTVServerToChannelAndGetTimeshiftFilename(string userName, int channelId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult SendHeartbeat(string userName);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult CancelCurrentTimeShifting(string userName);
    #endregion

    #region EPG
    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebProgramBasic> GetProgramsBasicForChannel(int channelId, DateTime startTime, DateTime endTime, string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebProgramDetailed> GetProgramsDetailedForChannel(int channelId, DateTime startTime, DateTime endTime, string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelPrograms<WebProgramBasic>> GetProgramsBasicForGroup(int groupId, DateTime startTime, DateTime endTime, string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebChannelPrograms<WebProgramDetailed>> GetProgramsDetailedForGroup(int groupId, DateTime startTime, DateTime endTime, string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebProgramDetailed GetCurrentProgramOnChannel(int channelId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebProgramDetailed GetNextProgramOnChannel(int channelId);


    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebIntResult SearchProgramsCount(string searchTerm);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebProgramBasic> SearchProgramsBasic(string searchTerm, string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebProgramBasic> SearchProgramsBasicByRange(string searchTerm, int start, int end, string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebProgramDetailed> SearchProgramsDetailed(string searchTerm, string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebProgramDetailed> SearchProgramsDetailedByRange(string searchTerm, int start, int end, string filter = null);


    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebProgramBasic> GetNowNextWebProgramBasicForChannel(int channelId, string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    IList<WebProgramDetailed> GetNowNextWebProgramDetailedForChannel(int channelId, string filter = null);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebProgramBasic GetProgramBasicById(int programId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebProgramDetailed GetProgramDetailedById(int programId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult GetProgramIsScheduledOnChannel(int channelId, int programId);

    [OperationContract]
    [WebGet(ResponseFormat = WebMessageFormat.Json)]
    WebBoolResult GetProgramIsScheduled(int programId);
    #endregion
  }
}
