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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MediaPortal.Backend.Database;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.Settings;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.Plugins.SlimTv.Service;
using MediaPortal.Plugins.SlimTv.UPnP;

namespace Tests.Server.TvServer
{
  public class MockTvServer : AbstractSlimTvService<ChannelGroup, Channel, Program, Schedule, ScheduleRule, Recording, TuningDetail, Conflict>
  {
    private int _nextScheduleId = 1;
    private int _nextScheduleRuleId = 1;
    private DateTime _startTime;

    public List<Recording> Recordings { get; } = new List<Recording>();
    public List<Channel> Channels { get; } = new List<Channel>();
    public List<ChannelGroup> ChannelGroups { get; } = new List<ChannelGroup>();
    public List<Schedule> Schedules { get; } = new List<Schedule>();
    public List<Schedule> CancelledSchedules { get; } = new List<Schedule>();
    public List<ScheduleRule> ScheduleRules { get; } = new List<ScheduleRule>();
    public List<Program> Programs { get; } = new List<Program>();
    public List<Conflict> Conflicts { get; } = new List<Conflict>();
    public List<Card> Cards { get; } = new List<Card>();
    public List<VirtualCard> VirtualCards { get; } = new List<VirtualCard>();
    public Dictionary<int, Dictionary<int, TuningDetail>> CardTuningDetails { get; } = new Dictionary<int, Dictionary<int, TuningDetail>>();
    public List<MediaItem> SeriesMediaItems { get; } = new List<MediaItem>();
    public Dictionary<Guid, List<MediaItem>> EpisodeMediaItems { get; } = new Dictionary<Guid, List<MediaItem>>();

    public EpisodeManagementScheme EpisodeManagement
    {
      get => (EpisodeManagementScheme)_serverSettings.EpisodeManagementScheme;
      set => _serverSettings.EpisodeManagementScheme = (int)value;
    }
    public bool DetectMovedPrograms
    {
      get => _serverSettings.DetectMovedPrograms;
      set => _serverSettings.DetectMovedPrograms = value;
    }
    public double MovedProgramsDetectionWindow
    {
      get => _serverSettings.MovedProgramsDetectionWindow;
      set => _serverSettings.MovedProgramsDetectionWindow = value;
    }

    public double MovedProgramsDetectionOffset
    {
      get => _serverSettings.MovedProgramsDetectionOffset;
      set => _serverSettings.MovedProgramsDetectionOffset = value;
    }

    public MockTvServer(DateTime? start = null)
    {
      _startTime = (start ?? DateTime.Now).Date;
      _epgColorSettings = new SlimTvGenreColorSettings();
      _serverSettings = new SlimTvServerSettings();
    }

    public async Task PreCheckSchedulesAsync(DateTime now, bool forceFull = false)
    {
      foreach (var schedule in Schedules)
      {
        var progs = await GetProviderProgramsForScheduleAsync(schedule);
        foreach (var prog in progs.Result)
        {
          var realProg = Programs.FirstOrDefault(p => p.ProgramId == prog.ProgramId);
          realProg.RecordingStatus = RecordingStatus.Scheduled;
          if (schedule.IsSeries)
            realProg.RecordingStatus |= RecordingStatus.SeriesScheduled;
        }
      }

      if (forceFull)
        _checkCacheUpToDate = false;
      await CheckSchedulesAsync(now);
    }

    #region Init

    public override bool Init()
    {
      _localRuleHandling = true;
      _nowTime = _startTime;
      _initComplete.TrySetResult(true);
      return true;
    }

    public override bool DeInit()
    {
      return true;
    }

    protected override void PrepareFilterRegistrations()
    {
    }

    protected override void InitTvCore()
    {
    }

    protected override void PrepareIntegrationProvider()
    {
    }

    protected override void PrepareConnection(ITransaction transaction)
    {
    }

    protected override bool RegisterEvents()
    {
      return true;
    }

    protected override void OnTvServerEvent(object sender, EventArgs eventArgs)
    {
    }

    protected override bool GetRecordingConfiguration(out List<string> recordingFolders, out string singlePattern, out string seriesPattern)
    {
      recordingFolders = new List<string>();
      singlePattern = "";
      seriesPattern = "";
      return true;
    }

    protected override Task InitGenreMapAsync()
    {
      return Task.CompletedTask;
    }

    protected override bool NeedsExtract()
    {
      return false;
    }

    protected override void PrepareProgramData()
    {
    }

    public async Task<IList<ISchedule>> GetRecordedSchedulesAsync(double days)
    {
      var cache = await InitCacheAsync(days, true);
      return cache.CardAssignments.SelectMany(ca => ca.Value).Select(a => a.Schedule).ToList();
    }

