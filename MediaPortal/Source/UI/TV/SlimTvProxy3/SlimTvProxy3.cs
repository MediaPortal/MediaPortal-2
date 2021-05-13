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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Backend.Database;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.Utilities.FileSystem;
using TvLibrary.Interfaces.Integration;
using IChannel = MediaPortal.Plugins.SlimTv.Interfaces.Items.IChannel;
using IPathManager = MediaPortal.Common.PathManager.IPathManager;
using ScheduleRecordingType = MediaPortal.Plugins.SlimTv.Interfaces.ScheduleRecordingType;
using MediaPortal.Utilities;
using TvLibrary.Implementations.DVB;
using TvControl;
using TvDatabase;
using TvEngine.Events;
using TvService;
using Card = TvDatabase.Card;
using IUser = TvControl.IUser;
using User = TvControl.User;
using VirtualCard = TvControl.VirtualCard;
using SlimTvCard = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Card;
using SlimTvVirtualCard = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.VirtualCard;
using SlimTvUser = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.User;
using System.Globalization;
using Gentle.Framework;
using System.Collections;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SlimTv.Proxy.Settings;
using TvLibrary.Interfaces;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Common.Async;
using MediaPortal.Plugins.SlimTv.UPnP;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public class SlimTvService : 
    AbstractSlimTvService<TvDatabase.ChannelGroup, TvDatabase.Channel, TvDatabase.Program, TvDatabase.Schedule, ScheduleRule, TvDatabase.Recording, TvDatabase.TuningDetail, TvDatabase.Conflict>
  {
    private Timer _checkForRecordingTimer = null;
    private int _startDelay = 10000;
    private int _checkInterval = 60000;
    private Dictionary<Card, string> _currentlyRecording = new Dictionary<TvDatabase.Card, string>();
    private List<Card> _allCards = null;
    private object _recordingSync = new object();

    protected readonly Dictionary<string, IUser> _tvUsers = new Dictionary<string, IUser>();

    public SlimTvService()
    {
      _serviceName = "SlimTv.Proxy3";
    }

    #region Database and program data initialization

    protected override void PrepareIntegrationProvider()
    {
      IntegrationProviderHelper.Register(@"Plugins\" + _serviceName, "Plugins\\" + _serviceName + "\\castle.config");
      var pathManager = ServiceRegistration.Get<IPathManager>();
      pathManager.SetPath("TVCORE", "<DATA>\\SlimTVCore\\v3.0");
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
          Logger.Error("SlimTvProxy: Failed to register filter {0}", ex, filter.Value);
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
        Logger.Error("SlimTvProxy: Error creating TVE3 folders", ex);
      }
    }

    protected override void InitTvCore()
    {
      if (!InitializeGentle())
      {
        DeInit();
        return;
      }

      // Handle events from TvEngine
      if (!RegisterEvents())
      {
        Logger.Error("SlimTvProxy: Failed to register events. This happens only if startup failed. Stopping plugin now.");
        DeInit();
      }
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
          Logger.Info("SlimTvProxy: Cannot find Gentle.config file, assuming TVEngine isn't installed...");
          return false;
        }
        Gentle.Common.Configurator.AddFileHandler(gentleConfigFile);

        //Load settings
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        SlimTvProxySettings settings = settingsManager.Load<SlimTvProxySettings>();
        settingsManager.Save(settings);
        RemoteControl.HostName = settings.HostName;
        ProviderFactory.SetDefaultProviderConnectionString(settings.DatabaseConnectionString);
        ProviderFactory.SetDefaultProvider(settings.DatabaseProvider);

        _allCards = new List<Card>(Card.ListAll());
      }
      catch (Exception ex)
      {
        Logger.Error("SlimTvProxy: Failed to connect to TVEngine", ex);
        return false;
      }
      return true;
    }

    public override bool DeInit()
    {
      base.DeInit();
      RemoteControl.Clear();
      lock (_recordingSync)
      {
        if (_checkForRecordingTimer != null) _checkForRecordingTimer.Dispose();
        _checkForRecordingTimer = null;
        if (_allCards != null) _allCards.Clear();
        _allCards = null;
        _currentlyRecording.Clear();
        _currentlyRecording = null;
      }
      return true;
    }

    #endregion

    #region Recordings / MediaLibrary synchronization

    private void CheckForRecordings(object oState)
    {
      lock (_recordingSync)
      {
        TvServerEventType eventType = TvServerEventType.RecordingStarted;
        if (_allCards != null)
        {
          foreach (Card card in _allCards)
          {
            if (card == null) continue;
            if (RemoteControl.Instance.CardPresent(card.IdCard) == false)
            {
              if (_currentlyRecording.ContainsKey(card))
                _currentlyRecording.Remove(card);
              continue;
            }

            bool cardRecording = false;
            var users = RemoteControl.Instance.GetUsersForCard(card.IdCard);
            if (users == null) continue;
            for (int i = 0; i < users.Count(); i++)
            {
              if (RemoteControl.Instance.IsRecording(ref users[i]) == true)
              {
                cardRecording = true;
                if (_currentlyRecording.ContainsKey(card) == false)
                {
                  _currentlyRecording.Add(card, RemoteControl.Instance.RecordingFileName(ref users[i]));
                  eventType = TvServerEventType.RecordingStarted;
                  if (File.Exists(_currentlyRecording[card]))
                  {
                    Logger.Info("SlimTvProxy: {0}: {1}", eventType, _currentlyRecording[card]);
                    ImportRecording(_currentlyRecording[card]);
                  }
                  break;
                }
              }
            }

            if (cardRecording == false)
            {
              if (_currentlyRecording.ContainsKey(card) == true)
              {
                eventType = TvServerEventType.RecordingEnded;
                Logger.Info("SlimTvProxy: {0}: {1}", eventType, _currentlyRecording[card]);
                ImportRecording(_currentlyRecording[card]);
                _currentlyRecording.Remove(card);
              }
            }

          }
        }
      }
    }

    protected override bool RegisterEvents()
    {
      _checkForRecordingTimer = new Timer(new TimerCallback(CheckForRecordings), null, _startDelay, _checkInterval);
      if (_checkForRecordingTimer == null)
        return false;

      return true;
    }

    protected override void OnTvServerEvent(object sender, EventArgs eventArgs)
    {
      try
      {
        TvServerEventArgs tvEvent = (TvServerEventArgs)eventArgs;

        if (tvEvent.EventType == TvServerEventType.RecordingStarted || tvEvent.EventType == TvServerEventType.RecordingEnded)
        {
          var recording = TvDatabase.Recording.Retrieve(tvEvent.Recording.IdRecording);
          if (recording != null)
          {
            Logger.Info("SlimTvProxy: {0}: {1}", tvEvent.EventType, recording.FileName);
            ImportRecording(recording.FileName);
          }
        }
        if (tvEvent.EventType == TvServerEventType.ImportEpgPrograms || tvEvent.EventType == TvServerEventType.ProgramUpdated)
        {
          _ = ProgramsChangedAsync();
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("SlimTvProxy: Exception while handling TvServerEvent", ex);
      }
    }

    protected override bool GetRecordingConfiguration(out List<string> recordingFolders, out string singlePattern, out string seriesPattern)
    {
      singlePattern = string.Empty;
      seriesPattern = string.Empty;
      recordingFolders = new List<string>();

      if (_allCards == null)
        return false;

      // Get all different recording folders
      recordingFolders = _allCards.Select(c => c.RecordingFolder).Where(f => !string.IsNullOrEmpty(f)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

      TvBusinessLayer layer = new TvBusinessLayer();
      singlePattern = layer.GetSetting("moviesformat", string.Empty).Value;
      seriesPattern = layer.GetSetting("seriesformat", string.Empty).Value;
      return recordingFolders.Count > 0;
    }

    #endregion

    #region ITvProvider implementation

    private IUser GetUserByUserName(string userName)
    {
      return _allCards
        .Where(c => c != null && c.Enabled)
        .SelectMany(c => { var users = RemoteControl.Instance.GetUsersForCard(c.IdCard); return users ?? new IUser[] { }; })
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
      if (!int.TryParse(TvDatabase.Setting.RetrieveByTag("genreMapMovieGenreId").Value, out genreMapMovieGenreId))
      {
        genreMapMovieGenreId = -1;
      }

      // Each genre map value is a '{' delimited list of "program" genre names (those that may be compared with the genre from the program listings).
      // It is an error if a single "program" genre is mapped to more than one guide genre; behavior is undefined for this condition.
      int genreIndex = 0;
      while (true)
      {
        // The genremap key is an integer value that is added to a base value in order to locate the correct localized genre name string.
        genre = TvDatabase.Setting.RetrieveByTag("genreMapName" + genreIndex).Value;
        if (string.IsNullOrEmpty(genre))
          break;

        // Get the status of the mp genre.
        if (!bool.TryParse(TvDatabase.Setting.RetrieveByTag("genreMapNameEnabled" + genreIndex).Value, out enabled))
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
          string genreMapEntry = TvDatabase.Setting.RetrieveByTag("genreMapEntry" + genreIndex).Value;
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
      return Task.FromResult(RemoteControl.Instance.StopTimeShifting(ref user));
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
      var programs = GetPrograms(TvDatabase.Channel.Retrieve(channel.ChannelId), from, to)
        .Select(tvProgram => GetProgram(tvProgram, true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      var success = programs.Count > 0;
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    private string GetDateTimeString()
    {
      string provider = Gentle.Framework.ProviderFactory.GetDefaultProvider().Name.ToLower();
      if (provider == "mysql") return "yyyy-MM-dd HH:mm:ss";
      return "yyyyMMdd HH:mm:ss";
    }

    private List<TvDatabase.Program> GetPrograms(TvDatabase.Channel channel, DateTime from, DateTime to)
    {
      IFormatProvider mmddFormat = new CultureInfo(String.Empty, false);
      SqlBuilder sb = new SqlBuilder(Gentle.Framework.StatementType.Select, typeof(TvDatabase.Program));
      sb.AddConstraint(Operator.Equals, "idChannel", channel.IdChannel);
      sb.AddConstraint(String.Format("startTime>='{0}'", from.ToString(GetDateTimeString(), mmddFormat)));
      sb.AddConstraint(String.Format("endTime<='{0}'", to.ToString(GetDateTimeString(), mmddFormat)));
      sb.AddOrderByField(true, "startTime");
      SqlStatement stmt = sb.GetStatement(true);
      IList programs = ObjectFactory.GetCollection(typeof(TvDatabase.Program), stmt.Execute());
      return (List<TvDatabase.Program>)programs;
    }

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsAsync(string title, DateTime from, DateTime to)
    {
      var programs = SearchPrograms(title).Where(p => p.StartTime >= from && p.StartTime <= to || p.EndTime >= from && p.EndTime <= to)
        .Select(tvProgram => GetProgram(tvProgram, true))
        .Distinct(ProgramComparer.Instance)
        .ToList();
      var success = programs.Count > 0;
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    private List<TvDatabase.Program> SearchPrograms(string title)
    {
      IFormatProvider mmddFormat = new CultureInfo(String.Empty, false);
      SqlBuilder sb = new SqlBuilder(Gentle.Framework.StatementType.Select, typeof(TvDatabase.Program));
      sb.AddConstraint(Operator.Like, "title", title);
      sb.AddOrderByField(true, "startTime");
      SqlStatement stmt = sb.GetStatement(true);
      IList programs = ObjectFactory.GetCollection(typeof(TvDatabase.Program), stmt.Execute());
      return (List<TvDatabase.Program>)programs;
    }

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsGroupAsync(IChannelGroup channelGroup, DateTime from, DateTime to)
    {
      var programs = new List<IProgram>();
      if (channelGroup.ChannelGroupId < 0)
      {
        foreach (var channel in GetRadioGuideChannelsForGroup(-channelGroup.ChannelGroupId))
          CollectionUtils.AddAll(programs, GetPrograms(TvDatabase.Channel.Retrieve(channel.IdChannel), from, to).Select(p => GetProgram(p, true)));
      }
      else
      {
        foreach (var channel in GetTVGuideChannelsForGroup(channelGroup.ChannelGroupId))
          CollectionUtils.AddAll(programs, GetPrograms(TvDatabase.Channel.Retrieve(channel.IdChannel), from, to).Select(p => GetProgram(p, true)));
      }
      var success = programs.Count > 0;
      return Task.FromResult(new AsyncResult<IList<IProgram>>(success, programs));
    }

    private List<TvDatabase.Channel> GetRadioGuideChannelsForGroup(int groupFilterId)
    {
      List<TvDatabase.Channel> refChannels = new List<TvDatabase.Channel>();
      IList<TvDatabase.ChannelGroup> groups = TvDatabase.ChannelGroup.ListAll();
      foreach (TvDatabase.ChannelGroup group in groups)
      {
        if (group.IdGroup == groupFilterId)
        {
          IList<GroupMap> maps = group.ReferringGroupMap();
          foreach (GroupMap map in maps)
          {
            TvDatabase.Channel channel = map.ReferencedChannel();
            if (channel != null && channel.IsRadio) refChannels.Add(channel);
          }
          break;
        }
      }
      return refChannels;
    }

    private List<TvDatabase.Channel> GetTVGuideChannelsForGroup(int groupFilterId)
    {
      List<TvDatabase.Channel> refChannels = new List<TvDatabase.Channel>();
      IList<TvDatabase.ChannelGroup> groups = TvDatabase.ChannelGroup.ListAll();
      foreach (TvDatabase.ChannelGroup group in groups)
      {
        if (group.IdGroup == groupFilterId)
        {
          IList<GroupMap> maps = group.ReferringGroupMap();
          foreach (GroupMap map in maps)
          {
            TvDatabase.Channel channel = map.ReferencedChannel();
            if (channel != null && channel.IsTv) refChannels.Add(channel);
          }
          break;
        }
      }
      return refChannels;
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
      return Task.FromResult(new AsyncResult<IChannel>(true, channel));
    }

    protected override Task<AsyncResult<ITuningDetail>> GetProviderTuningDetailsAsync(ICard card, IChannel channel)
    {
      if (!card.Enabled)
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      var cl = TvDatabase.Channel.Retrieve(channel.ChannelNumber);
      var tuningDetails = cl?.ReferringTuningDetail();
      if (!(tuningDetails?.Count > 0))
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      var cdType = RemoteControl.Instance.Type(card.CardId);
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
        channels = GetChannelsInGroup(group.ChannelGroupId)
          // Bug? SortOrder contains logical channel number, not the group sort order?
          // .OrderBy(c => c.SortOrder)
          .Where(c => c != null && c.VisibleInGuide)
          .Select(c => (IChannel)ConvertToChannel(c))
          .Where(c => c != null)
          .ToList();
      }
      return Task.FromResult(new AsyncResult<IList<IChannel>>(true, channels));
    }

    private List<TvDatabase.Channel> GetChannelsInGroup(int groupId)
    {
      List<TvDatabase.Channel> refChannels = new List<TvDatabase.Channel>();
      IList<TvDatabase.ChannelGroup> groups = TvDatabase.ChannelGroup.ListAll();
      foreach (TvDatabase.ChannelGroup group in groups)
      {
        if (group.IdGroup == groupId)
        {
          IList<GroupMap> maps = group.ReferringGroupMap();
          foreach (GroupMap map in maps)
          {
            TvDatabase.Channel channel = map.ReferencedChannel();
            if (channel != null) refChannels.Add(channel);
          }
          break;
        }
      }
      return refChannels;
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
      ISchedule schedule;
      var tvProgram = TvDatabase.Program.Retrieve(program.ProgramId);
      if (tvProgram == null)
      {
        return Task.FromResult(new AsyncResult<ISchedule>(false, null));
      }
      if (CreateProgram(tvProgram, (int)recordingType, out schedule))
      {
        RemoteControl.Instance.OnNewSchedule();
      }
      var success = schedule != null;
      return Task.FromResult(new AsyncResult<ISchedule>(success, schedule));
    }

    private bool CreateProgram(TvDatabase.Program program, int scheduleType, out ISchedule currentSchedule)
    {
      Logger.Debug("SlimTvProxy: CreateProgram - program = {0}", program.ToString());
      TvDatabase.Schedule schedule;
      TvDatabase.Schedule saveSchedule = null;
      TvBusinessLayer layer = new TvBusinessLayer();
      if (IsRecordingProgram(program, out schedule, false)) // check if schedule is already existing
      {
        Logger.Debug("SlimTvProxy: CreateProgram - series schedule found ID={0}, Type={1}", schedule.IdSchedule, schedule.ScheduleType);
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
        Logger.Debug("SlimTvProxy: CreateProgram - no series schedule");
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
        Logger.Debug("SlimTvProxy: CreateProgram - UnCancelSerie at {0}", program.StartTime);
        saveSchedule.UnCancelSerie(program.StartTime, program.IdChannel);
        saveSchedule.Persist();
        currentSchedule = ConvertToSchedule(saveSchedule);
      }
      else
      {
        Logger.Debug("SlimTvProxy: CreateProgram - create schedule = {0}", schedule.ToString());
        schedule.Persist();
        currentSchedule = ConvertToSchedule(schedule);
      }
      return currentSchedule != null;
    }

    private bool IsRecordingProgram(TvDatabase.Program program, out TvDatabase.Schedule recordingSchedule, bool filterCanceledRecordings)
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

    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleByTimeAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType)
    {
      TvBusinessLayer layer = new TvBusinessLayer();
      TvDatabase.Schedule tvSchedule = layer.AddSchedule(channel.ChannelId, title, from, to, (int)recordingType);
      tvSchedule.PreRecordInterval = Int32.Parse(layer.GetSetting("preRecordInterval", "5").Value);
      tvSchedule.PostRecordInterval = Int32.Parse(layer.GetSetting("postRecordInterval", "5").Value);
      tvSchedule.Persist();
      RemoteControl.Instance.OnNewSchedule();
      var schedule = ConvertToSchedule(tvSchedule);
      return Task.FromResult(new AsyncResult<ISchedule>(true, schedule));
    }

    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleDetailedAsync(IChannel channel, string title, DateTime from, DateTime to, ScheduleRecordingType recordingType, int preRecordInterval, int postRecordInterval, string directory, int priority)
    {
      TvBusinessLayer layer = new TvBusinessLayer();
      TvDatabase.Schedule tvSchedule = layer.AddSchedule(channel.ChannelId, title, from, to, (int)recordingType);
      tvSchedule.PreRecordInterval = preRecordInterval >= 0 ? preRecordInterval : Int32.Parse(layer.GetSetting("preRecordInterval", "5").Value);
      tvSchedule.PostRecordInterval = postRecordInterval >= 0 ? postRecordInterval : Int32.Parse(layer.GetSetting("postRecordInterval", "5").Value);
      if (!String.IsNullOrEmpty(directory))
      {
        tvSchedule.Directory = directory;
      }
      if (priority >= 0)
      {
        tvSchedule.Priority = priority;
      }
      tvSchedule.Persist();
      RemoteControl.Instance.OnNewSchedule();
      var schedule = ConvertToSchedule(tvSchedule);
      return Task.FromResult(new AsyncResult<ISchedule>(true, schedule));
    }

    protected override Task<bool> EditProviderScheduleAsync(ISchedule schedule, IChannel channel = null, string title = null, DateTime? from = null, DateTime? to = null, ScheduleRecordingType? recordingType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      try
      {
        Logger.Debug("SlimTvProxy: Editing schedule {0} on channel {1} for {2}, {3} till {4}, type {5}", schedule.ScheduleId, channel.ChannelId, title, from, to, recordingType);
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

        RemoteControl.Instance.OnNewSchedule(); // I don't think this is needed, but doesn't hurt either
        return Task.FromResult(true);
      }
      catch (Exception ex)
      {
        Logger.Warn(String.Format("SlimTvProxy: Failed to edit schedule {0}", schedule.ScheduleId), ex);
        return Task.FromResult(false);
      }
    }

    protected override Task<bool> UnCancelProviderScheduleAsync(IProgram program)
    {
      var tvProgram = TvDatabase.Program.Retrieve(program.ProgramId);
      try
      {
        Logger.Debug("SlimTvProxy: Uncancelling schedule for programId {0}", tvProgram.IdProgram);
        foreach (TvDatabase.Schedule schedule in TvDatabase.Schedule.ListAll().Where(schedule => schedule.IsSerieIsCanceled(program.StartTime, tvProgram.IdChannel)))
        {
          schedule.UnCancelSerie(program.StartTime, tvProgram.IdChannel);
          schedule.Persist();
        }
        return Task.FromResult(true);
      }
      catch (Exception ex)
      {
        Logger.Warn(String.Format("SlimTvProxy: Failed to uncancel schedule for programId {0}", program.ProgramId), ex);
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

    protected override Task<bool> RemoveProviderScheduleForProgramAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      var canceledProgram = TvDatabase.Program.Retrieve(program.ProgramId);
      if (canceledProgram == null)
        return Task.FromResult(false);
      foreach (TvDatabase.Schedule schedule in TvDatabase.Schedule.ListAll().Where(schedule => schedule.IsRecordingProgram(canceledProgram, true)))
      {
        RemoteControl.Instance.StopRecordingSchedule(schedule.IdSchedule);
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
        RemoteControl.Instance.OnNewSchedule();
      }
      return Task.FromResult(true);
    }

    protected override Task<bool> RemoveProviderScheduleAsync(ISchedule schedule)
    {
      TvDatabase.Schedule tvSchedule = TvDatabase.Schedule.Retrieve(schedule.ScheduleId);
      // Already deleted somewhere else?
      if (tvSchedule == null)
        return Task.FromResult(true);
      RemoteControl.Instance.StopRecordingSchedule(tvSchedule.IdSchedule);
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
      RemoteControl.Instance.OnNewSchedule(); // I don't think this is needed, but doesn't hurt either
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

      var fileOrStream = recording.FileName; // FileName represents a local filesystem path on the server. It cannot be used directly in multiseat (RTSP required).
      return Task.FromResult(new AsyncResult<string>(true, recording.FileName));
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
        Logger.Error("SlimTvProxy: Called SwitchTVServerToChannel with empty userName");
        throw new ArgumentNullException("userName");
      }

      IUser currentUser = UserFactory.CreateBasicUser(userName, -1);
      Logger.Debug("SlimTvProxy: Starting timeshifiting with username {0} on channel id {1}", userName, channelId);

      // actually start timeshifting
      VirtualCard card;
      TvResult result = RemoteControl.Instance.StartTimeShifting(ref currentUser, channelId, out card);
      // make sure result is correct and return
      if (result != TvResult.Succeeded)
      {
        Logger.Error("SlimTvProxy: Starting timeshifting failed with result {0}", result);
        return null;
      }
      if (card == null)
      {
        Logger.Error("SlimTvProxy: Couldn't get virtual card");
        return null;
      }

      return Task.FromResult(userName.StartsWith(Consts.LOCAL_USERNAME + "-") ? card.TimeShiftFileName : card.RTSPUrl);
    }

    protected IUser GetUserByUserName(string userName, bool create = false)
    {
      if (userName == null)
      {
        Logger.Warn("SlimTvProxy: Used user with null name");
        return null;
      }

      if (!_tvUsers.ContainsKey(userName) && !create)
        return null;

      if (!_tvUsers.ContainsKey(userName) && create)
        _tvUsers.Add(userName, new User(userName, false));

      return _tvUsers[userName];
    }

    protected override Task<AsyncResult<IList<ICard>>> GetProviderCardsAsync()
    {
      TvBusinessLayer layer = new TvBusinessLayer();
      var cards = layer.Cards.Select(card => new SlimTvCard()
      {
        Name = card.Name,
        CardId = card.IdCard,
        EpgIsGrabbing = card.GrabEPG,
        HasCam = card.CAM,
        CamType = card.CamType == (int)CamType.Default ? SlimTvCamType.Default : SlimTvCamType.Astoncrypt2,
        DecryptLimit = card.DecryptLimit,
        Enabled = card.Enabled,
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

      var cards = new List<IVirtualCard>();
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

    protected override Task<AsyncResult<ISchedule>> IsProviderCurrentlyRecordingAsync(string fileName)
    {
      TvDatabase.Recording recording;
      if (GetRecording(fileName, out recording) || recording.Idschedule <= 0)
        return Task.FromResult(new AsyncResult<ISchedule>(false, null));
      var schedule = ConvertToSchedule(TvDatabase.Schedule.ListAll().FirstOrDefault(s => s.IdSchedule == recording.Idschedule));
      return Task.FromResult(new AsyncResult<ISchedule>(schedule != null, schedule));
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
        IsManual = recording.IsManual,
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
