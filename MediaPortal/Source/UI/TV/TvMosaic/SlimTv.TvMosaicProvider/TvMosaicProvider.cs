﻿#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using MediaPortal.Common.Async;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.ServerSettings;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TvMosaic.API;
using TvMosaic.Shared;
using MPChannel = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Channel;
using MPProgram = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Program;
using MPSchedule = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Schedule;

namespace SlimTv.TvMosaicProvider
{
  public class TvMosaicChannel : MPChannel
  {
    public string TvMosaicId { get; set; }
  }

  public class TvMosaicProvider : ITvProvider, IChannelAndGroupInfoAsync, ITimeshiftControlAsync, IProgramInfoAsync, IScheduleControlAsync
  {
    private static readonly string LOCAL_SYSTEM = SystemName.LocalHostName;
    private HttpDataProvider _dvbLink;
    private readonly object _syncObj = new object();
    private readonly IDictionary<string, int> _idMapping = new ConcurrentDictionary<string, int>();
    private readonly IDictionary<int, List<IChannel>> _channelGroupMap = new ConcurrentDictionary<int, List<IChannel>>();
    private readonly IList<IChannelGroup> _channelGroups = new List<IChannelGroup>();
    private readonly IList<IChannel> _mpChannels = new List<IChannel>();
    private readonly Dictionary<int, IChannel> _tunedChannels = new Dictionary<int, IChannel>();
    private readonly Dictionary<int, long> _tunedChannelHandles = new Dictionary<int, long>();
    private bool _supportsTimeshift;
    private string _host;

    public bool Init()
    {
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>(false);
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();

      var settings = serverSettings != null ?
        serverSettings.Load<TvMosaicProviderSettings>() :
        settingsManager.Load<TvMosaicProviderSettings>();

      _host = settings.Host;
      _dvbLink = new HttpDataProvider(_host, settings.Port, settings.Username ?? string.Empty, settings.Password ?? string.Empty);
      var caps = _dvbLink.GetStreamingCapabilities(new CapabilitiesRequest()).Result;
      if (caps.Status == StatusCode.STATUS_OK)
      {
        var streamingCapabilities = caps.Result;
        _supportsTimeshift = streamingCapabilities.SupportsTimeshift;
        ServiceRegistration.Get<ILogger>().Info("TvMosaic: Initialized connection. Caps: Record {0}; Timeshift {1}; DeviceManagement {2}; SupTranscoders {3}; SupPbTranscoders: {4}",
          streamingCapabilities.CanRecord,
          streamingCapabilities.SupportsTimeshift,
          streamingCapabilities.DeviceManagement,
          streamingCapabilities.SupTranscoders,
          streamingCapabilities.SupPbTranscoders);
        return true;
      }

      ServiceRegistration.Get<ILogger>().Error("TvMosaic: Could not initialize connection. Status: {0}; ", caps.Status);
      return false;
    }

    public bool DeInit()
    {
      // TODO
      return true;
    }

    public string Name { get; } = "TV Mosaic";

    #region IChannelAndGroupInfoAsync

    private int GetId(string key)
    {
      if (!_idMapping.ContainsKey(key))
        return _idMapping[key] = _idMapping.Count + 1;
      return _idMapping[key];
    }

    private string GetTvMosaicId(int key)
    {
      KeyValuePair<string, int> map = _idMapping.FirstOrDefault(m => m.Value == key);
      return map.Key;
    }