    public async Task UpdateProgramAsync(double days)
    {
      var cache = await InitCacheAsync(days, false);
      await UpdateProgramCacheAsync(cache.ProgramRecordingStatuses);
    }

    #endregion

    #region Setup

    public void AddCard(int cardId, string name, bool hasCam, bool supportSubChannels)
    {
      Cards.Add(new Card
      {
        CardId = cardId,
        Name = name,
        CamType = SlimTvCamType.Default,
        Priority = cardId,
        DecryptLimit = hasCam ? 1: 0,
        HasCam = hasCam,
        SupportSubChannels = supportSubChannels,
        Enabled = true,
      });
    }

    public void AddTvChannel(int channelId, string name, string group)
    {
      Channels.Add(new Channel
      {
        ChannelId = channelId,
        Name = name,
        MediaType = MediaType.TV,
        ChannelNumber = channelId,
        GroupNames = new List<string>(new[] { group }),
        VisibleInGuide = true
      });
      if (!ChannelGroups.Any(g => g.Name == group))
      {
        ChannelGroups.Add(new ChannelGroup
        {
          ChannelGroupId = ChannelGroups.Count + 1,
          Name = group,
          MediaType = MediaType.TV,
          SortOrder = ChannelGroups.Count + 1
        });
      }
    }

    public void AddDvbCTvTuningDetail(int tuningDetailId, string name, int cardId, int channelId, int frequency, int modulation, int symbolrate, bool isEncrypted)
    {
      if (!CardTuningDetails.ContainsKey(cardId))
        CardTuningDetails.Add(cardId, new Dictionary<int, TuningDetail>());

      CardTuningDetails[cardId][channelId] = new TuningDetail
      {
        TuningDetailId = tuningDetailId,
        ChannelId = channelId,
        Name = name,
        ChannelType = ChannelType.DvbC,
        Frequency = frequency,
        Modulation = modulation,
        Symbolrate = symbolrate,
        IsEncrypted = isEncrypted,
        MediaType = MediaType.TV,
        NetworkId = 1,
      };
    }

    public void AddProgram(int programId, int channelId, string name, string description, DateTime start, DateTime end, string genre, int rating)
    {
      Programs.Add(new Program
      {
        ProgramId = programId,
        Title = name,
        Description = description,
        ChannelId = channelId,
        StartTime = start,
        EndTime = end,
        Genre = genre,
        StarRating = rating
      });
    }

    public void AddSeriesProgram(int programId, int channelId, string name, string description, DateTime start, DateTime end, string genre, int rating, int seasonNo, int episodeNo, string episodeName)
    {
      Programs.Add(new Program
      {
        ProgramId = programId,
        Title = name,
        Description = description,
        ChannelId = channelId,
        StartTime = start,
        EndTime = end,
        Genre = genre,
        StarRating = rating,
        SeasonNumber = seasonNo >= 0 ? seasonNo.ToString() : null,
        EpisodeNumber = episodeNo >= 0 ? episodeNo.ToString() : null,
        EpisodeNumberDetailed = seasonNo >= 0 && episodeNo >= 0 ? $"S{seasonNo.ToString("00")}E{episodeNo.ToString("00")}" : null,
        EpisodeTitle = episodeName
      });
    }

    public void AddGenreMapping(EpgGenre genre, string genreText)
    {
      _tvGenres.TryAdd(genre, new List<string>());
      if (_tvGenres.TryGetValue(genre, out var genreTexts))
        genreTexts.Add(genreText);
    }

    public void AddRecording(string name)
    {
      Recordings.Add(new Recording
      {
        Title = name
      });
    }

    #endregion

    #region MediaLibrary

    public void AddSeriesMediaItem(Guid seriesId, string name)
    {
      MediaItem item = new MediaItem(seriesId);
      MediaItemAspect.SetAttribute(item.Aspects, SeriesAspect.ATTR_SERIES_NAME, name);
      MediaItemAspect.SetAttribute(item.Aspects, SeriesAspect.ATTR_ORIG_SERIES_NAME, name);
      SeriesMediaItems.Add(item);
    }

    public void AddSeriesEpisodeMediaItem(Guid seriesId, string episodeName, int seasonNo, int episodeNo)
    {
      if (!EpisodeMediaItems.ContainsKey(seriesId))
        EpisodeMediaItems[seriesId] = new List<MediaItem>();

      MediaItem item = new MediaItem(Guid.NewGuid());
      MediaItemAspect.SetAttribute(item.Aspects, EpisodeAspect.ATTR_EPISODE_NAME, episodeName);
      MediaItemAspect.SetAttribute(item.Aspects, EpisodeAspect.ATTR_SEASON, seasonNo);
      MediaItemAspect.SetCollectionAttribute(item.Aspects, EpisodeAspect.ATTR_EPISODE, new int[] { episodeNo });
      EpisodeMediaItems[seriesId].Add(item);
    }

