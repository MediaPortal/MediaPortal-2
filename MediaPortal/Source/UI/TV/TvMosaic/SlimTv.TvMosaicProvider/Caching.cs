using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using TvMosaic.API;
using TvMosaic.Shared;

namespace SlimTv.TvMosaicProvider
{
  public abstract class AutoRefreshCache<TKey, TValue>
  {
    protected readonly ConcurrentDictionary<TKey, TValue> _entries = new ConcurrentDictionary<TKey, TValue>();

    protected AutoRefreshCache(TimeSpan interval)
    {
      var timer = new System.Timers.Timer();
      timer.Interval = interval.TotalMilliseconds;
      timer.AutoReset = true;
      timer.Elapsed += (o, e) =>
      {
        ((System.Timers.Timer)o).Stop();
        RefreshAll();
        ((System.Timers.Timer)o).Start();
      };
      timer.Start();
    }

    public TValue Get(TKey key)
    {
      return _entries.GetOrAdd(key, Load);
    }

    public virtual async Task<TValue> GetAsync(TKey key)
    {
      return _entries.GetOrAdd(key, Load);
    }

    public virtual void RefreshAll()
    {
      var keys = _entries.Keys;
      foreach (var key in keys)
      {
        _entries.AddOrUpdate(key, k => Load(key), (k, v) => Load(key));
      }
    }

    protected abstract TValue Load(TKey key);
  }

  public class TimeshiftStatusCache : AutoRefreshCache<int, TimeshiftStatus>
  {
    private readonly HttpDataProvider _dataProvider;
    private readonly Dictionary<int, long> _tunedChannelHandles;

    public TimeshiftStatusCache(TimeSpan interval, HttpDataProvider dataProvider, Dictionary<int, long> tunedChannelHandles)
      : base(interval)
    {
      _dataProvider = dataProvider;
      _tunedChannelHandles = tunedChannelHandles;
    }

    public TimeshiftStatusCache(TimeSpan interval) :
      base(interval)
    {
    }

    protected override TimeshiftStatus Load(int key)
    {
      var timeshiftGetStats = new TimeshiftGetStats { ChannelHandle = _tunedChannelHandles[key] };
      var status = _dataProvider.GetTimeshiftStatus(timeshiftGetStats).Result;
      if (status.Status == StatusCode.STATUS_OK)
        return status.Result;
      return null;
    }
  }

  public class NowNextProgramsCache : AutoRefreshCache<IChannel, IProgram[]>
  {
    private readonly TvMosaicProvider _provider;

    public NowNextProgramsCache(TimeSpan interval, TvMosaicProvider provider)
      : base(interval)
    {
      _provider = provider;
    }

    public NowNextProgramsCache(TimeSpan interval) :
      base(interval)
    {
    }

    protected override IProgram[] Load(IChannel key)
    {
      var asyncResult = _provider.GetNowNextProgramInternalAsync(key).Result;
      return asyncResult.Success ? asyncResult.Result : null;
    }
  }

  public class FullDayProgramsCache : AutoRefreshCache<IChannel, IList<IProgram>>
  {
    private readonly TvMosaicProvider _provider;
    private DateTime _cacheStart;
    private DateTime _cacheEnd;
    private Task _initTask;

    public DateTime CacheStart => _cacheStart;
    public DateTime CacheEnd => _cacheEnd;

    public FullDayProgramsCache(TimeSpan interval, TvMosaicProvider provider)
      : base(interval)
    {
      _provider = provider;
      _initTask = FullRefreshAsync();
    }

    public override void RefreshAll()
    {
      Task.Run(FullRefreshAsync).Wait();
    }

    public async Task FullRefreshAsync()
    {
      _entries.Clear();
      _cacheStart = DateTime.Now.AddHours(-4);
      _cacheEnd = DateTime.Now.AddHours(+24);
      var channels = await _provider.GetChannelsAsync().ConfigureAwait(false);
      if (!channels.Success)
        return;
      var programs = await _provider.GetAllPrograms(_cacheStart, _cacheEnd).ConfigureAwait(false);
      var programMap = programs.Result.GroupBy(p => p.ChannelId).ToDictionary(p => p.Key, p => p.ToList());
      foreach (var channel in channels.Result)
        _entries[channel] = programMap.TryGetValue(channel.ChannelId, out var channelPrograms) ? channelPrograms : new List<IProgram>();
    }

    public override async Task<IList<IProgram>> GetAsync(IChannel key)
    {
      await _initTask.ConfigureAwait(false);
      return await base.GetAsync(key);
    }

    public FullDayProgramsCache(TimeSpan interval) :
      base(interval)
    {
    }

    protected override IList<IProgram> Load(IChannel key)
    {
      var asyncResult = _provider.GetProgramsInternal(new List<IChannel> { key }, _cacheStart, _cacheEnd).Result;
      return asyncResult.Success ? asyncResult.Result : null;
    }
  }
}
