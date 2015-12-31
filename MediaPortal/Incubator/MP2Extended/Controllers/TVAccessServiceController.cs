using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Controllers.Interfaces;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Misc;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Recording;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Misc;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using Microsoft.AspNet.Mvc;

namespace MediaPortal.Plugins.MP2Extended.Controllers
{
  [Route("[Controller]/json/[Action]")]
  public class TVAccessServiceController : Controller, ITVAccessServiceController
  {

    #region Misc

    public WebTVServiceDescription GetServiceDescription()
    {
      return new GetServiceDescription().Process();
    }

    public WebBoolResult TestConnectionToTVService()
    {
      return new TestConnectionToTVService().Process();
    }

    public WebStringResult ReadSettingFromDatabase(string tagName)
    {
      return new ReadSettingFromDatabase().Process(tagName);
    }

    public WebBoolResult WriteSettingToDatabase(string tagName, string value)
    {
      throw new NotImplementedException();
    }

    public IList<WebDiskSpaceInformation> GetLocalDiskInformation()
    {
      return new GetLocalDiskInformation().Process();
    }

    /*public IList<WebTVSearchResult> Search(string text, WebTVSearchResultType? type = null)
    {
      throw new NotImplementedException();
    }

    public WebDictionary<string> GetExternalMediaInfo(WebMediaType? type, string id)
    {
      throw new NotImplementedException();
    }

    public IList<WebTVSearchResult> SearchResultsByRange(string text, int start, int end, WebTVSearchResultType? type = null)
    {
      throw new NotImplementedException();
    }*/

    public IList<WebCard> GetCards()
    {
      return new GetCards().Process();
    }

    public IList<WebVirtualCard> GetActiveCards()
    {
      return new GetActiveCards().Process();
    }

    public IList<WebUser> GetActiveUsers()
    {
      return new GetActiveUsers().Process();
    }

    /*public IList<WebRtspClient> GetStreamingClients(string filter = null)
    {
      throw new NotImplementedException();
    }*/

    #endregion

    #region Recording

    public IList<WebDiskSpaceInformation> GetAllRecordingDiskInformation()
    {
      return new GetAllRecordingDiskInformation().Process();
    }

    public WebDiskSpaceInformation GetRecordingDiskInformationForCard(int id)
    {
      throw new NotImplementedException();
    }


    public WebBoolResult StartRecordingManual(string userName, int channelId, string title)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region Schedule

    public WebBoolResult AddSchedule(int channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType)
    {
      return new AddSchedule().Process(channelId, title, startTime, startTime, scheduleType);
    }

    public WebBoolResult AddScheduleDetailed(int channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType, int preRecordInterval, int postRecordInterval, string directory, int priority)
    {
      return new AddScheduleDetailed().Process(channelId, title, startTime, endTime, scheduleType, preRecordInterval, postRecordInterval, directory, priority);
    }

    public WebIntResult GetScheduleCount()
    {
      return new GetScheduleCount().Process();
    }

    public IList<WebScheduleBasic> GetSchedules(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetSchedules().Process(filter, sort, order);
    }

    public IList<WebScheduleBasic> GetSchedulesByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetSchedulesByRange().Process(start, end, filter, sort, order);
    }

    public WebScheduleBasic GetScheduleById(int scheduleId)
    {
      return new GetScheduleById().Process(scheduleId);
    }

    public WebBoolResult CancelSchedule(int programId)
    {
      return new CancelSchedule().Process(programId);
    }

    public WebBoolResult EditSchedule(int scheduleId, int? channelId = null, string title = null, DateTime? startTime = null, DateTime? endTime = null, WebScheduleType? scheduleType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      return new EditSchedule().Process(scheduleId, scheduleId, title, startTime, endTime, scheduleType, preRecordInterval, postRecordInterval, directory, priority);
    }

    public WebBoolResult DeleteSchedule(int scheduleId)
    {
      throw new NotImplementedException();
    }

    public WebBoolResult StopRecording(int scheduleId)
    {
      throw new NotImplementedException();
    }