    protected override IList<MediaItem> GetSeriesFromMediaLibrary()
    {
      return SeriesMediaItems;
    }

    protected override IList<MediaItem> GetSeriesEpisodesFromMediaLibrary(Guid seriesId)
    {
      if (!EpisodeMediaItems.ContainsKey(seriesId))
        return new List<MediaItem>();

      return EpisodeMediaItems[seriesId];
    }

    #endregion

    #region Cards

    protected override Task<AsyncResult<IList<ICard>>> GetProviderCardsAsync()
    {
      var cards = Cards.ToList();
      return Task.FromResult(new AsyncResult<IList<ICard>>(cards.Any(), cards.Select(c => (ICard)c).ToList()));
    }

    protected override Task<AsyncResult<IList<IVirtualCard>>> GetProviderActiveVirtualCardsAsync()
    {
      var cards = VirtualCards.ToList();
      return Task.FromResult(new AsyncResult<IList<IVirtualCard>>(cards.Any(), cards.Select(c => (IVirtualCard)c).ToList()));
    }

    protected override Task<AsyncResult<ITuningDetail>> GetProviderTuningDetailsAsync(ICard card, IChannel channel)
    {
      if (!CardTuningDetails.ContainsKey(card.CardId))
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      if (!CardTuningDetails[card.CardId].ContainsKey(channel.ChannelNumber))
        return Task.FromResult(new AsyncResult<ITuningDetail>(false, null));

      var details = CardTuningDetails[card.CardId][channel.ChannelNumber];
      return Task.FromResult(new AsyncResult<ITuningDetail>(details != null, details));
    }

    #endregion

    #region Channels / Channel groups

    protected override Task<AsyncResult<IChannel>> GetProviderChannelAsync(IProgram program)
    {
      var channel = Channels.FirstOrDefault(c => c.ChannelId == program.ChannelId);
      return Task.FromResult(new AsyncResult<IChannel>(channel != null, channel));
    }

    protected override Task<AsyncResult<IList<IChannelGroup>>> GetProviderChannelGroupsAsync()
    {
      var groups = ChannelGroups.ToList();
      return Task.FromResult(new AsyncResult<IList<IChannelGroup>>(groups.Any(), groups.Select(g => (IChannelGroup)g).ToList()));
    }

    protected override Task<AsyncResult<IList<IChannel>>> GetProviderChannelsAsync(IChannelGroup @group)
    {
      var channels = Channels.Where(c => c.GroupNames.Contains(@group.Name)).ToList();
      return Task.FromResult(new AsyncResult<IList<IChannel>>(channels.Any(), channels.Select(g => (IChannel)g).ToList()));
    }

    protected override Task<AsyncResult<IChannel>> GetProviderChannelAsync(int channelId)
    {
      var channel = Channels.FirstOrDefault(c => c.ChannelId == channelId);
      return Task.FromResult(new AsyncResult<IChannel>(channel != null, channel));
    }

    #endregion

    #region Timeshift

    protected override Task<string> SwitchProviderToChannelAsync(string userName, int channelId)
    {
      return Task.FromResult(userName.StartsWith(Consts.LOCAL_USERNAME + "-") ? $"File-{userName}-{channelId}.ts" : $"rtsp://localhost/File-{userName}-{channelId}.ts");
    }

    protected override Task<bool> StopProviderTimeshiftAsync(string userName, int slotIndex)
    {
      return Task.FromResult(true);
    }

    #endregion

    #region Recordings

    protected override Task<AsyncResult<IList<IRecording>>> GetProviderRecordingsAsync(string name)
    {
      var recs = Recordings.Where(r => r.Title.Equals(name, StringComparison.InvariantCultureIgnoreCase)).ToList();
      return Task.FromResult(new AsyncResult<IList<IRecording>>(recs.Any(), recs.Select(r => (IRecording)r).ToList()));
    }

    protected override Task<AsyncResult<RecordingStatus>> GetProviderRecordingStatusAsync(IProgram program)
    {
      var prog = Programs.FirstOrDefault(p => p.ProgramId == program.ProgramId);
      return Task.FromResult(new AsyncResult<RecordingStatus>(prog != null, prog?.RecordingStatus ?? RecordingStatus.None));
    }

    protected override Task<AsyncResult<string>> GetProviderRecordingFileOrStreamAsync(IProgram program)
    {
      return Task.FromResult(new AsyncResult<string>(false, null));
    }

    protected override Task<AsyncResult<ISchedule>> IsProviderCurrentlyRecordingAsync(string fileName)
    {
      return Task.FromResult(new AsyncResult<ISchedule>(false, null));
    }

    #endregion

    #region Programs

