using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using TvMosaic.API;
using MPChannel = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Channel;

namespace SlimTv.TvMosaicProvider
{
  public class TvMosaicChannel : MPChannel
  {
    public string TvMosaicId { get; set; }
  }

  public class TvMosaicProvider : ITvProvider, IChannelAndGroupInfoAsync
  {
    HttpDataProvider _dvbLink;
    private readonly IDictionary<string, int> _idMapping = new ConcurrentDictionary<string, int>();
    private readonly IDictionary<IChannelGroup, List<IChannel>> _channelGroups = new ConcurrentDictionary<IChannelGroup, List<IChannel>>();
    private readonly IList<IChannel> _mpChannels = new List<IChannel>();

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
      if (_channelGroups.Any() || _mpChannels.Any())
        return true;
      DVBLinkResponse<Channels> channels = await _dvbLink.GetChannels(new ChannelsRequest());
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

      var favorites = await _dvbLink.GetFavorites(new FavoritesRequest());

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
  }
}
