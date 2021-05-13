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
using Mediaportal.TV.Server.TVService.Interfaces.CardHandler;
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
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.Plugins.SlimTv.UPnP;
using Mediaportal.TV.Server.TVLibrary.Interfaces;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public class SlimTvService : 
    AbstractSlimTvService<Mediaportal.TV.Server.TVDatabase.Entities.ChannelGroup, Mediaportal.TV.Server.TVDatabase.Entities.Channel,
      Mediaportal.TV.Server.TVDatabase.Entities.Program, Mediaportal.TV.Server.TVDatabase.Entities.Schedule, ScheduleRule, Mediaportal.TV.Server.TVDatabase.Entities.Recording, 
      Mediaportal.TV.Server.TVDatabase.Entities.TuningDetail, Mediaportal.TV.Server.TVDatabase.Entities.Conflict>
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
      if (transaction.Connection.GetCloneFactory(Consts.TVDB_NAME, out _dbProviderFactory, out _cloneConnection))
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
        Logger.Error("SlimTvService: Failed to start TV service thread within {0} seconds.", MAX_WAIT_MS / 1000);
      }

      // Handle events from TvEngine
      if (!RegisterEvents())
      {
        Logger.Error("SlimTvService: Failed to register events. This happens only if startup failed. Stopping plugin now.");
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
      base.DeInit();
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
        .Select(r => (ISchedule)ConvertToSchedule(r.Schedule)).ToList();

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
            Logger.Info("SlimTvService: {0}: {1}", tvEvent.EventType, recording.FileName);
            ImportRecording(recording.FileName);
          }
        }
        if (tvEvent.EventType == TvServerEventType.ImportEpgPrograms || tvEvent.EventType == TvServerEventType.ProgramUpdated || tvEvent.EventType == TvServerEventType.EpgGrabbingStopped)
        {
          _ = ProgramsChangedAsync();
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("SlimTvService: Exception while handling TvServerEvent", ex);
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

    protected override Task<bool> StopProviderTimeshiftAsync(string userName, int slotIndex)
    {
      IUser user;
      IInternalControllerService control = GlobalServiceProvider.Instance.Get<IInternalControllerService>();
      var name = GetUserName(userName, slotIndex);
      var result = control.StopTimeShifting(name, out user);
      return Task.FromResult(result);
    }

    protected override async Task<MediaItem> CreateMediaItemAsync(int slotIndex, string streamUrl, IChannel channel)
    {
      // Channel is usually only passed as placeholder with ID only, so query the details here
      IChannelService channelService = GlobalServiceProvider.Instance.Get<IChannelService>();
      Channel fullChannel = channelService.GetChannel(channel.ChannelId);
      bool isTv = fullChannel.MediaType == 0;
      return await CreateMediaItemAsync(slotIndex, streamUrl, channel, isTv, ConvertToChannel(fullChannel));
    }

    protected override Task<AsyncResult<IProgram[]>> GetProviderNowNextProgramAsync(IChannel channel)
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

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsAsync(IChannel channel, DateTime from, DateTime to)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      var programs = programService.GetProgramsByChannelAndStartEndTimes(channel.ChannelId, from, to)
        .Select(tvProgram => GetProgram(tvProgram, true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      var success = programs.Count > 0;
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsAsync(string title, DateTime from, DateTime to)
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

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsGroupAsync(IChannelGroup channelGroup, DateTime from, DateTime to)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      IChannelGroupService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelGroupService>();

      var channels = channelGroupService.GetChannelGroup(channelGroup.ChannelGroupId).GroupMaps.Select(groupMap => groupMap.Channel);
      IDictionary<int, IList<Program>> programEntities = programService.GetProgramsForAllChannels(from, to, channels);

      var programs = programEntities.Values.SelectMany(x => x).Select(p => GetProgram(p)).Distinct(ProgramComparer.Instance).ToList();
      var success = programs.Count > 0;
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsForScheduleAsync(ISchedule schedule)
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

    protected override Task<AsyncResult<IChannel>> GetProviderChannelAsync(IProgram program)
    {
      IChannelService channelService = GlobalServiceProvider.Instance.Get<IChannelService>();
      var channel = ConvertToChannel(channelService.GetChannel(program.ChannelId));
      return Task.FromResult(new AsyncResult<IChannel>(true, channel));
    }

    protected override Task<AsyncResult<ITuningDetail>> GetProviderTuningDetailsAsync(ICard card, IChannel channel)
    {
      if (!card.Enabled)
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      IChannelService channelService = GlobalServiceProvider.Instance.Get<IChannelService>();
      var cl = channelService.GetChannel(channel.ChannelId);
      if (!(cl?.TuningDetails?.Count > 0))
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      IInternalControllerService control = GlobalServiceProvider.Instance.Get<IInternalControllerService>();
      if (!control.CardCollection.TryGetValue(card.CardId, out var cd))
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      var dbCd = CardManagement.GetCard(card.CardId);
      if (dbCd == null)
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      if (!CardManagement.CanViewTvChannel(dbCd, channel.ChannelId))
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      var tuningDetail = cl.TuningDetails.FirstOrDefault(d => (d.ChannelType == 0 && cd.Card.TunerType == CardType.Analog) ||
                                                              (d.ChannelType == 1 && cd.Card.TunerType == CardType.Atsc) ||
                                                              (d.ChannelType == 2 && cd.Card.TunerType == CardType.DvbC) ||
                                                              (d.ChannelType == 3 && cd.Card.TunerType == CardType.DvbS) ||
                                                              (d.ChannelType == 4 && cd.Card.TunerType == CardType.DvbT) ||
                                                              (d.ChannelType == 7 && cd.Card.TunerType == CardType.DvbIP));

      return Task.FromResult(new AsyncResult<ITuningDetail>(tuningDetail != null, ConvertToTuningDetail(tuningDetail)));
    }

    protected override Task<AsyncResult<IProgram>> GetProviderProgramAsync(int programId)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      var program = GetProgram(programService.GetProgram(programId));
      return Task.FromResult(new AsyncResult<IProgram>(program != null, program));
    }

    protected override Task<AsyncResult<IList<IChannelGroup>>> GetProviderChannelGroupsAsync()
    {
      IChannelGroupService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelGroupService>();
      var groups = channelGroupService.ListAllChannelGroups()
        .OrderBy(tvGroup => tvGroup.MediaType)
        .ThenBy(tvGroup => tvGroup.SortOrder)
        .Select(tvGroup => (IChannelGroup)ConvertToChannelGroup(tvGroup))
        .ToList();
      return Task.FromResult(new AsyncResult<IList<IChannelGroup>>(true, groups));
    }

    protected override Task<AsyncResult<IChannel>> GetProviderChannelAsync(int channelId)
    {
      IChannelService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelService>();
      var channel = ConvertToChannel(channelGroupService.GetChannel(channelId));
      return Task.FromResult(new AsyncResult<IChannel>(channel != null, channel));
    }

    protected override Task<AsyncResult<IList<IChannel>>> GetProviderChannelsAsync(IChannelGroup group)
    {
      IChannelGroupService channelGroupService = GlobalServiceProvider.Instance.Get<IChannelGroupService>();
      var channels = channelGroupService.GetChannelGroup(group.ChannelGroupId).GroupMaps
        .Where(groupMap => groupMap.Channel.VisibleInGuide)
        .OrderBy(groupMap => groupMap.SortOrder)
        .Select(groupMap => (IChannel)ConvertToChannel(groupMap.Channel))
        .ToList();
      return Task.FromResult(new AsyncResult<IList<IChannel>>(true, channels));
    }

    protected override Task<AsyncResult<IList<IRecording>>> GetProviderRecordingsAsync(string name)
    {
      IRecordingService recordingService = GlobalServiceProvider.Instance.Get<IRecordingService>();
      var recordings = recordingService.ListAllRecordingsByMediaType(MediaTypeEnum.TV).Select(s => (IRecording)ConvertToRecording(s)).ToList();
      recordings.Concat(recordingService.ListAllRecordingsByMediaType(MediaTypeEnum.Radio).Select(s => (IRecording)ConvertToRecording(s)).ToList());
      return Task.FromResult(new AsyncResult<IList<IRecording>>(true, recordings));
    }

    protected override Task<AsyncResult<IList<ISchedule>>> GetProviderSchedulesAsync()
    {
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      var schedules = scheduleService.ListAllSchedules().Select(s => (ISchedule)ConvertToSchedule(s)).ToList();
      return Task.FromResult(new AsyncResult<IList<ISchedule>>(true, schedules));
    }

    protected override Task<AsyncResult<IList<ISchedule>>> GetProviderCanceledSchedulesAsync()
    {
      var canceledSchedules = ScheduleManagement.ListAllSchedules(ScheduleIncludeRelationEnum.CanceledSchedules);
      return Task.FromResult(new AsyncResult<IList<ISchedule>>(canceledSchedules.Count > 0, canceledSchedules.Select(s => (ISchedule)ConvertToSchedule(s)).ToList()));

      //IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      //IList<ISchedule> canceledSchedules = new List<ISchedule>();
      //foreach (var s in scheduleService.ListAllSchedules())
      //{
      //  var duration = s.EndTime - s.StartTime;
      //  foreach (var canceledSchedule in s.CanceledSchedules)
      //  {
      //    var schedule = ScheduleFactory.Clone(s);
      //    schedule.StartTime = canceledSchedule.CancelDateTime;
      //    schedule.EndTime = schedule.StartTime;

      //    canceledSchedules.Add(schedule.ToSchedule());
      //  }
      //}
    }

    protected override Task<AsyncResult<ISchedule>> IsProviderCurrentlyRecordingAsync(string fileName)
    {
      // TODO:
      return Task.FromResult(new AsyncResult<ISchedule>(false, null));
    }

    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      Schedule tvschedule = ScheduleFactory.CreateSchedule(program.ChannelId, program.Title, program.StartTime, program.EndTime);
      tvschedule.PreRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("preRecordInterval", 5);
      tvschedule.PostRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("postRecordInterval", 5);
      tvschedule.ScheduleType = (int)recordingType;
      scheduleService.SaveSchedule(tvschedule);
      var schedule = ConvertToSchedule(tvschedule);
      var success = schedule != null;
      return Task.FromResult(new AsyncResult<ISchedule>(success, schedule));

    }

    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleByTimeAsync(IChannel channel, DateTime from, DateTime to, ScheduleRecordingType recordingType)
    {
      return CreateScheduleByTimeAsync(channel, $"{Consts.MANUAL_RECORDING_TITLE_PREFIX}{Consts.MANUAL_RECORDING_TITLE}", from, to, recordingType);
    }

    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleByTimeAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
      Schedule tvSchedule = ScheduleFactory.CreateSchedule(channel.ChannelId, title, from, to);
      tvSchedule.PreRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("preRecordInterval", 5);
      tvSchedule.PostRecordInterval = ServiceAgents.Instance.SettingServiceAgent.GetValue("postRecordInterval", 5);
      tvSchedule.ScheduleType = (int)recordingType;
      scheduleService.SaveSchedule(tvSchedule);
      var schedule = ConvertToSchedule(tvSchedule);
      return Task.FromResult(new AsyncResult<ISchedule>(true, schedule));
    }

    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleDetailedAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType, int preRecordInterval, int postRecordInterval, string directory, int priority)
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
      ISchedule schedule = ConvertToSchedule(tvSchedule);
      return Task.FromResult(new AsyncResult<ISchedule>(true, schedule));
    }

    protected override Task<bool> EditProviderScheduleAsync(ISchedule schedule, IChannel channel = null, string title = null, DateTime? from = null, DateTime? to = null, ScheduleRecordingType? recordingType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      try
      {
        Logger.Debug("SlimTvService: Editing schedule {0} on channel {1} for {2}, {3} till {4}, type {5}", schedule.ScheduleId, channel.ChannelId, title, from, to, recordingType);
        IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
        Schedule tvSchedule = scheduleService.GetSchedule(schedule.ScheduleId);

        tvSchedule.IdChannel = channel.ChannelId;
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
        Logger.Warn(String.Format("SlimTvService: Failed to edit schedule {0}", schedule.ScheduleId), ex);
        return Task.FromResult(false);
      }
    }

    protected override Task<bool> RemoveProviderScheduleForProgramAsync(IProgram program, ScheduleRecordingType recordingType)
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
      return Task.FromResult(true);
    }

    protected override Task<bool> RemoveProviderScheduleAsync(ISchedule schedule)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      if (scheduleService == null)
        return Task.FromResult(false);

      scheduleService.DeleteSchedule(schedule.ScheduleId);
      return Task.FromResult(true);
    }

    protected override Task<bool> UnCancelProviderScheduleAsync(IProgram program)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      IScheduleService scheduleService = GlobalServiceProvider.Instance.Get<IScheduleService>();
      var tvProgram = programService.GetProgram(program.ProgramId);
      try
      {
        Logger.Debug("SlimTvService: Uncancelling schedule for programId {0}", tvProgram.IdProgram);
        foreach (Schedule schedule in scheduleService.ListAllSchedules().Where(schedule => schedule.StartTime == program.StartTime && schedule.IdChannel == tvProgram.IdChannel))
        {
          scheduleService.UnCancelSerie(schedule, program.StartTime, tvProgram.IdChannel);
        }

        return Task.FromResult(true);
      }
      catch (Exception ex)
      {
        Logger.Warn(String.Format("SlimTvService: Failed to uncancel schedule for programId {0}", program.ProgramId), ex);
        return Task.FromResult(false);
      }
    }

    protected override Task<AsyncResult<IList<IConflict>>> GetProviderConflictsAsync()
    {
      IConflictService conflictService = GlobalServiceProvider.Instance.Get<IConflictService>();
      var conflictList = conflictService.ListAllConflicts();
      var conflicts = conflictList.Select(c => (IConflict)ConvertToConflict(c)).ToList();
      return Task.FromResult(new AsyncResult<IList<IConflict>>(conflicts.Count > 0, conflicts));
    }

    protected override Task<bool> RemoveAllProviderConflictsAsync()
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      IConflictService conflictService = GlobalServiceProvider.Instance.Get<IConflictService>();

      //Clear all existing program conflicts
      var programList = programService.GetProgramsByState(ProgramState.Conflict);
      foreach (var program in programList)
      {
        ProgramState state = (ProgramState)program.State;
        program.State = (int)(state & ~ProgramState.Conflict);
        programService.SaveProgram(program);
      }

      // Clear all conflicts
      var conflictList = conflictService.ListAllConflicts();
      foreach (var conflict in conflictList)
        ConflictManagement.DeleteConflict(conflict.IdConflict);

      return Task.FromResult(true);
    }

    protected override Task<bool> SaveProviderConflictsAsync(IList<IConflict> conflicts)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      IConflictService conflictService = GlobalServiceProvider.Instance.Get<IConflictService>();

      //Add new program conflicts
      foreach (var conflict in conflicts)
      {
        var prg = programService.GetProgramAt(conflict.ProgramStartTime, conflict.ChannelId);
        if (prg == null)
          continue;

        ProgramState state = (ProgramState)prg.State;
        prg.State = (int)(state | ProgramState.Conflict);
        programService.SaveProgram(prg);
      }

      // Add new conflicts
      foreach (var conflict in conflicts.Where(c => c.ConflictingScheduleId > 0))
      {
        Mediaportal.TV.Server.TVDatabase.Entities.Conflict newConflict = new Mediaportal.TV.Server.TVDatabase.Entities.Conflict
        {
          IdCard = conflict.CardId,
          IdChannel = conflict.ChannelId,
          IdSchedule = conflict.ScheduleId,
          IdConflictingSchedule = conflict.ConflictingScheduleId,
          ConflictDate = conflict.ProgramStartTime
        };
        conflictService.SaveConflict(newConflict);
      }

      return Task.FromResult(true);
    }

    private void CancelSingleSchedule(Schedule schedule, Program canceledProgram)
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

    private void GetParentAndSpawnSchedule(ref Schedule schedule, out Schedule parentSchedule)
    {
      parentSchedule = schedule.ParentSchedule;
      if (parentSchedule != null)
        return;

      parentSchedule = schedule;
      Schedule spawn = ServiceAgents.Instance.ScheduleServiceAgent.RetrieveSpawnedSchedule(parentSchedule.IdSchedule, parentSchedule.StartTime);
      if (spawn != null)
        schedule = spawn;
    }

    private bool StopRecording(Schedule schedule)
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
          Logger.Error("SlimTvService: Error deleting schedule with ID '{0}'", ex,
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

    protected override Task<AsyncResult<RecordingStatus>> GetProviderRecordingStatusAsync(IProgram program)
    {
      IProgramService programService = GlobalServiceProvider.Instance.Get<IProgramService>();
      IProgramRecordingStatus recProgram = (IProgramRecordingStatus)GetProgram(programService.GetProgram(program.ProgramId), true);
      return Task.FromResult(new AsyncResult<RecordingStatus>(true, recProgram.RecordingStatus));
    }

    protected override Task<AsyncResult<string>> GetProviderRecordingFileOrStreamAsync(IProgram program)
    {
      Mediaportal.TV.Server.TVDatabase.Entities.Recording recording;
      if (!GetRecording(program, out recording))
        return Task.FromResult(new AsyncResult<string>(false, null));

      // FileName represents a local filesystem path on the server. It cannot be used directly in multiseat (RTSP required).
      return Task.FromResult(new AsyncResult<string>(true, recording.FileName));
    }

    private static bool GetRecording(IProgram program, out Mediaportal.TV.Server.TVDatabase.Entities.Recording recording)
    {
      IRecordingService recordingService = GlobalServiceProvider.Instance.Get<IRecordingService>();
      recording = recordingService.GetActiveRecordingByTitleAndChannel(program.Title, program.ChannelId);
      return recording != null;
    }

    protected override Task<string> SwitchProviderToChannelAsync(string userName, int channelId)
    {
      if (String.IsNullOrEmpty(userName))
      {
        Logger.Error("SlimTvService: Called SwitchTVServerToChannel with empty userName");
        throw new ArgumentNullException("userName");
      }

      IUser currentUser = UserFactory.CreateBasicUser(userName, -1);
      Logger.Debug("SlimTvService: Starting timeshifiting with username {0} on channel id {1}", userName, channelId);

      IInternalControllerService control = GlobalServiceProvider.Instance.Get<IInternalControllerService>();

      IVirtualCard card;
      IUser user;
      TvResult result = control.StartTimeShifting(currentUser.Name, channelId, out card, out user);
      Logger.Debug("SlimTvService: Tried to start timeshifting, result {0}", result);

      if (result != TvResult.Succeeded)
      {
        // TODO: should we retry?
        Logger.Error("SlimTvService: Starting timeshifting failed with result {0}", result);
        return null;
      }
      return Task.FromResult(userName.StartsWith(Consts.LOCAL_USERNAME + "-") ? card.TimeShiftFileName : card.RTSPUrl);
    }

    protected IUser GetUserByUserName(string userName, bool create = false)
    {
      if (userName == null)
      {
        Logger.Warn("SlimTvService: Used user with null name");
        return null;
      }

      if (!_tvUsers.ContainsKey(userName) && !create)
        return null;

      if (!_tvUsers.ContainsKey(userName) && create)
        _tvUsers.Add(userName, new User(userName, UserType.Normal));
      return _tvUsers[userName];
    }

    protected override Task<AsyncResult<IList<ICard>>> GetProviderCardsAsync()
    {
      IInternalControllerService control = GlobalServiceProvider.Instance.Get<IInternalControllerService>();
      List<ICard> cards = control.CardCollection.Select(card => new SlimTvCard()
      {
        Name = card.Value.CardName,
        CardId = card.Value.Card.TunerId,
        EpgIsGrabbing = card.Value.Epg.IsGrabbing,
        HasCam = card.Value.DataBaseCard.UseConditionalAccess, 
        CamType = card.Value.Card.CamType == CamType.Default ? SlimTvCamType.Default : SlimTvCamType.Astoncrypt2, 
        DecryptLimit = card.Value.DataBaseCard.DecryptLimit, Enabled = card.Value.DataBaseCard.Enabled, 
        RecordingFolder = card.Value.DataBaseCard.RecordingFolder, 
        TimeshiftFolder = card.Value.DataBaseCard.TimeshiftingFolder, 
        DevicePath = card.Value.DataBaseCard.DevicePath, 
        PreloadCard = card.Value.DataBaseCard.PreloadCard, 
        Priority = card.Value.DataBaseCard.Priority
      }).Cast<ICard>().ToList();

      return Task.FromResult(new AsyncResult<IList<ICard>>(cards.Count > 0, cards));
    }

    protected override Task<AsyncResult<IList<SlimTvIVirtualCard>>> GetProviderActiveVirtualCardsAsync()
    {
      List<SlimTvIVirtualCard> cards = new List<SlimTvIVirtualCard>();
      foreach (var card in ServiceAgents.Instance.CardServiceAgent.ListAllCards())
      {
        IDictionary<string, IUser> usersForCard = ServiceAgents.Instance.ControllerServiceAgent.GetUsersForCard(card.IdCard);
        
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
                  IdChannel = subchannel.IdChannel,
                  //IsAdmin = vcard.User.IsAdmin,
                  SubChannel = subchannel.Id,
                  TvStoppedReason = (SlimTvStoppedReason)Enum.Parse(typeof(SlimTvStoppedReason), vcard.User.TvStoppedReason.ToString()),
                } : null
              });
            }
          }          
        }
      }

      return Task.FromResult(new AsyncResult<IList<SlimTvIVirtualCard>>(cards.Count > 0, cards));
    }

    #endregion

    #region Conversion

    protected override Interfaces.UPnP.Items.Program ConvertToProgram(Mediaportal.TV.Server.TVDatabase.Entities.Program tvProgram, bool includeRecordingStatus = false)
    {
      if (tvProgram == null)
        return null;
      Interfaces.UPnP.Items.Program program = new Interfaces.UPnP.Items.Program
      {
        ChannelId = tvProgram.IdChannel,
        ProgramId = tvProgram.IdProgram,
        Title = tvProgram.Title,
        Description = tvProgram.Description,
        Genre = tvProgram.ProgramCategory?.Category,
        StartTime = tvProgram.StartTime,
        EndTime = tvProgram.EndTime,
        OriginalAirDate = tvProgram.OriginalAirDate,
        Classification = tvProgram.Classification,
        ParentalRating = tvProgram.ParentalRating,
        StarRating = tvProgram.StarRating,
        SeasonNumber = tvProgram.SeriesNum,
        EpisodeNumber = tvProgram.EpisodeNum,
        EpisodeNumberDetailed = tvProgram.EpisodeNum,  // TVE3.5 doesn't have Episode.Number?
        EpisodePart = tvProgram.EpisodePart,
        EpisodeTitle = tvProgram.EpisodeName,
      };

      ProgramBLL programLogic = new ProgramBLL(tvProgram);
      program.RecordingStatus = programLogic.IsRecording ? RecordingStatus.Recording : RecordingStatus.None;
      if (programLogic.IsRecordingOncePending || programLogic.IsRecordingOnce)
        program.RecordingStatus |= RecordingStatus.Scheduled;
      if (programLogic.IsRecordingSeriesPending || programLogic.IsRecordingSeries)
        program.RecordingStatus |= RecordingStatus.SeriesScheduled;
      if (programLogic.IsRecordingOnce)
        program.RecordingStatus |= RecordingStatus.RecordingOnce;
      if (programLogic.IsRecordingSeries)
        program.RecordingStatus |= RecordingStatus.RecordingSeries;
      if (programLogic.IsRecordingManual)
        program.RecordingStatus |= RecordingStatus.RecordingManual;
      program.HasConflict = programLogic.HasConflict;

      return program;
    }

    protected override Interfaces.UPnP.Items.Channel ConvertToChannel(Mediaportal.TV.Server.TVDatabase.Entities.Channel tvChannel)
    {
      return new Interfaces.UPnP.Items.Channel
      {
        ChannelId = tvChannel.IdChannel,
        ChannelNumber = tvChannel.ChannelNumber,
        Name = tvChannel.DisplayName,
        MediaType = (MediaType)tvChannel.MediaType,
        EpgHasGaps = tvChannel.EpgHasGaps,
        ExternalId = tvChannel.ExternalId,
        GrapEpg = tvChannel.GrabEpg,
        LastGrabTime = tvChannel.LastGrabTime,
        TimesWatched = tvChannel.TimesWatched,
        TotalTimeWatched = tvChannel.TotalTimeWatched,
        VisibleInGuide = tvChannel.VisibleInGuide,
        GroupNames = tvChannel.GroupMaps.Select(group => group.ChannelGroup.GroupName).ToList()
      };
    }

    protected override Interfaces.UPnP.Items.ChannelGroup ConvertToChannelGroup(Mediaportal.TV.Server.TVDatabase.Entities.ChannelGroup tvGroup)
    {
      if (tvGroup == null)
        return null;

      return new Interfaces.UPnP.Items.ChannelGroup
      {
        ChannelGroupId = tvGroup.IdGroup,
        Name = tvGroup.GroupName,
        MediaType = tvGroup.MediaType == 0 ? MediaType.TV : MediaType.Radio,
        SortOrder = tvGroup.SortOrder
      };
    }

    protected override Interfaces.UPnP.Items.Schedule ConvertToSchedule(Mediaportal.TV.Server.TVDatabase.Entities.Schedule schedule)
    {
      return new Interfaces.UPnP.Items.Schedule
      {
        ChannelId = schedule.IdChannel,
        Name = schedule.ProgramName,
        KeepDate = schedule.KeepDate,
        KeepMethod = (Interfaces.Items.KeepMethodType)schedule.KeepMethod,
        PreRecordInterval = TimeSpan.FromMinutes(schedule.PreRecordInterval),
        PostRecordInterval = TimeSpan.FromMinutes(schedule.PostRecordInterval),
        Priority = (PriorityType)schedule.Priority,
        StartTime = schedule.StartTime,
        EndTime = schedule.EndTime,
        ScheduleId = schedule.IdSchedule,
        ParentScheduleId = schedule.IdParentSchedule,
        RecordingType = (ScheduleRecordingType)schedule.ScheduleType
      };
    }

    protected override Interfaces.UPnP.Items.TuningDetail ConvertToTuningDetail(Mediaportal.TV.Server.TVDatabase.Entities.TuningDetail tuningDetail)
    {
      return new Interfaces.UPnP.Items.TuningDetail
      {
        TuningDetailId = tuningDetail.IdTuning,
        InnerFecRate = tuningDetail.InnerFecRate,
        ChannelId = tuningDetail.IdChannel,
        Name = tuningDetail.Name,
        Provider = tuningDetail.Provider,
        ChannelType = tuningDetail.ChannelType == 0 ? ChannelType.Analog :
          tuningDetail.ChannelType == 1 ? ChannelType.Atsc :
          tuningDetail.ChannelType == 2 ? ChannelType.DvbC :
          tuningDetail.ChannelType == 3 ? ChannelType.DvbS :
          tuningDetail.ChannelType == 4 ? ChannelType.DvbT :
          tuningDetail.ChannelType == 7 ? ChannelType.DvbIP : ChannelType.Unsupported,
        PhysicalChannelNumber = tuningDetail.ChannelNumber,
        Frequency = tuningDetail.Frequency,
        CountryId = tuningDetail.CountryId,
        MediaType = (MediaType)tuningDetail.MediaType,
        NetworkId = tuningDetail.NetworkId,
        TransportId = tuningDetail.TransportId,
        ServiceId = tuningDetail.ServiceId,
        PmtPid = tuningDetail.PmtPid,
        IsEncrypted = !tuningDetail.FreeToAir,
        Modulation = tuningDetail.Modulation,
        Polarisation = tuningDetail.Polarisation,
        Symbolrate = tuningDetail.Symbolrate,
        Bandwidth = tuningDetail.Bandwidth,
        LogicalChannelNumber = $"{tuningDetail.MajorChannel}.{tuningDetail.MinorChannel}",
        VideoSource = tuningDetail.VideoSource,
        AudioSource = tuningDetail.AudioSource,
        IsVCRSignal = tuningDetail.IsVCRSignal,
        TuningSource = tuningDetail.TuningSource,
        Pilot = tuningDetail.Pilot,
        RollOff = tuningDetail.RollOff,
        Url = tuningDetail.Url,
      };
    }

    protected override Interfaces.UPnP.Items.Recording ConvertToRecording(Mediaportal.TV.Server.TVDatabase.Entities.Recording recording)
    {
      return new Interfaces.UPnP.Items.Recording
      {
        RecordingId = recording.IdRecording,
        ChannelId = recording.IdChannel,
        ScheduleId = recording.IdSchedule,
        Title = recording.Title,
        Description = recording.Description,
        Genre = recording.ProgramCategory?.Category,
        StartTime = recording.StartTime,
        EndTime = recording.EndTime,
        IsManual = IsManualTitle(recording.Title),
        SeasonNumber = recording.SeriesNum,
        EpisodeNumber = recording.EpisodeNum,
        EpisodeNumberDetailed = $"S{recording.SeriesNum}E{recording.EpisodeNum}",
        EpisodePart = recording.EpisodePart,
        EpisodeTitle = recording.EpisodeName,
        KeepDate = recording.KeepUntilDate,
        KeepMethod = (Interfaces.Items.KeepMethodType)recording.KeepUntil
      };
    }

    protected override Interfaces.UPnP.Items.ScheduleRule ConvertToScheduleRule(Interfaces.UPnP.Items.ScheduleRule scheduleRule)
    {
      return scheduleRule;
    }

    protected override Interfaces.UPnP.Items.Conflict ConvertToConflict(Mediaportal.TV.Server.TVDatabase.Entities.Conflict conflict)
    {
      return new Interfaces.UPnP.Items.Conflict
      {
        ConflictId = conflict.IdConflict,
        CardId = conflict.IdCard ?? 0,
        ChannelId = conflict.IdChannel,
        ScheduleId = conflict.IdSchedule,
        ProgramStartTime = conflict.ConflictDate,
        ConflictingScheduleId = conflict.IdConflictingSchedule,
      };
    }

    #endregion
  }
}
