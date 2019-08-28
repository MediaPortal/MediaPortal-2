using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using TvMosaic.API;
using MPChannel = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Channel;

namespace SlimTv.TvMosaicProvider
{
  public class TvMosaicChannel : MPChannel
  {
    public string TvMosaicId { get; set; }
  }

  public class TvMosaicProvider : ITvProvider, IChannelAndGroupInfoAsync, ITimeshiftControlAsync, IProgramInfoAsync
  {
    private static readonly string LOCAL_SYSTEM = SystemName.LocalHostName;
    private HttpDataProvider _dvbLink;
    private readonly object _syncObj = new object();
    private readonly IDictionary<string, int> _idMapping = new ConcurrentDictionary<string, int>();
    private readonly IDictionary<IChannelGroup, List<IChannel>> _channelGroups = new ConcurrentDictionary<IChannelGroup, List<IChannel>>();
    private readonly IList<IChannel> _mpChannels = new List<IChannel>();
    private readonly Dictionary<int, IChannel> _tunedChannels = new Dictionary<int, IChannel>();

    public bool Init()
    {
      // TODO
      _dvbLink = new HttpDataProvider("127.0.0.1", 9270, string.Empty, string.Empty);
      return true;
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

    private async Task<bool> LoadChannels()
    {
      lock (_syncObj)
      {
        if (_channelGroups.Any() || _mpChannels.Any())
          return true;
      }

      DVBLinkResponse<Channels> channels = await _dvbLink.GetChannels(new ChannelsRequest());
      lock (_syncObj)
      {
        foreach (var channel in channels.Result)
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
        foreach (var favorite in favorites.Result)
        {
          var groupId = favorite.Id.ToString();
          IChannelGroup group = new ChannelGroup
          {
            Name = favorite.Name,
            ChannelGroupId = GetId(groupId)
          };

          IEnumerable<IChannel> groupChannels = _mpChannels.OfType<TvMosaicChannel>().Where(c => favorite.Channels.Contains(c.TvMosaicId));
          _channelGroups[group] = new List<IChannel>(groupChannels);
        }
      }

      return true;
    }

    public async Task<AsyncResult<IList<IChannelGroup>>> GetChannelGroupsAsync()
    {
      // We first need all known channels, then look at the group (which only references the channel IDs)
      if (!await LoadChannels())
        return new AsyncResult<IList<IChannelGroup>>(false, null);

      var groups = _channelGroups.Keys.ToList();

      return new AsyncResult<IList<IChannelGroup>>(groups.Count > 0, groups);
    }

    public async Task<AsyncResult<IList<IChannel>>> GetChannelsAsync(IChannelGroup group)
    {
      if (await LoadChannels())
      {
        var channelGroup = _channelGroups.Keys.FirstOrDefault(g => g.ChannelGroupId == group.ChannelGroupId);
        if (channelGroup != null)
          return new AsyncResult<IList<IChannel>>(true, _channelGroups[channelGroup]);
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
    public int SelectedChannelGroupId { get; set; } = 0;

    #endregion

    #region IProgramInfoAsync

    public async Task<AsyncResult<IProgram[]>> GetNowNextProgramAsync(IChannel channel)
    {
      return new AsyncResult<IProgram[]>(false, null);
    }

    public async Task<AsyncResult<IDictionary<int, IProgram[]>>> GetNowAndNextForChannelGroupAsync(IChannelGroup channelGroup)
    {
      return new AsyncResult<IDictionary<int, IProgram[]>>(false, null);
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(IChannel channel, DateTime @from, DateTime to)
    {
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(string title, DateTime @from, DateTime to)
    {
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsGroupAsync(IChannelGroup channelGroup, DateTime @from, DateTime to)
    {
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    public async Task<AsyncResult<IChannel>> GetChannelAsync(IProgram program)
    {
      return new AsyncResult<IChannel>(false, null);
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
        var serverAddress = "127.0.0.1";
        Transcoder transcoder = null;
        var reqStream = new RequestStream(serverAddress, tvMosaicChannel.TvMosaicId, GetTimeshiftUserName(slotIndex), "raw_http_timeshift", transcoder);
        DVBLinkResponse<Streamer> strm = await _dvbLink.PlayChannel(reqStream);
        if (strm.Status == StatusCode.STATUS_OK)
        {
          var streamUrl = strm.Result.Url;

          _tunedChannels[slotIndex] = channel;

          // assign a MediaItem, can be null if streamUrl is the same.
          var timeshiftMediaItem = CreateMediaItem(slotIndex, streamUrl, channel);
          return new AsyncResult<MediaItem>(true, timeshiftMediaItem);
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
      var request = new StopStream(GetTimeshiftUserName(slotIndex));
      var result = await _dvbLink.StopStream(request);
      return true;
    }

    public IChannel GetChannel(int slotIndex)
    {
      return _tunedChannels.TryGetValue(slotIndex, out IChannel channel) ? channel : null;
    }


    public MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      LiveTvMediaItem tvStream = SlimTvMediaItemBuilder.CreateMediaItem(slotIndex, streamUrl, channel);
      if (tvStream != null)
      {
        IList<MultipleMediaItemAspect> providerAspects;
        if (MediaItemAspect.TryGetAspects(tvStream.Aspects, ProviderResourceAspect.Metadata, out providerAspects))
        {
          var providerResourceAspect = providerAspects.First();
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, LiveTvMediaItem.MIME_TYPE_TV_STREAM);
        }
        // Add program infos to the LiveTvMediaItem
        //IProgram currentProgram;
        //if (GetCurrentProgram(channel, out currentProgram))
        //  tvStream.AdditionalProperties[LiveTvMediaItem.CURRENT_PROGRAM] = currentProgram;

        //IProgram nextProgram;
        //if (GetNextProgram(channel, out nextProgram))
        //  tvStream.AdditionalProperties[LiveTvMediaItem.NEXT_PROGRAM] = nextProgram;

        return tvStream;
      }
      return null;
    }
  }
}
