using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Controllers.Interfaces;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Channels;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Misc;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Radio;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Recording;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Timeshiftings;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Misc;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using Microsoft.AspNet.Mvc;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv;

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
    }*/

    public WebDictionary<string> GetExternalMediaInfo(WebMediaType? type, string id)
    {
      throw new NotImplementedException();
    }

    /*public IList<WebTVSearchResult> SearchResultsByRange(string text, int start, int end, WebTVSearchResultType? type = null)
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

    public IList<WebScheduleBasic> GetSchedules(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      return new GetSchedules().Process(filter, sort, order);
    }

    public IList<WebScheduleBasic> GetSchedulesByRange(int start, int end, WebSortField? sort, WebSortOrder? order, string filter = null)
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
      return new DeleteSchedule().Process(scheduleId);
    }

    public WebBoolResult StopRecording(int scheduleId)
    {
      throw new NotImplementedException();
    }

    public IList<WebScheduledRecording> GetScheduledRecordingsForDate(DateTime date, WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      return new GetScheduledRecordingsForDate().Process(date, sort, order, filter);
    }

    public IList<WebScheduledRecording> GetScheduledRecordingsForToday(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      return new GetScheduledRecordingsForToday().Process(sort, order, filter);
    }

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

    public WebIntResult GetRecordingCount()
    {
      return new GetRecordingCount().Process();
    }

    public IList<WebRecordingBasic> GetRecordings(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      return new GetRecordings().Process(sort, order, filter);
    }

    public IList<WebRecordingBasic> GetRecordingsByRange(int start, int end, WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      return new GetRecordingsByRange().Process(start, end, sort, order, filter);
    }

    public WebRecordingBasic GetRecordingById(Guid id)
    {
      return new GetRecordingById().Process(id);
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

    #endregion

    #region Tv

    public WebChannelBasic GetChannelBasicById(int channelId)
    {
      return new GetChannelBasicById().Process(channelId);
    }

    public WebChannelDetailed GetChannelDetailedById(int channelId)
    {
      return new GetChannelDetailedById().Process(channelId);
    }

    public WebIntResult GetGroupCount()
    {
      return new GetGroupCount().Process();
    }

    public IList<WebChannelGroup> GetGroups(WebSortField? sort, WebSortOrder? order)
    {
      return new GetGroups().Process(sort, order);
    }

    public IList<WebChannelGroup> GetGroupsByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return new GetGroupsByRange().Process(start, end, sort, order);
    }

    public WebChannelGroup GetGroupById(int groupId)
    {
      return new GetGroupById().Process(groupId);
    }

    public WebIntResult GetChannelCount(int? groupId = null)
    {
      return new GetChannelCount().Process(groupId);
    }

    public IList<WebChannelBasic> GetChannelsBasic(int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      return new GetChannelsBasic().Process(sort, order, groupId);
    }

    public IList<WebChannelBasic> GetChannelsBasicByRange(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      return new GetChannelsBasicByRange().Process(start, end, sort, order, groupId);
    }

    public IList<WebChannelDetailed> GetChannelsDetailed(int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      return new GetChannelsDetailed().Process(groupId, sort, order);
    }

    public IList<WebChannelDetailed> GetChannelsDetailedByRange(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      return new GetChannelsDetailedByRange().Process(start, end, groupId, sort, order);
    }

    #endregion

    #region Radio

    public WebIntResult GetRadioGroupCount()
    {
      return new GetRadioGroupCount().Process();
    }

    public IList<WebChannelGroup> GetRadioGroups(WebSortField? sort, WebSortOrder? order)
    {
      return new GetRadioGroups().Process(sort, order);
    }

    public IList<WebChannelGroup> GetRadioGroupsByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return new GetRadioGroupsByRange().Process(start, end, sort, order);
    }

    public WebChannelGroup GetRadioGroupById(int groupId)
    {
      return new GetRadioGroupById().Process(groupId);
    }

    public WebIntResult GetRadioChannelCount(int? groupId = null)
    {
      return new GetRadioChannelCount().Process(groupId);
    }

    public IList<WebChannelBasic> GetRadioChannelsBasic(int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      return new GetRadioChannelsBasic().Process(groupId, sort, order);
    }

    public IList<WebChannelBasic> GetRadioChannelsBasicByRange(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      return new GetRadioChannelsBasicByRange().Process(start, end, groupId, sort, order);
    }

    public IList<WebChannelDetailed> GetRadioChannelsDetailed(int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      return new GetRadioChannelsDetailed().Process(groupId, sort, order);
    }

    public IList<WebChannelDetailed> GetRadioChannelsDetailedByRange(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      return new GetRadioChannelsDetailedByRange().Process(start, end, groupId, sort, order);
    }

    public IList<WebChannelState> GetAllRadioChannelStatesForGroup(int groupId, string userName)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region Channels

    public WebChannelState GetChannelState(int channelId, string userName)
    {
      return new GetChannelState().Process(channelId, userName);
    }

    public IList<WebChannelState> GetAllChannelStatesForGroup(int groupId, string userName)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region Timeshifting

    public WebVirtualCard SwitchTVServerToChannelAndGetVirtualCard(string userName, int channelId)
    {
      throw new NotImplementedException();
    }

    public WebStringResult SwitchTVServerToChannelAndGetStreamingUrl(string userName, int channelId)
    {
      return new SwitchTVServerToChannelAndGetStreamingUrl().Process(userName, channelId);
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
      return new CancelCurrentTimeShifting().Process(userName);
    }

    #endregion

    #region EPG

    public IList<WebProgramBasic> GetProgramsBasicForChannel(int channelId, DateTime startTime, DateTime endTime)
    {
      return new GetProgramsBasicForChannel().Process(channelId, startTime, endTime);
    }

    public IList<WebProgramDetailed> GetProgramsDetailedForChannel(int channelId, DateTime startTime, DateTime endTime)
    {
      return new GetProgramsDetailedForChannel().Process(channelId, startTime, endTime);
    }

    public IList<WebChannelPrograms<WebProgramBasic>> GetProgramsBasicForGroup(int groupId, DateTime startTime, DateTime endTime)
    {
      return new GetProgramsBasicForGroup().Process(groupId, startTime, endTime);
    }

    public IList<WebChannelPrograms<WebProgramDetailed>> GetProgramsDetailedForGroup(int groupId, DateTime startTime, DateTime endTime)
    {
      return new GetProgramsDetailedForGroup().Process(groupId, startTime, endTime);
    }

    public WebProgramDetailed GetCurrentProgramOnChannel(int channelId)
    {
      return new GetCurrentProgramOnChannel().Process(channelId);
    }

    public WebProgramDetailed GetNextProgramOnChannel(int channelId)
    {
      return new GetNextProgramOnChannel().Process(channelId);
    }

    public WebIntResult SearchProgramsCount(string searchTerm)
    {
      return new SearchProgramsCount().Process(searchTerm);
    }

    public IList<WebProgramBasic> SearchProgramsBasic(string searchTerm)
    {
      return new SearchProgramsBasic().Process(searchTerm);
    }

    public IList<WebProgramBasic> SearchProgramsBasicByRange(string searchTerm, int start, int end)
    {
      return new SearchProgramsBasicByRange().Process(searchTerm, start, end);
    }

    public IList<WebProgramDetailed> SearchProgramsDetailed(string searchTerm)
    {
      return new SearchProgramsDetailed().Process(searchTerm);
    }

    public IList<WebProgramDetailed> SearchProgramsDetailedByRange(string searchTerm, int start, int end)
    {
      return new SearchProgramsDetailedByRange().Process(searchTerm, start, end);
    }

    public IList<WebProgramBasic> GetNowNextWebProgramBasicForChannel(int channelId)
    {
      return new GetNowNextWebProgramBasicForChannel().Process(channelId);
    }

    public IList<WebProgramDetailed> GetNowNextWebProgramDetailedForChannel(int channelId)
    {
      return new GetNowNextWebProgramDetailedForChannel().Process(channelId);
    }

    public WebProgramBasic GetProgramBasicById(int programId)
    {
      return new GetProgramBasicById().Process(programId);
    }

    public WebProgramDetailed GetProgramDetailedById(int programId)
    {
      return new GetProgramDetailedById().Process(programId);
    }

    public WebBoolResult GetProgramIsScheduledOnChannel(int channelId, int programId)
    {
      throw new NotImplementedException();
    }

    public WebBoolResult GetProgramIsScheduled(int programId)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
