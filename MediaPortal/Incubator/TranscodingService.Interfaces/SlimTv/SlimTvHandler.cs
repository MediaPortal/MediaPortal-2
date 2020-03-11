#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using System;
using System.Linq;
using MediaPortal.Extensions.TranscodingService.Interfaces.MetaData;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using MediaPortal.Extensions.TranscodingService.Interfaces.Helpers;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.SlimTv
{
  public class SlimTvHandler : IDisposable
  {
    private class ChannelInfo
    {
      public int ChannelId;
      public int SlotIndex;
      public TranscodeChannel Channel;
    }

    private const string TV_USER_NAME = "Transcode";
    private readonly ILogger _logger = null;
    private readonly ConcurrentDictionary<string, ChannelInfo> _clientChannels = new ConcurrentDictionary<string, ChannelInfo>();

    public SlimTvHandler()
    {
      _logger = ServiceRegistration.Get<ILogger>();
    }

    private IResourceAccessor GetResolvedAccessor(ResourcePath resourcePath)
    {
      // Parse slotindex from path and cut the prefix off.
      int slotIndex;
      string path = resourcePath.BasePathSegment.Path;
      if (!Int32.TryParse(path.Substring(0, 1), out slotIndex))
      {
        _logger.Error("SlimTvHandler: Error resolving accessor for path {0}", path);
        return null;
      }
      path = path.Substring(2, path.Length - 2);
      //Resolve host first because ffprobe can hang when resolving host
      var resolvedUrl = UrlHelper.ResolveHostToIPv4Url(path);
      return SlimTvResourceProvider.GetResourceAccessor(slotIndex, resolvedUrl);
    }

    public Task<IResourceAccessor> GetAnalysisAccessorAsync(int channelId)
    {
      try
      {
        var client = _clientChannels.FirstOrDefault(c => c.Value.ChannelId == channelId);
        if (client.Value?.Channel != null)
        {
          var resourcePath = ResourcePath.Deserialize(client.Value.Channel.MediaItem.PrimaryProviderResourcePath());
          //Resolve host first because ffprobe can hang when resolving host
          var accessor = GetResolvedAccessor(resourcePath);
          return Task.FromResult(accessor);
        }
        return null;
      }
      catch (Exception ex)
      {
        _logger.Error("SlimTvHandler: Error getting analysis accessor for channel {0}", ex, channelId);
        return null;
      }
    }

    public Task<IResourceAccessor> GetDefaultAccessorAsync(int channelId)
    {
      try
      {
        var client = _clientChannels.FirstOrDefault(c => c.Value.ChannelId == channelId);
        if (client.Value?.Channel != null)
        {
          var resourcePath = ResourcePath.Deserialize(client.Value.Channel.MediaItem.PrimaryProviderResourcePath());
          //Resolve host first because ffmpeg can hang when resolving host
          var accessor = GetResolvedAccessor(resourcePath);
          return Task.FromResult(accessor);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("SlimTvHandler: Error getting default accessor for channel {0}", ex, channelId);
      }
      return Task.FromResult<IResourceAccessor>(null);
    }

    public async Task<(bool Success, MediaItem LiveMediaItem)> StartTuningAsync(string clientId, int channelId)
    {
      try
      {
        var client = _clientChannels.FirstOrDefault(c => c.Value.ChannelId == channelId);
        if(client.Value?.Channel?.MediaItem != null)
        {
          //Check if already streaming
          if(client.Key == clientId)
            return (true, client.Value.Channel.MediaItem);

          //Use same stream url as other channel
          if (!_clientChannels.TryAdd(clientId, client.Value))
            return (false, null);
          else
            return (true, client.Value.Channel.MediaItem);
        }

        if (ServiceRegistration.IsRegistered<ITvProvider>())
        {
          IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
          var channelResult = await channelAndGroupInfo.GetChannelAsync(channelId).ConfigureAwait(false);
          if (!channelResult.Success)
          {
            _logger.Error("SlimTvHandler: Couldn't find channel {0}", channelId);
            return (false, null);
          }

          var slotIndex = GetFreeSlot();
          if (slotIndex == null)
          {
            _logger.Error("SlimTvHandler: Couldn't find free slot for channel {0}", channelId);
            return (false, null);
          }

          ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
          var mediaItem = (await timeshiftControl.StartTimeshiftAsync(TV_USER_NAME, slotIndex.Value, channelResult.Result).ConfigureAwait(false));
          if (!mediaItem.Success)
          {
            _logger.Error("SlimTvHandler: Couldn't start timeshifting for channel {0}", channelId);
            return (false, null);
          }

          try
          {
            //Initiate channel cache
            ChannelInfo newChannel = new ChannelInfo
            {
              Channel = new TranscodeChannel(),
              SlotIndex = slotIndex.Value,
              ChannelId = channelId,
            };
            if (!_clientChannels.TryAdd(clientId, newChannel))
            {
              await timeshiftControl.StopTimeshiftAsync(TV_USER_NAME, slotIndex.Value);
              return (false, null);
            }

            newChannel.Channel.SetChannel(mediaItem.Result);
          }
          catch
          {
            _clientChannels.TryRemove(clientId, out ChannelInfo c);
            await timeshiftControl.StopTimeshiftAsync(TV_USER_NAME, slotIndex.Value);
            throw;
          }
          return (true, mediaItem.Result);
        }

        return (false, null);
      }
      catch (Exception ex)
      {
        _logger.Error("SlimTvHandler: Error starting tuning of channel {0}", ex, channelId);
        return (false, null);
      }
    }

    public async Task<bool> EndTuningAsync(string clientId)
    {
      try
      {
        if (!_clientChannels.TryRemove(clientId, out ChannelInfo channel))
          return false;

        if (channel != null && _clientChannels.All(c => c.Value?.ChannelId != channel.ChannelId))
        {
          ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
          if (!(await timeshiftControl.StopTimeshiftAsync(TV_USER_NAME, channel.SlotIndex).ConfigureAwait(false)))
          {
            _logger.Error("SlimTvHandler: Couldn't stop timeshifting for channel {0}", channel.ChannelId);
            return false;
          }
        }

        return true;
      }
      catch (Exception ex)
      {
        _logger.Error("SlimTvHandler: Error ending tuning for client {0}", ex, clientId);
      }
      return false;
    }

    public void Dispose()
    {
      try
      {
        if (ServiceRegistration.Get<ITvProvider>() is ITimeshiftControlEx timeshiftControl)
        {
          foreach (var client in _clientChannels)
          {
            List<int> stoppedChannels = new List<int>();
            if (client.Value != null && !stoppedChannels.Contains(client.Value.ChannelId) && client.Value.ChannelId > 0)
            {
              stoppedChannels.Add(client.Value.ChannelId);
              timeshiftControl.StopTimeshiftAsync(TV_USER_NAME, client.Value.ChannelId).Wait();
            }
            client.Value?.Channel?.Dispose();
          }
        }
      }
      catch
      {}
    }

    private int? GetFreeSlot()
    {
      var availableIndexes = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }; 
      var usedIndexes = _clientChannels.Select(c => c.Value.SlotIndex).Distinct();
      var freeIndexes = availableIndexes.Except(usedIndexes);
      if (!freeIndexes.Any())
        return null;
      return freeIndexes.First();
    }
  }
}
