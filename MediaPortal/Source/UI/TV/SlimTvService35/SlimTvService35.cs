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
using System.Data.Common;
using System.Linq;
using MediaPortal.Backend.Database;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using Mediaportal.TV.Server.TVDatabase.Entities.Enums;
using Mediaportal.TV.Server.TVLibrary.IntegrationProvider.Interfaces;
using IChannel = MediaPortal.Plugins.SlimTv.Interfaces.Items.IChannel;
using ILogger = MediaPortal.Common.Logging.ILogger;
using ScheduleRecordingType = MediaPortal.Plugins.SlimTv.Interfaces.ScheduleRecordingType;
using MediaPortal.Common.Utils;
using MediaPortal.Plugins.SlimTv.Service.Helpers;
using Mediaportal.TV.Server.TVControl;
using Mediaportal.TV.Server.TVControl.Events;
using Mediaportal.TV.Server.TVControl.Interfaces.Events;
using Mediaportal.TV.Server.TVControl.Interfaces.Services;
using Mediaportal.TV.Server.TVControl.ServiceAgents;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVDatabase.Entities.Factories;
using Mediaportal.TV.Server.TVDatabase.EntityModel.ObjContext;
using Mediaportal.TV.Server.TVDatabase.TVBusinessLayer;
using Mediaportal.TV.Server.TVDatabase.TVBusinessLayer.Entities;
using Mediaportal.TV.Server.TVLibrary;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Integration;
using Mediaportal.TV.Server.TVService.Interfaces;
using Mediaportal.TV.Server.TVService.Interfaces.Enums;
using Mediaportal.TV.Server.TVService.Interfaces.Services;
using Channel = Mediaportal.TV.Server.TVDatabase.Entities.Channel;
using Program = Mediaportal.TV.Server.TVDatabase.Entities.Program;
using Schedule = Mediaportal.TV.Server.TVDatabase.Entities.Schedule;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public class SlimTvService : AbstractSlimTvService
  {
    private TvServiceThread _tvServiceThread;
    protected readonly Dictionary<string, IUser> _tvUsers = new Dictionary<string, IUser>();

    public SlimTvService()
    {
      _serviceName = "SlimTv.Service35";
    }

    #region Database and program data initialization

    protected override void PrepareIntegrationProvider()
    {
      IntegrationProviderHelper.Register(@"Plugins\" + _serviceName, "Plugins\\" + _serviceName + "\\castle.config");
      // This access is intended to force an initialization of PathManager service!
      var pm = GlobalServiceProvider.Instance.Get<IIntegrationProvider>().PathManager;
    }

    protected override void PrepareConnection(ITransaction transaction)
    {
      if (transaction.Connection.GetCloneFactory(TVDB_NAME, out _dbProviderFactory, out _cloneConnection))
      {
        EntityFrameworkHelper.AssureKnownFactory(_dbProviderFactory);
        // Register our factory to create new cloned connections
        ObjectContextManager.SetDbConnectionCreator(ClonedConnectionFactory);
      }
    }

    protected override void PrepareFilterRegistrations()
    {
      // TVE3.5 doesn't require filter registrations
    }

    protected override void InitTvCore()
    {
      _tvServiceThread = new TvServiceThread(Environment.GetCommandLineArgs()[0]);
      _tvServiceThread.Start();
      if (!_tvServiceThread.InitializedEvent.WaitOne(MAX_WAIT_MS))
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to start TV service thread within {0} seconds.", MAX_WAIT_MS / 1000);
      }

      // Handle events from TvEngine
      if (!RegisterEvents())
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to register events. This happens only if startup failed. Stopping plugin now.");
        DeInit();
      }
    }

    /// <summary>
    /// Creates a new <see cref="DbConnection"/> on each request. This is used by the Tve35 EF model handling.
    /// </summary>
    /// <returns>Connection, still closed</returns>
    private DbConnection ClonedConnectionFactory()
    {
      DbConnection connection = _dbProviderFactory.CreateConnection();
      if (connection == null)
        return null;
      connection.ConnectionString = _cloneConnection;
      return connection;
    }


    public override bool DeInit()
    {
      if (_tvServiceThread != null)
      {
        _tvServiceThread.Stop(MAX_WAIT_MS);
        _tvServiceThread = null;
      }
      return true;
    }

    #endregion

    #region Recordings / MediaLibrary synchronization

    protected override bool RegisterEvents()
    {
      ITvServerEvent tvServerEvent = GlobalServiceProvider.Instance.Get<ITvServerEvent>();
      if (tvServerEvent == null)
        return false;
      tvServerEvent.OnTvServerEvent += OnTvServerEvent;
      return true;
    }

    protected override void OnTvServerEvent(object sender, EventArgs eventArgs)
    {
      try
      {
        TvServerEventArgs tvEvent = (TvServerEventArgs)eventArgs;

        if (tvEvent.EventType == TvServerEventType.RecordingStarted || tvEvent.EventType == TvServerEventType.RecordingEnded)
        {
          var recording = ServiceAgents.Instance.RecordingServiceAgent.GetRecording(tvEvent.Recording);
          if (recording != null)
          {
            ServiceRegistration.Get<ILogger>().Info("SlimTvService: {0}: {1}", tvEvent.EventType, recording.FileName);
            ImportRecording(recording.FileName);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("SlimTvService: Exception while handling TvServerEvent", ex);
      }
    }

    protected override bool GetRecordingConfiguration(out List<string> recordingFolders, out string singlePattern, out string seriesPattern)
    {
      IList<Card> allCards = ServiceAgents.Instance.CardServiceAgent.ListAllCards(CardIncludeRelationEnum.None);
      // Get all different recording folders
      recordingFolders = allCards.Select(c => c.RecordingFolder).Where(f => !string.IsNullOrEmpty(f)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

      singlePattern = ServiceAgents.Instance.SettingServiceAgent.GetValue("moviesformat", string.Empty);
      seriesPattern = ServiceAgents.Instance.SettingServiceAgent.GetValue("seriesformat", string.Empty);
      return recordingFolders.Count > 0;
    }

    #endregion

    #region ITvProvider implementation

    public override bool StopTimeshift(string userName, int slotIndex)
    {
      IUser user;
      IInternalControllerService control = GlobalServiceProvider.Instance.Get<IInternalControllerService>();
      return control.StopTimeShifting(GetUserName(userName, slotIndex), out user);
    }

    public override MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      // Channel is usually only passed as placeholder with ID only, so query the details here
      IChannelService channelService = GlobalServiceProvider.Instance.Get<IChannelService>();
      Channel fullChannel = channelService.GetChannel(channel.ChannelId);
      bool isTv = fullChannel.MediaType == 0;
      return CreateMediaItem(slotIndex, streamUrl, channel, isTv, fullChannel.ToChannel());
    }

    public override bool GetNowNextProgram(IChannel channel, out IProgram programNow, out IProgram programNext)
    {
      programNow = null;
      programNext = null;
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      var programs = programService.GetNowAndNextProgramsForChannel(channel.ChannelId).Select(p => p.ToProgram()).Distinct(ProgramComparer.Instance).ToList();
      var count = programs.Count;
      if (count >= 1)
        programNow = programs[0];
      if (count >= 2)
        programNext = programs[1];
      return programNow != null || programNext != null;
    }

    public override bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      programs = programService.GetProgramsByChannelAndStartEndTimes(channel.ChannelId, from, to)
        .Select(tvProgram => tvProgram.ToProgram(true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      return programs.Count > 0;
    }

    public override bool GetPrograms(string title, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      programs = programService.GetProgramsByTitleAndStartEndTimes(title, from, to)
        .Select(tvProgram => tvProgram.ToProgram(true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      return programs.Count > 0;
    }

    public override bool GetProgramsGroup(IChannelGroup channelGroup, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      IChannelGroupService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelGroupService>();

      var channels = channelGroupService.GetChannelGroup(channelGroup.ChannelGroupId).GroupMaps.Select(groupMap => groupMap.Channel);
      IDictionary<int, IList<Program>> programEntities = programService.GetProgramsForAllChannels(from, to, channels);

      programs = programEntities.Values.SelectMany(x => x).Select(p => p.ToProgram()).Distinct(ProgramComparer.Instance).ToList();
      return programs.Count > 0;
    }

    public override bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs)
    {
      programs = new List<IProgram>();
      Schedule scheduleEntity = ScheduleManagement.GetSchedule(schedule.ScheduleId);
      if (scheduleEntity == null)
        return false;
      IList<Program> programEntities = ProgramManagement.GetProgramsForSchedule(scheduleEntity);
      programs = programEntities.Select(p => p.ToProgram()).Distinct(ProgramComparer.Instance).ToList();
      return true;
    }

    public override bool GetChannel(IProgram program, out IChannel channel)
    {
      IChannelService channelService = GlobalServiceProvider.Instance.Get<IChannelService>();
      channel = channelService.GetChannel(program.ChannelId).ToChannel();
      return true;
    }

    public override bool GetProgram(int programId, out IProgram program)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      program = programService.GetProgram(programId).ToProgram();
      return program != null;
    }

    public override bool GetChannelGroups(out IList<IChannelGroup> groups)
    {
      IChannelGroupService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelGroupService>();
      groups = channelGroupService.ListAllChannelGroups()
        .OrderBy(tvGroup => tvGroup.MediaType)
        .ThenBy(tvGroup => tvGroup.SortOrder)
        .Select(tvGroup => tvGroup.ToChannelGroup())
        .ToList();
      return true;
    }

    public override bool GetChannel(int channelId, out IChannel channel)
    {
      IChannelService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelService>();
      channel = channelGroupService.GetChannel(channelId).ToChannel();
      return true;
    }

    public override bool GetChannels(IChannelGroup group, out IList<IChannel> channels)
    {
      IChannelGroupService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelGroupService>();
      channels = channelGroupService.GetChannelGroup(group.ChannelGroupId).GroupMaps
        .Where(groupMap => groupMap.Channel.VisibleInGuide)
        .OrderBy(groupMap => groupMap.SortOrder)
        .Select(groupMap => groupMap.Channel.ToChannel())
        .ToList();
      return true;
    }

    public override bool GetSchedules(out IList<ISchedule> schedules)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      schedules = scheduleService.ListAllSchedules().Select(s => s.ToSchedule()).ToList();
      return true;
    }

    public override bool CreateSchedule(IProgram program, ScheduleRecordingType recordingType, out ISchedule schedule)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      Schedule tvschedule = ScheduleFactory.CreateSchedule(program.ChannelId, program.Title, program.StartTime, program.EndTime);
      tvschedule.PreRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("preRecordInterval", 5);
      tvschedule.PostRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("postRecordInterval", 5);
      tvschedule.ScheduleType = (int)recordingType;
      scheduleService.SaveSchedule(tvschedule);
      schedule = tvschedule.ToSchedule();
      return true;
    }

    public override bool CreateScheduleByTime(IChannel channel, DateTime from, DateTime to, out ISchedule schedule)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
      Schedule tvSchedule = ScheduleFactory.CreateSchedule(channel.ChannelId, "Manual", from, to);
      tvSchedule.PreRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("preRecordInterval", 5);
      tvSchedule.PostRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("postRecordInterval", 5);
      tvSchedule.ScheduleType = (int)ScheduleRecordingType.Once;
      scheduleService.SaveSchedule(tvSchedule);
      schedule = tvSchedule.ToSchedule();
      return true;
    }

    public override bool RemoveScheduleForProgram(IProgram program, ScheduleRecordingType recordingType)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      var canceledProgram = programService.GetProgram(program.ProgramId);
      if (canceledProgram == null)
        return false;

      foreach (Schedule schedule in scheduleService.ListAllSchedules().Where(schedule => new ScheduleBLL(schedule).IsRecordingProgram(canceledProgram, true)))
      {
        switch (schedule.ScheduleType)
        {
          case (int)ScheduleRecordingType.Once:
            scheduleService.DeleteSchedule(schedule.IdSchedule);
            break;
          default:
            // If only single program should be canceled
            if (recordingType == ScheduleRecordingType.Once)
            {
              CancelSingleSchedule(schedule, canceledProgram);
            }
            // Full schedule is canceled, including all programs
            else
            {
              CancelFullSchedule(schedule);
            }
            break;
        }
      }
      return true;
    }

    public override bool RemoveSchedule(ISchedule schedule)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      if (scheduleService == null)
        return false;

      scheduleService.DeleteSchedule(schedule.ScheduleId);
      return true;
    }

    private static void CancelSingleSchedule(Schedule schedule, Program canceledProgram)
    {
      ICanceledScheduleService canceledScheduleService = GlobalServiceProvider.Instance.Get<ICanceledScheduleService>();

      CanceledSchedule canceledSchedule = CanceledScheduleFactory.CreateCanceledSchedule(schedule.IdSchedule, canceledProgram.IdChannel, canceledProgram.StartTime);
      canceledScheduleService.SaveCanceledSchedule(canceledSchedule);
      StopRecording(schedule);
    }

    private void CancelFullSchedule(Schedule schedule)
    {
      Schedule currentSchedule = schedule;
      Schedule parentSchedule = null;
      GetParentAndSpawnSchedule(ref currentSchedule, out parentSchedule);
      StopRecording(currentSchedule);
      DeleteEntireOrOnceSchedule(currentSchedule, parentSchedule);
    }

    private static void GetParentAndSpawnSchedule(ref Schedule schedule, out Schedule parentSchedule)
    {
      parentSchedule = schedule.ParentSchedule;
      if (parentSchedule != null)
        return;

      parentSchedule = schedule;
      Schedule spawn = ServiceAgents.Instance.ScheduleServiceAgent.RetrieveSpawnedSchedule(parentSchedule.IdSchedule, parentSchedule.StartTime);
      if (spawn != null)
        schedule = spawn;
    }

    private static bool StopRecording(Schedule schedule)
    {
      bool stoppedRec = false;
      bool isRec = ServiceAgents.Instance.ScheduleServiceAgent.IsScheduleRecording(schedule.IdSchedule);
      if (isRec)
      {
        ServiceAgents.Instance.ControllerServiceAgent.StopRecordingSchedule(schedule.IdSchedule);
        stoppedRec = true;
      }
      return stoppedRec;
    }

    private bool DeleteEntireOrOnceSchedule(Schedule schedule, Schedule parentSchedule)
    {
      //is the schedule recording, then stop it now.
      bool wasDeleted = false;
      foreach (var currentSchedule in new List<Schedule> { schedule, parentSchedule })
      {
        try
        {
          if (currentSchedule != null)
            wasDeleted |= DeleteSchedule(currentSchedule.IdSchedule);
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error deleting schedule with ID '{0}'", ex,
            currentSchedule != null ? currentSchedule.IdSchedule.ToString() : "<null>");
        }
      }
      return wasDeleted;
    }

    private bool DeleteSchedule(int idSchedule)
    {
      Schedule schedule = ServiceAgents.Instance.ScheduleServiceAgent.GetSchedule(idSchedule);
      if (schedule == null)
        return false;

      ServiceAgents.Instance.ScheduleServiceAgent.DeleteSchedule(schedule.IdSchedule);
      return true;
    }

    public override bool GetRecordingStatus(IProgram program, out RecordingStatus recordingStatus)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      IProgramRecordingStatus recProgram = (IProgramRecordingStatus)programService.GetProgram(program.ProgramId).ToProgram(true);
      recordingStatus = recProgram.RecordingStatus;
      return true;
    }

    public override bool GetRecordingFileOrStream(IProgram program, out string fileOrStream)
    {
      fileOrStream = null;
      Recording recording;
      if (!GetRecording(program, out recording))
        return false;

      fileOrStream = recording.FileName; // FileName represents a local filesystem path on the server. It cannot be used directly in multiseat (RTSP required).
      return true;
    }

    private static bool GetRecording(IProgram program, out Recording recording)
    {
      IRecordingService recordingService = GlobalServiceProvider.Instance.Get<IRecordingService>();
      recording = recordingService.GetActiveRecordingByTitleAndChannel(program.Title, program.ChannelId);
      return recording != null;
    }

    protected override string SwitchTVServerToChannel(string userName, int channelId)
    {
      if (String.IsNullOrEmpty(userName))
      {
        ServiceRegistration.Get<ILogger>().Error("Called SwitchTVServerToChannel with empty userName");
        throw new ArgumentNullException("userName");
      }

      IUser currentUser = UserFactory.CreateBasicUser(userName, -1);
      ServiceRegistration.Get<ILogger>().Debug("Starting timeshifiting with username {0} on channel id {1}", userName, channelId);

      IInternalControllerService control = GlobalServiceProvider.Instance.Get<IInternalControllerService>();

      IVirtualCard card;
      IUser user;
      TvResult result = control.StartTimeShifting(currentUser.Name, channelId, out card, out user);
      ServiceRegistration.Get<ILogger>().Debug("Tried to start timeshifting, result {0}", result);

      if (result != TvResult.Succeeded)
      {
        // TODO: should we retry?
        ServiceRegistration.Get<ILogger>().Error("Starting timeshifting failed with result {0}", result);
        throw new Exception("Failed to start tv stream: " + result);
      }
      return userName.StartsWith(LOCAL_USERNAME + "-") ? card.TimeShiftFileName : card.RTSPUrl;
    }

    protected IUser GetUserByUserName(string userName, bool create = false)
    {
      if (userName == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("Used user with null name");
        return null;
      }

      if (!_tvUsers.ContainsKey(userName) && !create)
        return null;

      if (!_tvUsers.ContainsKey(userName) && create)
        _tvUsers.Add(userName, new User(userName, UserType.Normal));
      return _tvUsers[userName];
    }

    #endregion
  }
}