    private async Task<bool> LoadChannels()
    {
      lock (_syncObj)
      {
        if (_channelGroupMap.Any() || _mpChannels.Any())
          return true;
      }

      DVBLinkResponse<Channels> channels = await _dvbLink.GetChannels(new ChannelsRequest());
      lock (_syncObj)
      {
        if (channels.Status != StatusCode.STATUS_OK)
          return false;

        foreach (var channel in channels.Result.OrderBy(c => c.Number == 0 ? 100000 : c.Number).ThenBy(c => c.SubNumber).ThenBy(c => c.Name))
        {
          var mappedId = GetId(channel.Id);
          IChannel mpChannel = new TvMosaicChannel
          {
            TvMosaicId = channel.Id,
            Name = channel.Name,
            ChannelId = mappedId,
            MediaType = MediaType.TV,
            ChannelNumber = channel.Number
          };
          _mpChannels.Add(mpChannel);
        }
      }

      var favorites = await _dvbLink.GetFavorites(new FavoritesRequest());
      lock (_syncObj)
      {
        _channelGroups.Clear();
        foreach (var favorite in favorites.Result)
        {
          var groupId = favorite.Id.ToString();
          IChannelGroup group = new ChannelGroup
          {
            Name = favorite.Name,
            ChannelGroupId = GetId(groupId)
          };

          IEnumerable<IChannel> groupChannels = _mpChannels.OfType<TvMosaicChannel>().Where(c => favorite.Channels.Contains(c.TvMosaicId));
          _channelGroups.Add(group);
          _channelGroupMap[group.ChannelGroupId] = new List<IChannel>(groupChannels);
        }
      }

      return true;
    }

    public async Task<AsyncResult<IList<IChannelGroup>>> GetChannelGroupsAsync()
    {
      // We first need all known channels, then look at the group (which only references the channel IDs)
      if (!await LoadChannels())
        return new AsyncResult<IList<IChannelGroup>>(false, null);

      List<IChannelGroup> groups;
      lock (_syncObj)
        groups = _channelGroups.ToList();

      return new AsyncResult<IList<IChannelGroup>>(groups.Count > 0, groups);
    }

    public async Task<AsyncResult<IList<IChannel>>> GetChannelsAsync(IChannelGroup group)
    {
      if (await LoadChannels())
      {
        if (_channelGroupMap.TryGetValue(group.ChannelGroupId, out var channels))
          return new AsyncResult<IList<IChannel>>(true, channels);
      }

      return new AsyncResult<IList<IChannel>>(false, null);
    }

    public async Task<AsyncResult<IChannel>> GetChannelAsync(int channelId)
    {
      if (!await LoadChannels())
        return new AsyncResult<IChannel>(false, null);

      var mpChannel = _mpChannels.FirstOrDefault(c => c.ChannelId == channelId);
      return new AsyncResult<IChannel>(mpChannel != null, mpChannel);
    }

    public int SelectedChannelId { get; set; } = 0;

    public int SelectedChannelGroupId
    {
      get
      {
        TvMosaicProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TvMosaicProviderSettings>();
        return settings.LastChannelGroupId;
      }
      set
      {
        TvMosaicProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TvMosaicProviderSettings>();
        settings.LastChannelGroupId = value;
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
      }
    }

    #endregion

    #region IProgramInfoAsync

    public async Task<AsyncResult<IProgram[]>> GetNowNextProgramAsync(IChannel channel)
    {
      var programs = await GetPrograms(new List<IChannel> { channel }, DateTime.Now, DateTime.Now.AddHours(3));
      var result = ToNowNext(programs)?.Values.FirstOrDefault();
      return new AsyncResult<IProgram[]>(result != null, result);
    }

    public async Task<AsyncResult<IDictionary<int, IProgram[]>>> GetNowAndNextForChannelGroupAsync(IChannelGroup channelGroup)
    {
      var channels = await GetChannelsAsync(channelGroup);
      var programs = await GetPrograms(channels.Result, DateTime.Now, DateTime.Now.AddHours(3));
      var result = ToNowNext(programs);
      return new AsyncResult<IDictionary<int, IProgram[]>>(result != null, result);
    }