    private IList<string> GetProgramGenreNames()
    {
      return (IList<string>)new List<string>()
      {
        "Documentary",
        "Kids",
        "Movie",
        "Music",
        "News",
        "Special",
        "Sports"
      };
    }

    private DateTime CreateDateTimeFromTimeSpan(TimeSpan timeSpan)
    {
      return _startTime.Add(timeSpan);
    }

    private bool IsProgramInTimeSlot(IProgram p, DateTime @from, DateTime to)
    {
      return ((p.EndTime > @from && p.EndTime < to) || (p.StartTime >= @from && p.StartTime <= to) || (p.StartTime <= @from && p.EndTime >= to));
    }

    private bool IsProgramInSchedule(IProgram p, ISchedule schedule)
    {
      var start = schedule.StartTime;
      var end = schedule.EndTime;
      int offset = start.Day == end.Day ? 0 : 1;
      foreach (var dayOffset in new int[] { 0, +1, -1 })
      {
        if (p.StartTime < p.StartTime.Date.AddDays(offset + dayOffset).Add(end.TimeOfDay) && p.EndTime > p.StartTime.Date.AddDays(dayOffset).Add(start.TimeOfDay))
          return true;
      }
      return false;
    }

    protected override Task<AsyncResult<IProgram[]>> GetProviderNowNextProgramAsync(IChannel channel)
    {
      var progs = Programs.Where(p => p.ChannelId == channel.ChannelId).OrderBy(p => p.StartTime).ToList();
      if (progs.Count >= 2)
        return Task.FromResult(new AsyncResult<IProgram[]>(true, new[] { progs[0], progs[1] }));
      return Task.FromResult(new AsyncResult<IProgram[]>(false, null));
    }

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsAsync(IChannel channel, DateTime @from, DateTime to)
    {
      var progs = Programs.Where(p => p.ChannelId == channel.ChannelId && IsProgramInTimeSlot(p, from, to)).OrderBy(p => p.StartTime).ToList();
      return Task.FromResult(new AsyncResult<IList<IProgram>>(progs.Any(), progs.Select(p => GetProgram(p)).ToList()));
    }

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsAsync(string title, DateTime @from, DateTime to)
    {
      var progs = Programs.Where(p => 
        p.Title.Equals(title, StringComparison.InvariantCultureIgnoreCase) && IsProgramInTimeSlot(p, from, to)).OrderBy(p => p.StartTime).ToList();
      return Task.FromResult(new AsyncResult<IList<IProgram>>(progs.Any(), progs.Select(p => GetProgram(p)).ToList()));
    }

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsGroupAsync(IChannelGroup channelGroup, DateTime @from, DateTime to)
    {
      var channels = Channels.Where(c => c.GroupNames.Contains(channelGroup.Name)).ToList();
      var progs = Programs.Where(p => channels.Any(c => c.ChannelId == p.ChannelId) && IsProgramInTimeSlot(p, from, to)).OrderBy(p => p.StartTime).ToList();
      return Task.FromResult(new AsyncResult<IList<IProgram>>(progs.Any(), progs.Select(p => GetProgram(p)).ToList()));
    }

    protected override Task<AsyncResult<IProgram>> GetProviderProgramAsync(int programId)
    {
      var prog = Programs.FirstOrDefault(p => p.ProgramId == programId);
      return Task.FromResult(new AsyncResult<IProgram>(prog != null, GetProgram(prog)));
    }

    protected override Task<AsyncResult<IList<IProgram>>> GetProviderProgramsForScheduleAsync(ISchedule schedule)
    {
      List<Program> progs = new List<Program>();
      List<DayOfWeek> workDays = new List<DayOfWeek>(new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday });
      List<DayOfWeek> weekendDays = new List<DayOfWeek>(new [] { DayOfWeek.Saturday, DayOfWeek.Sunday });
      switch (schedule.RecordingType)
      {
        case ScheduleRecordingType.Once:
          progs.AddRange(Programs.Where(p => p.Title.Equals(schedule.Name) && p.StartTime == schedule.StartTime && p.EndTime == schedule.EndTime && p.ChannelId == schedule.ChannelId));
          break;
        case ScheduleRecordingType.Daily:
          progs.AddRange(Programs.Where(p => p.EndTime >= _startTime && p.ChannelId == schedule.ChannelId && IsProgramInSchedule(p, schedule)));
          break;
        case ScheduleRecordingType.Weekly:
          progs.AddRange(Programs.Where(p => p.EndTime >= _startTime && p.ChannelId == schedule.ChannelId && p.StartTime.DayOfWeek == schedule.StartTime.DayOfWeek && IsProgramInSchedule(p, schedule)));
          break;
        case ScheduleRecordingType.EveryTimeOnThisChannel:
          progs.AddRange(Programs.Where(p => p.Title.Equals(schedule.Name) && p.EndTime >= _startTime && p.ChannelId == schedule.ChannelId));
          break;
        case ScheduleRecordingType.EveryTimeOnEveryChannel:
          progs.AddRange(Programs.Where(p => p.Title.Equals(schedule.Name) && p.EndTime >= _startTime));
          break;
        case ScheduleRecordingType.Weekends:
          progs.AddRange(Programs.Where(p => p.EndTime >= _startTime && p.ChannelId == schedule.ChannelId && weekendDays.Contains(p.StartTime.DayOfWeek) && IsProgramInSchedule(p, schedule)));
          break;
        case ScheduleRecordingType.WorkingDays:
          progs.AddRange(Programs.Where(p => p.EndTime >= _startTime && p.ChannelId == schedule.ChannelId && workDays.Contains(p.StartTime.DayOfWeek) && IsProgramInSchedule(p, schedule)));
          break;
        case ScheduleRecordingType.WeeklyEveryTimeOnThisChannel:
          progs.AddRange(Programs.Where(p => p.Title.Equals(schedule.Name) && p.EndTime >= _startTime && p.ChannelId == schedule.ChannelId && p.StartTime.DayOfWeek == schedule.StartTime.DayOfWeek));
          break;
      }
      return Task.FromResult(new AsyncResult<IList<IProgram>>(progs.Any(), progs.Select(p => GetProgram(p)).ToList()));
    }

