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
using MediaPortal.Utilities.FileSystem;
using TvLibrary.Interfaces.Integration;
using IChannel = MediaPortal.Plugins.SlimTv.Interfaces.Items.IChannel;
using IPathManager = MediaPortal.Common.PathManager.IPathManager;
using ScheduleRecordingType = MediaPortal.Plugins.SlimTv.Interfaces.ScheduleRecordingType;
using MediaPortal.Plugins.SlimTv.Service3;
using MediaPortal.Utilities;
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
using TvService;
using Card = TvDatabase.Card;
using SlimTvCard = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Card;
using SlimTvVirtualCard = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.VirtualCard;
using SlimTvUser = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.User;
using IUser = TvControl.IUser;
using User = TvControl.User;
using VirtualCard = TvControl.VirtualCard;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.Plugins.SlimTv.UPnP;
using System.Text.RegularExpressions;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public class SlimTvService : 
    AbstractSlimTvService<TvDatabase.ChannelGroup, TvDatabase.Channel, TvDatabase.Program, TvDatabase.Schedule, ScheduleRule, TvDatabase.Recording, TvDatabase.TuningDetail, TvDatabase.Conflict>
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
          Logger.Error("SlimTvService: Failed to register filter {0}", ex, filter.Value);
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
        Logger.Error("SlimTvService: Error creating TVE3 folders", ex);
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
        Logger.Error("SlimTvService: Failed to start TV service thread within {0} seconds.", MAX_WAIT_MS / 1000);
      }

      InitializeTVE();
      // Handle events from TvEngine
      if (!RegisterEvents())
      {
        Logger.Error("SlimTvService: Failed to register events. This happens only if startup failed. Stopping plugin now.");
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
          Logger.Info("SlimTvService: Cannot find Gentle.config file, assuming TVEngine isn't installed...");
          return false;
        }
        Gentle.Common.Configurator.AddFileHandler(gentleConfigFile);
      }
      catch (Exception ex)
      {
        Logger.Error("SlimTvService: Failed to connect to TVEngine", ex);
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
        Logger.Error("SlimTvService: Failed to connect to TVEngine", ex);
      }
    }

    public override bool DeInit()
    {
      base.DeInit();
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
          Logger.Error("SlimTvService: Failed to deinit TVEngine", ex);
        }
      }
      return true;
    }

    #endregion

    #region Server state

    protected void UpdateServerState()
    {
      IList<ISchedule> currentlyRecordingSchedules = TvDatabase.Recording.ListAllActive().Where(r => r.IsRecording)
        .Select(r => (ISchedule)ConvertToSchedule(r.ReferencedSchedule())).ToList();

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
          var recording = TvDatabase.Recording.Retrieve(tvEvent.Recording.IdRecording);
          if (recording != null)
          {
            Logger.Info("SlimTvService: {0}: {1}", tvEvent.EventType, recording.FileName);
            ImportRecording(recording.FileName);
          }
        }
        if (tvEvent.EventType == TvServerEventType.RecordingEnded)
        {
          _ = CleanUpRecordingsAsync();
        }
        if (tvEvent.EventType == TvServerEventType.ImportEpgPrograms || tvEvent.EventType == TvServerEventType.ProgramUpdated)
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
        Logger.Error("SlimTvService: Exception while getting recording folders", ex);
      }
      recordingFolders = null;
      singlePattern = null;
      seriesPattern = null;
      return false;
    }

    protected override string GetRecordingFolderForProgram(int cardId, int programId, bool isSeries)
    {
      TvBusinessLayer layer = new TvBusinessLayer();
      IList<Card> allCards = Card.ListAll();

      string recordingPath = allCards.FirstOrDefault(c => c.IdCard == cardId)?.RecordingFolder;
      if (string.IsNullOrWhiteSpace(recordingPath))
        return null;

      var program = TvDatabase.Program.Retrieve(programId);
      if (program == null)
        return null;

      Setting setting;
      if (!isSeries)
        setting = layer.GetSetting("moviesformat", "%title%");
      else
        setting = layer.GetSetting("seriesformat", "%title%");

      // Get the absolute path by applying all tags 
      string strInput = "title%";
      if (setting?.Value != null)
        strInput = setting.Value;
      
      Dictionary<string, string> tags = new Dictionary<string, string>()
      {
        { "%channel%", program.ReferencedChannel().DisplayName.Trim() },
        { "%title%", program.Title.Trim() },
        { "%name%", program.EpisodeName.Trim() },
        { "%series%", program.SeriesNum.Trim() },
        { "%episode%", program.EpisodeNum.Trim() },
        { "%part%", program.EpisodePart.Trim() },
        { "%date%", program.StartTime.ToString("yyyy-MM-dd") },
        { "%start%", program.StartTime.ToShortTimeString() },
        { "%end%", program.EndTime.ToShortTimeString() },
        { "%genre%", program.Genre.Trim() },
        { "%startday%", program.StartTime.ToString("dd") },
        { "%startmonth%", program.StartTime.ToString("MM") },
        { "%startyear%", program.StartTime.ToString("yyyy") },
        { "%starthh%", program.StartTime.ToString("HH") },
        { "%startmm%", program.StartTime.ToString("mm") },
        { "%endday%", program.EndTime.ToString("dd") },
        { "%endmonth%", program.EndTime.ToString("MM") },
        { "%endyear%", program.EndTime.ToString("yyyy") },
        { "%endhh%", program.EndTime.ToString("HH") },
        { "%endmm%", program.EndTime.ToString("mm") },
      };

      return GetRecordingFolderFromTags(recordingPath, strInput, tags);
    }

    protected async Task CleanUpRecordingsAsync()
    {
      await Task.Run(() =>
      {
        Logger.Info("SlimTvService: Begin recordings auto-cleanup");
        int countDeleted = 0;
        var allRecordings = TvDatabase.Recording.ListAll();
        ICollection<string> nonExistingRootPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (TvDatabase.Recording recording in allRecordings.Where(r => !r.IsRecording))
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
            Logger.Debug("SlimTvService: Remove '{0}'", recording.FileName);
            countDeleted++;
            recording.Delete();
          }
        }
        Logger.Info("SlimTvService: Removed {0} no longer existing recordings.", countDeleted);
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

    protected override Task InitGenreMapAsync()
    {
      if (_tvGenresInited)
        return Task.CompletedTask;

      _tvGenresInited = true;

      string genre;
      bool enabled;
      IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
      if (converter == null)
        return Task.CompletedTask;

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
            _tvGenres.TryAdd(epgGenre.Value, new HashSet<string>(genreMapEntry.Split(new char[] { '{' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.InvariantCultureIgnoreCase));
        }
        genreIndex++;
      }

      return Task.CompletedTask;
    }

    protected override Task<bool> StopProviderTimeshiftAsync(string userName, int slotIndex)
    {
      IUser user;
      user = GetUserByUserName(GetUserName(userName, slotIndex));
      if (user == null)
        return Task.FromResult(false);
      return Task.FromResult(_tvControl.StopTimeShifting(ref user));
    }

    protected override async Task<MediaItem> CreateMediaItemAsync(int slotIndex, string streamUrl, IChannel channel)
    {
      // Channel is usually only passed as placeholder with ID only, so query the details here
      TvDatabase.Channel fullChannel = TvDatabase.Channel.Retrieve(channel.ChannelId);
      bool isTv = fullChannel.IsTv;
      return await CreateMediaItemAsync(slotIndex, streamUrl, channel, isTv, ConvertToChannel(fullChannel));
    }

    protected override Task<AsyncResult<IProgram[]>> GetProviderNowNextProgramAsync(IChannel channel)
    {
      var tvChannel = TvDatabase.Channel.Retrieve(channel.ChannelId);
      var programNow = GetProgram(tvChannel.CurrentProgram);
      var programNext = GetProgram(tvChannel.NextProgram);
      var success = programNow != null || programNext != null;
      return Task.FromResult(new AsyncResult<IProgram[]>(success, new[] { programNow, programNext }));
    }

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsAsync(IChannel channel, DateTime from, DateTime to)
    {
      var programs = _tvBusiness.GetPrograms(TvDatabase.Channel.Retrieve(channel.ChannelId), from, to)
        .Select(tvProgram => GetProgram(tvProgram, true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      var success = programs.Count > 0;
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsAsync(string title, DateTime from, DateTime to)
    {
      var programs = _tvBusiness.SearchPrograms(title).Where(p => p.StartTime >= from && p.StartTime <= to || p.EndTime >= from && p.EndTime <= to)
        .Select(tvProgram => GetProgram(tvProgram, true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      var success = programs.Count > 0;
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsGroupAsync(IChannelGroup channelGroup, DateTime from, DateTime to)
    {
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
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsForScheduleAsync(ISchedule schedule)
    {
      var tvSchedule = TvDatabase.Schedule.Retrieve(schedule.ScheduleId);
      if (tvSchedule == null)
        return Task.FromResult(new AsyncResult<IList<IProgram>>(false, null));

      var programs = TvDatabase.Schedule.GetProgramsForSchedule(tvSchedule).Select(p => GetProgram(p)).ToList();
      var success = programs.Count > 0;
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    protected override Task<AsyncResult<IChannel>> GetProviderChannelAsync(IProgram program)
    {
      var channel = ConvertToChannel(TvDatabase.Channel.Retrieve(program.ChannelId));
      return Task.FromResult(new AsyncResult<IChannel>(channel != null, channel));
    }

    protected override Task<AsyncResult<IProgram>> GetProviderProgramAsync(int programId)
    {
      var program = GetProgram(TvDatabase.Program.Retrieve(programId));
      return Task.FromResult(new AsyncResult<IProgram>(program != null, program));
    }

    protected override Task<AsyncResult<IList<IChannelGroup>>> GetProviderChannelGroupsAsync()
    {
      var groups = TvDatabase.ChannelGroup.ListAll()
        .OrderBy(tvGroup => tvGroup.SortOrder)
        .Select(tvGroup => (IChannelGroup)ConvertToChannelGroup(tvGroup))
        .Union(
          RadioChannelGroup.ListAll()
          .OrderBy(radioGroup => radioGroup.SortOrder)
          .Select(radioGroup => (IChannelGroup)ConvertToChannelGroup(radioGroup))
        )
        .ToList();
      return Task.FromResult(new AsyncResult<IList<IChannelGroup>>(true, groups));
    }

    protected override Task<AsyncResult<IChannel>> GetProviderChannelAsync(int channelId)
    {
      var channel = ConvertToChannel(TvDatabase.Channel.Retrieve(channelId));
      var success = channel != null;
      return Task.FromResult(new AsyncResult<IChannel>(success, channel));
    }

    protected override Task<AsyncResult<ITuningDetail>> GetProviderTuningDetailsAsync(ICard card, IChannel channel)
    {
      if (!card.Enabled)
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      var cl = TvDatabase.Channel.Retrieve(channel.ChannelNumber);
      var tuningDetails = cl?.ReferringTuningDetail();
      if (!(tuningDetails?.Count > 0))
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      var cdType = _tvControl.Type(card.CardId);
      if (cdType == CardType.Unknown)
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      var cd = TvDatabase.Card.Retrieve(card.CardId);
      if (!cd.canViewTvChannel(channel.ChannelId))
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      var tuningDetail = tuningDetails.FirstOrDefault(d => (d.ChannelType == 0 && cdType == CardType.Analog) ||
                                                              (d.ChannelType == 1 && cdType == CardType.Atsc) ||
                                                              (d.ChannelType == 2 && cdType == CardType.DvbC) ||
                                                              (d.ChannelType == 3 && cdType == CardType.DvbS) ||
                                                              (d.ChannelType == 4 && cdType == CardType.DvbT) ||
                                                              (d.ChannelType == 7 && cdType == CardType.DvbIP));

      return Task.FromResult(new AsyncResult<ITuningDetail>(tuningDetail != null, ConvertToTuningDetail(tuningDetail)));
    }

    protected override Task<AsyncResult<IList<IChannel>>> GetProviderChannelsAsync(IChannelGroup group)
    {
      List<IChannel> channels;
      if (group.ChannelGroupId < 0)
      {
        var radioGroup = RadioChannelGroup.Retrieve(-group.ChannelGroupId);
        var radioChannels = radioGroup.ReferringRadioGroupMap().OrderBy(rgm => rgm.SortOrder).Select(rgm => rgm.ReferencedChannel());
        channels = radioChannels
          .Where(c => c.VisibleInGuide)
          .Select(c => (IChannel)ConvertToChannel(c))
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
          .Select(c => (IChannel)ConvertToChannel(c))
          .ToList();
      }
      return Task.FromResult(new AsyncResult<IList<IChannel>>(true, channels));
    }

    protected override Task<AsyncResult<IList<IRecording>>> GetProviderRecordingsAsync(string name)
    {
      var recordings = TvDatabase.Recording.ListAll().Select(s => (IRecording)ConvertToRecording(s)).ToList();
      return Task.FromResult(new AsyncResult<IList<IRecording>>(true, recordings));
    }

    protected override Task<AsyncResult<IList<ISchedule>>> GetProviderSchedulesAsync()
    {
      var schedules = TvDatabase.Schedule.ListAll().Select(s => (ISchedule)ConvertToSchedule(s)).ToList();
      return Task.FromResult(new AsyncResult<IList<ISchedule>>(true, schedules));
    }

    protected override Task<AsyncResult<IList<ISchedule>>> GetProviderCanceledSchedulesAsync()
    {
      IList<ISchedule> canceledSchedules = new List<ISchedule>();
      var canceled = CanceledSchedule.ListAll();
      foreach (var canceledSchedule in canceled)
      {
        var s = TvDatabase.Schedule.Retrieve(canceledSchedule.IdSchedule);
        var schedule = ConvertToSchedule(s);
        var duration = s.EndTime - s.StartTime;
        schedule.StartTime = canceledSchedule.CancelDateTime;
        schedule.EndTime = schedule.StartTime.Add(duration);
        canceledSchedules.Add(schedule);
      }
      
      return Task.FromResult(new AsyncResult<IList<ISchedule>>(canceledSchedules.Count > 0, canceledSchedules));
    }


    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      var tvProgram = TvDatabase.Program.Retrieve(program.ProgramId);
      ISchedule schedule;
      if (tvProgram == null)
      {
        return Task.FromResult(new AsyncResult<ISchedule>(false, null));
      }
      if (CreateProgram(tvProgram, (int)recordingType, out schedule))
      {
        _tvControl.OnNewSchedule();
      }
      var success = schedule != null;
      return Task.FromResult(new AsyncResult<ISchedule>(success, schedule));
    }

    private bool CreateProgram(TvDatabase.Program program, int scheduleType, out ISchedule currentSchedule)
    {
      Logger.Debug("SlimTvService: CreateProgram - program = {0}", program.ToString());
      TvDatabase.Schedule schedule;
      TvDatabase.Schedule saveSchedule = null;
      TvBusinessLayer layer = new TvBusinessLayer();
      if (IsRecordingProgram(program, out schedule, false)) // check if schedule is already existing
      {
        Logger.Debug("SlimTvService: CreateProgram - series schedule found ID={0}, Type={1}", schedule.IdSchedule, schedule.ScheduleType);
        Logger.Debug("                            - schedule= {0}", schedule.ToString());
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
        Logger.Debug("SlimTvService: CreateProgram - no series schedule");
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
        Logger.Debug("SlimTvService: CreateProgram - UnCancelSerie at {0}", program.StartTime);
        saveSchedule.UnCancelSerie(program.StartTime, program.IdChannel);
        saveSchedule.Persist();
        currentSchedule = ConvertToSchedule(saveSchedule);
      }
      else
      {
        Logger.Debug("SlimTvService: CreateProgram - create schedule = {0}", schedule.ToString());
        schedule.Persist();
        currentSchedule = ConvertToSchedule(schedule);
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

    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleByTimeAsync(IChannel channel, DateTime from, DateTime to, ScheduleRecordingType recordingType)
    {
      return CreateScheduleByTimeAsync(channel, $"{Consts.MANUAL_RECORDING_TITLE_PREFIX}{Consts.MANUAL_RECORDING_TITLE}", from, to, recordingType);
    }

    protected override async Task<AsyncResult<ISchedule>> CreateProviderScheduleByTimeAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType)
    {
      TvDatabase.Schedule tvSchedule = new TvDatabase.Schedule(channel.ChannelId, title, from, to);
      tvSchedule.ScheduleType = (int)recordingType;
      tvSchedule.PreRecordInterval = Int32.Parse(_tvBusiness.GetSetting("preRecordInterval", "5").Value);
      tvSchedule.PostRecordInterval = Int32.Parse(_tvBusiness.GetSetting("postRecordInterval", "5").Value);
      tvSchedule.Persist();
      _tvControl.OnNewSchedule();
      var schedule = ConvertToSchedule(tvSchedule);
      return new AsyncResult<ISchedule>(true, schedule);
    }

    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleDetailedAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType, int preRecordInterval, int postRecordInterval, string directory, int priority)
    {
      TvDatabase.Schedule tvSchedule = _tvBusiness.AddSchedule(channel.ChannelId, title, from, to, (int)recordingType);
      tvSchedule.PreRecordInterval = preRecordInterval >= 0 ? preRecordInterval : Int32.Parse(_tvBusiness.GetSetting("preRecordInterval", "5").Value);
      tvSchedule.PostRecordInterval = postRecordInterval >= 0 ? postRecordInterval : Int32.Parse(_tvBusiness.GetSetting("postRecordInterval", "5").Value);
      if (!String.IsNullOrEmpty(directory))
      {
        tvSchedule.Directory = directory;
      }
      if (priority >= 0)
      {
        tvSchedule.Priority = priority;
      }
      tvSchedule.Persist();
      _tvControl.OnNewSchedule();
      var schedule = ConvertToSchedule(tvSchedule);
      return Task.FromResult(new AsyncResult<ISchedule>(true, schedule));
    }

    protected override Task<bool> EditProviderScheduleAsync(ISchedule schedule, IChannel channel = null, string title = null, DateTime? from = null, DateTime? to = null, ScheduleRecordingType? recordingType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      try
      {
        Logger.Debug("SlimTvService: Editing schedule {0} on channel {1} for {2}, {3} till {4}, type {5}", schedule.ScheduleId, channel.ChannelId, title, from, to, recordingType);
        TvDatabase.Schedule tvSchedule = TvDatabase.Schedule.Retrieve(schedule.ScheduleId);

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

        tvSchedule.Persist();

        _tvControl.OnNewSchedule(); // I don't think this is needed, but doesn't hurt either
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
      var canceledProgram = TvDatabase.Program.Retrieve(program.ProgramId);
      if (canceledProgram == null)
        return Task.FromResult(false);
      var allSchedules = TvDatabase.Schedule.ListAll();
      var matchingSchedules = allSchedules.Where(schedule => schedule.IsRecordingProgram(canceledProgram, true)).ToList();
      if (!matchingSchedules.Any())
      {
        List<TvDatabase.Schedule> manualSchedules = new List<TvDatabase.Schedule>();
        //Check for matching manual recordings because they will not match any programs start and/or end times
        foreach (TvDatabase.Schedule schedule in allSchedules.Where(schedule => schedule.IsManual || IsManualTitle(schedule.ProgramName)))
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
        _tvControl.StopRecordingSchedule(schedule.IdSchedule);
        if (schedule.ScheduleType == (int)ScheduleRecordingType.Once || recordingType != ScheduleRecordingType.Once)
        {
          // Delete single schedule, or whole series
          schedule.Delete();
        }
        else
        {
          // Delete this program only
          CanceledSchedule canceledSchedule = new CanceledSchedule(schedule.IdSchedule, schedule.IdChannel, program.StartTime);
          canceledSchedule.Persist();
        }
        _tvControl.OnNewSchedule();
      }
      return Task.FromResult(true);
    }

    protected override Task<bool> RemoveProviderScheduleAsync(ISchedule schedule)
    {
      TvDatabase.Schedule tvSchedule = TvDatabase.Schedule.Retrieve(schedule.ScheduleId);
      // Already deleted somewhere else?
      if (tvSchedule == null)
        return Task.FromResult(true);
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
      return Task.FromResult(true);
    }

    protected override Task<bool> UnCancelProviderScheduleAsync(IProgram program)
    {
      var tvProgram = TvDatabase.Program.Retrieve(program.ProgramId);
      try
      {
        Logger.Debug("SlimTvService: Uncancelling schedule for programId {0}", tvProgram.IdProgram);
        foreach (TvDatabase.Schedule schedule in TvDatabase.Schedule.ListAll().Where(schedule => schedule.IsSerieIsCanceled(program.StartTime, tvProgram.IdChannel)))
        {
          schedule.UnCancelSerie(program.StartTime, tvProgram.IdChannel);
          schedule.Persist();
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
      var conflictList = TvDatabase.Conflict.ListAll();
      var conflicts = conflictList.Select(c => (IConflict)ConvertToConflict(c)).ToList();
      return Task.FromResult(new AsyncResult<IList<IConflict>>(conflicts.Count > 0, conflicts));
    }

    protected override Task<bool> RemoveAllProviderConflictsAsync()
    {
      //Clear all existing program conflicts
      var programList = TvDatabase.Program.ListAll();
      foreach (var program in programList.Where(p => p.HasConflict))
      {
        program.HasConflict = false;
        program.Persist();
      }

      // Clear all conflicts
      var conflictList = TvDatabase.Conflict.ListAll();
      foreach (var conflict in conflictList)
        conflict.Remove();

      return Task.FromResult(true);
    }

    protected override Task<bool> SaveProviderConflictsAsync(IList<IConflict> conflicts)
    {
      var programList = TvDatabase.Program.ListAll();

      //Add new program conflicts
      foreach (var conflict in conflicts)
      {
        var prg = programList.FirstOrDefault(p => p.IdChannel == conflict.ChannelId && p.StartTime == conflict.ProgramStartTime);
        if (prg == null)
          continue;

        prg.HasConflict = true;
        prg.Persist();
      }

      // Add new conflicts
      foreach (var conflict in conflicts.Where(c => c.ConflictingScheduleId > 0))
      {
        TvDatabase.Conflict newConflict = new TvDatabase.Conflict(conflict.ScheduleId, conflict.ConflictingScheduleId,
          conflict.ChannelId, conflict.ProgramStartTime, conflict.CardId);
        newConflict.Persist();
      }

      return Task.FromResult(true);
    }

    protected override Task<AsyncResult<RecordingStatus>> GetProviderRecordingStatusAsync(IProgram program)
    {
      var tvProgram = (IProgramRecordingStatus)GetProgram(TvDatabase.Program.Retrieve(program.ProgramId), true);
      var recordingStatus = tvProgram.RecordingStatus;
      return Task.FromResult(new AsyncResult<RecordingStatus>(true, recordingStatus));
    }

    protected override Task<AsyncResult<string>> GetProviderRecordingFileOrStreamAsync(IProgram program)
    {
      TvDatabase.Recording recording;
      if (!GetRecording(program, out recording))
        return Task.FromResult(new AsyncResult<string>(false, null));

      return Task.FromResult(new AsyncResult<string>(true, recording.FileName));
    }

    protected override Task<AsyncResult<ISchedule>> IsProviderCurrentlyRecordingAsync(string fileName)
    {
      TvDatabase.Recording recording;
      if (!GetRecording(fileName, out recording) || recording.Idschedule <= 0)
        return Task.FromResult(new AsyncResult<ISchedule>(false, null));

      var schedule = ConvertToSchedule(TvDatabase.Schedule.ListAll().FirstOrDefault(s => s.IdSchedule == recording.Idschedule));
      return Task.FromResult(new AsyncResult<ISchedule>(schedule != null, schedule));
    }

    private bool GetRecording(IProgram program, out TvDatabase.Recording recording)
    {
      recording = TvDatabase.Recording.ListAllActive().FirstOrDefault(r => r.IsRecording && r.IdChannel == program.ChannelId && r.Title == program.Title);
      return recording != null;
    }

    private bool GetRecording(string filename, out TvDatabase.Recording recording)
    {
      recording = TvDatabase.Recording.ListAllActive().FirstOrDefault(r => r.IsRecording && string.Equals(r.FileName, filename, StringComparison.OrdinalIgnoreCase));
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

      // actually start timeshifting
      VirtualCard card;
      TvResult result = _tvControl.StartTimeShifting(ref currentUser, channelId, out card);
      // make sure result is correct and return
      if (result != TvResult.Succeeded)
      {
        Logger.Error("SlimTvService: Starting timeshifting failed with result {0}", result);
        return Task.FromResult<string>(null);
      }
      if (card == null)
      {
        Logger.Error("SlimTvService: Couldn't get virtual card");
        return Task.FromResult<string>(null);
      }
      return Task.FromResult(userName.StartsWith(Consts.LOCAL_USERNAME + "-") ? card.TimeShiftFileName : card.RTSPUrl);
    }

    protected override Task<AsyncResult<IList<ICard>>> GetProviderCardsAsync()
    {
      List<ICard> cards = _tvBusiness.Cards.Select(card => new SlimTvCard()
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

      return Task.FromResult(new AsyncResult<IList<ICard>>(cards.Count > 0, cards));
    }

    protected override Task<AsyncResult<IList<IVirtualCard>>> GetProviderActiveVirtualCardsAsync()
    {
      IEnumerable<VirtualCard> virtualCards = Card.ListAll()
                .Where(card => RemoteControl.Instance.CardPresent(card.IdCard))
                .Select(card => RemoteControl.Instance.GetUsersForCard(card.IdCard))
                .Where(users => users != null)
                .SelectMany(user => user)
                .Select(user => new VirtualCard(user, RemoteControl.HostName))
                .Where(tvCard => tvCard.IsTimeShifting || tvCard.IsRecording);

      List<IVirtualCard> cards = new List<IVirtualCard>();
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

      return Task.FromResult(new AsyncResult<IList<IVirtualCard>>(cards.Count > 0, cards));
    }

    #endregion

    #region Conversion

    protected override Interfaces.UPnP.Items.Program ConvertToProgram(TvDatabase.Program tvProgram, bool includeRecordingStatus = false)
    {
      if (tvProgram == null)
        return null;
      Interfaces.UPnP.Items.Program program = new Interfaces.UPnP.Items.Program
      {
        ChannelId = tvProgram.IdChannel,
        ProgramId = tvProgram.IdProgram,
        Title = tvProgram.Title,
        Description = tvProgram.Description,
        Genre = tvProgram.Genre,
        StartTime = tvProgram.StartTime,
        EndTime = tvProgram.EndTime,
        OriginalAirDate = tvProgram.OriginalAirDate,
        Classification = tvProgram.Classification,
        ParentalRating = tvProgram.ParentalRating,
        StarRating = tvProgram.StarRating,
        SeasonNumber = tvProgram.SeriesNum,
        EpisodeNumber = tvProgram.EpisodeNum,
        EpisodeNumberDetailed = tvProgram.EpisodeNumber,
        EpisodePart = tvProgram.EpisodePart,
        EpisodeTitle = tvProgram.EpisodeName,
      };

      program.RecordingStatus = tvProgram.IsRecording ? RecordingStatus.Recording : RecordingStatus.None;
      if (tvProgram.IsRecordingOncePending || tvProgram.IsRecordingOnce)
        program.RecordingStatus |= RecordingStatus.Scheduled;
      if (tvProgram.IsRecordingSeriesPending || tvProgram.IsRecordingSeries)
        program.RecordingStatus |= RecordingStatus.SeriesScheduled;
      if (tvProgram.IsRecordingOnce)
        program.RecordingStatus |= RecordingStatus.RecordingOnce;
      if (tvProgram.IsRecordingSeries)
        program.RecordingStatus |= RecordingStatus.RecordingSeries;
      if (tvProgram.IsRecordingManual)
        program.RecordingStatus |= RecordingStatus.RecordingManual;
      program.HasConflict = tvProgram.HasConflict;

      return program;
    }

    protected override Interfaces.UPnP.Items.Channel ConvertToChannel(TvDatabase.Channel tvChannel)
    {
      if (tvChannel == null)
        return null;
      return new Interfaces.UPnP.Items.Channel
      {
        ChannelId = tvChannel.IdChannel,
        ChannelNumber = tvChannel.ChannelNumber,
        Name = tvChannel.DisplayName,
        MediaType = tvChannel.IsTv ? MediaType.TV : MediaType.Radio,
        EpgHasGaps = tvChannel.EpgHasGaps,
        ExternalId = tvChannel.ExternalId,
        GrapEpg = tvChannel.GrabEpg,
        LastGrabTime = tvChannel.LastGrabTime,
        TimesWatched = tvChannel.TimesWatched,
        TotalTimeWatched = tvChannel.TotalTimeWatched,
        VisibleInGuide = tvChannel.VisibleInGuide,
        GroupNames = tvChannel.GroupNames.ToList()
      };
    }

    protected override Interfaces.UPnP.Items.ChannelGroup ConvertToChannelGroup(TvDatabase.ChannelGroup tvGroup)
    {
      if (tvGroup == null)
        return null;
      return new Interfaces.UPnP.Items.ChannelGroup
      {
        ChannelGroupId = tvGroup.IdGroup,
        Name = tvGroup.GroupName,
        MediaType = MediaType.TV,
        SortOrder = tvGroup.SortOrder
      };
    }

    protected Interfaces.UPnP.Items.ChannelGroup ConvertToChannelGroup(TvDatabase.RadioChannelGroup radioGroup)
    {
      if (radioGroup == null)
        return null;
      // Note: this temporary workaround uses negative group ids to be able to separate them later. This can be removed once there is a 
      // dedicated radio group interface (if required).
      return new Interfaces.UPnP.Items.ChannelGroup
      {
        ChannelGroupId = -radioGroup.IdGroup,
        Name = radioGroup.GroupName,
        MediaType = MediaType.Radio,
        SortOrder = radioGroup.SortOrder
      };
    }

    protected override Interfaces.UPnP.Items.Schedule ConvertToSchedule(TvDatabase.Schedule schedule)
    {
      if (schedule == null)
        return null;
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

    protected override Interfaces.UPnP.Items.TuningDetail ConvertToTuningDetail(TvDatabase.TuningDetail tuningDetail)
    {
      return new Interfaces.UPnP.Items.TuningDetail
      {
        TuningDetailId = tuningDetail.IdTuning,
        InnerFecRate = tuningDetail.InnerFecRate,
        ChannelId = tuningDetail.IdChannel,
        Name = tuningDetail.Name,
        Provider = tuningDetail.Provider,
        ChannelType = tuningDetail.ChannelType == 0 ? Interfaces.Items.ChannelType.Analog :
          tuningDetail.ChannelType == 1 ? Interfaces.Items.ChannelType.Atsc :
          tuningDetail.ChannelType == 2 ? Interfaces.Items.ChannelType.DvbC :
          tuningDetail.ChannelType == 3 ? Interfaces.Items.ChannelType.DvbS :
          tuningDetail.ChannelType == 4 ? Interfaces.Items.ChannelType.DvbT :
          tuningDetail.ChannelType == 7 ? Interfaces.Items.ChannelType.DvbIP : Interfaces.Items.ChannelType.Unsupported,
        PhysicalChannelNumber = tuningDetail.ChannelNumber,
        Frequency = tuningDetail.Frequency,
        CountryId = tuningDetail.CountryId,
        MediaType = tuningDetail.IsRadio ? MediaType.Radio : MediaType.TV,
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

    protected override Interfaces.UPnP.Items.Recording ConvertToRecording(TvDatabase.Recording recording)
    {
      return new Interfaces.UPnP.Items.Recording
      {
        RecordingId = recording.IdRecording,
        ChannelId = recording.IdChannel,
        ScheduleId = recording.Idschedule,
        Title = recording.Title,
        Description = recording.Description,
        Genre = recording.Genre,
        StartTime = recording.StartTime,
        EndTime = recording.EndTime,
        IsManual = recording.IsManual || IsManualTitle(recording.Title),
        SeasonNumber = recording.SeriesNum,
        EpisodeNumber = recording.EpisodeNum,
        EpisodeNumberDetailed = recording.EpisodeNumber,
        EpisodePart = recording.EpisodePart,
        EpisodeTitle = recording.EpisodeName,
        KeepDate = recording.KeepUntilDate.Year > 1900 ? recording.KeepUntilDate : (DateTime?)null,
        KeepMethod = (Interfaces.Items.KeepMethodType)recording.KeepUntil,
      };
    }

    protected override Interfaces.UPnP.Items.ScheduleRule ConvertToScheduleRule(Interfaces.UPnP.Items.ScheduleRule scheduleRule)
    {
      return scheduleRule;
    }

    protected override Interfaces.UPnP.Items.Conflict ConvertToConflict(TvDatabase.Conflict conflict)
    {
      return new Interfaces.UPnP.Items.Conflict
      {
        ConflictId = conflict.IdConflict,
        CardId = conflict.IdCard,
        ChannelId = conflict.IdChannel,
        ScheduleId = conflict.IdSchedule,
        ProgramStartTime = conflict.ConflictDate,
        ConflictingScheduleId = conflict.IdConflictingSchedule,
      };
    }

    #endregion
  }
}
