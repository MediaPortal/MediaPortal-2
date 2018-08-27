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
    public async Task<WebTVServiceDescription> GetServiceDescription()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetServiceDescription().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    [AllowAnonymous]
    public async Task<WebBoolResult> TestConnectionToTVService()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new TestConnectionToTVService().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebStringResult> ReadSettingFromDatabase(string tagName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new ReadSettingFromDatabase().ProcessAsync(Request.GetOwinContext(), tagName);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> WriteSettingToDatabase(string tagName, string value)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebDiskSpaceInformation>> GetLocalDiskInformation()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetLocalDiskInformation().ProcessAsync(Request.GetOwinContext());
    }

    /*public async Task<IList<WebTVSearchResult>> Search(string text, WebTVSearchResultType? type = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }*/

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebDictionary<string>> GetExternalMediaInfo(WebMediaType? type, string id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    /*[HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVSearchResult>> SearchResultsByRange(string text, int start, int end, WebTVSearchResultType? type = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }*/

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebCard>> GetCards()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetCards().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebVirtualCard>> GetActiveCards()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetActiveCards().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebUser>> GetActiveUsers()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetActiveUsers().ProcessAsync(Request.GetOwinContext());
    }

    /*[HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebRtspClient>> GetStreamingClients(string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }*/

    #endregion

    #region Schedule

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> AddSchedule(int channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new AddSchedule().ProcessAsync(Request.GetOwinContext(), channelId, title, startTime, startTime, scheduleType);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> AddScheduleDetailed(int channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType, int preRecordInterval, int postRecordInterval, string directory, int priority)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new AddScheduleDetailed().ProcessAsync(Request.GetOwinContext(), channelId, title, startTime, endTime, scheduleType, preRecordInterval, postRecordInterval, directory, priority);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetScheduleCount()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetScheduleCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebScheduleBasic>> GetSchedules(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetSchedules().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebScheduleBasic>> GetSchedulesByRange(int start, int end, WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetSchedulesByRange().ProcessAsync(Request.GetOwinContext(), start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebScheduleBasic> GetScheduleById(int scheduleId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetScheduleById().ProcessAsync(Request.GetOwinContext(), scheduleId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> CancelSchedule(int programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new CancelSchedule().ProcessAsync(Request.GetOwinContext(), programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> UnCancelSchedule(int programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new UnCancelSchedule().ProcessAsync(Request.GetOwinContext(), programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> EditSchedule(int scheduleId, int? channelId = null, string title = null, DateTime? startTime = null, DateTime? endTime = null, WebScheduleType? scheduleType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new EditSchedule().ProcessAsync(Request.GetOwinContext(), scheduleId, scheduleId, title, startTime, endTime, scheduleType, preRecordInterval, postRecordInterval, directory, priority);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> DeleteSchedule(int scheduleId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new DeleteSchedule().ProcessAsync(Request.GetOwinContext(), scheduleId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> StopRecording(int scheduleId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebScheduledRecording>> GetScheduledRecordingsForDate(DateTime date, WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetScheduledRecordingsForDate().ProcessAsync(Request.GetOwinContext(), date, sort, order, filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebScheduledRecording>> GetScheduledRecordingsForToday(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetScheduledRecordingsForToday().ProcessAsync(Request.GetOwinContext(), sort, order, filter);
    }

    #endregion

    #region Recording

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebDiskSpaceInformation>> GetAllRecordingDiskInformation()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetAllRecordingDiskInformation().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebDiskSpaceInformation> GetRecordingDiskInformationForCard(int id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRecordingDiskInformationForCard().ProcessAsync(Request.GetOwinContext(), id);
    }


    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> StartRecordingManual(string userName, int channelId, string title)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetRecordingCount()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRecordingCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebRecordingBasic>> GetRecordings(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRecordings().ProcessAsync(Request.GetOwinContext(), sort, order, filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebRecordingBasic>> GetRecordingsByRange(int start, int end, WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRecordingsByRange().ProcessAsync(Request.GetOwinContext(), start, end, sort, order, filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebRecordingBasic> GetRecordingById(Guid id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRecordingById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> DeleteRecording(int id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebRecordingFileInfo> GetRecordingFileInfo(int id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<Stream> ReadRecordingFile(int id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    #endregion

    #region Tv

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebChannelBasic> GetChannelBasicById(int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetChannelBasicById().ProcessAsync(Request.GetOwinContext(), channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebChannelDetailed> GetChannelDetailedById(int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetChannelDetailedById().ProcessAsync(Request.GetOwinContext(), channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetGroupCount()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetGroupCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelGroup>> GetGroups(WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetGroups().ProcessAsync(Request.GetOwinContext(), sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelGroup>> GetGroupsByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetGroupsByRange().ProcessAsync(Request.GetOwinContext(), start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebChannelGroup> GetGroupById(int groupId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetGroupById().ProcessAsync(Request.GetOwinContext(), groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetChannelCount(int? groupId = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetChannelCount().ProcessAsync(Request.GetOwinContext(), groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelBasic>> GetChannelsBasic(int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetChannelsBasic().ProcessAsync(Request.GetOwinContext(), sort, order, groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelBasic>> GetChannelsBasicByRange(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetChannelsBasicByRange().ProcessAsync(Request.GetOwinContext(), start, end, sort, order, groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelDetailed>> GetChannelsDetailed(int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetChannelsDetailed().ProcessAsync(Request.GetOwinContext(), groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelDetailed>> GetChannelsDetailedByRange(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetChannelsDetailedByRange().ProcessAsync(Request.GetOwinContext(), start, end, groupId, sort, order);
    }

    #endregion

    #region Radio

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetRadioGroupCount()
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRadioGroupCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelGroup>> GetRadioGroups(WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRadioGroups().ProcessAsync(Request.GetOwinContext(), sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelGroup>> GetRadioGroupsByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRadioGroupsByRange().ProcessAsync(Request.GetOwinContext(), start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebChannelGroup> GetRadioGroupById(int groupId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRadioGroupById().ProcessAsync(Request.GetOwinContext(), groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetRadioChannelCount(int? groupId = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRadioChannelCount().ProcessAsync(Request.GetOwinContext(), groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelBasic>> GetRadioChannelsBasic(int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRadioChannelsBasic().ProcessAsync(Request.GetOwinContext(), groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelBasic>> GetRadioChannelsBasicByRange(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRadioChannelsBasicByRange().ProcessAsync(Request.GetOwinContext(), start, end, groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelDetailed>> GetRadioChannelsDetailed(int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRadioChannelsDetailed().ProcessAsync(Request.GetOwinContext(), groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelDetailed>> GetRadioChannelsDetailedByRange(int start, int end, int? groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetRadioChannelsDetailedByRange().ProcessAsync(Request.GetOwinContext(), start, end, groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelState>> GetAllRadioChannelStatesForGroup(int groupId, string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    #endregion

    #region Channels

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebChannelState> GetChannelState(int channelId, string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetChannelState().ProcessAsync(Request.GetOwinContext(), channelId, userName);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelState>> GetAllChannelStatesForGroup(int groupId, string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetAllChannelStatesForGroup().ProcessAsync(Request.GetOwinContext(), groupId, userName);
    }

    #endregion

    #region Timeshifting

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebVirtualCard> SwitchTVServerToChannelAndGetVirtualCard(string userName, int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebStringResult> SwitchTVServerToChannelAndGetStreamingUrl(string userName, int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new SwitchTVServerToChannelAndGetStreamingUrl().ProcessAsync(Request.GetOwinContext(), userName, channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebStringResult> SwitchTVServerToChannelAndGetTimeshiftFilename(string userName, int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new SwitchTVServerToChannelAndGetTimeshiftFilename().ProcessAsync(Request.GetOwinContext(), userName, channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> SendHeartbeat(string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> CancelCurrentTimeShifting(string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new CancelCurrentTimeShifting().ProcessAsync(Request.GetOwinContext(), userName);
    }

    #endregion

    #region EPG

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebProgramBasic>> GetProgramsBasicForChannel(int channelId, DateTime startTime, DateTime endTime)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetProgramsBasicForChannel().ProcessAsync(Request.GetOwinContext(), channelId, startTime, endTime);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebProgramDetailed>> GetProgramsDetailedForChannel(int channelId, DateTime startTime, DateTime endTime)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetProgramsDetailedForChannel().ProcessAsync(Request.GetOwinContext(), channelId, startTime, endTime);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelPrograms<WebProgramBasic>>> GetProgramsBasicForGroup(int groupId, DateTime startTime, DateTime endTime)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetProgramsBasicForGroup().ProcessAsync(Request.GetOwinContext(), groupId, startTime, endTime);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebChannelPrograms<WebProgramDetailed>>> GetProgramsDetailedForGroup(int groupId, DateTime startTime, DateTime endTime)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetProgramsDetailedForGroup().ProcessAsync(Request.GetOwinContext(), groupId, startTime, endTime);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebProgramDetailed> GetCurrentProgramOnChannel(int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetCurrentProgramOnChannel().ProcessAsync(Request.GetOwinContext(), channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebProgramDetailed> GetNextProgramOnChannel(int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetNextProgramOnChannel().ProcessAsync(Request.GetOwinContext(), channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> SearchProgramsCount(string searchTerm)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new SearchProgramsCount().ProcessAsync(Request.GetOwinContext(), searchTerm);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebProgramBasic>> SearchProgramsBasic(string searchTerm)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new SearchProgramsBasic().ProcessAsync(Request.GetOwinContext(), searchTerm);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebProgramBasic>> SearchProgramsBasicByRange(string searchTerm, int start, int end)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new SearchProgramsBasicByRange().ProcessAsync(Request.GetOwinContext(), searchTerm, start, end);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebProgramDetailed>> SearchProgramsDetailed(string searchTerm)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new SearchProgramsDetailed().ProcessAsync(Request.GetOwinContext(), searchTerm);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebProgramDetailed>> SearchProgramsDetailedByRange(string searchTerm, int start, int end)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new SearchProgramsDetailedByRange().ProcessAsync(Request.GetOwinContext(), searchTerm, start, end);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebProgramBasic>> GetNowNextWebProgramBasicForChannel(int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetNowNextWebProgramBasicForChannel().ProcessAsync(Request.GetOwinContext(), channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebProgramDetailed>> GetNowNextWebProgramDetailedForChannel(int channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetNowNextWebProgramDetailedForChannel().ProcessAsync(Request.GetOwinContext(), channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebProgramBasic> GetProgramBasicById(int programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetProgramBasicById().ProcessAsync(Request.GetOwinContext(), programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebProgramDetailed> GetProgramDetailedById(int programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetProgramDetailedById().ProcessAsync(Request.GetOwinContext(), programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> GetProgramIsScheduledOnChannel(int channelId, int programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> GetProgramIsScheduled(int programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetOwinContext().Request.Uri);
      return await new GetProgramIsScheduled().ProcessAsync(Request.GetOwinContext(), programId);
    }

    #endregion

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