    #endregion

    #region Schedules

    private bool IsRecordingProgram(ISchedule schedule, IProgram program, bool filterCanceledRecordings)
    {
      if (program == null)
        return false;

      List<DayOfWeek> workDays = new List<DayOfWeek>(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday });
      List<DayOfWeek> weekendDays = new List<DayOfWeek>(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });
      switch (schedule.RecordingType)
      {
        case ScheduleRecordingType.Once:
          if (program.StartTime == schedule.StartTime && program.EndTime == schedule.EndTime && program.ChannelId == schedule.ChannelId && (!filterCanceledRecordings || !IsSerieIsCanceled(schedule.StartTime, schedule.ChannelId)))
            return true;
          break;
        case ScheduleRecordingType.Daily:
          if (program.ChannelId == schedule.ChannelId)
            return IsRecordingProgramWithinTimeRange(schedule, program, filterCanceledRecordings);
          break;
        case ScheduleRecordingType.Weekly:
          if (program.ChannelId == schedule.ChannelId)
          {
            DateTime startTime = schedule.StartTime;
            int dayOfWeek1 = (int)startTime.DayOfWeek;
            startTime = program.StartTime;
            int dayOfWeek2 = (int)startTime.DayOfWeek;
            if (dayOfWeek1 == dayOfWeek2)
              return IsRecordingProgramWithinTimeRange(schedule, program, filterCanceledRecordings);
            return false;
          }
          break;
        case ScheduleRecordingType.EveryTimeOnThisChannel:
          if (program.Title == schedule.Name && program.ChannelId == schedule.ChannelId && (!filterCanceledRecordings || !IsSerieIsCanceled(GetSchedStartTimeForProg(schedule, program), program.ChannelId)))
            return true;
          break;
        case ScheduleRecordingType.EveryTimeOnEveryChannel:
          if (program.Title == schedule.Name && (!filterCanceledRecordings || !IsSerieIsCanceled(GetSchedStartTimeForProg(schedule, program), program.ChannelId)))
            return true;
          break;
        case ScheduleRecordingType.Weekends:
          if (weekendDays.Contains(program.StartTime.DayOfWeek) && program.ChannelId == schedule.ChannelId)
            return IsRecordingProgramWithinTimeRange(schedule, program, filterCanceledRecordings);
          break;
        case ScheduleRecordingType.WorkingDays:
          if (workDays.Contains(program.StartTime.DayOfWeek) && program.ChannelId == schedule.ChannelId)
            return IsRecordingProgramWithinTimeRange(schedule, program, filterCanceledRecordings);
          break;
        case ScheduleRecordingType.WeeklyEveryTimeOnThisChannel:
          if (program.Title == schedule.Name && program.ChannelId == schedule.ChannelId)
          {
            DateTime startTime = schedule.StartTime;
            int dayOfWeek1 = (int)startTime.DayOfWeek;
            startTime = program.StartTime;
            int dayOfWeek2 = (int)startTime.DayOfWeek;
            if (dayOfWeek1 == dayOfWeek2 && (!filterCanceledRecordings || !IsSerieIsCanceled(GetSchedStartTimeForProg(schedule, program), program.ChannelId)))
              return true;
            break;
          }
          break;
      }
      return false;
    }

    private bool IsRecordingProgramWithinTimeRange(ISchedule schedule, IProgram program, bool filterCanceledRecordings)
    {
      DateTime scheduleStart;
      DateTime scheduleEnd;
      if (GetAdjustedScheduleTimeRange(schedule, program, out scheduleStart, out scheduleEnd))
        return !filterCanceledRecordings || !IsSerieIsCanceled(scheduleStart, program.ChannelId);
      return false;
    }

    private bool GetAdjustedScheduleTimeRange(ISchedule schedule, IProgram program, out DateTime scheduleStart, out DateTime scheduleEnd)
    {
      DateTime dateTime1 = program.StartTime;
      int year = dateTime1.Year;
      dateTime1 = program.StartTime;
      int month = dateTime1.Month;
      dateTime1 = program.StartTime;
      int day = dateTime1.Day;
      int hour = schedule.StartTime.Hour;
      int minute = schedule.StartTime.Minute;
      dateTime1 = new DateTime(year, month, day, hour, minute, 0);
      DateTime dateTime2 = dateTime1.AddDays(-1.0);
      scheduleStart = dateTime2;
      scheduleEnd = scheduleStart.Add(schedule.EndTime.Subtract(schedule.StartTime));
      if (program.StartTime >= scheduleEnd || program.EndTime <= scheduleStart)
      {
        scheduleEnd = scheduleEnd.AddDays(1.0);
        scheduleStart = scheduleStart.AddDays(1.0);
        if (program.StartTime >= scheduleEnd || program.EndTime <= scheduleStart)
        {
          scheduleEnd = scheduleEnd.AddDays(1.0);
          scheduleStart = scheduleStart.AddDays(1.0);
          if (program.StartTime >= scheduleEnd || program.EndTime <= scheduleStart)
            return false;
        }
      }
      return true;
    }

    private DateTime GetSchedStartTimeForProg(ISchedule schedule, IProgram prog)
    {
      DateTime scheduleStart;
      DateTime scheduleEnd;
      if (schedule.RecordingType != ScheduleRecordingType.EveryTimeOnEveryChannel && schedule.RecordingType != ScheduleRecordingType.EveryTimeOnThisChannel && 
          (schedule.RecordingType != ScheduleRecordingType.WeeklyEveryTimeOnThisChannel && GetAdjustedScheduleTimeRange(schedule, prog, out scheduleStart, out scheduleEnd)))
        return scheduleStart;
      return prog.StartTime;
    }

    private bool IsSerieIsCanceled(DateTime startTime, int channelId)
    {
      return CancelledSchedules.Any(c => c.ChannelId == channelId && c.StartTime == startTime);
    }

    private Schedule GetNewSchedule()
    {
      var schedule = new Schedule
      {
        ScheduleId = _nextScheduleId,
        ChannelId = 0,
        Name = "",
        KeepDate = null,
        KeepMethod = KeepMethodType.Always,
        PreRecordInterval = TimeSpan.FromMinutes(5),
        PostRecordInterval = TimeSpan.FromMinutes(5),
        Priority = PriorityType.Normal,
        StartTime = DateTime.Now,
        EndTime = DateTime.Now,
        RecordingType = ScheduleRecordingType.Once
      };
      _nextScheduleId++;
      return schedule;
    }

    protected override Task<AsyncResult<IList<ISchedule>>> GetProviderSchedulesAsync()
    {
      var schedules = Schedules.ToList();
      return Task.FromResult(new AsyncResult<IList<ISchedule>>(schedules.Any(), schedules.Select(s => (ISchedule)s).ToList()));
    }

    protected override Task<AsyncResult<IList<ISchedule>>> GetProviderCanceledSchedulesAsync()
    {
      var schedules = CancelledSchedules.ToList();
      return Task.FromResult(new AsyncResult<IList<ISchedule>>(schedules.Any(), schedules.Select(s => (ISchedule)s).ToList()));
    }

    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      var schedule = GetNewSchedule();
      schedule.ChannelId = program.ChannelId;
      schedule.Name = program.Title;
      schedule.StartTime = program.StartTime;
      schedule.EndTime = program.EndTime;
      schedule.RecordingType = recordingType;
      Schedules.Add(schedule);
      return Task.FromResult(new AsyncResult<ISchedule>(true, schedule));
    }

    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleByTimeAsync(IChannel channel, DateTime @from, DateTime to, ScheduleRecordingType recordingType)
    {
      var schedule = GetNewSchedule();
      schedule.ChannelId = channel.ChannelId;
      schedule.Name = $"{Consts.MANUAL_RECORDING_TITLE_PREFIX}{Consts.MANUAL_RECORDING_TITLE}";
      schedule.StartTime = @from;
      schedule.EndTime = to;
      schedule.RecordingType = recordingType;
      Schedules.Add(schedule);
      return Task.FromResult(new AsyncResult<ISchedule>(true, schedule));
    }

    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleByTimeAsync(IChannel channel, string title, DateTime @from, DateTime to, ScheduleRecordingType recordingType)
    {
      var schedule = GetNewSchedule();
      schedule.ChannelId = channel.ChannelId;
      schedule.Name = title;
      schedule.StartTime = @from;
      schedule.EndTime = to;
      schedule.RecordingType = recordingType;
      Schedules.Add(schedule);
      return Task.FromResult(new AsyncResult<ISchedule>(true, schedule));
    }

    protected override Task<AsyncResult<ISchedule>> CreateProviderScheduleDetailedAsync(IChannel channel, string title, DateTime @from, DateTime to, ScheduleRecordingType recordingType, int preRecordInterval, int postRecordInterval, string directory, int priority)
    {
      var schedule = GetNewSchedule();
      schedule.ChannelId = channel.ChannelId;
      schedule.Name = title;
      schedule.StartTime = @from;
      schedule.EndTime = to;
      schedule.RecordingType = recordingType;
      schedule.PreRecordInterval = TimeSpan.FromMinutes(preRecordInterval);
      schedule.PostRecordInterval = TimeSpan.FromMinutes(postRecordInterval);
      schedule.Priority = (PriorityType)priority;
      Schedules.Add(schedule);
      return Task.FromResult(new AsyncResult<ISchedule>(true, schedule));
    }

    protected override Task<bool> EditProviderScheduleAsync(ISchedule schedule, IChannel channel = null, string title = null, DateTime? @from = null, DateTime? to = null, ScheduleRecordingType? recordingType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      var existingSchedule = Schedules.FirstOrDefault(s => s.ScheduleId == schedule.ScheduleId) as Schedule;
      if (existingSchedule == null)
        return Task.FromResult(false);

      existingSchedule.ChannelId = channel?.ChannelId ?? existingSchedule.ChannelId;
      existingSchedule.Name = title ?? existingSchedule.Name;
      existingSchedule.StartTime = @from ?? existingSchedule.StartTime;
      existingSchedule.EndTime = to ?? existingSchedule.EndTime;
      existingSchedule.RecordingType = recordingType ?? existingSchedule.RecordingType;
      existingSchedule.PreRecordInterval = preRecordInterval != null ? TimeSpan.FromMinutes(preRecordInterval.Value) : existingSchedule.PreRecordInterval;
      existingSchedule.PostRecordInterval = postRecordInterval != null ? TimeSpan.FromMinutes(postRecordInterval.Value) : existingSchedule.PostRecordInterval;
      existingSchedule.Priority = priority != null ? (PriorityType)priority.Value : existingSchedule.Priority;
      return Task.FromResult(true);
    }

    protected override Task<bool> RemoveProviderScheduleForProgramAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      var canceledProgram = program;
      if (canceledProgram == null)
        return Task.FromResult(false);

      var allSchedules = Schedules;
      var matchingSchedules = allSchedules.Where(schedule => IsRecordingProgram(schedule, canceledProgram, true)).ToList();
      if (!matchingSchedules.Any())
      {
        List<Schedule> manualSchedules = new List<Schedule>();
        //Check for matching manual recordings because they will not match any programs start and/or end times
        foreach (var schedule in allSchedules.Where(schedule => IsManualTitle(schedule.Name)))
        {
          if ((canceledProgram.StartTime <= schedule.StartTime && canceledProgram.EndTime >= schedule.StartTime) || //Recording was started during this program
            (canceledProgram.StartTime <= schedule.EndTime && canceledProgram.EndTime >= schedule.EndTime) || //Recording is ending during this program
            (canceledProgram.StartTime >= schedule.StartTime && canceledProgram.EndTime <= schedule.StartTime)) //The program is "inside" the recording
            manualSchedules.Add(schedule);
        }
        matchingSchedules = manualSchedules;
      }
      //Delete matching schedules
      foreach (var schedule in matchingSchedules)
      {
        if (schedule.RecordingType == ScheduleRecordingType.Once || recordingType != ScheduleRecordingType.Once)
        {
          // Delete single schedule, or whole series
          Schedules.Remove(schedule);
        }
        else
        {
          // Delete this program only
          var clone = CreateScheduleClone(schedule);
          clone.StartTime = program.StartTime;
          clone.EndTime = program.StartTime.Add(schedule.EndTime - schedule.StartTime);
          CancelledSchedules.Add(clone);
        }
      }
      return Task.FromResult(true);
    }

    protected override Task<bool> RemoveProviderScheduleAsync(ISchedule schedule)
    {
      var existingSchedule = Schedules.FirstOrDefault(s => s.ScheduleId == schedule.ScheduleId);
      if (existingSchedule == null)
        return Task.FromResult(false);

      return Task.FromResult(Schedules.Remove(existingSchedule));
    }

    protected override Task<bool> UnCancelProviderScheduleAsync(IProgram program)
    {
      var schedule = CancelledSchedules.FirstOrDefault(s => s.ChannelId == program.ChannelId && s.StartTime == program.StartTime);
      if (schedule == null)
        return Task.FromResult(false);

      return Task.FromResult(CancelledSchedules.Remove(schedule));
    }

    #endregion

    #region Schedule rules

    protected override Task<AsyncResult<IScheduleRule>> CreateProviderScheduleRuleAsync(IScheduleRule scheduleRule)
    {
      var rule = scheduleRule as ScheduleRule;
      if (rule == null)
        return Task.FromResult(new AsyncResult<IScheduleRule>(false, null));

      rule.RuleId = _nextScheduleRuleId;
      ScheduleRules.Add(rule);
      _nextScheduleRuleId++;
      return Task.FromResult(new AsyncResult<IScheduleRule>(true, rule));
    }

    protected override Task<bool> EditProviderScheduleRuleAsync(IScheduleRule scheduleRule)
    {
      var existingSchedule = ScheduleRules.FirstOrDefault(s => s.RuleId == scheduleRule.RuleId);
      if (existingSchedule == null)
        return Task.FromResult(false);

      ScheduleRule rule = scheduleRule as ScheduleRule;
      if (rule == null)
        return Task.FromResult(false);

      if (!ScheduleRules.Remove(existingSchedule))
        return Task.FromResult(false);

      rule.RuleId = existingSchedule.RuleId;
      ScheduleRules.Add(rule);
      return Task.FromResult(true);
    }

    protected override Task<AsyncResult<IList<IScheduleRule>>> GetProviderScheduleRulesAsync()
    {
      var schedules = ScheduleRules.ToList();
      return Task.FromResult(new AsyncResult<IList<IScheduleRule>>(schedules.Any(), schedules.Select(r => (IScheduleRule)r).ToList()));
    }

    protected override Task<bool> RemoveProviderScheduleRuleAsync(IScheduleRule scheduleRule)
    {
      var existingSchedule = ScheduleRules.FirstOrDefault(s => s.RuleId == scheduleRule.RuleId);
      if (existingSchedule == null)
        return Task.FromResult(false);

      return Task.FromResult(ScheduleRules.Remove(existingSchedule));
    }

    #endregion

    #region Conflicts

    protected override Task<AsyncResult<IList<IConflict>>> GetProviderConflictsAsync()
    {
      var conflicts = Conflicts.ToList();
      return Task.FromResult(new AsyncResult<IList<IConflict>>(conflicts.Any(), conflicts.Select(c => (IConflict)c).ToList()));
    }

    protected override Task<bool> RemoveAllProviderConflictsAsync()
    {
      Conflicts.Clear();
      return Task.FromResult(true);
    }

    protected override Task<bool> SaveProviderConflictsAsync(IList<IConflict> conflicts)
    {
      int idStart = Conflicts.Count;
      Conflicts.AddRange(conflicts.Select(c => new Conflict
      {
        CardId = c.CardId,
        ChannelId = c.ChannelId,
        ConflictId = idStart++,
        ConflictingScheduleId = c.ConflictingScheduleId,
        ProgramStartTime = c.ProgramStartTime,
        ScheduleId = c.ScheduleId
      }).ToList());
      return Task.FromResult(true);
    }

    #endregion

    #region Conversion

    protected override Program ConvertToProgram(Program tvProgram, bool includeRecordingStatus = false)
    {
      return tvProgram;
    }

    protected override Channel ConvertToChannel(Channel tvChannel)
    {
      return tvChannel;
    }

    protected override ChannelGroup ConvertToChannelGroup(ChannelGroup tvGroup)
    {
      return tvGroup;
    }

    protected override Schedule ConvertToSchedule(Schedule schedule)
    {
      return schedule;
    }

    protected override TuningDetail ConvertToTuningDetail(TuningDetail tuningDetail)
    {
      return tuningDetail;
    }

    protected override Recording ConvertToRecording(Recording recording)
    {
      return recording;
    }

    protected override ScheduleRule ConvertToScheduleRule(ScheduleRule scheduleRule)
    {
      return scheduleRule;
    }

    protected override Conflict ConvertToConflict(Conflict conflict)
    {
      return conflict;
    }

    protected override Task<MediaItem> CreateMediaItemAsync(int slotIndex, string streamUrl, IChannel channel)
    {
      // Channel is usually only passed as placeholder with ID only, so query the details here
      var fullChannel = Channels.FirstOrDefault(c => c.ChannelId == channel.ChannelId);
      bool isTv = fullChannel.MediaType == MediaType.TV;
      return CreateMediaItemAsync(slotIndex, streamUrl, channel, isTv, fullChannel);
    }

    #endregion
  }
}
