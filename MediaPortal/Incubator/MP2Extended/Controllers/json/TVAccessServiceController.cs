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
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.ResourceAccess;
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
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Tv;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Misc;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;

namespace MediaPortal.Plugins.MP2Extended.Controllers.json
{
  [RoutePrefix("MPExtended/TVAccessService/json")]
  [Route("{action}")]
  [MediaPortalAuthorize]
  public class TVAccessServiceController : ApiController, ITVAccessServiceController
  {
    #region Misc

    [HttpGet]
    [ApiExplorerSettings]
    [AllowAnonymous]
    public Task<WebTVServiceDescription> GetServiceDescription()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetServiceDescription().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    [AllowAnonymous]
    public Task<WebBoolResult> TestConnectionToTVService()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new TestConnectionToTVService().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebStringResult> ReadSettingFromDatabase(string tagName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new ReadSettingFromDatabase().ProcessAsync(Request.GetOwinContext(), tagName);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> WriteSettingToDatabase(string tagName, string value)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebDiskSpaceInformation>> GetLocalDiskInformation()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetLocalDiskInformation().ProcessAsync(Request.GetOwinContext());
    }

    /*public Task<IList<WebTVSearchResult>> Search(string text, WebTVSearchResultType? type = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }*/

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebDictionary<string>> GetExternalMediaInfo(WebMediaType? type, string id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    /*[HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebTVSearchResult>> SearchResultsByRange(string text, int start, int end, WebTVSearchResultType? type = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }*/

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebCard>> GetCards()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetCards().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebVirtualCard>> GetActiveCards()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetActiveCards().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebUser>> GetActiveUsers()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetActiveUsers().ProcessAsync(Request.GetOwinContext());
    }

    /*[HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebRtspClient>> GetStreamingClients(string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }*/

    #endregion

