#region Copyright (C) 2007-2021 Team MediaPortal

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
using SlimTv.TvMosaicProvider.Utils;
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

    public const string MIME_TYPE_TVMOSAIC_STREAM = "SlimTV/TvMosaicPlayer";
    public const string MIME_TYPE_TVMOSAIC_RADIO_STREAM = "SlimTV/TvMosaicRadioPlayer";

    private static readonly string LOCAL_SYSTEM = SystemName.LocalHostName;
    private HttpDataProvider _dvbLink;
    private readonly object _syncObj = new object();

    private readonly IdMapper<string> _channelAndGroupIdMap = new IdMapper<string>();
    private readonly IdMapper<(string channel, string program)> _programIdMap = new IdMapper<(string, string)>();
    private readonly IDictionary<int, List<IChannel>> _channelGroupMap = new ConcurrentDictionary<int, List<IChannel>>();
    private readonly IList<IChannelGroup> _channelGroups = new List<IChannelGroup>();
    private readonly IList<IChannel> _mpChannels = new List<IChannel>();
    private readonly Dictionary<int, IChannel> _tunedChannels = new Dictionary<int, IChannel>();
    private readonly Dictionary<int, long> _tunedChannelHandles = new Dictionary<int, long>();
    private bool _supportsTimeshift;
    private string _host;
    private TimeshiftStatusCache _timeshiftStatusCache = null;
    private NowNextProgramsCache _nowNextProgramsCache = null;
    private FullDayProgramsCache _fullDayProgramsCache = null;

    public bool Init()
    {
      lock (_syncObj)
      {
        // Init only once
        if (_dvbLink != null)
          return true;

        IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>(false);
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();

        var settings = serverSettings != null ? serverSettings.Load<TvMosaicProviderSettings>() : settingsManager.Load<TvMosaicProviderSettings>();

        _host = settings.Host;
        _dvbLink = new HttpDataProvider(_host, settings.Port, settings.Username ?? string.Empty, settings.Password ?? string.Empty);
        var caps = _dvbLink.GetStreamingCapabilities(new CapabilitiesRequest()).Result;
        if (caps.Status == StatusCode.STATUS_OK)
        {
          var streamingCapabilities = caps.Result;
          _supportsTimeshift = streamingCapabilities.SupportsTimeshift;
          ServiceRegistration.Get<ILogger>()
            .Info("TvMosaic: Initialized connection. Caps: Record {0}; Timeshift {1}; DeviceManagement {2}; SupTranscoders {3}; SupPbTranscoders: {4}",
              streamingCapabilities.CanRecord,
              streamingCapabilities.SupportsTimeshift,
              streamingCapabilities.DeviceManagement,
              streamingCapabilities.SupTranscoders,
              streamingCapabilities.SupPbTranscoders);

          _timeshiftStatusCache = new TimeshiftStatusCache(TimeSpan.FromSeconds(2), _dvbLink, _tunedChannelHandles);
          _nowNextProgramsCache = new NowNextProgramsCache(TimeSpan.FromMinutes(1), this);
          _fullDayProgramsCache = new FullDayProgramsCache(TimeSpan.FromHours(1), this);
          return true;
        }

        ServiceRegistration.Get<ILogger>().Error("TvMosaic: Could not initialize connection. Status: {0}; ", caps.Status);
        return false;
      }
    }

    public bool DeInit()
    {
      foreach (int slotIndex in _tunedChannels.Keys)
      {
        // Stop streaming by Client ID stops all simultaneous streams
        if (_tunedChannels.TryGetValue(slotIndex, out var channel) && channel != null)
          _dvbLink.StopStream(new StopStream(GetTimeshiftUserName(slotIndex))).Wait();
      }
      return true;
    }

    public string Name { get; } = "TV Mosaic";

    #region IChannelAndGroupInfoAsync

    private int GetSlimTvChannelId(string tvMosaicId)
    {
      return _channelAndGroupIdMap.GetOrCreateId(tvMosaicId);
    }

    private string GetTvMosaicChannelId(int slimTvId)
    {
      return _channelAndGroupIdMap.GetKey(slimTvId);
    }

    private async Task<bool> LoadChannels()
    {
      lock (_syncObj)
      {
        if (_channelGroupMap.Any() || _mpChannels.Any())
          return true;
      }

      DVBLinkResponse<Channels> channels = await _dvbLink.GetChannels(new ChannelsRequest()).ConfigureAwait(false);
      DVBLinkResponse<Favorites> favorites = channels.Status == StatusCode.STATUS_OK ? await _dvbLink.GetFavorites(new FavoritesRequest()).ConfigureAwait(false) : null;

      lock (_syncObj)
      {
        // Another thread may have loaded the channels whilst
        // this thread was waiting for the responses above
        if (_channelGroupMap.Any() || _mpChannels.Any())
          return true;

        if (channels.Status != StatusCode.STATUS_OK)
          return false;

        foreach (var channel in channels.Result.OrderBy(c => c.Number == 0 ? 100000 : c.Number).ThenBy(c => c.SubNumber).ThenBy(c => c.Name))
        {
          var mappedId = GetSlimTvChannelId(channel.Id);
          IChannel mpChannel = new TvMosaicChannel
          {
            TvMosaicId = channel.Id,
            Name = channel.Name,
            ChannelId = mappedId,
            MediaType = channel.ChannelType == 1 ? MediaType.Radio : MediaType.TV,
            ChannelNumber = channel.Number
          };
          _mpChannels.Add(mpChannel);
        }

        _channelGroups.Clear();
        foreach (var favorite in favorites.Result)
        {
          IEnumerable<IChannel> groupChannels = _mpChannels.OfType<TvMosaicChannel>().Where(c => favorite.Channels.Contains(c.TvMosaicId));
          var groupId = favorite.Id.ToString();
          IChannelGroup group = new ChannelGroup
          {
            Name = favorite.Name,
            ChannelGroupId = GetSlimTvChannelId(groupId),
            MediaType = groupChannels.All(c => c.MediaType == MediaType.Radio) ? MediaType.Radio : MediaType.TV,
          };

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

    public async Task<AsyncResult<IList<IChannel>>> GetChannelsAsync()
    {
      if (await LoadChannels().ConfigureAwait(false))
        return new AsyncResult<IList<IChannel>>(true, _mpChannels);
      return new AsyncResult<IList<IChannel>>(false, null);
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

    public int SelectedRadioChannelId { get; set; } = 0;

    public int SelectedRadioChannelGroupId
    {
      get
      {
        TvMosaicProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TvMosaicProviderSettings>();
        return settings.LastRadioChannelGroupId;
      }
      set
      {
        TvMosaicProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TvMosaicProviderSettings>();
        settings.LastRadioChannelGroupId = value;
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
      }
    }

    #endregion

    #region IProgramInfoAsync

    public async Task<AsyncResult<IProgram[]>> GetNowNextProgramAsync(IChannel channel)
    {
      var result = _nowNextProgramsCache.Get(channel);
      return new AsyncResult<IProgram[]>(result != null, result);
    }

    /// <summary>
    /// Called by cache
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public async Task<AsyncResult<IProgram[]>> GetNowNextProgramInternalAsync(IChannel channel)
    {
      var programs = await GetPrograms(new List<IChannel> { channel }, DateTime.Now, null, null, 2);
      var result = ToNowNext(programs)?.Values.FirstOrDefault();
      return new AsyncResult<IProgram[]>(result != null, result);
    }

    public async Task<AsyncResult<IDictionary<int, IProgram[]>>> GetNowAndNextForChannelGroupAsync(IChannelGroup channelGroup)
    {
      var channels = await GetChannelsAsync(channelGroup);
      var programs = await GetPrograms(channels.Result, DateTime.Now, null, null, 2);
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

    public async Task<AsyncResult<IList<IProgram>>> GetAllPrograms(DateTime? @from, DateTime? to)
    {
      return await GetProgramsInternal(_mpChannels, from, to);
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsInternal(IEnumerable<IChannel> channels, DateTime? @from, DateTime? to)
    {
      EpgSearcher epgSearcher = new EpgSearcher
      {
        StartTime = @from.ToUnixTime(),
        EndTime = to.ToUnixTime(),
        ChannelsIDs = new ChannelIDList(channels.Select(c => GetTvMosaicChannelId(c.ChannelId)).ToList())
      };

      var programs = await _dvbLink.SearchEpg(epgSearcher).ConfigureAwait(false);
      if (programs.Status == StatusCode.STATUS_OK && programs.Result.Any())
      {
        return new AsyncResult<IList<IProgram>>(true, ToProgram(programs.Result));
      }
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    protected async Task<AsyncResult<IList<IProgram>>> GetPrograms(IEnumerable<IChannel> channels, DateTime? @from, DateTime? to, string keyWord = null, int maxPrograms = -1)
    {
      EpgSearcher epgSearcher = new EpgSearcher
      {
        StartTime = @from.ToUnixTime(),
        EndTime = to.ToUnixTime(),
        Keyword = keyWord,
        RequestedCount = maxPrograms
      };
      if (channels != null)
        epgSearcher.ChannelsIDs = new ChannelIDList(channels.Select(c => GetTvMosaicChannelId(c.ChannelId)).ToList());

      // Regular loading without keyword or limits can be fetched from cache
      //if (keyWord == null && maxPrograms == -1 &&
      //    (!from.HasValue || from.Value >= _fullDayProgramsCache.CacheStart) &&
      //    (!to.HasValue || to.Value <= _fullDayProgramsCache.CacheEnd))
      //{
      //  List<IProgram> cachedPrograms = new List<IProgram>();
      //  foreach (IChannel channel in channels)
      //  {
      //    var dayPrograms = await _fullDayProgramsCache.GetAsync(channel);
      //    if (dayPrograms == null)
      //      continue;
      //    IEnumerable<IProgram> filtered = dayPrograms;
      //    if (from.HasValue)
      //      filtered = filtered.Where(p => p.EndTime >= from.Value || p.StartTime >= from.Value);
      //    if (to.HasValue)
      //      filtered = filtered.Where(p => p.StartTime <= to.Value);

      //    cachedPrograms.AddRange(filtered);
      //  }
      //  var result = new AsyncResult<IList<IProgram>>(cachedPrograms.Count > 0, cachedPrograms);
      //  return result;
      //}

      var programs = await _dvbLink.SearchEpg(epgSearcher).ConfigureAwait(false);
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
        ChannelId = GetSlimTvChannelId(manualSchedule.ChannelId),
        Title = "Manual", // required for SlimTV handling of manual schedules (localized label). // manualSchedule.Title,
        StartTime = startTime,
        EndTime = endTime,
        RecordingStatus = RecordingStatus.Scheduled
      };
      return mpProgram;
    }

    private MPProgram ToProgram(TvMosaic.API.Program tvMosaicProgram, string channelId)
    {
      var startTime = tvMosaicProgram.StartTime.FromUnixTime();
      var endTime = startTime.AddSeconds(tvMosaicProgram.Duration);
      var mpProgram = new MPProgram
      {
        ChannelId = GetSlimTvChannelId(channelId),
        ProgramId = GetSlimTvProgramId(channelId, tvMosaicProgram.Id),
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

      var genres = tvMosaicProgram.MapGenres();
      if (genres.Count > 0)
        mpProgram.Genre = string.Join(", ", genres.ToArray());

      return mpProgram;
    }

    public int GetSlimTvProgramId(string tvMosaicChannelId, string tvMosaicProgramId)
    {
      return _programIdMap.GetOrCreateId((tvMosaicChannelId, tvMosaicProgramId));
    }

    public string GetTvMosaicProgramId(int slimTvId)
    {
      return _programIdMap.GetKey(slimTvId).program;
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
          var streamType = _supportsTimeshift ? RequestStream.RAW_HTTP_TS_TYPE : RequestStream.RAW_HTTP_TYPE;
          var reqStream = new RequestStream(serverAddress, tvMosaicChannel.TvMosaicId, GetTimeshiftUserName(slotIndex), streamType, transcoder);
          DVBLinkResponse<Streamer> strm = await _dvbLink.PlayChannel(reqStream).ConfigureAwait(false);
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

    public TimeshiftStatus GetTimeshiftStatusCached(int slotContext)
    {
      return _timeshiftStatusCache?.Get(slotContext);
    }

    public async Task<TimeshiftStatus> GetTimeshiftStatus(int slotContext)
    {
      var timeshiftGetStats = new TimeshiftGetStats
      {
        ChannelHandle = _tunedChannelHandles[slotContext]
      };
      var status = await _dvbLink.GetTimeshiftStatus(timeshiftGetStats).ConfigureAwait(false);
      if (status.Status == StatusCode.STATUS_OK)
        return status.Result;
      return null;
    }

    public async Task<bool> StopTimeshiftAsync(int slotIndex)
    {
      if (!_tunedChannelHandles.TryGetValue(slotIndex, out long tunedChannelHandle) || tunedChannelHandle == 0)
        return false;

      var request = new StopStream(tunedChannelHandle);
      var result = await _dvbLink.StopStream(request).ConfigureAwait(false);
      _tunedChannelHandles[slotIndex] = 0;
      _tunedChannels[slotIndex] = null;

      return true;
    }

    public async Task<bool> SeekAbsolute(int slotContext, ulong positionSeconds)
    {
      var timeshiftGetStats = new TimeshiftSeek
      {
        ChannelHandle = _tunedChannelHandles[slotContext],
        Type = 1, // By seconds
        Offset = (long)positionSeconds,
        SeekOrigin = 0 // offset is calculated from the beginning of the timeshift buffer
      };
      var status = await _dvbLink.TimeshiftSeek(timeshiftGetStats).ConfigureAwait(false);
      return status;
    }

    public async Task<bool> SeekRelative(int slotContext, int offsetSeconds)
    {
      var timeshiftGetStats = new TimeshiftSeek
      {
        ChannelHandle = _tunedChannelHandles[slotContext],
        Type = 1, // By seconds
        Offset = offsetSeconds,
        SeekOrigin = 1 // offset is calculated from the current playback position
      };
      var status = await _dvbLink.TimeshiftSeek(timeshiftGetStats).ConfigureAwait(false);
      return status;
    }

    public IChannel GetChannel(int slotIndex)
    {
      return _tunedChannels.TryGetValue(slotIndex, out IChannel channel) ? channel : null;
    }

    public MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      bool isTv = channel.MediaType == MediaType.TV;
      string mimeType = isTv ? MIME_TYPE_TVMOSAIC_STREAM : MIME_TYPE_TVMOSAIC_RADIO_STREAM;
      LiveTvMediaItem tvStream = SlimTvMediaItemBuilder.CreateMediaItem(slotIndex, streamUrl, channel, isTv, mimeType);
      return tvStream;
    }

    public async Task<AsyncResult<ISchedule>> CreateScheduleAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      var channelId = GetTvMosaicChannelId(program.ChannelId);
      var programId =  GetTvMosaicProgramId(program.ProgramId); // StartTime.ToUnixTime().ToString(); // Translate start time back to timestamp
      var byEpg = new ByEpgSchedule(channelId, programId);
      byEpg.IsRepeat = recordingType != ScheduleRecordingType.Once;
      var scheduleRequest = new TvMosaic.API.Schedule(byEpg);
      var result = await _dvbLink.AddSchedule(scheduleRequest).ConfigureAwait(false);
      if (result.Status == StatusCode.STATUS_OK)
      {
        var sResult = await _dvbLink.GetSchedules(new SchedulesRequest()).ConfigureAwait(false);
        if (sResult.Status == StatusCode.STATUS_OK)
        {
          var createdSchedule = sResult.Result.FirstOrDefault(s => s.ByEpg != null && s.ByEpg.ChannelId == channelId && s.ByEpg.ProgramId == programId);
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
          ChannelId = GetSlimTvChannelId(createdSchedule.ByEpg.ChannelId),
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
          ChannelId = GetSlimTvChannelId(createdSchedule.Manual.ChannelId),
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
      var channelId = GetTvMosaicChannelId(channel.ChannelId);
      int dayMask = 0;
      var startTime = @from.ToUnixTime();
      var manualSchedule = new ManualSchedule(channelId, "Manual", startTime, (int)(to - from).TotalSeconds, dayMask);
      var scheduleRequest = new TvMosaic.API.Schedule(manualSchedule);
      var result = await _dvbLink.AddSchedule(scheduleRequest).ConfigureAwait(false);
      if (result.Status == StatusCode.STATUS_OK)
      {
        var sResult = await _dvbLink.GetSchedules(new SchedulesRequest()).ConfigureAwait(false);
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
      var scheduledPrograms = await _dvbLink.GetRecordings(new RecordingsRequest()).ConfigureAwait(false);
      bool success = scheduledPrograms.Status == StatusCode.STATUS_OK;
      if (success)
      {
        foreach (var scheduledProgram in scheduledPrograms.Result)
        {
          if (GetSlimTvProgramId(scheduledProgram.ChannelId, scheduledProgram.Program.Id) == program.ProgramId)
          {
            // Only remove single program from schedule
            if (recordingType == ScheduleRecordingType.Once)
            {
              var response = await _dvbLink.RemoveRecording(new RecordingRemover(scheduledProgram.RecordingId)).ConfigureAwait(false);
              success = response.Status == StatusCode.STATUS_OK;
            }
            else
            {
              // Remove full schedule
              var response = await _dvbLink.RemoveSchedule(new ScheduleRemover(scheduledProgram.ScheduleId)).ConfigureAwait(false);
              success = response.Status == StatusCode.STATUS_OK;
            }
          }
        }
      }
      return success;
    }

    public async Task<bool> RemoveScheduleAsync(ISchedule schedule)
    {
      // Schedule means here the single / series definition what to record. The actual "timer" for a single program is named "recording" inside TvMosaic API.
      var result = await _dvbLink.RemoveSchedule(new ScheduleRemover(schedule.ScheduleId.ToString())).ConfigureAwait(false);
      return result.Status == StatusCode.STATUS_OK;
    }

    public Task<bool> UnCancelScheduleAsync(IProgram program)
    {
      throw new NotImplementedException();
    }

    public async Task<AsyncResult<RecordingStatus>> GetRecordingStatusAsync(IProgram program)
    {
      var scheduledPrograms = await _dvbLink.GetRecordings(new RecordingsRequest()).ConfigureAwait(false);
      IList<IProgram> programs = new List<IProgram>();
      RecordingStatus status = RecordingStatus.None;
      bool success = scheduledPrograms.Status == StatusCode.STATUS_OK;
      if (success)
      {
        foreach (var scheduledProgram in scheduledPrograms.Result)
        {
          if (GetSlimTvProgramId(scheduledProgram.ChannelId, scheduledProgram.Program.Id) == program.ProgramId)
          {
            status = RecordingStatus.Scheduled;
          }
        }
      }
      return new AsyncResult<RecordingStatus>(success, status);
    }

    public Task<AsyncResult<string>> GetRecordingFileOrStreamAsync(IProgram program)
    {
      throw new NotImplementedException();
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsForScheduleAsync(ISchedule schedule)
    {
      var scheduledPrograms = await _dvbLink.GetRecordings(new RecordingsRequest()).ConfigureAwait(false);
      IList<IProgram> programs = new List<IProgram>();
      bool success = scheduledPrograms.Status == StatusCode.STATUS_OK;
      if (success)
      {
        foreach (Recording recording in scheduledPrograms.Result.Where(r => r.ScheduleId == schedule.ScheduleId.ToString() && !string.IsNullOrEmpty(r.Program.Id)))
        {
          MPProgram program = ToProgram(recording.Program, recording.ChannelId);
          programs.Add(program);
        }
      }
      return new AsyncResult<IList<IProgram>>(success, programs);
    }

    public async Task<AsyncResult<IList<ISchedule>>> GetSchedulesAsync()
    {
      var schedules = await _dvbLink.GetSchedules(new SchedulesRequest()).ConfigureAwait(false);
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
