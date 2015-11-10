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
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.Backend.Database;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using Mediaportal.TV.Server.TVLibrary.IntegrationProvider.Interfaces;
using MediaPortal.Utilities.FileSystem;
using TvLibrary.Interfaces.Integration;
using IChannel = MediaPortal.Plugins.SlimTv.Interfaces.Items.IChannel;
using ILogger = MediaPortal.Common.Logging.ILogger;
using IPathManager = MediaPortal.Common.PathManager.IPathManager;
using ScheduleRecordingType = MediaPortal.Plugins.SlimTv.Interfaces.ScheduleRecordingType;
using MediaPortal.Plugins.SlimTv.Service3;
using MediaPortal.Utilities;
using TvLibrary.Implementations.DVB;
using TvControl;
using TvDatabase;
using TvEngine.Events;
using TvLibrary.Interfaces;
using TvService;
using Card = TvDatabase.Card;
using SlimTvCard = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Card;
using SlimTvVirtualCard = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.VirtualCard;
using SlimTvUser = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.User;
using IUser = TvControl.IUser;
using User = TvControl.User;
using VirtualCard = TvControl.VirtualCard;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public class SlimTvService : AbstractSlimTvService
  {
    private TvServiceThread _tvServiceThread;
    protected readonly Dictionary<string, IUser> _tvUsers = new Dictionary<string, IUser>();
    protected IController _tvControl;
    protected TvBusinessLayer _tvBusiness;
    protected Thread _serviceThread;

    public SlimTvService()
    {
      _serviceName = "SlimTv.Service3";
    }

    #region Database and program data initialization

    protected override void PrepareIntegrationProvider()
    {
      IntegrationProviderHelper.Register(@"Plugins\" + _serviceName, "Plugins\\" + _serviceName + "\\castle.config");
      // This access is intended to force an initialization of PathManager service!
      var pm = GlobalServiceProvider.Instance.Get<IIntegrationProvider>().PathManager;
    }

    protected override void PrepareConnection(ITransaction transaction)
    { }

    protected override void PrepareFilterRegistrations()
    {
      Dictionary<Guid, string> filters = new Dictionary<Guid, string>
      {
        { new Guid("{D3DD4C59-D3A7-4B82-9727-7B9203EB67C0}"), "MPIPTvSource.ax"},
        { new Guid("{7F2BBEAF-E11C-4D39-90E8-938FB5A86045}"), "PDMpgMux.ax"}
      };
      foreach (var filter in filters)
      {
        try
        {
          if (!FilterGraphTools.IsThisComObjectInstalled(filter.Key))
          {
            var filterPath = FileUtils.BuildAssemblyRelativePath(filter.Value);
            COMRegistration.Register(filterPath, true);
          }
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to register filter {0}", ex, filter.Value);
        }
      }
    }

    protected override void PrepareProgramData()
    {
      base.PrepareProgramData();
      // TVE3 doesn't allow all kind of required modifications of paths yet, so we need to use some "old" paths here
      try
      {
        string mpTveServer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Team MediaPortal", "MediaPortal TV Server");
        string logFolder = Path.Combine(mpTveServer, "log");
        if (!Directory.Exists(logFolder))
          Directory.CreateDirectory(logFolder);


        string mpIpTvConfig = "MPIPTVSource.ini";
        string target = Path.Combine(mpTveServer, mpIpTvConfig);
        if (!File.Exists(target))
        {
          File.Copy(Path.Combine(ServiceRegistration.Get<IPathManager>().GetPath("<TVCORE>"), mpIpTvConfig), target);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Error creating TVE3 folders", ex);
      }
    }

    protected override void InitTvCore()
    {
      _tvServiceThread = new TvServiceThread(Environment.GetCommandLineArgs()[0]);
      if (!InitializeGentle())
      {
        DeInit();
        return;
      }

      Start();
      if (!_tvServiceThread.InitializedEvent.WaitOne(MAX_WAIT_MS))
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to start TV service thread within {0} seconds.", MAX_WAIT_MS / 1000);
      }

      InitializeTVE();
      // Handle events from TvEngine
      if (!RegisterEvents())
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Failed to register events. This happens only if startup failed. Stopping plugin now.");
        DeInit();
      }
    }

    protected void Start()
    {
      var tvServiceThreadStart = new ThreadStart(() =>
      {
        try
        {
          _tvServiceThread.OnStart();
        }
        catch
        {
          // Only exit the process if the caller forces this behavior.
        }
      });
      _serviceThread = new Thread(tvServiceThreadStart) { IsBackground = false };
      _serviceThread.Start();
    }

    protected bool InitializeGentle()
    {
      try
      {
        // Use the same Gentle.config as the TVEngine
        string gentleConfigFile = Path.Combine(ServiceRegistration.Get<IPathManager>().GetPath("<TVCORE>"), "Gentle.config");
        // but be quiet when it doesn't exists, as not everyone has the TV Engine installed
        if (!File.Exists(gentleConfigFile))
        {
          ServiceRegistration.Get<ILogger>().Info("Cannot find Gentle.config file, assuming TVEngine isn't installed...");
          return false;
        }
        Gentle.Common.Configurator.AddFileHandler(gentleConfigFile);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Failed to connect to TVEngine", ex);
        return false;
      }
      return true;
    }

    protected void InitializeTVE()
    {
      try
      {
        _tvControl = _tvServiceThread.Controller;
        _tvBusiness = new TvBusinessLayer();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Failed to connect to TVEngine", ex);
      }
    }

    public override bool DeInit()
    {
      if (_serviceThread != null && _serviceThread.IsAlive)
      {
        bool joined = _serviceThread.Join(MAX_WAIT_MS);
        if (!joined)
        {
          _serviceThread.Abort();
          _serviceThread.Join();
        }
        _tvServiceThread = null;
      }
      return true;
    }

    #endregion

    #region Recordings / MediaLibrary synchronization

    protected override bool RegisterEvents()
    {
      ITvServerEvent tvServerEvent = GlobalServiceProvider.Instance.TryGet<ITvServerEvent>();
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
          var recording = Recording.Retrieve(tvEvent.Recording.IdRecording);
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
      TvBusinessLayer layer = new TvBusinessLayer();
      IList<Card> allCards = Card.ListAll();
      // Get all different recording folders
      recordingFolders = allCards.Select(c => c.RecordingFolder).Where(f => !string.IsNullOrEmpty(f)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

      singlePattern = layer.GetSetting("moviesformat", string.Empty).Value;
      seriesPattern = layer.GetSetting("seriesformat", string.Empty).Value;
      return recordingFolders.Count > 0;
    }

    #endregion

    #region ITvProvider implementation

    private IUser GetUserByUserName(string userName)
    {
      return Card.ListAll()
        .Where(c => c != null && c.Enabled)
        .SelectMany(c => { var users = _tvControl.GetUsersForCard(c.IdCard); return users ?? new IUser[] { }; })
        .FirstOrDefault(u => u.Name == userName);
    }

    public override bool StopTimeshift(string userName, int slotIndex)
    {
      IUser user;
      user = GetUserByUserName(GetUserName(userName, slotIndex));
      if (user == null)
        return false;
      return _tvControl.StopTimeShifting(ref user);
    }

    public override MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      // Channel is usually only passed as placeholder with ID only, so query the details here
      TvDatabase.Channel fullChannel = TvDatabase.Channel.Retrieve(channel.ChannelId);
      bool isTv = fullChannel.IsTv;
      return CreateMediaItem(slotIndex, streamUrl, channel, isTv, fullChannel.ToChannel());
    }

    public override bool GetNowNextProgram(IChannel channel, out IProgram programNow, out IProgram programNext)
    {
      var tvChannel = TvDatabase.Channel.Retrieve(channel.ChannelId);
      programNow = tvChannel.CurrentProgram.ToProgram();
      programNext = tvChannel.NextProgram.ToProgram();
      return programNow != null || programNext != null;
    }

    public override bool GetNowAndNextForChannelGroup(IChannelGroup channelGroup, out IDictionary<int, IProgram[]> nowNextPrograms)
    {
      nowNextPrograms = new Dictionary<int, IProgram[]>();
      IList<IChannel> channels;
      if (!GetChannels(channelGroup, out channels))
        return false;

      foreach (IChannel channel in channels)
      {
        IProgram programNow;
        IProgram programNext;
        if (GetNowNextProgram(channel, out programNow, out programNext))
          nowNextPrograms[channel.ChannelId] = new[] { programNow, programNext };
      }
      return true;
    }

    public override bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      programs = _tvBusiness.GetPrograms(TvDatabase.Channel.Retrieve(channel.ChannelId), from, to)
        .Select(tvProgram => tvProgram.ToProgram(true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      return programs.Count > 0;
    }

    public override bool GetPrograms(string title, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      programs = _tvBusiness.SearchPrograms(title).Where(p => p.StartTime >= from && p.StartTime <= to || p.EndTime >= from && p.EndTime <= to)
        .Select(tvProgram => tvProgram.ToProgram(true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      return programs.Count > 0;
    }

    public override bool GetProgramsGroup(IChannelGroup channelGroup, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      programs = new List<IProgram>();
      if (channelGroup.ChannelGroupId < 0)
      {
        foreach (var channel in _tvBusiness.GetRadioGuideChannelsForGroup(-channelGroup.ChannelGroupId))
          CollectionUtils.AddAll(programs, _tvBusiness.GetPrograms(TvDatabase.Channel.Retrieve(channel.IdChannel), from, to).Select(p => p.ToProgram()));
      }
      else
      {
      foreach (var channel in _tvBusiness.GetTVGuideChannelsForGroup(channelGroup.ChannelGroupId))
        CollectionUtils.AddAll(programs, _tvBusiness.GetPrograms(TvDatabase.Channel.Retrieve(channel.IdChannel), from, to).Select(p => p.ToProgram()));
      }
      return programs.Count > 0;
    }

    public override bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs)
    {
      programs = new List<IProgram>();
      var tvSchedule = TvDatabase.Schedule.Retrieve(schedule.ScheduleId);
      if (tvSchedule == null)
        return false;

      programs = TvDatabase.Schedule.GetProgramsForSchedule(tvSchedule).Select(p => p.ToProgram()).ToList();
      return programs.Count > 0;
    }

    public override bool GetChannel(IProgram program, out IChannel channel)
    {
      channel = TvDatabase.Channel.Retrieve(program.ChannelId).ToChannel();
      return true;
    }

    public override bool GetProgram(int programId, out IProgram program)
    {
      program = TvDatabase.Program.Retrieve(programId).ToProgram();
      return program != null;
    }

    public override bool GetChannelGroups(out IList<IChannelGroup> groups)
    {
      groups = TvDatabase.ChannelGroup.ListAll()
        .OrderBy(tvGroup => tvGroup.SortOrder)
        .Select(tvGroup => tvGroup.ToChannelGroup())
        .Union(
          RadioChannelGroup.ListAll()
          .OrderBy(radioGroup => radioGroup.SortOrder)
          .Select(radioGroup => radioGroup.ToChannelGroup())
        )
        .ToList();
      return true;
    }

    public override bool GetChannel(int channelId, out IChannel channel)
    {
      channel = TvDatabase.Channel.Retrieve(channelId).ToChannel();
      return channel != null;
    }

    public override bool GetChannels(IChannelGroup group, out IList<IChannel> channels)
    {
      if (group.ChannelGroupId < 0)
      {
        var radioGroup = RadioChannelGroup.Retrieve(-group.ChannelGroupId);
        var radioChannels = radioGroup.ReferringRadioGroupMap().OrderBy(rgm => rgm.SortOrder).Select(rgm => rgm.ReferencedChannel());
        channels = radioChannels
          .Where(c => c.VisibleInGuide)
          .Select(c => c.ToChannel())
          .ToList();
      }
      else
      {
        channels = _tvBusiness.GetChannelsInGroup(TvDatabase.ChannelGroup.Retrieve(group.ChannelGroupId))
          // Bug? SortOrder contains logical channel number, not the group sort order?
          // .OrderBy(c => c.SortOrder)
          .Where(c => c.VisibleInGuide)
          .Select(c => c.ToChannel())
          .ToList();
      }
      return true;
    }

    public override bool GetSchedules(out IList<ISchedule> schedules)
    {
      schedules = TvDatabase.Schedule.ListAll().Select(s => s.ToSchedule()).ToList();
      return true;
    }

    public override bool CreateSchedule(IProgram program, ScheduleRecordingType recordingType, out ISchedule schedule)
    {
      TvDatabase.Schedule tvSchedule = _tvBusiness.AddSchedule(program.ChannelId, program.Title, program.StartTime, program.EndTime, (int)recordingType);
      tvSchedule.ScheduleType = (int)recordingType;
      tvSchedule.PreRecordInterval = Int32.Parse(_tvBusiness.GetSetting("preRecordInterval", "5").Value);
      tvSchedule.PostRecordInterval = Int32.Parse(_tvBusiness.GetSetting("postRecordInterval", "5").Value);
      tvSchedule.Persist();
      _tvControl.OnNewSchedule();
      schedule = tvSchedule.ToSchedule();
      return true;
    }

    public override bool CreateScheduleByTime(IChannel channel, DateTime from, DateTime to, out ISchedule schedule)
    {
      TvDatabase.Schedule tvSchedule = _tvBusiness.AddSchedule(channel.ChannelId, "Manual", from, to, (int)ScheduleRecordingType.Once);
      tvSchedule.PreRecordInterval = Int32.Parse(_tvBusiness.GetSetting("preRecordInterval", "5").Value);
      tvSchedule.PostRecordInterval = Int32.Parse(_tvBusiness.GetSetting("postRecordInterval", "5").Value);
      tvSchedule.Persist();
      _tvControl.OnNewSchedule();
      schedule = tvSchedule.ToSchedule();
      return true;
    }

    public override bool RemoveScheduleForProgram(IProgram program, ScheduleRecordingType recordingType)
    {
      var canceledProgram = TvDatabase.Program.Retrieve(program.ProgramId);
      if (canceledProgram == null)
        return false;
      foreach (TvDatabase.Schedule schedule in TvDatabase.Schedule.ListAll().Where(schedule => schedule.IsRecordingProgram(canceledProgram, true)))
      {
        switch (schedule.ScheduleType)
        {
          case (int)ScheduleRecordingType.Once:
            schedule.Delete();
            _tvControl.OnNewSchedule();
            break;
          default:
            CanceledSchedule canceledSchedule = new CanceledSchedule(schedule.IdSchedule, schedule.IdChannel, schedule.StartTime);
            canceledSchedule.Persist();
            _tvControl.OnNewSchedule();
            break;
        }
      }
      return true;
    }

    public override bool RemoveSchedule(ISchedule schedule)
    {
      TvDatabase.Schedule tvSchedule = TvDatabase.Schedule.Retrieve(schedule.ScheduleId);
      _tvControl.StopRecordingSchedule(tvSchedule.IdSchedule);
      // delete canceled schedules first
      foreach (var cs in CanceledSchedule.ListAll().Where(x => x.IdSchedule == tvSchedule.IdSchedule))
        cs.Remove();
      tvSchedule.Remove();
      _tvControl.OnNewSchedule(); // I don't think this is needed, but doesn't hurt either
      return true;
    }

    public override bool GetRecordingStatus(IProgram program, out RecordingStatus recordingStatus)
    {
      var tvProgram = (IProgramRecordingStatus)TvDatabase.Program.Retrieve(program.ProgramId).ToProgram(true);
      recordingStatus = tvProgram.RecordingStatus;
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
      recording = Recording.ListAll().FirstOrDefault(r => r.IsRecording && r.IdChannel == program.ChannelId && r.Title == program.Title);
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

      // actually start timeshifting
      VirtualCard card;
      TvResult result = _tvControl.StartTimeShifting(ref currentUser, channelId, out card);
      // make sure result is correct and return
      if (result != TvResult.Succeeded)
      {
        ServiceRegistration.Get<ILogger>().Error("Starting timeshifting failed with result {0}", result);
        return null;
      }
      if (card == null)
      {
        ServiceRegistration.Get<ILogger>().Error("Couldn't get virtual card");
        return null;
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
        _tvUsers.Add(userName, new User(userName, false));

      return _tvUsers[userName];
    }

    public override bool GetCards(out List<ICard> cards)
    {
      cards = _tvBusiness.Cards.Select(card => new SlimTvCard()
      {
        Name = card.Name, 
        CardId = card.IdCard, 
        EpgIsGrabbing = card.GrabEPG, 
        HasCam = card.CAM, 
        CamType = card.CamType == (int)CamType.Default ? SlimTvCamType.Default : SlimTvCamType.Astoncrypt2, 
        DecryptLimit = card.DecryptLimit, Enabled = card.Enabled, 
        RecordingFolder = card.RecordingFolder, 
        TimeshiftFolder = card.TimeShiftFolder, 
        DevicePath = card.DevicePath, 
        PreloadCard = card.PreloadCard, 
        Priority = card.Priority, 
        SupportSubChannels = card.supportSubChannels, 
        RecordingFormat = card.RecordingFormat
      }).Cast<ICard>().ToList();

      return cards.Count > 0;
    }

    public override bool GetActiveVirtualCards(out List<IVirtualCard> cards)
    {
      IEnumerable<VirtualCard> virtualCards = Card.ListAll()
                .Where(card => RemoteControl.Instance.CardPresent(card.IdCard))
                .Select(card => RemoteControl.Instance.GetUsersForCard(card.IdCard))
                .Where(users => users != null)
                .SelectMany(user => user)
                .Select(user => new VirtualCard(user, RemoteControl.HostName))
                .Where(tvCard => tvCard.IsTimeShifting || tvCard.IsRecording);

      cards = new List<IVirtualCard>();
      foreach (var card in virtualCards)
      {
        cards.Add(new SlimTvVirtualCard
        {
          BitRateMode = (int)card.BitRateMode,
          ChannelName = card.ChannelName,
          Device = card.Device,
          Enabled = card.Enabled,
          GetTimeshiftStoppedReason = (int)card.GetTimeshiftStoppedReason,
          GrabTeletext = card.GrabTeletext,
          HasTeletext = card.HasTeletext,
          Id = card.Id,
          ChannelId = card.IdChannel,
          IsGrabbingEpg = card.IsGrabbingEpg,
          IsRecording = card.IsRecording,
          IsScanning = card.IsScanning,
          IsScrambled = card.IsScrambled,
          IsTimeShifting = card.IsTimeShifting,
          IsTunerLocked = card.IsTunerLocked,
          MaxChannel = card.MaxChannel,
          MinChannel = card.MinChannel,
          Name = card.Name,
          QualityType = (int)card.QualityType,
          RecordingFileName = card.RecordingFileName,
          RecordingFolder = card.RecordingFolder,
          RecordingFormat = card.RecordingFormat,
          RecordingScheduleId = card.RecordingScheduleId,
          RecordingStarted = card.RecordingStarted != DateTime.MinValue ? card.RecordingStarted : new DateTime(2000, 1, 1),
          RemoteServer = card.RemoteServer,
          RTSPUrl = card.RTSPUrl,
          SignalLevel = card.SignalLevel,
          SignalQuality = card.SignalQuality,
          TimeShiftFileName = card.TimeShiftFileName,
          TimeShiftFolder = card.TimeshiftFolder,
          TimeShiftStarted = card.TimeShiftStarted != DateTime.MinValue ? card.TimeShiftStarted : new DateTime(2000, 1, 1),
          Type = (SlimTvCardType)Enum.Parse(typeof(SlimTvCardType), card.Type.ToString()),
          User = card.User != null ? new SlimTvUser
          {
            Priority = card.User.Priority,
            ChannelStates = card.User.ChannelStates.ToDictionary(item => item.Key, item => (SlimTvChannelState)Enum.Parse(typeof(SlimTvChannelState), item.ToString())),
            CardId = card.User.CardId,
            Name = card.User.Name,
            FailedCardId = card.User.FailedCardId,
            HeartBeat = card.User.HeartBeat,
            History = card.User.History,
            IdChannel = card.User.IdChannel,
            IsAdmin = card.User.IsAdmin,
            SubChannel = card.User.SubChannel,
            TvStoppedReason = (SlimTvStoppedReason)Enum.Parse(typeof(SlimTvStoppedReason), card.User.TvStoppedReason.ToString()),
          } : null
        });
      }

      return cards.Count > 0;
    }

    #endregion
  }
}