    #region Schedule

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> AddSchedule(int channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new AddSchedule().ProcessAsync(Request.GetOwinContext(), channelId, title, startTime, startTime, scheduleType);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> AddScheduleDetailed(int channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType, int preRecordInterval, int postRecordInterval, string directory, int priority)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new AddScheduleDetailed().ProcessAsync(Request.GetOwinContext(), channelId, title, startTime, endTime, scheduleType, preRecordInterval, postRecordInterval, directory, priority);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> GetScheduleCount()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetScheduleCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebScheduleBasic>> GetSchedules(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetSchedules().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebScheduleBasic>> GetSchedulesByRange(int start, int end, WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetSchedulesByRange().ProcessAsync(Request.GetOwinContext(), start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebScheduleBasic> GetScheduleById(int scheduleId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetScheduleById().ProcessAsync(Request.GetOwinContext(), scheduleId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> CancelSchedule(int programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new CancelSchedule().ProcessAsync(Request.GetOwinContext(), programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> UnCancelSchedule(int programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new UnCancelSchedule().ProcessAsync(Request.GetOwinContext(), programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> EditSchedule(int scheduleId, int? channelId = null, string title = null, DateTime? startTime = null, DateTime? endTime = null, WebScheduleType? scheduleType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new EditSchedule().ProcessAsync(Request.GetOwinContext(), scheduleId, scheduleId, title, startTime, endTime, scheduleType, preRecordInterval, postRecordInterval, directory, priority);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> DeleteSchedule(int scheduleId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new DeleteSchedule().ProcessAsync(Request.GetOwinContext(), scheduleId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> StopRecording(int scheduleId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebScheduledRecording>> GetScheduledRecordingsForDate(DateTime date, WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetScheduledRecordingsForDate().ProcessAsync(Request.GetOwinContext(), date, sort, order, filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebScheduledRecording>> GetScheduledRecordingsForToday(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetScheduledRecordingsForToday().ProcessAsync(Request.GetOwinContext(), sort, order, filter);
    }

    #endregion

    #region Recording

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebDiskSpaceInformation>> GetAllRecordingDiskInformation()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetAllRecordingDiskInformation().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebDiskSpaceInformation> GetRecordingDiskInformationForCard(int id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRecordingDiskInformationForCard().ProcessAsync(Request.GetOwinContext(), id);
    }


    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> StartRecordingManual(string userName, int channelId, string title)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> GetRecordingCount()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRecordingCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebRecordingBasic>> GetRecordings(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRecordings().ProcessAsync(Request.GetOwinContext(), sort, order, filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebRecordingBasic>> GetRecordingsByRange(int start, int end, WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRecordingsByRange().ProcessAsync(Request.GetOwinContext(), start, end, sort, order, filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebRecordingBasic> GetRecordingById(Guid id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRecordingById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> DeleteRecording(int id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebRecordingFileInfo> GetRecordingFileInfo(int id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<Stream> ReadRecordingFile(int id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    #endregion

    #region Tv

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebChannelBasic> GetChannelBasicById(int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetChannelBasicById().ProcessAsync(Request.GetOwinContext(), channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebChannelDetailed> GetChannelDetailedById(int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetChannelDetailedById().ProcessAsync(Request.GetOwinContext(), channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> GetGroupCount()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetGroupCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelGroup>> GetGroups(WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetGroups().ProcessAsync(Request.GetOwinContext(), sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelGroup>> GetGroupsByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetGroupsByRange().ProcessAsync(Request.GetOwinContext(), start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebChannelGroup> GetGroupById(int groupId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetGroupById().ProcessAsync(Request.GetOwinContext(), groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> GetChannelCount(int? groupId = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetChannelCount().ProcessAsync(Request.GetOwinContext(), groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelBasic>> GetChannelsBasic(int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetChannelsBasic().ProcessAsync(Request.GetOwinContext(), sort, order, groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelBasic>> GetChannelsBasicByRange(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetChannelsBasicByRange().ProcessAsync(Request.GetOwinContext(), start, end, sort, order, groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelDetailed>> GetChannelsDetailed(int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetChannelsDetailed().ProcessAsync(Request.GetOwinContext(), groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelDetailed>> GetChannelsDetailedByRange(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetChannelsDetailedByRange().ProcessAsync(Request.GetOwinContext(), start, end, groupId, sort, order);
    }

    #endregion

    #region Radio

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> GetRadioGroupCount()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRadioGroupCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelGroup>> GetRadioGroups(WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRadioGroups().ProcessAsync(Request.GetOwinContext(), sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelGroup>> GetRadioGroupsByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRadioGroupsByRange().ProcessAsync(Request.GetOwinContext(), start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebChannelGroup> GetRadioGroupById(int groupId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRadioGroupById().ProcessAsync(Request.GetOwinContext(), groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> GetRadioChannelCount(int? groupId = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRadioChannelCount().ProcessAsync(Request.GetOwinContext(), groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelBasic>> GetRadioChannelsBasic(int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRadioChannelsBasic().ProcessAsync(Request.GetOwinContext(), groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelBasic>> GetRadioChannelsBasicByRange(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRadioChannelsBasicByRange().ProcessAsync(Request.GetOwinContext(), start, end, groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelDetailed>> GetRadioChannelsDetailed(int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRadioChannelsDetailed().ProcessAsync(Request.GetOwinContext(), groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelDetailed>> GetRadioChannelsDetailedByRange(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetRadioChannelsDetailedByRange().ProcessAsync(Request.GetOwinContext(), start, end, groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelState>> GetAllRadioChannelStatesForGroup(int groupId, string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    #endregion

    #region Channels

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebChannelState> GetChannelState(int channelId, string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetChannelState().ProcessAsync(Request.GetOwinContext(), channelId, userName);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelState>> GetAllChannelStatesForGroup(int groupId, string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetAllChannelStatesForGroup().ProcessAsync(Request.GetOwinContext(), groupId, userName);
    }

    #endregion

    #region Timeshifting

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebVirtualCard> SwitchTVServerToChannelAndGetVirtualCard(string userName, int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebStringResult> SwitchTVServerToChannelAndGetStreamingUrl(string userName, int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new SwitchTVServerToChannelAndGetStreamingUrl().ProcessAsync(Request.GetOwinContext(), userName, channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebStringResult> SwitchTVServerToChannelAndGetTimeshiftFilename(string userName, int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new SwitchTVServerToChannelAndGetTimeshiftFilename().ProcessAsync(Request.GetOwinContext(), userName, channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> SendHeartbeat(string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> CancelCurrentTimeShifting(string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new CancelCurrentTimeShifting().ProcessAsync(Request.GetOwinContext(), userName);
    }

    #endregion

    #region EPG

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramBasic>> GetProgramsBasicForChannel(int channelId, DateTime startTime, DateTime endTime)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetProgramsBasicForChannel().ProcessAsync(Request.GetOwinContext(), channelId, startTime, endTime);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramDetailed>> GetProgramsDetailedForChannel(int channelId, DateTime startTime, DateTime endTime)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetProgramsDetailedForChannel().ProcessAsync(Request.GetOwinContext(), channelId, startTime, endTime);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelPrograms<WebProgramBasic>>> GetProgramsBasicForGroup(int groupId, DateTime startTime, DateTime endTime)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetProgramsBasicForGroup().ProcessAsync(Request.GetOwinContext(), groupId, startTime, endTime);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelPrograms<WebProgramDetailed>>> GetProgramsDetailedForGroup(int groupId, DateTime startTime, DateTime endTime)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetProgramsDetailedForGroup().ProcessAsync(Request.GetOwinContext(), groupId, startTime, endTime);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebProgramDetailed> GetCurrentProgramOnChannel(int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetCurrentProgramOnChannel().ProcessAsync(Request.GetOwinContext(), channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebProgramDetailed> GetNextProgramOnChannel(int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetNextProgramOnChannel().ProcessAsync(Request.GetOwinContext(), channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> SearchProgramsCount(string searchTerm)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new SearchProgramsCount().ProcessAsync(Request.GetOwinContext(), searchTerm);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramBasic>> SearchProgramsBasic(string searchTerm)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new SearchProgramsBasic().ProcessAsync(Request.GetOwinContext(), searchTerm);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramBasic>> SearchProgramsBasicByRange(string searchTerm, int start, int end)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new SearchProgramsBasicByRange().ProcessAsync(Request.GetOwinContext(), searchTerm, start, end);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramDetailed>> SearchProgramsDetailed(string searchTerm)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new SearchProgramsDetailed().ProcessAsync(Request.GetOwinContext(), searchTerm);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramDetailed>> SearchProgramsDetailedByRange(string searchTerm, int start, int end)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new SearchProgramsDetailedByRange().ProcessAsync(Request.GetOwinContext(), searchTerm, start, end);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramBasic>> GetNowNextWebProgramBasicForChannel(int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetNowNextWebProgramBasicForChannel().ProcessAsync(Request.GetOwinContext(), channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramDetailed>> GetNowNextWebProgramDetailedForChannel(int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetNowNextWebProgramDetailedForChannel().ProcessAsync(Request.GetOwinContext(), channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebProgramBasic> GetProgramBasicById(int programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetProgramBasicById().ProcessAsync(Request.GetOwinContext(), programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebProgramDetailed> GetProgramDetailedById(int programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetProgramDetailedById().ProcessAsync(Request.GetOwinContext(), programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> GetProgramIsScheduledOnChannel(int channelId, int programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> GetProgramIsScheduled(int programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return new GetProgramIsScheduled().ProcessAsync(Request.GetOwinContext(), programId);
    }

    #endregion

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
