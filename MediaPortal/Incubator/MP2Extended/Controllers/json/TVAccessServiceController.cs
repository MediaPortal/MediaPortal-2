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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Controllers.Interfaces;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Misc;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.MP2Extended.Controllers.json
{
  [ApiController]
  [Route("MPExtended/TVAccessService/json/[action]")]
  [MediaPortalAuthorize]
  public class TVAccessServiceController : Controller, ITVAccessServiceController
  {
    #region Misc

    [HttpGet]
    [ApiExplorerSettings]
    [AllowAnonymous]
    public Task<WebTVServiceDescription> GetServiceDescription()
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Misc.GetServiceDescription.ProcessAsync(ControllerContext.HttpContext);
    }

    [HttpGet]
    [ApiExplorerSettings]
    [AllowAnonymous]
    public Task<WebBoolResult> TestConnectionToTVService()
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Misc.TestConnectionToTVService.ProcessAsync(ControllerContext.HttpContext);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebStringResult> ReadSettingFromDatabase(string tagName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Misc.ReadSettingFromDatabase.ProcessAsync(ControllerContext.HttpContext, tagName);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> WriteSettingToDatabase(string tagName, string value)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Misc.WriteSettingToDatabase.ProcessAsync(ControllerContext.HttpContext, tagName, value);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebDiskSpaceInformation>> GetLocalDiskInformation()
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Misc.GetLocalDiskInformation.ProcessAsync(ControllerContext.HttpContext);
    }

    //[HttpGet]
    //[ApiExplorerSettings]
    //public Task<IList<WebTVSearchResult>> Search(string text, WebTVSearchResultType? type = null)
    //{
    //  Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());

    //}

    //[HttpGet]
    //[ApiExplorerSettings]
    //public Task<WebDictionary<string>> GetExternalMediaInfo(WebMediaType? type, string id)
    //{
    //  Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());

    //}

    //[HttpGet]
    //[ApiExplorerSettings]
    //public Task<IList<WebTVSearchResult>> SearchResultsByRange(string text, int start, int end, WebTVSearchResultType? type = null)
    //{
    //  Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());

    //}

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebCard>> GetCards()
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Misc.GetCards.ProcessAsync(ControllerContext.HttpContext);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebVirtualCard>> GetActiveCards()
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Misc.GetActiveCards.ProcessAsync(ControllerContext.HttpContext);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebUser>> GetActiveUsers()
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Misc.GetActiveUsers.ProcessAsync(ControllerContext.HttpContext);
    }

    //[HttpGet]
    //[ApiExplorerSettings]
    //public Task<IList<WebRtspClient>> GetStreamingClients(string filter = null)
    //{
    //  Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());

    //}

    #endregion

    #region Schedule

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> AddSchedule(string channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.AddSchedule.ProcessAsync(ControllerContext.HttpContext, channelId, title, startTime, endTime, scheduleType);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> AddScheduleDetailed(string channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType, int preRecordInterval, int postRecordInterval, string directory, int priority)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.AddScheduleDetailed.ProcessAsync(ControllerContext.HttpContext, channelId, title, startTime, endTime, scheduleType, preRecordInterval, postRecordInterval, directory, priority);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> GetScheduleCount()
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.GetScheduleCount.ProcessAsync(ControllerContext.HttpContext);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebScheduleBasic>> GetSchedules(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.GetSchedules.ProcessAsync(ControllerContext.HttpContext, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebScheduleBasic>> GetSchedulesByRange(int start, int end, WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.GetSchedulesByRange.ProcessAsync(ControllerContext.HttpContext, start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebScheduleBasic> GetScheduleById(string scheduleId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.GetScheduleById.ProcessAsync(ControllerContext.HttpContext, scheduleId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> CancelSchedule(string programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.CancelSchedule.ProcessAsync(ControllerContext.HttpContext, programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> UnCancelSchedule(string programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.UnCancelSchedule.ProcessAsync(ControllerContext.HttpContext, programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> EditSchedule(string scheduleId, string channelId = null, string title = null, DateTime? startTime = null, DateTime? endTime = null, WebScheduleType? scheduleType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.EditSchedule.ProcessAsync(ControllerContext.HttpContext, scheduleId, scheduleId, title, startTime, endTime, scheduleType, preRecordInterval, postRecordInterval, directory, priority);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> DeleteSchedule(string scheduleId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.DeleteSchedule.ProcessAsync(ControllerContext.HttpContext, scheduleId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> StopRecording(string scheduleId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.DeleteSchedule.ProcessAsync(ControllerContext.HttpContext, scheduleId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebScheduledRecording>> GetScheduledRecordingsForDate(DateTime date, WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.GetScheduledRecordingsForDate.ProcessAsync(ControllerContext.HttpContext, date, sort, order, filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebScheduledRecording>> GetScheduledRecordingsForToday(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.GetScheduledRecordingsForToday.ProcessAsync(ControllerContext.HttpContext, sort, order, filter);
    }

    #endregion

    #region Recording

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebDiskSpaceInformation>> GetAllRecordingDiskInformation()
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Recording.GetAllRecordingDiskInformation.ProcessAsync(ControllerContext.HttpContext);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebDiskSpaceInformation> GetRecordingDiskInformationForCard(string id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Recording.GetRecordingDiskInformationForCard.ProcessAsync(ControllerContext.HttpContext, id);
    }


    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> StartRecordingManual(string userName, string channelId, string title)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.AddSchedule.ProcessAsync(ControllerContext.HttpContext, channelId, title, DateTime.Now, DateTime.Now.AddDays(1),  WebScheduleType.Once);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> GetRecordingCount()
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Recording.GetRecordingCount.ProcessAsync(ControllerContext.HttpContext);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebRecordingBasic>> GetRecordings(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Recording.GetRecordings.ProcessAsync(ControllerContext.HttpContext, sort, order, filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebRecordingBasic>> GetRecordingsByRange(int start, int end, WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Recording.GetRecordingsByRange.ProcessAsync(ControllerContext.HttpContext, start, end, sort, order, filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebRecordingBasic> GetRecordingById(string id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Recording.GetRecordingById.ProcessAsync(ControllerContext.HttpContext, id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> DeleteRecording(string id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Recording.DeleteRecording.ProcessAsync(ControllerContext.HttpContext, id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebRecordingFileInfo> GetRecordingFileInfo(string id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Recording.GetRecordingFileInfo.ProcessAsync(ControllerContext.HttpContext, id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<Stream> ReadRecordingFile(string id)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Recording.ReadRecordingFile.ProcessAsync(ControllerContext.HttpContext, id);
    }

    #endregion

    #region Tv

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebChannelBasic> GetChannelBasicById(string channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Tv.GetChannelBasicById.ProcessAsync(ControllerContext.HttpContext, channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebChannelDetailed> GetChannelDetailedById(string channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Tv.GetChannelDetailedById.ProcessAsync(ControllerContext.HttpContext, channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> GetGroupCount()
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Tv.GetGroupCount.ProcessAsync(ControllerContext.HttpContext);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelGroup>> GetGroups(WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Tv.GetGroups.ProcessAsync(ControllerContext.HttpContext, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelGroup>> GetGroupsByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Tv.GetGroupsByRange.ProcessAsync(ControllerContext.HttpContext, start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebChannelGroup> GetGroupById(string groupId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Tv.GetGroupById.ProcessAsync(ControllerContext.HttpContext, groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> GetChannelCount(string groupId = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Tv.GetChannelCount.ProcessAsync(ControllerContext.HttpContext, groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelBasic>> GetChannelsBasic(string groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Tv.GetChannelsBasic.ProcessAsync(ControllerContext.HttpContext, sort, order, groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelBasic>> GetChannelsBasicByRange(int start, int end, string groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Tv.GetChannelsBasicByRange.ProcessAsync(ControllerContext.HttpContext, start, end, sort, order, groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelDetailed>> GetChannelsDetailed(string groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Tv.GetChannelsDetailed.ProcessAsync(ControllerContext.HttpContext, groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelDetailed>> GetChannelsDetailedByRange(int start, int end, string groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Tv.GetChannelsDetailedByRange.ProcessAsync(ControllerContext.HttpContext, start, end, groupId, sort, order);
    }

    #endregion

    #region Radio

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> GetRadioGroupCount()
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Radio.GetRadioGroupCount.ProcessAsync(ControllerContext.HttpContext);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelGroup>> GetRadioGroups(WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Radio.GetRadioGroups.ProcessAsync(ControllerContext.HttpContext, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelGroup>> GetRadioGroupsByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Radio.GetRadioGroupsByRange.ProcessAsync(ControllerContext.HttpContext, start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebChannelGroup> GetRadioGroupById(string groupId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Radio.GetRadioGroupById.ProcessAsync(ControllerContext.HttpContext, groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> GetRadioChannelCount(string groupId = null)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Radio.GetRadioChannelCount.ProcessAsync(ControllerContext.HttpContext, groupId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelBasic>> GetRadioChannelsBasic(string groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Radio.GetRadioChannelsBasic.ProcessAsync(ControllerContext.HttpContext, groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelBasic>> GetRadioChannelsBasicByRange(int start, int end, string groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Radio.GetRadioChannelsBasicByRange.ProcessAsync(ControllerContext.HttpContext, start, end, groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelDetailed>> GetRadioChannelsDetailed(string groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Radio.GetRadioChannelsDetailed.ProcessAsync(ControllerContext.HttpContext, groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelDetailed>> GetRadioChannelsDetailedByRange(int start, int end, string groupId, WebSortField? sort, WebSortOrder? order)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Radio.GetRadioChannelsDetailedByRange.ProcessAsync(ControllerContext.HttpContext, start, end, groupId, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelState>> GetAllRadioChannelStatesForGroup(string groupId, string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Radio.GetAllRadioChannelStatesForGroup.ProcessAsync(ControllerContext.HttpContext, groupId, userName);
    }

    #endregion

    #region Channels

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebChannelState> GetChannelState(string channelId, string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Channels.GetChannelState.ProcessAsync(ControllerContext.HttpContext, channelId, userName);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelState>> GetAllChannelStatesForGroup(string groupId, string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Channels.GetAllChannelStatesForGroup.ProcessAsync(ControllerContext.HttpContext, groupId, userName);
    }

    #endregion

    #region Timeshifting

    //[HttpGet]
    //[ApiExplorerSettings]
    //public Task<WebVirtualCard> SwitchTVServerToChannelAndGetVirtualCard(string userName, string channelId)
    //{
    //  Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());

    //}

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebStringResult> SwitchTVServerToChannelAndGetStreamingUrl(string userName, string channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Timeshiftings.SwitchTVServerToChannelAndGetStreamingUrl.ProcessAsync(ControllerContext.HttpContext, userName, channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebStringResult> SwitchTVServerToChannelAndGetTimeshiftFilename(string userName, string channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Timeshiftings.SwitchTVServerToChannelAndGetTimeshiftFilename.ProcessAsync(ControllerContext.HttpContext, userName, channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> SendHeartbeat(string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return Task.FromResult(new WebBoolResult { Result = false });
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> CancelCurrentTimeShifting(string userName)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Timeshiftings.CancelCurrentTimeShifting.ProcessAsync(ControllerContext.HttpContext, userName);
    }

    #endregion

    #region EPG

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramBasic>> GetProgramsBasicForChannel(string channelId, DateTime startTime, DateTime endTime)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.GetProgramsBasicForChannel.ProcessAsync(ControllerContext.HttpContext, channelId, startTime, endTime);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramDetailed>> GetProgramsDetailedForChannel(string channelId, DateTime startTime, DateTime endTime)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.GetProgramsDetailedForChannel.ProcessAsync(ControllerContext.HttpContext, channelId, startTime, endTime);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelPrograms<WebProgramBasic>>> GetProgramsBasicForGroup(string groupId, DateTime startTime, DateTime endTime)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.GetProgramsBasicForGroup.ProcessAsync(ControllerContext.HttpContext, groupId, startTime, endTime);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebChannelPrograms<WebProgramDetailed>>> GetProgramsDetailedForGroup(string groupId, DateTime startTime, DateTime endTime)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.GetProgramsDetailedForGroup.ProcessAsync(ControllerContext.HttpContext, groupId, startTime, endTime);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebProgramDetailed> GetCurrentProgramOnChannel(string channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.GetCurrentProgramOnChannel.ProcessAsync(ControllerContext.HttpContext, channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebProgramDetailed> GetNextProgramOnChannel(string channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.GetNextProgramOnChannel.ProcessAsync(ControllerContext.HttpContext, channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebIntResult> SearchProgramsCount(string searchTerm)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.SearchProgramsCount.ProcessAsync(ControllerContext.HttpContext, searchTerm);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramBasic>> SearchProgramsBasic(string searchTerm)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.SearchProgramsBasic.ProcessAsync(ControllerContext.HttpContext, searchTerm);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramBasic>> SearchProgramsBasicByRange(string searchTerm, int start, int end)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.SearchProgramsBasicByRange.ProcessAsync(ControllerContext.HttpContext, searchTerm, start, end);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramDetailed>> SearchProgramsDetailed(string searchTerm)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.SearchProgramsDetailed.ProcessAsync(ControllerContext.HttpContext, searchTerm);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramDetailed>> SearchProgramsDetailedByRange(string searchTerm, int start, int end)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.SearchProgramsDetailedByRange.ProcessAsync(ControllerContext.HttpContext, searchTerm, start, end);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramBasic>> GetNowNextWebProgramBasicForChannel(string channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.GetNowNextWebProgramBasicForChannel.ProcessAsync(ControllerContext.HttpContext, channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<IList<WebProgramDetailed>> GetNowNextWebProgramDetailedForChannel(string channelId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.GetNowNextWebProgramDetailedForChannel.ProcessAsync(ControllerContext.HttpContext, channelId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebProgramBasic> GetProgramBasicById(string programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.GetProgramBasicById.ProcessAsync(ControllerContext.HttpContext, programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebProgramDetailed> GetProgramDetailedById(string programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.GetProgramDetailedById.ProcessAsync(ControllerContext.HttpContext, programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> GetProgramIsScheduledOnChannel(string channelId, string programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.EPG.GetProgramIsScheduledOnChannel.ProcessAsync(ControllerContext.HttpContext, channelId, programId);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public Task<WebBoolResult> GetProgramIsScheduled(string programId)
    {
      Logger.Debug("TAS Request: {0}", Request.GetDisplayUrl());
      return ResourceAccess.TAS.Schedule.GetProgramIsScheduled.ProcessAsync(ControllerContext.HttpContext, programId);
    }

    #endregion

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
