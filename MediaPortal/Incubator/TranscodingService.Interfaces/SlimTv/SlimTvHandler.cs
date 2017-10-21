#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using System.Text;
using System;
using System.Threading;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Plugins.Transcoding.Interfaces.Helpers;

namespace MediaPortal.Plugins.Transcoding.Interfaces.SlimTv
{
  public class SlimTvHandler : IDisposable
  {
    private ILogger _logger = null;
    private Dictionary<string, int> _clients = new Dictionary<string, int>();
    private Dictionary<int, Channel> _channels = new Dictionary<int, Channel>();
    private Dictionary<string, int> _timeShiftings = new Dictionary<string, int>();
    private Dictionary<int, int> _slotChannels = new Dictionary<int, int>()
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

    private int LockChannel(int channelId)
    {
      lock(_slotChannels)
      {
        int slot = 2;
        while(_slotChannels.ContainsKey(slot))
        {
          if (_slotChannels[slot] == 0)
          {
            _slotChannels[slot] = channelId;
            return slot;
          }
          slot++;
        }
        return -1;
      }
    }

    private int GetChannelSlot(int channelId)
    {
      lock (_slotChannels)
      {
        int slot = 2;
        while (_slotChannels.ContainsKey(slot))
        {
          if (_slotChannels[slot] == channelId)
          {
            return _slotChannels[slot];
          }
          slot++;
        }
      }
      return -1;
    }

    private void ReleaseChannel(int channelId)
    {
      lock (_slotChannels)
      {
        int slot = 2;
        while (_slotChannels.ContainsKey(slot))
        {
          if (_slotChannels[slot] == channelId)
          {
            _slotChannels[slot] = 0;
            return;
          }
          slot++;
        }
      }
    }

    public IResourceAccessor GetAnalysisAccessor(int ChannelId)
    {
      lock(_channels)
      {
        if (_channels.ContainsKey(ChannelId))
        {
          string resourcePathStr = (string)MediaItemHelper.GetAttributeValue(_channels[ChannelId].MetaData.Aspects, ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
          var resourcePath = ResourcePath.Deserialize(resourcePathStr);
          IResourceAccessor stra = SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path);
          if (stra is ILocalFsResourceAccessor)
          {
            string masterFile = ((ILocalFsResourceAccessor)stra).LocalFileSystemPath;
            string path = Path.GetDirectoryName(masterFile);

            FileStream file = File.Open(masterFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] buffer = new byte[file.Length];
            file.Read(buffer, 0, buffer.Length);
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
                _logger.Debug("SlimTvHandler: timed out while waiting for buffer file to become available");
                return null;
              }
              Thread.Sleep(1);
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
      }
      return null;
    }

    public IResourceAccessor GetDefaultAccessor(int ChannelId)
    {
      lock (_channels)
      {
        if (_channels.ContainsKey(ChannelId))
        {
          string resourcePathStr = (string)MediaItemHelper.GetAttributeValue(_channels[ChannelId].MetaData.Aspects, ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
          var resourcePath = ResourcePath.Deserialize(resourcePathStr);
          IResourceAccessor stra = SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path);
          return stra;
        }
        return null;
      }
    }

    public bool AttachConverterStreamHook(string ClientId, Stream LiveStreamHook)
    {
      int channelId = 0;
      lock (_channels)
      {
        if (_clients.ContainsKey(ClientId) == false) return false;
        channelId = _clients[ClientId];
        if (_channels.ContainsKey(channelId))
        {
          string resourcePathStr = (string)MediaItemHelper.GetAttributeValue(_channels[channelId].MetaData.Aspects, ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
          var resourcePath = ResourcePath.Deserialize(resourcePathStr);
          IResourceAccessor stra = SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path);
          if (stra is INetworkResourceAccessor)
          {
            return false;
          }
        }

        lock (_channels[channelId].ClientStreams)
        {
          if (_channels[channelId].ClientStreams.ContainsKey(ClientId) == false)
          {
            _channels[channelId].ClientStreams.Add(ClientId, LiveStreamHook);
            return true;
          }
          return false;
        }
      }
    }

    public bool StartTuning(string ClientId, int ChannelId, out MediaItem LiveMediaItem)
    {
      LiveMediaItem = null;
      lock (_channels)
      {
        if (_channels.ContainsKey(ChannelId))
        {
          //Channel already tuned
          if (_channels[ChannelId].Clients.Contains(ClientId))
          {
            //Client already streaming
            LiveMediaItem = _channels[ChannelId].MetaData;
            return true;
          }
          else
          {
            //Initiate client stream
            _channels[ChannelId].Clients.Add(ClientId);
            _clients.Add(ClientId, ChannelId);
            LiveMediaItem = _channels[ChannelId].MetaData;
            return true;
          }
        }

        if (ServiceRegistration.IsRegistered<ITvProvider>())
        {
          IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
          IChannel channel;
          if (channelAndGroupInfo.GetChannel(ChannelId, out channel))
          {
            ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;

            int slot = LockChannel(ChannelId);
            if (!timeshiftControl.StartTimeshift(ClientId, slot, channel, out LiveMediaItem))
            {
              _logger.Error("SlimTvHandler: Couldn't start timeshifting for channel {0}", ChannelId);
              ReleaseChannel(ChannelId);
              return false;
            }

            //Initiate channel cache
            _channels.Add(ChannelId, new Channel());
            _channels[ChannelId].Clients.Add(ClientId);
            _channels[ChannelId].MetaData = LiveMediaItem;
            _clients.Add(ClientId, ChannelId);
            _timeShiftings.Add(ClientId, slot);

            string resourcePathStr = (string)MediaItemHelper.GetAttributeValue(LiveMediaItem.Aspects, ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
            var resourcePath = ResourcePath.Deserialize(resourcePathStr);
            IResourceAccessor stra = SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path);
            if (stra is ILocalFsResourceAccessor)
            {
              using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(stra.CanonicalLocalResourcePath))
              {
                MultiFileReader reader = new MultiFileReader(true);
                reader.SetFileAccessor((ILocalFsResourceAccessor)stra);
                _channels[ChannelId].FileReader = reader;
                ThreadPool.QueueUserWorkItem(new WaitCallback(ChannelStreamReader), ChannelId);
              }
            }
            //Allow channel content to become available or stream analysis/transoding will fail
            Thread.Sleep(5000);
            return true;
          }
        }
        return false;
      }
    }

