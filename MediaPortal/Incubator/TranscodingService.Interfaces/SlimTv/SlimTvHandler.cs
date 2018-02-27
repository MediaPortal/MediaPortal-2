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
using System.Threading;
using System.Linq;
using MediaPortal.Plugins.Transcoding.Interfaces.MetaData;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace MediaPortal.Plugins.Transcoding.Interfaces.SlimTv
{
  public class SlimTvHandler : IDisposable
  {
    private class ChannelInfo
    {
      public int ChannelId;
      public int Slot;
      public TranscodeChannel Channel;
    }

    private readonly ILogger _logger = null;
    private readonly SemaphoreSlim _slotLock = new SemaphoreSlim(1, 1);
    private readonly ConcurrentDictionary<string, ChannelInfo> _clientChannels = new ConcurrentDictionary<string, ChannelInfo>();
    private readonly Dictionary<int, int> _slotChannels = new Dictionary<int, int>()
    {
      { 1, 0 },
      { 2, 0 },
      { 3, 0 },
      { 4, 0 },
      { 5, 0 },
      { 6, 0 },
      { 7, 0 },
      { 8, 0 },
      { 9, 0 } 
      //Slot 10 and above not supported
    };

    public SlimTvHandler()
    {
      _logger = ServiceRegistration.Get<ILogger>();
    }

    private async Task<int> LockChannelAsync(int channelId)
    {
      try
      {
        await _slotLock.WaitAsync().ConfigureAwait(false);
        try
        {
          int slot = _slotChannels.Where(s => s.Value == 0).Select(s => s.Key).FirstOrDefault();
          if (slot > 0)
            return slot;
          return -1;
        }
        finally
        {
          _slotLock.Release();
        }
      }
      catch (Exception ex)
      {
        _logger.Error("SlimTvHandler: Error locking channel {0}", ex, channelId);
        return -1;
      }
    }

    private async Task ReleaseChannelAsync(int channelId)
    {
      try
      {
        await _slotLock.WaitAsync().ConfigureAwait(false);
        try
        {
          int slot = _slotChannels.Where(s => s.Value == channelId).Select(s => s.Key).FirstOrDefault();
          if (slot > 0)
            _slotChannels[slot] = 0;
        }
        finally
        {
          _slotLock.Release();
        }
      }
      catch (Exception ex)
      {
        _logger.Error("SlimTvHandler: Error releasing channel {0}", ex, channelId);
      }
    }

    public Task<IResourceAccessor> GetAnalysisAccessorAsync(int ChannelId)
    {
      try
      {
        var client = _clientChannels.FirstOrDefault(c => c.Value.ChannelId == ChannelId);
        if (client.Value?.Channel != null)
        {
          var resourcePath = ResourcePath.Deserialize(client.Value.Channel.MediaItem.PrimaryProviderResourcePath());
          return Task.FromResult(SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path));
        }
        return null;
      }
      catch (Exception ex)
      {
        _logger.Error("SlimTvHandler: Error getting analysis accessor for channel {0}", ex, ChannelId);
        return null;
      }
    }

    public Task<IResourceAccessor> GetDefaultAccessorAsync(int ChannelId)
    {
      try
      {
        var client = _clientChannels.FirstOrDefault(c => c.Value.ChannelId == ChannelId);
        if (client.Value?.Channel != null)
        {
          var resourcePath = ResourcePath.Deserialize(client.Value.Channel.MediaItem.PrimaryProviderResourcePath());
          IResourceAccessor stra = SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path);
          return Task.FromResult(stra);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("SlimTvHandler: Error getting default accessor for channel {0}", ex, ChannelId);
      }
      return Task.FromResult<IResourceAccessor>(null);
    }

    public async Task<(bool Success, MediaItem LiveMediaItem)> StartTuningAsync(string ClientId, int ChannelId)
    {
      try
      {
        var client = _clientChannels.FirstOrDefault(c => c.Value.ChannelId == ChannelId);
        if(client.Value?.Channel?.MediaItem != null)
        {
          //Check if already streaming
          if(client.Key == ClientId)
            return (true, client.Value.Channel.MediaItem);

          //Use same stream url as other channel
          if (!_clientChannels.TryAdd(ClientId, client.Value))
            return (false, null);
          else
            return (true, client.Value.Channel.MediaItem);
        }

        if (ServiceRegistration.IsRegistered<ITvProvider>())
        {
          IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
          var channelResult = await channelAndGroupInfo.GetChannelAsync(ChannelId).ConfigureAwait(false);
          if (!channelResult.Success)
            return (false, null);

          ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
          int slot = await LockChannelAsync(ChannelId).ConfigureAwait(false);
          var mediaItem = (await timeshiftControl.StartTimeshiftUrlAsync(ClientId, slot, channelResult.Result).ConfigureAwait(false));
          if (!mediaItem.Success)
          {
            _logger.Error("SlimTvHandler: Couldn't start timeshifting for channel {0}", ChannelId);
            await ReleaseChannelAsync(ChannelId).ConfigureAwait(false);
            return (false, null);
          }

          try
          {
            //Initiate channel cache
            ChannelInfo newChannel = new ChannelInfo
            {
              Channel = new TranscodeChannel(),
              ChannelId = ChannelId,
              Slot = slot
            };
            if (!_clientChannels.TryAdd(ClientId, newChannel))
            {
              await timeshiftControl.StopTimeshiftAsync(ClientId, slot);
              return (false, null);
            }

            newChannel.Channel.SetChannel(mediaItem.Result);
          }
          catch
          {
            _clientChannels.TryRemove(ClientId, out ChannelInfo c);
            await timeshiftControl.StopTimeshiftAsync(ClientId, slot);
            throw;
          }

          //Allow channel content to become available or stream analysis/transoding will fail
          await Task.Delay(5000).ConfigureAwait(false);
          return (true, mediaItem.Result);
        }

        return (false, null);
      }
      catch (Exception ex)
      {
        _logger.Error("SlimTvHandler: Error starting tuning of channel {0}", ex, ChannelId);
        return (false, null);
      }
    }

    public async Task<bool> EndTuningAsync(string ClientId)
    {
      try
      {
        if (!_clientChannels.TryRemove(ClientId, out ChannelInfo channel))
          return false;

        if (channel != null && !_clientChannels.Any(c => c.Value?.ChannelId == channel.ChannelId))
        {
          ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
          if (!(await timeshiftControl.StopTimeshiftAsync(ClientId, channel.Slot).ConfigureAwait(false)))
          {
            _logger.Error("SlimTvHandler: Couldn't stop timeshifting for channel {0}", channel.ChannelId);
            return false;
          }
          await ReleaseChannelAsync(channel.ChannelId).ConfigureAwait(false);
        }

        return true;
      }
      catch (Exception ex)
      {
        _logger.Error("SlimTvHandler: Error ending tuning for client {0}", ex, ClientId);
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
            List<int> stoppedSlots = new List<int>();
            if (client.Value != null && !stoppedSlots.Contains(client.Value.Slot) && client.Value.Slot > 0)
            {
              stoppedSlots.Add(client.Value.Slot);
              timeshiftControl.StopTimeshiftAsync(client.Key, client.Value.Slot).Wait();
            }
            client.Value?.Channel?.Dispose();
          }
        }
      }
      catch
      {}
    }
  }
}
