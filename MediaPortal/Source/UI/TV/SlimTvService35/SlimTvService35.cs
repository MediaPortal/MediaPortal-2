#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Mediaportal.TV.Server.TVControl;
using Mediaportal.TV.Server.TVControl.Events;
using Mediaportal.TV.Server.TVControl.Interfaces.Events;
using Mediaportal.TV.Server.TVControl.Interfaces.Services;
using Mediaportal.TV.Server.TVControl.ServiceAgents;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVDatabase.Entities.Factories;
using Mediaportal.TV.Server.TVDatabase.TVBusinessLayer;
using Mediaportal.TV.Server.TVDatabase.TVBusinessLayer.Entities;
using Mediaportal.TV.Server.TVLibrary;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Integration;
using Mediaportal.TV.Server.TVService.Interfaces.Enums;
using CamType = Mediaportal.TV.Server.TVLibrary.Interfaces.CamType;
using Card = Mediaportal.TV.Server.TVDatabase.Entities.Card;
using SlimTvCard = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Card;
using Channel = Mediaportal.TV.Server.TVDatabase.Entities.Channel;
using Program = Mediaportal.TV.Server.TVDatabase.Entities.Program;
using Schedule = Mediaportal.TV.Server.TVDatabase.Entities.Schedule;
using SlimTvVirtualCard = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.VirtualCard;
using SlimTvIVirtualCard = MediaPortal.Plugins.SlimTv.Interfaces.Items.IVirtualCard;
using SlimTvUser = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.User;
using User = Mediaportal.TV.Server.TVControl.User;
using IUser = Mediaportal.TV.Server.TVService.Interfaces.Services.IUser;
using VirtualCard = Mediaportal.TV.Server.TVControl.VirtualCard;
using IVirtualCard = Mediaportal.TV.Server.TVService.Interfaces.IVirtualCard;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Common.Async;
using System.Threading.Tasks;
using MediaPortal.Common.Reflection;
using NCode.ReparsePoints;

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
      IntegrationProviderHelper.Register(@"Plugins\" + _serviceName);
      // This access is intended to force an initialization of PathManager service!
      var pm = GlobalServiceProvider.Instance.Get<IIntegrationProvider>().PathManager;
    }

    protected override void PrepareConnection(ITransaction transaction)
    {
    }

    protected override void PrepareFilterRegistrations()
    {
      // TVE3.5 doesn't require filter registrations
    }

    protected override void InitTvCore()
    {
      // Load assemblies via file name without version check
      AssemblyResolver.RedirectAllAssemblies();

      // Unfortunately native images are expected at the level of the executing assembly. The LoadLibraryEx is not used, so we can't override the load path of SQLite.
      // So instead of making a copy of files, we create a Junction to point into SlimTV plugin subfolder.
      var executingFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
      var runtimeFolder = Path.Combine(executingFolder, "runtimes");
      if (!Directory.Exists(runtimeFolder))
      {
        var assemblyFolder = Path.GetDirectoryName(GetType().Assembly.Location);
        var targetFolder = Path.Combine(assemblyFolder, "runtimes");
        ReparsePointFactory.Provider.CreateLink(runtimeFolder, targetFolder, LinkType.Junction);
      }

      var dbPath = ServiceRegistration.Get<MediaPortal.Common.PathManager.IPathManager>().GetPath("<DATABASE>\\" + TVDB_NAME + ".s3db");
      ConnectionStringSettings connectionString = new ConnectionStringSettings("TvEngineDb", "Data Source=" + dbPath, "SQLite");
      _tvServiceThread = new TvServiceThread(Environment.GetCommandLineArgs()[0], connectionString);
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

    #region Server state

    protected void UpdateServerState()
    {
      IInternalControllerService controller = GlobalServiceProvider.Instance.Get<IInternalControllerService>();
      IRecordingService recordings = GlobalServiceProvider.Get<IRecordingService>();
      IList<ISchedule> currentlyRecordingSchedules = recordings.ListAllActiveRecordingsByMediaType(MediaTypeEnum.TV)
        .Union(recordings.ListAllActiveRecordingsByMediaType(MediaTypeEnum.Radio))
        .Where(r => r.Schedule != null)
        .Select(r => r.Schedule.ToSchedule()).ToList();

      TvServerState state = new TvServerState
      {
        IsRecording = controller.IsAnyCardRecording(),
        CurrentlyRecordingSchedules = currentlyRecordingSchedules
      };

      ServiceRegistration.Get<IServerStateService>().UpdateState(TvServerState.STATE_ID, state);
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
          UpdateServerState();
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

    public override Task<bool> StopTimeshiftAsync(string userName, int slotIndex)
    {
      IUser user;
      IInternalControllerService control = GlobalServiceProvider.Instance.Get<IInternalControllerService>();
      var name = GetUserName(userName, slotIndex);
      var result = control.StopTimeShifting(name, out user);
      return Task.FromResult(result);
    }

    public override async Task<MediaItem> CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      // Channel is usually only passed as placeholder with ID only, so query the details here
      IChannelService channelService = GlobalServiceProvider.Instance.Get<IChannelService>();
      Channel fullChannel = channelService.GetChannel(channel.ChannelId);
      bool isTv = fullChannel.MediaType == 0;
      return await CreateMediaItem(slotIndex, streamUrl, channel, isTv, fullChannel.ToChannel());
    }

    public override Task<AsyncResult<IProgram[]>> GetNowNextProgramAsync(IChannel channel)
    {
      IProgram programNow = null;
      IProgram programNext = null;
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      var programs = programService.GetNowAndNextProgramsForChannel(channel.ChannelId).Select(p => GetProgram(p)).Distinct(ProgramComparer.Instance).ToList();
      var count = programs.Count;
      if (count >= 1)
        programNow = programs[0];
      if (count >= 2)
        programNext = programs[1];
      var success = programNow != null || programNext != null;
      return Task.FromResult(new AsyncResult<IProgram[]>(success, new[] { programNow, programNext }));
    }

    public override Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(IChannel channel, DateTime from, DateTime to)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      var programs = programService.GetProgramsByChannelAndStartEndTimes(channel.ChannelId, from, to)
        .Select(tvProgram => GetProgram(tvProgram, true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      var success = programs.Count > 0;
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    public override Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(string title, DateTime from, DateTime to)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      // TVE3.5 does a "equal" comparision by default, while TVE3 did use "starts with". To emulate old behavior, we add a trailing %
      var programs = programService.GetProgramsByTitleAndStartEndTimes(title + "%", from, to)
        .Select(tvProgram => GetProgram(tvProgram, true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      var success = programs.Count > 0;
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    public override Task<AsyncResult<IList<IProgram>>> GetProgramsGroupAsync(IChannelGroup channelGroup, DateTime from, DateTime to)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      IChannelGroupService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelGroupService>();

      var channels = channelGroupService.GetChannelGroup(channelGroup.ChannelGroupId).GroupMaps.Select(groupMap => groupMap.Channel);
      IDictionary<int, IList<Program>> programEntities = programService.GetProgramsForAllChannels(from, to, channels);

      var programs = programEntities.Values.SelectMany(x => x).Select(p => GetProgram(p)).Distinct(ProgramComparer.Instance).ToList();
      var success = programs.Count > 0;
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    public override Task<AsyncResult<IList<IProgram>>> GetProgramsForScheduleAsync(ISchedule schedule)
    {
      var programs = new List<IProgram>();
      Schedule scheduleEntity = ScheduleManagement.GetSchedule(schedule.ScheduleId);
      if (scheduleEntity == null)
        return Task.FromResult(new AsyncResult<IList<IProgram>>(false, null));
      IList<Program> programEntities = ProgramManagement.GetProgramsForSchedule(scheduleEntity);
      programs = programEntities.Select(p => GetProgram(p)).Distinct(ProgramComparer.Instance).ToList();
      var success = programs.Count > 0;
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    public override Task<AsyncResult<IChannel>> GetChannelAsync(IProgram program)
    {
      IChannelService channelService = GlobalServiceProvider.Instance.Get<IChannelService>();
      var channel = channelService.GetChannel(program.ChannelId).ToChannel();
      return Task.FromResult(new AsyncResult<IChannel>(true, channel));
    }

    public override bool GetProgram(int programId, out IProgram program)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      program = GetProgram(programService.GetProgram(programId));
      return program != null;
    }

    public override Task<AsyncResult<IList<IChannelGroup>>> GetChannelGroupsAsync()
    {
      IChannelGroupService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelGroupService>();
      var groups = channelGroupService.ListAllChannelGroups()
        .OrderBy(tvGroup => tvGroup.MediaType)
        .ThenBy(tvGroup => tvGroup.SortOrder)
        .Select(tvGroup => tvGroup.ToChannelGroup())
        .ToList();
      return Task.FromResult(new AsyncResult<IList<IChannelGroup>>(true, groups));
    }

    public override Task<AsyncResult<IChannel>> GetChannelAsync(int channelId)
    {
      IChannelService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelService>();
      var channel = channelGroupService.GetChannel(channelId).ToChannel();
      return Task.FromResult(new AsyncResult<IChannel>(channel != null, channel));
    }

    public override Task<AsyncResult<IList<IChannel>>> GetChannelsAsync(IChannelGroup group)
    {
      IChannelGroupService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelGroupService>();
      var channels = channelGroupService.GetChannelGroup(group.ChannelGroupId).GroupMaps
        .Where(groupMap => groupMap.Channel.VisibleInGuide)
        .OrderBy(groupMap => groupMap.SortOrder)
        .Select(groupMap => groupMap.Channel.ToChannel())
        .ToList();
      return Task.FromResult(new AsyncResult<IList<IChannel>>(true, channels));
    }

    public override Task<AsyncResult<IList<ISchedule>>> GetSchedulesAsync()
    {
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      var schedules = scheduleService.ListAllSchedules().Select(s => s.ToSchedule()).ToList();
      return Task.FromResult(new AsyncResult<IList<ISchedule>>(true, schedules));
    }

    public override Task<AsyncResult<ISchedule>> IsCurrentlyRecordingAsync(string fileName)
    {
      // TODO:
      return Task.FromResult(new AsyncResult<ISchedule>(false, null));
    }

    public override Task<AsyncResult<ISchedule>> CreateScheduleAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      Schedule tvschedule = ScheduleFactory.CreateSchedule(program.ChannelId, program.Title, program.StartTime, program.EndTime);
      tvschedule.PreRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("preRecordInterval", 5);
      tvschedule.PostRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("postRecordInterval", 5);
      tvschedule.ScheduleType = (int)recordingType;
      scheduleService.SaveSchedule(tvschedule);
      var schedule = tvschedule.ToSchedule();
      var success = schedule != null;
      return Task.FromResult(new AsyncResult<ISchedule>(success, schedule));

    }

    public override Task<AsyncResult<ISchedule>> CreateScheduleByTimeAsync(IChannel channel, DateTime from, DateTime to, ScheduleRecordingType recordingType)
    {
      return CreateScheduleByTimeAsync(channel, "Manual", from, to, recordingType);
    }

    public override Task<AsyncResult<ISchedule>> CreateScheduleByTimeAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
      Schedule tvSchedule = ScheduleFactory.CreateSchedule(channel.ChannelId, title, from, to);
      tvSchedule.PreRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("preRecordInterval", 5);
      tvSchedule.PostRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("postRecordInterval", 5);
      tvSchedule.ScheduleType = (int)recordingType;
      scheduleService.SaveSchedule(tvSchedule);
      var schedule = tvSchedule.ToSchedule();
      return Task.FromResult(new AsyncResult<ISchedule>(true, schedule));
    }

    public override Task<AsyncResult<ISchedule>> CreateScheduleDetailedAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType, int preRecordInterval, int postRecordInterval, string directory, int priority)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
      Schedule tvSchedule = ScheduleFactory.CreateSchedule(channel.ChannelId, title, from, to);
      tvSchedule.PreRecordInterval = preRecordInterval >= 0 ? preRecordInterval : ServiceAgents.Instance.SettingServiceAgent.GetValue("preRecordInterval", 5);
      tvSchedule.PostRecordInterval = postRecordInterval >= 0 ? postRecordInterval : ServiceAgents.Instance.SettingServiceAgent.GetValue("postRecordInterval", 5);
      if (!String.IsNullOrEmpty(directory))
      {
        tvSchedule.Directory = directory;
      }
      if (priority >= 0)
      {
        tvSchedule.Priority = priority;
      }
      tvSchedule.PreRecordInterval = preRecordInterval;
      tvSchedule.PostRecordInterval = postRecordInterval;
      tvSchedule.ScheduleType = (int)recordingType;
      tvSchedule.Directory = directory;
      tvSchedule.Priority = priority;
      scheduleService.SaveSchedule(tvSchedule);
      ISchedule schedule = tvSchedule.ToSchedule();
      return Task.FromResult(new AsyncResult<ISchedule>(true, schedule));
    }

    public override Task<bool> EditScheduleAsync(ISchedule schedule, IChannel channel = null, string title = null, DateTime? from = null, DateTime? to = null, ScheduleRecordingType? recordingType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("Editing schedule {0} on channel {1} for {2}, {3} till {4}, type {5}", schedule.ScheduleId, channel.ChannelId, title, from, to, recordingType);
        IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
        Schedule tvSchedule = scheduleService.GetSchedule(schedule.ScheduleId);

        tvSchedule.ChannelId = channel.ChannelId;
        if (title != null)
        {
          tvSchedule.ProgramName = title;
        }
        if (from != null)
        {
          tvSchedule.StartTime = from.Value;
        }
        if (to != null)
        {
          tvSchedule.EndTime = to.Value;
        }

        if (recordingType != null)
        {
          ScheduleRecordingType scheduleRecType = recordingType.Value;
          tvSchedule.ScheduleType = (int)scheduleRecType;
        }

        if (preRecordInterval != null)
        {
          tvSchedule.PreRecordInterval = preRecordInterval.Value;
        }
        if (postRecordInterval != null)
        {
          tvSchedule.PostRecordInterval = postRecordInterval.Value;
        }

        if (directory != null)
        {
          tvSchedule.Directory = directory;
        }
        if (priority != null)
        {
          tvSchedule.Priority = priority.Value;
        }

        scheduleService.SaveSchedule(tvSchedule);

        return Task.FromResult(true);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn(String.Format("Failed to edit schedule {0}", schedule.ScheduleId), ex);
        return Task.FromResult(false);
      }
    }

    public override Task<bool> RemoveScheduleForProgramAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      var canceledProgram = programService.GetProgram(program.ProgramId);
      if (canceledProgram == null)
        return Task.FromResult(false);

      foreach (Schedule schedule in scheduleService.ListAllSchedules().Where(schedule => new ScheduleBLL(schedule).IsRecordingProgram(canceledProgram, true)))
      {
        switch (schedule.ScheduleType)
        {
          case (int)ScheduleRecordingType.Once:
            StopRecording(schedule);
            scheduleService.DeleteSchedule(schedule.ScheduleId);
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
      return Task.FromResult(true);
    }

    public override Task<bool> RemoveScheduleAsync(ISchedule schedule)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      if (scheduleService == null)
        return Task.FromResult(false);

      scheduleService.DeleteSchedule(schedule.ScheduleId);
      return Task.FromResult(true);
    }

    public override Task<bool> UnCancelScheduleAsync(IProgram program)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      var tvProgram = programService.GetProgram(program.ProgramId);
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("Uncancelling schedule for programId {0}", tvProgram.ProgramId);
        foreach (Schedule schedule in scheduleService.ListAllSchedules().Where(schedule => schedule.StartTime == program.StartTime && schedule.ChannelId == tvProgram.ChannelId))
        {
          scheduleService.UnCancelSerie(schedule, program.StartTime, tvProgram.ChannelId);
        }

        return Task.FromResult(true);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn(String.Format("Failed to uncancel schedule for programId {0}", program.ProgramId), ex);
        return Task.FromResult(false);
      }
    }

    private static void CancelSingleSchedule(Schedule schedule, Program canceledProgram)
    {
      ICanceledScheduleService canceledScheduleService = GlobalServiceProvider.Instance.Get<ICanceledScheduleService>();

      CanceledSchedule canceledSchedule = CanceledScheduleFactory.CreateCanceledSchedule(schedule.ScheduleId, canceledProgram.ChannelId, canceledProgram.StartTime);
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
      Schedule spawn = ServiceAgents.Instance.ScheduleServiceAgent.RetrieveSpawnedSchedule(parentSchedule.ScheduleId, parentSchedule.StartTime);
      if (spawn != null)
        schedule = spawn;
    }

    private static bool StopRecording(Schedule schedule)
    {
      bool stoppedRec = false;
      bool isRec = ServiceAgents.Instance.ScheduleServiceAgent.IsScheduleRecording(schedule.ScheduleId);
      if (isRec)
      {
        ServiceAgents.Instance.ControllerServiceAgent.StopRecordingSchedule(schedule.ScheduleId);
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
            wasDeleted |= DeleteSchedule(currentSchedule.ScheduleId);
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error deleting schedule with ID '{0}'", ex,
            currentSchedule != null ? currentSchedule.ScheduleId.ToString() : "<null>");
        }
      }
      return wasDeleted;
    }

    private bool DeleteSchedule(int idSchedule)
    {
      Schedule schedule = ServiceAgents.Instance.ScheduleServiceAgent.GetSchedule(idSchedule);
      if (schedule == null)
        return false;

      ServiceAgents.Instance.ScheduleServiceAgent.DeleteSchedule(schedule.ScheduleId);
      return true;
    }

    public override Task<AsyncResult<RecordingStatus>> GetRecordingStatusAsync(IProgram program)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      IProgramRecordingStatus recProgram = (IProgramRecordingStatus)GetProgram(programService.GetProgram(program.ProgramId), true);
      return Task.FromResult(new AsyncResult<RecordingStatus>(true, recProgram.RecordingStatus));
    }

    public override Task<AsyncResult<string>> GetRecordingFileOrStreamAsync(IProgram program)
    {
      Recording recording;
      if (!GetRecording(program, out recording))
        return Task.FromResult(new AsyncResult<string>(false, null));

      // FileName represents a local filesystem path on the server. It cannot be used directly in multiseat (RTSP required).
      return Task.FromResult(new AsyncResult<string>(true, recording.FileName));
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

    public override Task<AsyncResult<List<ICard>>> GetCardsAsync()
    {
      IInternalControllerService control = GlobalServiceProvider.Instance.Get<IInternalControllerService>();
      List<ICard> cards = control.CardCollection.Select(card => new SlimTvCard()
      {
        Name = card.Value.CardName,
        CardId = card.Value.Card.TunerId,
        EpgIsGrabbing = card.Value.Epg.IsGrabbing,
        HasCam = card.Value.DataBaseCard.UseConditionalAccess,
        CamType = card.Value.Card.CamType == CamType.Default ? SlimTvCamType.Default : SlimTvCamType.Astoncrypt2,
        DecryptLimit = card.Value.DataBaseCard.DecryptLimit,
        Enabled = card.Value.DataBaseCard.Enabled,
        RecordingFolder = card.Value.DataBaseCard.RecordingFolder,
        TimeshiftFolder = card.Value.DataBaseCard.TimeshiftingFolder,
        DevicePath = card.Value.DataBaseCard.DevicePath,
        PreloadCard = card.Value.DataBaseCard.PreloadCard,
        Priority = card.Value.DataBaseCard.Priority
      }).Cast<ICard>().ToList();

      return Task.FromResult(new AsyncResult<List<ICard>>(cards.Count > 0, cards));
    }

    public override Task<AsyncResult<List<SlimTvIVirtualCard>>> GetActiveVirtualCardsAsync()
    {
      List<SlimTvIVirtualCard> cards = new List<SlimTvIVirtualCard>();
      foreach (var card in ServiceAgents.Instance.CardServiceAgent.ListAllCards())
      {
        IDictionary<string, IUser> usersForCard = ServiceAgents.Instance.ControllerServiceAgent.GetUsersForCard(card.CardId);

        foreach (IUser user1 in usersForCard.Values)
        {
          foreach (var subchannel in user1.SubChannels.Values)
          {
            var vcard = new VirtualCard(user1);
            if (vcard.IsTimeShifting || vcard.IsRecording)
            {
              cards.Add(new SlimTvVirtualCard
              {
                BitRateMode = (int)vcard.BitRateMode,
                ChannelName = vcard.ChannelName,
                Device = card.DevicePath,
                Enabled = card.Enabled,
                /*GetTimeshiftStoppedReason = (int)vcard.GetTimeshiftStoppedReason,
                GrabTeletext = vcard.GrabTeletext,
                HasTeletext = vcard.HasTeletext,*/
                Id = vcard.Id,
                ChannelId = vcard.IdChannel,
                IsGrabbingEpg = vcard.IsGrabbingEpg,
                IsRecording = vcard.IsRecording,
                IsScanning = vcard.IsScanning,
                IsScrambled = vcard.IsScrambled,
                IsTimeShifting = vcard.IsTimeShifting,
                IsTunerLocked = vcard.IsTunerLocked,
                //MaxChannel = vcard.MaxChannel,
                //MinChannel = vcard.MinChannel,
                Name = card.Name,
                QualityType = (int)vcard.QualityType,
                RecordingFileName = vcard.RecordingFileName,
                RecordingFolder = vcard.RecordingFolder,
                RecordingFormat = vcard.RecordingFormat,
                RecordingScheduleId = vcard.RecordingScheduleId,
                //RecordingStarted = vcard.RecordingStarted != DateTime.MinValue ? vcard.RecordingStarted : new DateTime(2000, 1, 1),
                RemoteServer = vcard.RemoteServer,
                RTSPUrl = vcard.RTSPUrl,
                SignalLevel = vcard.SignalLevel,
                SignalQuality = vcard.SignalQuality,
                TimeShiftFileName = vcard.TimeShiftFileName,
                TimeShiftFolder = vcard.TimeshiftFolder,
                //TimeShiftStarted = vcard.TimeShiftStarted != DateTime.MinValue ? vcard.TimeShiftStarted : new DateTime(2000, 1, 1),
                Type = (SlimTvCardType)Enum.Parse(typeof(SlimTvCardType), vcard.Type.ToString()),
                User = vcard.User != null ? new SlimTvUser
                {
                  Priority = vcard.User.Priority,
                  ChannelStates = vcard.User.ChannelStates.ToDictionary(item => item.Key, item => (SlimTvChannelState)Enum.Parse(typeof(SlimTvChannelState), item.ToString())),
                  CardId = vcard.User.CardId,
                  Name = vcard.User.Name,
                  FailedCardId = vcard.User.FailedCardId,
                  HeartBeat = DateTime.Now, // TVE 3.5 doesn't have a heart beat
                  History = vcard.User.History,
                  IdChannel = subchannel.ChannelId,
                  //IsAdmin = vcard.User.IsAdmin,
                  SubChannel = subchannel.Id,
                  TvStoppedReason = (SlimTvStoppedReason)Enum.Parse(typeof(SlimTvStoppedReason), vcard.User.TvStoppedReason.ToString()),
                } : null
              });
            }
          }
        }
      }

      return Task.FromResult(new AsyncResult<List<SlimTvIVirtualCard>>(cards.Count > 0, cards));
    }

    #endregion
  }
}
