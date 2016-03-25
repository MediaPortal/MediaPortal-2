#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Net;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using MediaPortal.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace MediaPortal.Plugins.AspNetWebApi.Controllers.Tv
{
  /// <summary>
  /// AspNet MVC Controller for Tv Program Information
  /// </summary>
  [Route("v1/Tv/[Controller]")]
  public class ScheduleController : Controller
  {

    #region Private fields

    private readonly ILogger _logger;

    #endregion

    #region Constructor

    public ScheduleController(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger<ChannelAndGroupInfoController>();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// GET /api/v1/Tv/Schedule/ScheduleByProgram
    /// </summary>
    /// <param name="programId">Id of the program which should be scheduled</param>
    /// <param name="recordingType">Type of the recording: once, weekly,...</param>
    /// <returns>Retunrs a list with the successful Schedule</returns>
    [HttpPut("ScheduleByProgram/{programId}/{recordingType}")]
    public IList<ISchedule> CreateScheduleByProgram(int programId, ScheduleRecordingType recordingType)
    {
      TvHelper.TvAvailable();

      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IProgram program = GetProgram(programId);

      ISchedule schedule;
      if (scheduleControl == null || !scheduleControl.CreateSchedule(program, recordingType, out schedule))
        throw new HttpException(HttpStatusCode.BadRequest, "Failed to create Schedule");

      ProgramInfoController.ClearCache();

      return new List<ISchedule> { schedule };
    }

    /// <summary>
    /// GET /api/v1/Tv/Schedule/ScheduleByTime
    /// </summary>
    /// <param name="channelId">Id of the channel on which should be recorded</param>
    /// <param name="from">Start time of the recording</param>
    /// <param name="to">End time of the recording</param>
    /// <returns>Retunrs a list with the successful Schedule</returns>
    [HttpPut("ScheduleByTime/{channelId}/{from}/{to}")]
    public IList<ISchedule> CreateScheduleByTime(int channelId, DateTime from, DateTime to)
    {
      TvHelper.TvAvailable();

      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IChannel channel = GetChannel(channelId);

      ISchedule schedule;
      if (scheduleControl == null || !scheduleControl.CreateScheduleByTime(channel, from, to, out schedule))
        throw new HttpException(HttpStatusCode.BadRequest, "Failed to create Schedule");

      ProgramInfoController.ClearCache();

      return new List<ISchedule> { schedule };
    }

    /// <summary>
    /// GET /api/v1/Tv/Schedule/ScheduleByProgram
    /// </summary>
    /// <param name="programId">Id of the program which schedule should be deleted</param>
    /// <param name="recordingType">Type of the recording: once, weekly,...</param>
    /// <returns>Retunrs true on success, otherwise false</returns>
    [HttpDelete("ScheduleByProgram/{programId}/{recordingType}")]
    public bool RemoveScheduleByProgram(int programId, ScheduleRecordingType recordingType)
    {
      TvHelper.TvAvailable();

      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IProgram program = GetProgram(programId);

      bool result = false;
      if (scheduleControl != null)
        result = scheduleControl.RemoveScheduleForProgram(program, recordingType);

      ProgramInfoController.ClearCache();

      return result;
    }

    /// <summary>
    /// GET /api/v1/Tv/Schedule/Schedule
    /// </summary>
    /// <param name="scheduleIds">Ids of the schedules which should be delted</param>
    /// <returns>Retunrs true on success, otherwise false</returns>
    [HttpDelete("Schedule/{scheduleId}")]
    [HttpDelete("Schedules")]
    public bool RemoveSchedule(int[] scheduleIds)
    {
      TvHelper.TvAvailable();

      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;
      bool result = true;
      foreach (var scheduleId in scheduleIds)
      {
        ISchedule schedule = GetSchedule(scheduleId);

        if (scheduleControl != null)
          if (!scheduleControl.RemoveSchedule(schedule))
          {
            result = false;
          }
      }
      
      ProgramInfoController.ClearCache();

      return result;
    }

    /// <summary>
    /// GET /api/v1/Tv/Schedule/RecordingStatus
    /// </summary>
    /// <param name="programId">Id of the program which should be checked</param>
    /// <returns>Retunrs the <see cref="RecordingStatus"/></returns>
    [HttpGet("RecordingStatus/{programId}")]
    public RecordingStatus RecordingStatus(int programId)
    {
      TvHelper.TvAvailable();

      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IProgram program = GetProgram(programId);

      RecordingStatus recordingStatus;
      if (scheduleControl == null || !scheduleControl.GetRecordingStatus(program, out recordingStatus))
        throw new HttpException(HttpStatusCode.BadRequest, "Failed to retrieve recording status");


      return recordingStatus;
    }

    /// <summary>
    /// GET /api/v1/Tv/Schedule/RecordingFileOrStream
    /// </summary>
    /// <param name="programId">Id of the program which should be checked</param>
    /// <returns>Retunrs the Filepath or the stream path on the Server</returns>
   /* [HttpGet("RecordingFileOrStream/{programId}")]
    public string RecordingFileOrStream(int programId)
    {
      TvHelper.TvAvailable();

      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IProgram program = GetProgram(programId);

      string fileOrStream;
      if (scheduleControl == null || !scheduleControl.GetRecordingFileOrStream(program, out fileOrStream))
        throw new HttpException(HttpStatusCode.BadRequest, "Failed to retrieve Path");


      return fileOrStream;
    }*/

    /// <summary>
    /// GET /api/v1/Tv/Schedule/ProgramsBySchedule
    /// </summary>
    /// <param name="scheduleId">Id of the schedule</param>
    /// <returns>Tries to get a list of programs for the given <paramref name="scheduleId"/></returns>
    [HttpGet("ProgramsBySchedule/{scheduleId}")]
    public IDictionary<int, List<IProgram>> ProgramsBySchedule(int scheduleId)
    {
      TvHelper.TvAvailable();

      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      ISchedule schedule = GetSchedule(scheduleId);

      IList<IProgram> programList;
      if (scheduleControl == null || !scheduleControl.GetProgramsForSchedule(schedule, out programList))
        throw new HttpException(HttpStatusCode.BadRequest, "Failed to retrieve programs for schedule");

      IDictionary<int, List<IProgram>> output = ProgramListToDictionary(programList);

      return output;
    }

    /// <summary>
    /// GET /api/v1/Tv/Schedule/Schedules
    /// </summary>
    /// <returns>Returns a Dictionary with the channel Id as Key and a list of schedules as value</returns>
    [HttpGet("Schedules")]
    public IDictionary<int, List<ISchedule>> Schedules()
    {
      TvHelper.TvAvailable();

      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IList<ISchedule> scheduleList;
      if (scheduleControl == null || !scheduleControl.GetSchedules(out scheduleList))
        throw new HttpException(HttpStatusCode.BadRequest, "Failed to retrieve schedules");

      IDictionary<int, List<ISchedule>> output = ScheduleListToDictionary(scheduleList);

      return output;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Tries to get IProgram object for a given program Id
    /// </summary>
    /// <param name="programId"></param>
    /// <returns>IProgram object</returns>
    private IProgram GetProgram(int programId)
    {
      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;
      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IProgram program;
      if (programInfo == null || !programInfo.GetProgram(programId, out program))
        throw new HttpException(HttpStatusCode.NotFound, "No Program found");

      return program;
    }

    /// <summary>
    /// Tries to get IChannel object for a given channel Id
    /// </summary>
    /// <param name="channelId"></param>
    /// <returns>IChannel object</returns>
    private IChannel GetChannel(int channelId)
    {
      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

      IChannel channel;
      if (channelAndGroupInfo == null || !channelAndGroupInfo.GetChannel(channelId, out channel))
        throw new HttpException(HttpStatusCode.NotFound, "No Channel found");

      return channel;
    }

    /// <summary>
    /// Tries to get ISchedule object for a given schedule Id
    /// </summary>
    /// <param name="scheduleId"></param>
    /// <returns>IProgram object</returns>
    private ISchedule GetSchedule(int scheduleId)
    {
      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;
      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IList<ISchedule> scheduleList;
      if (scheduleControl == null || !scheduleControl.GetSchedules(out scheduleList))
        throw new HttpException(HttpStatusCode.NotFound, "No Schedules found");

      ISchedule schedule = scheduleList.FirstOrDefault(i => i.ScheduleId == scheduleId);

      if (schedule == null)
        throw new HttpException(HttpStatusCode.NotFound, $"No Schedule found with id: {scheduleId}");

      return schedule;
    }

    /// <summary>
    /// Converts the Program List to a Dictionary with the channel Id as Key and the programs for that Channel as Value
    /// </summary>
    /// <param name="programList"></param>
    /// <returns>Dictionary with Channel Id as Key and Programs as Value</returns>
    private IDictionary<int, List<IProgram>> ProgramListToDictionary(IList<IProgram> programList)
    {
      IDictionary<int, List<IProgram>> output = new Dictionary<int, List<IProgram>>();
      foreach (var program in programList)
      {
        if (output.ContainsKey(program.ChannelId))
          output[program.ChannelId].Add(program);
        else
          output.Add(program.ChannelId, new List<IProgram>
          {
            program
          });
      }

      return output;
    }

    /// <summary>
    /// Converts the Program List to a Dictionary with the channel Id as Key and the schedules for that Channel as Value
    /// </summary>
    /// <param name="scheduleList"></param>
    /// <returns>Dictionary with Channel Id as Key and Schedules as Value</returns>
    private IDictionary<int, List<ISchedule>> ScheduleListToDictionary(IList<ISchedule> scheduleList)
    {
      IDictionary<int, List<ISchedule>> output = new Dictionary<int, List<ISchedule>>();
      foreach (var schedule in scheduleList)
      {
        if (output.ContainsKey(schedule.ChannelId))
          output[schedule.ChannelId].Add(schedule);
        else
          output.Add(schedule.ChannelId, new List<ISchedule>
          {
            schedule
          });
      }

      return output;
    }

    #endregion
  }
}