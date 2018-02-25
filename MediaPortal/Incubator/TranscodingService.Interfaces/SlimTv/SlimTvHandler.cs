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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using System.Text;
using System;
using System.Threading;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using System.Linq;
using MediaPortal.Plugins.Transcoding.Interfaces.MetaData;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace MediaPortal.Plugins.Transcoding.Interfaces.SlimTv
{
  public class SlimTvHandler : IDisposable
  {
    private readonly ILogger _logger = null;
    private readonly SemaphoreSlim _slotLock = new SemaphoreSlim(1, 1);
    private readonly ConcurrentDictionary<int, TranscodeChannel> _channels = new ConcurrentDictionary<int, TranscodeChannel>();
    private readonly ConcurrentDictionary<string, int> _clientChannels = new ConcurrentDictionary<string, int>();
    private readonly ConcurrentDictionary<string, int> _timeShiftings = new ConcurrentDictionary<string, int>();
    private readonly Dictionary<int, int> _slotChannels = new Dictionary<int, int>()
    {
      //Slot 0 is used by client for main screen
      //Slot 1 is used by client for PIP screen
      { 2, 0 },
      { 3, 0 },
      { 4, 0 },
      { 5, 0 },
      { 6, 0 },
      { 7, 0 },
      { 8, 0 },
      { 9, 0 } 
      //Slot 10 and above not supported by SlimTv
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

    private async Task<int> GetChannelSlotAsync(int channelId)
    {
      try
      {
        await _slotLock.WaitAsync().ConfigureAwait(false);
        try
        {
          int slot = _slotChannels.Where(s => s.Value == channelId).Select(s => s.Key).FirstOrDefault();
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
        _logger.Error("SlimTvHandler: Error getting channel {0}", ex, channelId);
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

    public async Task<IResourceAccessor> GetAnalysisAccessorAsync(int ChannelId)
    {
      try
      {
        if (_channels.ContainsKey(ChannelId))
        {
          var resourcePath = ResourcePath.Deserialize(_channels[ChannelId].MetaData.PrimaryProviderResourcePath());
          IResourceAccessor stra = SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path);
          if (stra is ILocalFsResourceAccessor)
          {
            string masterFile = ((ILocalFsResourceAccessor)stra).LocalFileSystemPath;
            string path = Path.GetDirectoryName(masterFile);

            FileStream file = File.Open(masterFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] buffer = new byte[file.Length];
            await file.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            file.Close();

            int offset = 8 + 4 + 4;
            string fileNames = Encoding.Unicode.GetString(buffer, offset, buffer.Length - offset);
            string[] filenameParts = fileNames.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            string tsFileName = Path.Combine(path, Path.GetFileName(filenameParts[0]));
            DateTime tc = DateTime.Now;
            while (File.Exists(tsFileName) == false)
            {
              if ((DateTime.Now - tc).TotalMilliseconds > 2000)
              {
                _logger.Debug("SlimTvHandler: Timed out while waiting for buffer file to become available");
                return null;
              }
              await Task.Delay(5).ConfigureAwait(false);
            }

            LocalFsResourceProvider localFsResourceProvider = new LocalFsResourceProvider();
            IResourceAccessor resourceAccessor = new LocalFsResourceAccessor(localFsResourceProvider, tsFileName);
            return resourceAccessor;
          }
          else
          {
            return stra;
          }
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
        if (_channels.ContainsKey(ChannelId))
        {
          var resourcePath = ResourcePath.Deserialize(_channels[ChannelId].MetaData.PrimaryProviderResourcePath());
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

    public bool AttachConverterStreamHook(string ClientId, Stream LiveStreamHook)
    {
      int channelId = 0;
      if (!_clientChannels.TryGetValue(ClientId, out channelId))
        return false;

      TranscodeChannel channel;
      if (!_channels.TryGetValue(channelId, out channel))
        return false;

      if (!channel.Clients.TryAdd(ClientId, LiveStreamHook))
        return false;

      return true;
    }

    public async Task<(bool Success, MediaItem LiveMediaItem)> StartTuningAsync(string ClientId, int ChannelId)
    {
      try
      {
        TranscodeChannel channel;
        if (_channels.TryGetValue(ChannelId, out channel))
        {
          //Channel already tuned
          if (channel.Clients.ContainsKey(ClientId))
          {
            //Client already streaming
            return (true, channel.MetaData);
          }
          else
          {
            //Initiate client stream
            if (!channel.Clients.TryAdd(ClientId, null))
              return (false, null);

            _clientChannels.TryAdd(ClientId, ChannelId);

            return (true, channel.MetaData);
          }
        }

        if (ServiceRegistration.IsRegistered<ITvProvider>())
        {
          IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
          var channelResult = await channelAndGroupInfo.GetChannelAsync(ChannelId).ConfigureAwait(false);
          if (!channelResult.Success)
            return (false, null);

          ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
          int slot = await LockChannelAsync(ChannelId).ConfigureAwait(false);
          var mediaItem = (await timeshiftControl.StartTimeshiftAsync(ClientId, slot, channelResult.Result).ConfigureAwait(false));
          if (!mediaItem.Success)
          {
            _logger.Error("SlimTvHandler: Couldn't start timeshifting for channel {0}", ChannelId);
            await ReleaseChannelAsync(ChannelId).ConfigureAwait(false);
            return (false, null);
          }

          //Initiate channel cache
          channel = new TranscodeChannel();
          channel.Clients.TryAdd(ClientId, null);
          channel.SetChannel(mediaItem.Result);
          channel.StartStreaming();
          _channels.TryAdd(ChannelId, channel);
          _clientChannels.TryAdd(ClientId, ChannelId);
          _timeShiftings.TryAdd(ClientId, slot);

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
        int channelId = 0;
        if (!_clientChannels.TryRemove(ClientId, out channelId))
          return false;

        TranscodeChannel channel;
        if (!_channels.TryGetValue(channelId, out channel))
          return false;

        Stream stream = null;
        if (!channel.Clients.TryRemove(ClientId, out stream))
          return false;

        stream.Dispose();

        if (channel.Clients.Count == 0)
        {
          int slot = 0;
          _timeShiftings.TryRemove(ClientId, out slot);
          _channels.TryRemove(channelId, out channel);
          channel.StopStreaming();

          ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
          if (!(await timeshiftControl.StopTimeshiftAsync(ClientId, slot).ConfigureAwait(false)))
          {
            _logger.Error("SlimTvHandler: Couldn't stop timeshifting for channel {0}", channelId);
            return false;
          }
          await ReleaseChannelAsync(channelId).ConfigureAwait(false);
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
          foreach (string user in _timeShiftings.Keys)
          {
            timeshiftControl.StopTimeshiftAsync(user, _timeShiftings[user]);
          }
        }
      }
      catch
      {}
    }
  }
}
