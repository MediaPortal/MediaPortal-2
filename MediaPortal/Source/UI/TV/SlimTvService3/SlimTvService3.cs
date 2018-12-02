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

using Mediaportal.TV.Server.TVLibrary.IntegrationProvider.Interfaces;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Backend.Database;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.Plugins.SlimTv.Service3;
using MediaPortal.Utilities;
using MediaPortal.Utilities.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TvControl;
using TvDatabase;
using TvEngine.Events;
using TvLibrary.Implementations.DVB;
using TvLibrary.Interfaces;
using TvLibrary.Interfaces.Integration;
using TvService;
using IChannel = MediaPortal.Plugins.SlimTv.Interfaces.Items.IChannel;
using ILogger = MediaPortal.Common.Logging.ILogger;
using IPathManager = MediaPortal.Common.PathManager.IPathManager;
using ScheduleRecordingType = MediaPortal.Plugins.SlimTv.Interfaces.ScheduleRecordingType;

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

      FixupServer();

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
        return;
      }

      _ = CleanUpRecordingsAsync();
    }

    /// <summary>
    /// In case of changed server names/IP addresses the TVE core rejects startup. This impacts the whole MP2 service, so we update changed IPs first.
    /// </summary>
    protected void FixupServer()
    {
      var server = Server.ListAll().FirstOrDefault(s => s.IsMaster);
      if (server != null)
      {
        // Check for valid entries: this is the hostname and all IP addresses
        ICollection<string> nameAndAdresses = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        string hostName = Dns.GetHostName();
        nameAndAdresses.Add(hostName);
        IPHostEntry local = Dns.GetHostEntry(hostName);
        CollectionUtils.AddAll(nameAndAdresses, local.AddressList.Where(a => a.AddressFamily == AddressFamily.InterNetwork).Select(a => a.ToString()));
        if (!nameAndAdresses.Contains(server.HostName))
        {
          server.HostName = hostName;
          server.Persist();
        }
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
      var thread = _serviceThread;
      _tvServiceThread = null;
      if (thread != null && thread.IsAlive)
      {
        try
        {
          bool joined = thread.Join(MAX_WAIT_MS);
          if (!joined)
          {
            thread.Abort();
            thread.Join();
          }
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Failed to deinit TVEngine", ex);
        }
      }
      return true;
    }

    #endregion

    #region Server state

    protected void UpdateServerState()
    {
      IList<ISchedule> currentlyRecordingSchedules = Recording.ListAllActive().Where(r => r.IsRecording)
        .Select(r => r.ReferencedSchedule().ToSchedule()).ToList();

      TvServerState state = new TvServerState
      {
        IsRecording = _tvControl.IsAnyCardRecording(),
        CurrentlyRecordingSchedules = currentlyRecordingSchedules
      };

      ServiceRegistration.Get<IServerStateService>().UpdateState(TvServerState.STATE_ID, state);
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
          UpdateServerState();
          var recording = Recording.Retrieve(tvEvent.Recording.IdRecording);
          if (recording != null)
          {
            ServiceRegistration.Get<ILogger>().Info("SlimTvService: {0}: {1}", tvEvent.EventType, recording.FileName);
            ImportRecording(recording.FileName);
          }
        }
        if (tvEvent.EventType == TvServerEventType.RecordingEnded)
        {
          _ = CleanUpRecordingsAsync();
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("SlimTvService: Exception while handling TvServerEvent", ex);
      }
    }

    protected override bool GetRecordingConfiguration(out List<string> recordingFolders, out string singlePattern, out string seriesPattern)
    {
      try
      {
        TvBusinessLayer layer = new TvBusinessLayer();
        IList<Card> allCards = Card.ListAll();
        // Get all different recording folders
        recordingFolders = allCards.Select(c => c.RecordingFolder).Where(f => !string.IsNullOrEmpty(f)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        singlePattern = layer.GetSetting("moviesformat", string.Empty).Value;
        seriesPattern = layer.GetSetting("seriesformat", string.Empty).Value;
        return recordingFolders.Count > 0;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTvService: Exception while getting recording folders", ex);
      }
      recordingFolders = null;
      singlePattern = null;
      seriesPattern = null;
      return false;
    }

    protected async Task CleanUpRecordingsAsync()
    {
      await Task.Run(() =>
      {
        ServiceRegistration.Get<ILogger>().Info("SlimTvService: Begin recordings auto-cleanup");
        int countDeleted = 0;
        var allRecordings = Recording.ListAll();
        ICollection<string> nonExistingRootPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Recording recording in allRecordings.Where(r => !r.IsRecording))
        {
          var pathRoot = Path.GetPathRoot(recording.FileName);
          if (nonExistingRootPaths.Contains(pathRoot))
            continue;

          // If UNC path not available, cache information to avoid retry during this run.
          if (!Directory.Exists(pathRoot))
          {
            nonExistingRootPaths.Add(pathRoot);
            continue;
          }

          if (!File.Exists(recording.FileName))
          {
            ServiceRegistration.Get<ILogger>().Debug("SlimTvService: Remove '{0}'", recording.FileName);
            countDeleted++;
            recording.Delete();
          }
        }
        ServiceRegistration.Get<ILogger>().Info("SlimTvService: Removed {0} no longer existing recordings.", countDeleted);
      });
    }

    #endregion

    #region ITvProvider implementation

    private IUser GetUserByUserName(string userName)
    {
      if (_tvControl == null)
        return null;
      return Card.ListAll()
        .Where(c => c != null && c.Enabled)
        .SelectMany(c => { var users = _tvControl.GetUsersForCard(c.IdCard); return users ?? new IUser[] { }; })
        .FirstOrDefault(u => u.Name == userName);
    }

    protected override void InitGenreMap()
    {
      if (_tvGenresInited)
        return;

      _tvGenresInited = true;

      string genre;
      bool enabled;
      IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
      if (converter == null)
        return;

      // Get the id of the mp genre identified as the movie genre.
      int genreMapMovieGenreId;
      if (!int.TryParse(_tvBusiness.GetSetting("genreMapMovieGenreId").Value, out genreMapMovieGenreId))
      {
        genreMapMovieGenreId = -1;
      }

      // Each genre map value is a '{' delimited list of "program" genre names (those that may be compared with the genre from the program listings).
      // It is an error if a single "program" genre is mapped to more than one guide genre; behavior is undefined for this condition.
      int genreIndex = 0;
      while (true)
      {
        // The genremap key is an integer value that is added to a base value in order to locate the correct localized genre name string.
        genre = _tvBusiness.GetSetting("genreMapName" + genreIndex).Value;
        if (string.IsNullOrEmpty(genre))
          break;

        // Get the status of the mp genre.
        if (!bool.TryParse(_tvBusiness.GetSetting("genreMapNameEnabled" + genreIndex).Value, out enabled))
        {
          enabled = true;
        }
        EpgGenre? epgGenre = null;
        if (enabled && genreIndex == genreMapMovieGenreId)
        {
          epgGenre = EpgGenre.Movie;
        }
        else if (enabled && !string.IsNullOrEmpty(genre))
        {
          if (converter.GetGenreId(genre, GenreCategory.Epg, null, out int genreId))
            epgGenre = (EpgGenre)genreId;
        }
        if (epgGenre.HasValue)
        {
          string genreMapEntry = _tvBusiness.GetSetting("genreMapEntry" + genreIndex).Value;
          if (!string.IsNullOrEmpty(genreMapEntry))
            _tvGenres.TryAdd(epgGenre.Value, genreMapEntry.Split(new char[] { '{' }, StringSplitOptions.RemoveEmptyEntries));
        }
        genreIndex++;
      }
    }

    public override async Task<bool> StopTimeshiftAsync(string userName, int slotIndex)
    {
      await _initComplete.Task;

      IUser user;
      user = GetUserByUserName(GetUserName(userName, slotIndex));
      if (user == null)
        return false;
      return _tvControl.StopTimeShifting(ref user);
    }

    public override async Task<MediaItem> CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      // Channel is usually only passed as placeholder with ID only, so query the details here
      TvDatabase.Channel fullChannel = TvDatabase.Channel.Retrieve(channel.ChannelId);
      bool isTv = fullChannel.IsTv;
      return await CreateMediaItem(slotIndex, streamUrl, channel, isTv, fullChannel.ToChannel());
    }

    public override Task<AsyncResult<IProgram[]>> GetNowNextProgramAsync(IChannel channel)
    {
      var tvChannel = TvDatabase.Channel.Retrieve(channel.ChannelId);
      var programNow = GetProgram(tvChannel.CurrentProgram);
      var programNext = GetProgram(tvChannel.NextProgram);
      var success = programNow != null || programNext != null;
      return Task.FromResult(new AsyncResult<IProgram[]>(success, new[] { programNow, programNext }));
    }

    //public override async Task<AsyncResult<IDictionary<int, IProgram[]>>> GetNowAndNextForChannelGroupAsync(IChannelGroup channelGroup)
    //{
    //  var nowNextPrograms = new Dictionary<int, IProgram[]>();
    //  IList<IChannel> channels;
    //  if (!GetChannels(channelGroup, out channels))
    //    return false;

    //  foreach (IChannel channel in channels)
    //  {
    //    IProgram programNow;
    //    IProgram programNext;
    //    if (GetNowNextProgram(channel, out programNow, out programNext))
    //      nowNextPrograms[channel.ChannelId] = new[] { programNow, programNext };
    //  }
    //  return true;
    //}

    public override async Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(IChannel channel, DateTime from, DateTime to)
    {
      await _initComplete.Task;

      var programs = _tvBusiness.GetPrograms(TvDatabase.Channel.Retrieve(channel.ChannelId), from, to)
        .Select(tvProgram => GetProgram(tvProgram, true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      var success = programs.Count > 0;
      return new AsyncResult<IList<IProgram>>(success, programs);
    }

    public override async Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(string title, DateTime from, DateTime to)
    {
      await _initComplete.Task;

      var programs = _tvBusiness.SearchPrograms(title).Where(p => p.StartTime >= from && p.StartTime <= to || p.EndTime >= from && p.EndTime <= to)
        .Select(tvProgram => GetProgram(tvProgram, true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      var success = programs.Count > 0;
      return new AsyncResult<IList<IProgram>>(success, programs);
    }

    public override async Task<AsyncResult<IList<IProgram>>> GetProgramsGroupAsync(IChannelGroup channelGroup, DateTime from, DateTime to)
    {
      await _initComplete.Task;

      var programs = new List<IProgram>();
      if (channelGroup.ChannelGroupId < 0)
      {
        foreach (var channel in _tvBusiness.GetRadioGuideChannelsForGroup(-channelGroup.ChannelGroupId))
          CollectionUtils.AddAll(programs, _tvBusiness.GetPrograms(TvDatabase.Channel.Retrieve(channel.IdChannel), from, to).Select(p => GetProgram(p)));
      }
      else
      {
        foreach (var channel in _tvBusiness.GetTVGuideChannelsForGroup(channelGroup.ChannelGroupId))
          CollectionUtils.AddAll(programs, _tvBusiness.GetPrograms(TvDatabase.Channel.Retrieve(channel.IdChannel), from, to).Select(p => GetProgram(p)));
      }
      var success = programs.Count > 0;
      return new AsyncResult<IList<IProgram>>(success, programs);
    }

    public async override Task<AsyncResult<IList<IProgram>>> GetProgramsForScheduleAsync(ISchedule schedule)
    {
      await _initComplete.Task;

      var programs = new List<IProgram>();
      var tvSchedule = TvDatabase.Schedule.Retrieve(schedule.ScheduleId);
      if (tvSchedule == null)
        return new AsyncResult<IList<IProgram>>(false, null);

      programs = TvDatabase.Schedule.GetProgramsForSchedule(tvSchedule).Select(p => GetProgram(p)).ToList();
      var success = programs.Count > 0;
      return new AsyncResult<IList<IProgram>>(success, programs);
    }

    public override async Task<AsyncResult<IChannel>> GetChannelAsync(IProgram program)
    {
      await _initComplete.Task;

      var channel = TvDatabase.Channel.Retrieve(program.ChannelId).ToChannel();
      return new AsyncResult<IChannel>(true, channel);
    }

    public override bool GetProgram(int programId, out IProgram program)
    {
      program = GetProgram(TvDatabase.Program.Retrieve(programId));
      return program != null;
    }

    public override async Task<AsyncResult<IList<IChannelGroup>>> GetChannelGroupsAsync()
    {
      await _initComplete.Task;

      var groups = TvDatabase.ChannelGroup.ListAll()
        .OrderBy(tvGroup => tvGroup.SortOrder)
        .Select(tvGroup => tvGroup.ToChannelGroup())
        .Union(
          RadioChannelGroup.ListAll()
          .OrderBy(radioGroup => radioGroup.SortOrder)
          .Select(radioGroup => radioGroup.ToChannelGroup())
        )
        .ToList();
      return new AsyncResult<IList<IChannelGroup>>(true, groups);
    }

    public override async Task<AsyncResult<IChannel>> GetChannelAsync(int channelId)
    {
      await _initComplete.Task;

      var channel = TvDatabase.Channel.Retrieve(channelId).ToChannel();
      var success = channel != null;
      return new AsyncResult<IChannel>(success, channel);
    }

    public override async Task<AsyncResult<IList<IChannel>>> GetChannelsAsync(IChannelGroup group)
    {
      await _initComplete.Task;

      List<IChannel> channels;
      if (group.ChannelGroupId < 0)
      {
        var radioGroup = RadioChannelGroup.Retrieve(-group.ChannelGroupId);
        var radioChannels = radioGroup.ReferringRadioGroupMap().OrderBy(rgm => rgm.SortOrder).Select(rgm => rgm.ReferencedChannel());
        channels = radioChannels
          .Where(c => c.VisibleInGuide)
          .Select(c => c.ToChannel())
          .Where(c => c != null)
          .ToList();
      }
      else
      {
        channels = _tvBusiness.GetChannelsInGroup(TvDatabase.ChannelGroup.Retrieve(group.ChannelGroupId))
          .Where(c => c != null)
          // Bug? SortOrder contains logical channel number, not the group sort order?
          // .OrderBy(c => c.SortOrder)
          .Where(c => c.VisibleInGuide)
          .Select(c => c.ToChannel())
          .ToList();
      }
      return new AsyncResult<IList<IChannel>>(true, channels);
    }

    public override async Task<AsyncResult<IList<ISchedule>>> GetSchedulesAsync()
    {
      await _initComplete.Task;

      var schedules = TvDatabase.Schedule.ListAll().Select(s => s.ToSchedule()).ToList();
      return new AsyncResult<IList<ISchedule>>(true, schedules);
    }

    public override async Task<AsyncResult<ISchedule>> CreateScheduleAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      await _initComplete.Task;

      var tvProgram = TvDatabase.Program.Retrieve(program.ProgramId);
      ISchedule schedule;
      if (tvProgram == null)
      {
        return new AsyncResult<ISchedule>(false, null);
      }
      if (CreateProgram(tvProgram, (int)recordingType, out schedule))
      {
        _tvControl.OnNewSchedule();
      }
      var success = schedule != null;
      return new AsyncResult<ISchedule>(success, schedule);
    }

    public static bool CreateProgram(TvDatabase.Program program, int scheduleType, out ISchedule currentSchedule)
    {
      ServiceRegistration.Get<ILogger>().Debug("SlimTvService3.CreateProgram: program = {0}", program.ToString());
      TvDatabase.Schedule schedule;
      TvDatabase.Schedule saveSchedule = null;
      TvBusinessLayer layer = new TvBusinessLayer();
      if (IsRecordingProgram(program, out schedule, false)) // check if schedule is already existing
      {
        ServiceRegistration.Get<ILogger>().Debug("SlimTvService3.CreateProgram - series schedule found ID={0}, Type={1}", schedule.IdSchedule, schedule.ScheduleType);
        ServiceRegistration.Get<ILogger>().Debug("                            - schedule= {0}", schedule.ToString());
        if (schedule.IsSerieIsCanceled(schedule.GetSchedStartTimeForProg(program), program.IdChannel))
        {
          // Delete the cancelled schedule.
          saveSchedule = schedule;
          schedule = new TvDatabase.Schedule(program.IdChannel, program.Title, program.StartTime, program.EndTime)
          {
            PreRecordInterval = saveSchedule.PreRecordInterval,
            PostRecordInterval = saveSchedule.PostRecordInterval,
            ScheduleType = (int)ScheduleRecordingType.Once
          };
        }
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Debug("SlimTvService3.CreateProgram - no series schedule");
        // No series schedule => create it
        schedule = new TvDatabase.Schedule(program.IdChannel, program.Title, program.StartTime, program.EndTime)
        {
          PreRecordInterval = Int32.Parse(layer.GetSetting("preRecordInterval", "5").Value),
          PostRecordInterval = Int32.Parse(layer.GetSetting("postRecordInterval", "5").Value),
          ScheduleType = scheduleType
        };
      }

      if (saveSchedule != null)
      {
        ServiceRegistration.Get<ILogger>().Debug("SlimTvService3.CreateProgram - UnCancelSerie at {0}", program.StartTime);
        saveSchedule.UnCancelSerie(program.StartTime, program.IdChannel);
        saveSchedule.Persist();
        currentSchedule = saveSchedule.ToSchedule();
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Debug("SlimTvService3.CreateProgram - create schedule = {0}", schedule.ToString());
        schedule.Persist();
        currentSchedule = schedule.ToSchedule();
      }
      return currentSchedule != null;
    }

    public static bool IsRecordingProgram(TvDatabase.Program program, out TvDatabase.Schedule recordingSchedule, bool filterCanceledRecordings)
    {
      recordingSchedule = null;

      IList<TvDatabase.Schedule> schedules = TvDatabase.Schedule.ListAll();
      foreach (TvDatabase.Schedule schedule in schedules)
      {
        if (schedule.Canceled != TvDatabase.Schedule.MinSchedule || (filterCanceledRecordings && schedule.IsSerieIsCanceled(schedule.GetSchedStartTimeForProg(program), program.IdChannel)))
        {
          continue;
        }
        if (schedule.IsManual && schedule.IdChannel == program.IdChannel && schedule.EndTime >= program.EndTime)
        {
          TvDatabase.Schedule manual = schedule.Clone();
          manual.ProgramName = program.Title;
          manual.EndTime = program.EndTime;
          manual.StartTime = program.StartTime;
          if (manual.IsRecordingProgram(program, filterCanceledRecordings))
          {
            recordingSchedule = schedule;
            return true;
          }
        }
        else if (schedule.IsRecordingProgram(program, filterCanceledRecordings))
        {
          recordingSchedule = schedule;
          return true;
        }
      }
      return false;
    }

    public override async Task<AsyncResult<ISchedule>> CreateScheduleByTimeAsync(IChannel channel, DateTime from, DateTime to, ScheduleRecordingType recordingType)
    {
      await _initComplete.Task;

      TvDatabase.Schedule tvSchedule = new TvDatabase.Schedule(channel.ChannelId, "Manual", from, to);
      tvSchedule.ScheduleType = (int)recordingType;
      tvSchedule.PreRecordInterval = Int32.Parse(_tvBusiness.GetSetting("preRecordInterval", "5").Value);
      tvSchedule.PostRecordInterval = Int32.Parse(_tvBusiness.GetSetting("postRecordInterval", "5").Value);
      tvSchedule.Persist();
      _tvControl.OnNewSchedule();
      var schedule = tvSchedule.ToSchedule();
      return new AsyncResult<ISchedule>(true, schedule);
    }

    public override async Task<bool> RemoveScheduleForProgramAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      await _initComplete.Task;

      var canceledProgram = TvDatabase.Program.Retrieve(program.ProgramId);
      if (canceledProgram == null)
        return false;
      var allSchedules = TvDatabase.Schedule.ListAll();
      var matchingSchedules = allSchedules.Where(schedule => schedule.IsRecordingProgram(canceledProgram, true));
      if (matchingSchedules.Count() == 0)
      {
        List<TvDatabase.Schedule> manualSchedules = new List<TvDatabase.Schedule>();
        //Check for matching manual recordings because they will not match any programs start and/or end times
        foreach (TvDatabase.Schedule schedule in allSchedules.Where(schedule => schedule.IsManual || schedule.ProgramName == "Manual"))
        {
          if ((canceledProgram.StartTime <= schedule.StartTime && canceledProgram.EndTime >= schedule.StartTime) || //Recording was started during this program
            (canceledProgram.StartTime <= schedule.EndTime && canceledProgram.EndTime >= schedule.EndTime) || //Recording is ending during this program
            (canceledProgram.StartTime >= schedule.StartTime && canceledProgram.EndTime <= schedule.StartTime)) //The program is "inside" the recording
            manualSchedules.Add(schedule);
        }
        matchingSchedules = manualSchedules;
      }
      //Delete matching schedules
      foreach (TvDatabase.Schedule schedule in matchingSchedules)
      {
        switch (schedule.ScheduleType)
        {
          case (int)ScheduleRecordingType.Once:
            schedule.Delete();
            _tvControl.OnNewSchedule();
            break;
          default:
            CanceledSchedule canceledSchedule = new CanceledSchedule(schedule.IdSchedule, schedule.IdChannel, program.StartTime);
            canceledSchedule.Persist();
            _tvControl.OnNewSchedule();
            break;
        }
      }
      return true;
    }

    public override async Task<bool> RemoveScheduleAsync(ISchedule schedule)
    {
      await _initComplete.Task;

      TvDatabase.Schedule tvSchedule = TvDatabase.Schedule.Retrieve(schedule.ScheduleId);
      // Already deleted somewhere else?
      if (tvSchedule == null)
        return true;
      _tvControl.StopRecordingSchedule(tvSchedule.IdSchedule);
      // delete canceled schedules first
      foreach (var cs in CanceledSchedule.ListAll().Where(x => x.IdSchedule == tvSchedule.IdSchedule))
        cs.Remove();
      try
      {
        // can fail if "StopRecordingSchedule" already deleted the entry
        TvDatabase.Schedule.ResetProgramStates(tvSchedule.IdSchedule);
        tvSchedule.Remove();
      }
      catch { }
      _tvControl.OnNewSchedule(); // I don't think this is needed, but doesn't hurt either
      return true;
    }

    public override async Task<AsyncResult<RecordingStatus>> GetRecordingStatusAsync(IProgram program)
    {
      await _initComplete.Task;

      var tvProgram = (IProgramRecordingStatus)GetProgram(TvDatabase.Program.Retrieve(program.ProgramId), true);
      var recordingStatus = tvProgram.RecordingStatus;
      return new AsyncResult<RecordingStatus>(true, recordingStatus);
    }

    public override async Task<AsyncResult<string>> GetRecordingFileOrStreamAsync(IProgram program)
    {
      await _initComplete.Task;

      Recording recording;
      if (!GetRecording(program, out recording))
        return new AsyncResult<string>(false, null);

      return new AsyncResult<string>(true, recording.FileName);
    }

    public override async Task<AsyncResult<ISchedule>> IsCurrentlyRecordingAsync(string fileName)
    {
      await _initComplete.Task;

      Recording recording;
      if (!GetRecording(fileName, out recording) || recording.Idschedule <= 0)
        return new AsyncResult<ISchedule>(false, null);

      var schedule = TvDatabase.Schedule.ListAll().FirstOrDefault(s => s.IdSchedule == recording.Idschedule).ToSchedule();
      return new AsyncResult<ISchedule>(schedule != null, schedule);
    }

    private static bool GetRecording(IProgram program, out Recording recording)
    {
      recording = Recording.ListAllActive().FirstOrDefault(r => r.IsRecording && r.IdChannel == program.ChannelId && r.Title == program.Title);
      return recording != null;
    }

    private static bool GetRecording(string filename, out Recording recording)
    {
      recording = Recording.ListAllActive().FirstOrDefault(r => r.IsRecording && string.Equals(r.FileName, filename, StringComparison.OrdinalIgnoreCase));
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

    #endregion
  }
}