    private static IDictionary<int, IProgram[]> ToNowNext(AsyncResult<IList<IProgram>> programs)
    {
      if (programs == null || !programs.Success)
        return null;
      IDictionary<int, IProgram[]> result = new Dictionary<int, IProgram[]>();
      foreach (var program in programs.Result)
      {
        if (!result.ContainsKey(program.ChannelId))
          result[program.ChannelId] = new IProgram[2];
        else
        {
          // We processed programs for this channel already
          continue;
        }

        var channelPrograms = programs.Result.Where(p => p.ChannelId == program.ChannelId).Take(2).ToArray();
        if (channelPrograms.Length > 0)
          result[program.ChannelId][0] = channelPrograms[0];
        if (channelPrograms.Length > 1)
          result[program.ChannelId][1] = channelPrograms[1];
      }

      return result;
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(IChannel channel, DateTime @from, DateTime to)
    {
      return await GetPrograms(new List<IChannel> { channel }, from, to);
    }

    protected async Task<AsyncResult<IList<IProgram>>> GetPrograms(IEnumerable<IChannel> channels, DateTime @from, DateTime to, string keyWord = null)
    {
      EpgSearcher epgSearcher = channels != null ?
        new EpgSearcher(new ChannelIDList(channels.Select(c => GetTvMosaicId(c.ChannelId)).ToList()), false, from.ToUnixTime(), to.ToUnixTime()) :
        new EpgSearcher(keyWord, false, from.ToUnixTime(), to.ToUnixTime());

      var programs = await _dvbLink.SearchEpg(epgSearcher);
      if (programs.Status == StatusCode.STATUS_OK && programs.Result.Any())
      {
        return new AsyncResult<IList<IProgram>>(true, ToProgram(programs.Result));
      }
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    public IList<IProgram> ToProgram(ChannelsIdWithPrograms programs)
    {
      IList<IProgram> mpPrograms = new List<IProgram>();
      foreach (ChannelIdWithPrograms idWithPrograms in programs)
        foreach (var tvMosaicProgram in idWithPrograms.Programs)
        {
          var mpProgram = ToProgram(tvMosaicProgram, idWithPrograms.ChannelId);
          mpPrograms.Add(mpProgram);
        }
      // Order matters here, the client expects programs in time order
      return mpPrograms.OrderBy(p => p.ChannelId).ThenBy(p => p.StartTime).ToList();
    }

    private MPProgram ToProgram(TvMosaic.API.ManualSchedule manualSchedule)
    {
      var startTime = manualSchedule.StartTime.FromUnixTime();
      var endTime = startTime.AddSeconds(manualSchedule.Duration);
      var mpProgram = new MPProgram
      {
        ChannelId = GetId(manualSchedule.ChannelId),
        Title = "Manual", // required for SlimTV handling of manual schedules (localized label). // manualSchedule.Title,
        StartTime = startTime,
        EndTime = endTime,
        RecordingStatus = RecordingStatus.Scheduled
      };
      return mpProgram;
    }

    private MPProgram ToProgram(TvMosaic.API.Program tvMosaicProgram, string channelId = "")
    {
      var startTime = tvMosaicProgram.StartTime.FromUnixTime();
      var endTime = startTime.AddSeconds(tvMosaicProgram.Duration);
      var mpProgram = new MPProgram
      {
        ChannelId = GetId(channelId),
        ProgramId = ToUniqueProgramId(channelId, tvMosaicProgram.Id),
        Title = tvMosaicProgram.Title,
        Description = tvMosaicProgram.ShortDesc,
        StartTime = startTime,
        EndTime = endTime,
        SeasonNumber = tvMosaicProgram.SeasonNum > 0 ? tvMosaicProgram.SeasonNum.ToString() : null,
        EpisodeNumber = tvMosaicProgram.EpisodeNum > 0 ? tvMosaicProgram.EpisodeNum.ToString() : null,
        RecordingStatus = tvMosaicProgram.IsRecord ?
          (tvMosaicProgram.IsRepeatRecord ?
            RecordingStatus.SeriesScheduled :
            RecordingStatus.Scheduled)
          : RecordingStatus.None
      };

      List<string> genres = new List<string>();
      // Genres
      if (tvMosaicProgram.IsAction)
        genres.Add("Action");
      if (tvMosaicProgram.IsComedy)
        genres.Add("Comedy");
      if (tvMosaicProgram.IsReality)
        genres.Add("Reality");
      if (tvMosaicProgram.IsAdult)
        genres.Add("Adult");
      if (tvMosaicProgram.IsDocumentary)
        genres.Add("Documentary");
      if (tvMosaicProgram.IsDrama)
        genres.Add("Drama");
      if (tvMosaicProgram.IsEducational)
        genres.Add("Educational");
      if (tvMosaicProgram.IsHorror)
        genres.Add("Horror");
      if (tvMosaicProgram.IsKids)
        genres.Add("Kids");
      if (tvMosaicProgram.IsMovie)
        genres.Add("Movie");
      if (tvMosaicProgram.IsMusic)
        genres.Add("Music");
      if (tvMosaicProgram.IsRomance)
        genres.Add("Romance");
      if (tvMosaicProgram.IsNews)
        genres.Add("News");
      if (tvMosaicProgram.IsScifi)
        genres.Add("Science Fiction");
      if (tvMosaicProgram.IsSoap)
        genres.Add("Soap");
      if (tvMosaicProgram.IsSports)
        genres.Add("Sports");
      if (tvMosaicProgram.IsSpecial)
        genres.Add("Special");
      if (tvMosaicProgram.IsThriller)
        genres.Add("Thriller");

      if (genres.Count > 0)
        mpProgram.Genre = string.Join(", ", genres.ToArray());

      return mpProgram;
    }

    public static int ToUniqueProgramId(string channelId, string programId)
    {
      return int.Parse(programId); // (channelId + programId).GetHashCode();
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(string title, DateTime @from, DateTime to)
    {
      return await GetPrograms(null, from, to, title);
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsGroupAsync(IChannelGroup channelGroup, DateTime @from, DateTime to)
    {
      var channels = await GetChannelsAsync(channelGroup);
      return await GetPrograms(channels.Result, from, to);
    }

    public async Task<AsyncResult<IChannel>> GetChannelAsync(IProgram program)
    {
      return await GetChannelAsync(program.ChannelId);
    }

    public bool GetProgram(int programId, out IProgram program)
    {
      program = null;
      return false;
    }

    #endregion

    public String GetTimeshiftUserName(int slotIndex)
    {
      return String.Format("STC_{0}_{1}", LOCAL_SYSTEM, slotIndex);
    }

    public async Task<AsyncResult<MediaItem>> StartTimeshiftAsync(int slotIndex, IChannel channel)
    {
      try
      {
        var tvMosaicChannel = _mpChannels.OfType<TvMosaicChannel>().FirstOrDefault(c => c.ChannelId == channel.ChannelId);
        var serverAddress = _host;
        Transcoder transcoder = null;
        if (tvMosaicChannel != null)
        {
          // Currently always a new stream is started, so make sure to properly end the former
          await StopTimeshiftAsync(slotIndex);
          var streamType = _supportsTimeshift ? "raw_http_timeshift" : "raw_http";
          var reqStream = new RequestStream(serverAddress, tvMosaicChannel.TvMosaicId, GetTimeshiftUserName(slotIndex), streamType, transcoder);
          DVBLinkResponse<Streamer> strm = await _dvbLink.PlayChannel(reqStream);
          if (strm.Status == StatusCode.STATUS_OK)
          {
            var streamUrl = strm.Result.Url;

            _tunedChannelHandles[slotIndex] = strm.Result.ChannelHandle;
            _tunedChannels[slotIndex] = channel;

            // assign a MediaItem, can be null if streamUrl is the same.
            var timeshiftMediaItem = CreateMediaItem(slotIndex, streamUrl, channel);
            return new AsyncResult<MediaItem>(true, timeshiftMediaItem);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("TvMosaic: error playing channel {0}", ex, channel.Name);
      }

      return new AsyncResult<MediaItem>(false, null);
    }

    public async Task<bool> StopTimeshiftAsync(int slotIndex)
    {
      if (!_tunedChannelHandles.TryGetValue(slotIndex, out long tunedChannelHandle) || tunedChannelHandle == 0)
        return false;

      var request = new StopStream(tunedChannelHandle);
      var result = await _dvbLink.StopStream(request);
      _tunedChannelHandles[slotIndex] = 0;

      return true;
    }

    public IChannel GetChannel(int slotIndex)
    {
      return _tunedChannels.TryGetValue(slotIndex, out IChannel channel) ? channel : null;
    }

    public MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      bool isTv = true;
      LiveTvMediaItem tvStream = SlimTvMediaItemBuilder.CreateMediaItem(slotIndex, streamUrl, channel, isTv, LiveTvMediaItem.MIME_TYPE_TV_STREAM);
      return tvStream;
    }

    public async Task<AsyncResult<ISchedule>> CreateScheduleAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      var channelId = GetTvMosaicId(program.ChannelId);
      var programId = program.ProgramId.ToString(); // StartTime.ToUnixTime().ToString(); // Translate start time back to timestamp
      var byEpg = new ByEpgSchedule(channelId, programId);
      byEpg.IsRepeat = recordingType != ScheduleRecordingType.Once;
      var scheduleRequest = new TvMosaic.API.Schedule(byEpg);
      var result = await _dvbLink.AddSchedule(scheduleRequest);
      if (result.Status == StatusCode.STATUS_OK)
      {
        var sResult = await _dvbLink.GetSchedules(new SchedulesRequest());
        if (sResult.Status == StatusCode.STATUS_OK)
        {
          var createdSchedule = sResult.Result.FirstOrDefault(s => s.ByEpg != null && ToUniqueProgramId(s.ByEpg.ChannelId, s.ByEpg.ProgramId) == ToUniqueProgramId(channelId, programId));
          if (createdSchedule != null)
          {
            return new AsyncResult<ISchedule>(true, ToSchedule(createdSchedule));
          }
        }
      }
      return new AsyncResult<ISchedule>(false, null);
    }

    private ISchedule ToSchedule(TvMosaic.API.Schedule createdSchedule)
    {
      if (createdSchedule.ByEpg != null)
      {
        var program = ToProgram(createdSchedule.ByEpg.Program, createdSchedule.ByEpg.ChannelId);
        var mpSchedule = new MPSchedule
        {
          ChannelId = GetId(createdSchedule.ByEpg.ChannelId),
          StartTime = program.StartTime,
          EndTime = program.EndTime,
          ScheduleId = Int32.Parse(createdSchedule.ScheduleID),
          RecordingType = createdSchedule.ByEpg.IsRepeat ?
            ScheduleRecordingType.EveryTimeOnThisChannel :
            ScheduleRecordingType.Once,
          Name = program.Title
        };
        return mpSchedule;
      }

      if (createdSchedule.Manual != null)
      {
        var program = ToProgram(createdSchedule.Manual);
        var mpSchedule = new MPSchedule
        {
          ChannelId = GetId(createdSchedule.Manual.ChannelId),
          StartTime = program.StartTime,
          EndTime = program.EndTime,
          ScheduleId = Int32.Parse(createdSchedule.ScheduleID),
          RecordingType = ScheduleRecordingType.Once,
          Name = createdSchedule.Manual.Title
        };
        return mpSchedule;
      }

      return null;
    }

    public async Task<AsyncResult<ISchedule>> CreateScheduleByTimeAsync(IChannel channel, DateTime @from, DateTime to, ScheduleRecordingType recordingType)
    {
      var channelId = GetTvMosaicId(channel.ChannelId);
      int dayMask = 0;
      var startTime = @from.ToUnixTime();
      var manualSchedule = new ManualSchedule(channelId, "Manual schedule", startTime, (int)(to - from).TotalSeconds, dayMask);
      var scheduleRequest = new TvMosaic.API.Schedule(manualSchedule);
      var result = await _dvbLink.AddSchedule(scheduleRequest);
      if (result.Status == StatusCode.STATUS_OK)
      {
        var sResult = await _dvbLink.GetSchedules(new SchedulesRequest());
        if (sResult.Status == StatusCode.STATUS_OK)
        {
          var createdSchedule = sResult.Result.FirstOrDefault(s => s.Manual != null && s.Manual.ChannelId == channelId && s.Manual.StartTime == startTime);
          if (createdSchedule != null)
          {
            return new AsyncResult<ISchedule>(true, ToSchedule(createdSchedule));
          }
        }
      }
      return new AsyncResult<ISchedule>(false, null);
    }

    public Task<AsyncResult<ISchedule>> CreateScheduleByTimeAsync(IChannel channel, string title, DateTime @from, DateTime to, ScheduleRecordingType recordingType)
    {
      throw new NotImplementedException();
    }

    public Task<AsyncResult<ISchedule>> CreateScheduleDetailedAsync(IChannel channel, string title, DateTime @from, DateTime to, ScheduleRecordingType recordingType, int preRecordInterval, int postRecordInterval, string directory, int priority)
    {
      throw new NotImplementedException();
    }

    public Task<bool> EditScheduleAsync(ISchedule schedule, IChannel channel = null, string title = null, DateTime? @from = null, DateTime? to = null, ScheduleRecordingType? recordingType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      throw new NotImplementedException();
    }

    public async Task<bool> RemoveScheduleForProgramAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      var programId = program.ProgramId;
      var sResult = await _dvbLink.GetSchedules(new SchedulesRequest());
      if (sResult.Status == StatusCode.STATUS_OK)
      {
        var schedule = sResult.Result.FirstOrDefault(s => s.ByEpg != null && ToUniqueProgramId(s.ByEpg.ChannelId, s.ByEpg.ProgramId) == programId);
        if (schedule != null)
        {
          var result = await _dvbLink.RemoveSchedule(new ScheduleRemover(schedule.ScheduleID));
          return result.Status == StatusCode.STATUS_OK;
        }
      }

      return false;
    }

    public async Task<bool> RemoveScheduleAsync(ISchedule schedule)
    {
      var result = await _dvbLink.RemoveSchedule(new ScheduleRemover(schedule.ScheduleId.ToString()));
      return result.Status == StatusCode.STATUS_OK;
    }

    public Task<bool> UnCancelScheduleAsync(IProgram program)
    {
      throw new NotImplementedException();
    }

    public async Task<AsyncResult<RecordingStatus>> GetRecordingStatusAsync(IProgram program)
    {
      var programId = program.ProgramId;
      var sResult = await _dvbLink.GetSchedules(new SchedulesRequest());
      if (sResult.Status == StatusCode.STATUS_OK)
      {
        var createdSchedule = sResult.Result.FirstOrDefault(s => s.ByEpg != null && ToUniqueProgramId(s.ByEpg.ChannelId, s.ByEpg.ProgramId) == programId);
        if (createdSchedule != null)
        {
          return new AsyncResult<RecordingStatus>(true, RecordingStatus.Scheduled);
        }
      }
      return new AsyncResult<RecordingStatus>(true, RecordingStatus.None);
    }

    public Task<AsyncResult<string>> GetRecordingFileOrStreamAsync(IProgram program)
    {
      throw new NotImplementedException();
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsForScheduleAsync(ISchedule schedule)
    {
      var schedules = await _dvbLink.GetSchedules(new SchedulesRequest());
      if (schedules.Status == StatusCode.STATUS_OK)
      {
        var requestedSchedule = schedules.Result.FirstOrDefault(s => s.ScheduleID == schedule.ScheduleId.ToString());
        if (requestedSchedule != null)
        {
          MPProgram program = null;
          if (requestedSchedule.ByEpg != null)
          {
            program = ToProgram(requestedSchedule.ByEpg.Program, requestedSchedule.ByEpg.ChannelId);
            program.RecordingStatus = requestedSchedule.ByEpg.IsRepeat ? RecordingStatus.SeriesScheduled : RecordingStatus.Scheduled;
            var now = DateTime.Now;
            if (now > program.StartTime && now <= program.EndTime)
              program.RecordingStatus |= requestedSchedule.ByEpg.IsRepeat ? RecordingStatus.RecordingSeries : RecordingStatus.RecordingOnce;
          }

          if (requestedSchedule.Manual != null)
          {
            program = ToProgram(requestedSchedule.Manual);
            var now = DateTime.Now;
            if (now > program.StartTime && now <= program.EndTime)
              program.RecordingStatus |= RecordingStatus.RecordingOnce;
          }

          if (program == null)
            return new AsyncResult<IList<IProgram>>(false, null);

          IList<IProgram> programs = new List<IProgram> { program };
          return new AsyncResult<IList<IProgram>>(true, programs);
        }
      }
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    public async Task<AsyncResult<IList<ISchedule>>> GetSchedulesAsync()
    {
      var schedules = await _dvbLink.GetSchedules(new SchedulesRequest());
      if (schedules.Status == StatusCode.STATUS_OK)
      {
        var mpSchedules = schedules.Result.Select(ToSchedule).ToList();
        return new AsyncResult<IList<ISchedule>>(true, mpSchedules);
      }
      return new AsyncResult<IList<ISchedule>>(false, null);
    }

    public Task<AsyncResult<ISchedule>> IsCurrentlyRecordingAsync(string fileName)
    {
      return Task.FromResult(new AsyncResult<ISchedule>(false, null));
    }
  }
}