    public IList<WebScheduledRecording> GetScheduledRecordingsForDate(DateTime date, string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public IList<WebScheduledRecording> GetScheduledRecordingsForToday(string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public WebIntResult GetRecordingCount(string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebRecordingBasic> GetRecordings(string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public IList<WebRecordingBasic> GetRecordingsByRange(int start, int end, string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public WebRecordingBasic GetRecordingById(int id)
    {
      throw new NotImplementedException();
    }

    public WebBoolResult DeleteRecording(int id)
    {
      throw new NotImplementedException();
    }

    public WebRecordingFileInfo GetRecordingFileInfo(int id)
    {
      throw new NotImplementedException();
    }

    public Stream ReadRecordingFile(int id)
    {
      throw new NotImplementedException();
    }

    public WebIntResult GetGroupCount(string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelGroup> GetGroups(string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelGroup> GetGroupsByRange(int start, int end, string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public WebChannelGroup GetGroupById(int groupId)
    {
      throw new NotImplementedException();
    }

    public WebIntResult GetChannelCount(int? groupId = null, string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelBasic> GetChannelsBasic(int? groupId = null, string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelBasic> GetChannelsBasicByRange(int start, int end, int? groupId = null, string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelDetailed> GetChannelsDetailed(int? groupId = null, string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelDetailed> GetChannelsDetailedByRange(int start, int end, int? groupId = null, string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelState> GetAllChannelStatesForGroup(int groupId, string userName, string filter = null)
    {
      throw new NotImplementedException();
    }

    public WebIntResult GetRadioGroupCount(string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelGroup> GetRadioGroups(string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelGroup> GetRadioGroupsByRange(int start, int end, string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public WebChannelGroup GetRadioGroupById(int groupId)
    {
      throw new NotImplementedException();
    }

    public WebIntResult GetRadioChannelCount(int? groupId = null, string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelBasic> GetRadioChannelsBasic(int? groupId = null, string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelBasic> GetRadioChannelsBasicByRange(int start, int end, int? groupId = null, string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelDetailed> GetRadioChannelsDetailed(int? groupId = null, string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelDetailed> GetRadioChannelsDetailedByRange(int start, int end, int? groupId = null, string filter = null, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelState> GetAllRadioChannelStatesForGroup(int groupId, string userName, string filter = null)
    {
      throw new NotImplementedException();
    }

    public WebChannelBasic GetChannelBasicById(int channelId)
    {
      throw new NotImplementedException();
    }

    public WebChannelDetailed GetChannelDetailedById(int channelId)
    {
      throw new NotImplementedException();
    }

    public WebChannelState GetChannelState(int channelId, string userName)
    {
      throw new NotImplementedException();
    }

    public WebVirtualCard SwitchTVServerToChannelAndGetVirtualCard(string userName, int channelId)
    {
      throw new NotImplementedException();
    }

    public WebStringResult SwitchTVServerToChannelAndGetStreamingUrl(string userName, int channelId)
    {
      throw new NotImplementedException();
    }

    public WebStringResult SwitchTVServerToChannelAndGetTimeshiftFilename(string userName, int channelId)
    {
      throw new NotImplementedException();
    }

    public WebBoolResult SendHeartbeat(string userName)
    {
      throw new NotImplementedException();
    }

    public WebBoolResult CancelCurrentTimeShifting(string userName)
    {
      throw new NotImplementedException();
    }

    public IList<WebProgramBasic> GetProgramsBasicForChannel(int channelId, DateTime startTime, DateTime endTime, string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebProgramDetailed> GetProgramsDetailedForChannel(int channelId, DateTime startTime, DateTime endTime, string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelPrograms<WebProgramBasic>> GetProgramsBasicForGroup(int groupId, DateTime startTime, DateTime endTime, string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebChannelPrograms<WebProgramDetailed>> GetProgramsDetailedForGroup(int groupId, DateTime startTime, DateTime endTime, string filter = null)
    {
      throw new NotImplementedException();
    }

    public WebProgramDetailed GetCurrentProgramOnChannel(int channelId)
    {
      throw new NotImplementedException();
    }

    public WebProgramDetailed GetNextProgramOnChannel(int channelId)
    {
      throw new NotImplementedException();
    }

    public WebIntResult SearchProgramsCount(string searchTerm)
    {
      throw new NotImplementedException();
    }

    public IList<WebProgramBasic> SearchProgramsBasic(string searchTerm, string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebProgramBasic> SearchProgramsBasicByRange(string searchTerm, int start, int end, string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebProgramDetailed> SearchProgramsDetailed(string searchTerm, string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebProgramDetailed> SearchProgramsDetailedByRange(string searchTerm, int start, int end, string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebProgramBasic> GetNowNextWebProgramBasicForChannel(int channelId, string filter = null)
    {
      throw new NotImplementedException();
    }

    public IList<WebProgramDetailed> GetNowNextWebProgramDetailedForChannel(int channelId, string filter = null)
    {
      throw new NotImplementedException();
    }

    public WebProgramBasic GetProgramBasicById(int programId)
    {
      throw new NotImplementedException();
    }

    public WebProgramDetailed GetProgramDetailedById(int programId)
    {
      throw new NotImplementedException();
    }

    public WebBoolResult GetProgramIsScheduledOnChannel(int channelId, int programId)
    {
      throw new NotImplementedException();
    }

    public WebBoolResult GetProgramIsScheduled(int programId)
    {
      throw new NotImplementedException();
    }
  }
}
