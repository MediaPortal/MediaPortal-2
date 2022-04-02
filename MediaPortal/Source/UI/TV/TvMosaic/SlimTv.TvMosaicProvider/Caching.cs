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
    private readonly ConcurrentDictionary<TKey, TValue> _entries = new ConcurrentDictionary<TKey, TValue>();

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

    public void RefreshAll()
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
}