    public bool EndTuning(string ClientId)
    {
      lock (_channels)
      {
        int channelId = 0;
        try
        {
          if (_clients.ContainsKey(ClientId) == false) return true;
          channelId = _clients[ClientId];
          if (_channels.ContainsKey(channelId) == false) return true;
          _channels[channelId].Clients.Remove(ClientId);
          _clients.Remove(ClientId);
          lock (_channels[channelId].ClientStreams)
          {
            if (_channels[channelId].ClientStreams.ContainsKey(ClientId))
            {
              _channels[channelId].ClientStreams[ClientId].Close();
              _channels[channelId].ClientStreams.Remove(ClientId);
            }
          }
        }
        catch(Exception ex)
        {
          _logger.Error("SlimTvHandler: Couldn't remove client {0}", ex, ClientId);
        }

        if (_channels.ContainsKey(channelId) && _channels[channelId].Clients.Count == 0)
        {
          int slot = GetChannelSlot(channelId);
          try
          {
            LiveTvMediaItem mediaItem = (LiveTvMediaItem)_channels[channelId].MetaData;
            slot = (int)mediaItem.AdditionalProperties[LiveTvMediaItem.SLOT_INDEX];
            if (_channels[channelId].FileReader != null)
            {
              _channels[channelId].FileReader.CloseFile();
            }
            _channels.Remove(channelId);
          }
          catch (Exception ex)
          {
            _logger.Error("SlimTvHandler: Couldn't remove channel {0}", ex, channelId);
          }

          ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
          if (!timeshiftControl.StopTimeshift(ClientId, slot))
          {
            _logger.Error("SlimTvHandler: Couldn't stop timeshifting for channel {0}", channelId);
            return false;
          }
          ReleaseChannel(channelId);
          _timeShiftings.Remove(ClientId);
        }
        return true;
      }
    }

    private void ChannelStreamReader(object channel)
    {
      int channelId = (int)channel;
      byte[] buffer = new byte[1024];
      int readBytes = 0;
      try
      {
        Channel c = _channels[channelId];
        if (c.FileReader == null) return;
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor((c.FileReader.GetFileAccessor()).CanonicalLocalResourcePath))
        {
          if (c.FileReader.OpenFile() == false)
          {
            _logger.Error("SlimTvHandler: Couldn't start multi file reader for channel {0}", channelId);
            return;
          }
          while (_channels.ContainsKey(channelId))
          {
            if (c.FileReader.Read(buffer, 0, buffer.Length, out readBytes))
            {
              lock (c.ClientStreams)
              {
                if (c.ClientStreams.Keys.Count > 0 && readBytes > 0)
                {
                  foreach (Stream stream in c.ClientStreams.Values)
                  {
                    if (stream.CanWrite) stream.Write(buffer, 0, readBytes);
                  }
                }
              }
            }
          }
        }
      }
      catch(Exception ex)
      {
        _logger.Error("SlimTvHandler: Stream reading failed for channel {0}", ex, channelId);
      }
    }

    public void Dispose()
    {
      try
      {
        ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
        if (timeshiftControl != null)
        {
          foreach (string user in _timeShiftings.Keys)
          {
            timeshiftControl.StopTimeshift(user, _timeShiftings[user]);
          }
        }
      }
      catch
      {}
    }

    private class Channel
    {
      public List<string> Clients = new List<string>();
      public Dictionary<string, Stream> ClientStreams = new Dictionary<string, Stream>();
      public MultiFileReader FileReader = null;
      public MediaItem MetaData = null;
    }
  }
}
