#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System;
using System.Collections.Generic;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MediaServer.DLNA;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MediaServer.Objects.MediaLibrary;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.MediaServer.ResourceAccess
{
  static class StreamControl
  {
    private static readonly Dictionary<string, StreamItem> _streamItems = new Dictionary<string, StreamItem>();

    /// <summary>
    /// Returns a DLNA media item based on the given client and request
    /// </summary>
    /// <returns>Returns the requested DLNA media item otherwise null</returns>
    internal static DlnaMediaItem GetStreamMedia(EndPointSettings client, Uri uri)
    {
      Guid mediaItemGuid = Guid.Empty;
      DlnaMediaItem dlnaItem = null;
      StreamItem stream = new StreamItem();
      stream.ClientIp = uri.Host;
      bool isTV = false;
      int channel = 0;
      if (DlnaResourceAccessUtils.ParseMediaItem(uri, out mediaItemGuid))
      {
        isTV = true;
        if (!DlnaResourceAccessUtils.ParseTVChannel(uri, out channel))
        {
          isTV = false;
          DlnaResourceAccessUtils.ParseRadioChannel(uri, out channel);
        }
      }

      if (mediaItemGuid == Guid.Empty)
      {
        lock (_streamItems)
        {
          if (_streamItems.ContainsKey(client.ClientId) == false)
          {
            throw new BadRequestException(string.Format("Illegal request syntax. Correct syntax is '{0}'", DlnaResourceAccessUtils.SYNTAX));
          }
          else
          {
            mediaItemGuid = _streamItems[client.ClientId].RequestedMediaItem;
            if (mediaItemGuid == Guid.Empty)
            {
              throw new BadRequestException(string.Format("Illegal request syntax. Correct syntax is '{0}'", DlnaResourceAccessUtils.SYNTAX));
            }
            Logger.Debug("StreamControl: Attempting to reload last mediaitem {0}", mediaItemGuid.ToString());
          }
        }
      }

      if (client.DlnaMediaItems.ContainsKey(mediaItemGuid) == false)
      {
        // Attempt to grab the media item from the database.
        MediaItem item = MediaLibraryHelper.GetMediaItem(mediaItemGuid);
        if (item == null)
          throw new BadRequestException(string.Format("Media item '{0}' not found.", mediaItemGuid));

        dlnaItem = client.GetDlnaItem(item, false);
      }
      else
      {
        dlnaItem = client.DlnaMediaItems[mediaItemGuid];
      }

      if (dlnaItem == null)
        throw new BadRequestException(string.Format("DLNA media item '{0}' not found.", mediaItemGuid));

      if (channel > 0)
      {
        if (isTV == true) stream.Title = "Live TV";
        else stream.Title = "Live Radio";
        stream.IsLive = true;
        stream.LiveChannelId = channel;
      }
      else
      {
        stream.Title = (string)dlnaItem.MediaSource[MediaAspect.Metadata].GetAttributeValue(MediaAspect.ATTR_TITLE);
        stream.IsLive = false;
        stream.LiveChannelId = 0;
      }
      stream.RequestedMediaItem = mediaItemGuid;
      stream.TranscoderObject = dlnaItem;

      lock (_streamItems)
      {
        if (_streamItems.ContainsKey(client.ClientId) == false)
        {
          _streamItems.Add(client.ClientId, null);
        }
        _streamItems[client.ClientId] = stream;
      }
      return dlnaItem;
    }

    /// <summary>
    /// Returns a stream item based on the given client
    /// </summary>
    /// <returns>Returns the requested stream item otherwise null</returns>
    internal static StreamItem GetStreamItem(EndPointSettings client)
    {
      if (ValidateIdentifier(client))
      {
        return _streamItems[client.ClientId];
      }
      return null;
    }

    internal static bool ValidateIdentifier(EndPointSettings client)
    {
      return _streamItems.ContainsKey(client.ClientId);
    }

    /// <summary>
    /// Does the preparation to start a stream
    /// </summary>
    internal static void StartStreaming(EndPointSettings client, double startTime, double lengthTime)
    {
      if (ValidateIdentifier(client))
      {
        lock (_streamItems[client.ClientId].BusyLock)
        {
          if (_streamItems[client.ClientId].TranscoderObject == null) return;
          if (_streamItems[client.ClientId].TranscoderObject.StartTrancoding() == false)
          {
            Logger.Debug("StreamControl: Transcoding busy for mediaitem {0}", _streamItems[client.ClientId].RequestedMediaItem);
            return;
          }

          _streamItems[client.ClientId].TranscoderObject.StartStreaming();
          if (_streamItems[client.ClientId].IsLive == true)
          {
            _streamItems[client.ClientId].StreamContext = MediaConverter.GetLiveStream(client.ClientId, _streamItems[client.ClientId].TranscoderObject.TranscodingParameter, _streamItems[client.ClientId].LiveChannelId, true);
          }
          else
          {
            _streamItems[client.ClientId].StreamContext = MediaConverter.GetMediaStream(client.ClientId, _streamItems[client.ClientId].TranscoderObject.TranscodingParameter, startTime, lengthTime, true);
          }
          _streamItems[client.ClientId].StreamContext.InUse = true;
          _streamItems[client.ClientId].IsActive = true;
          _streamItems[client.ClientId].TranscoderObject.TranscodingContext = _streamItems[client.ClientId].StreamContext;
        }
      }
    }

    /// <summary>
    /// Stops the streaming
    /// </summary>
    internal static void StopStreaming(EndPointSettings client)
    {
      if (ValidateIdentifier(client))
      {
        lock (_streamItems[client.ClientId].BusyLock)
        {
          _streamItems[client.ClientId].IsActive = false;
          if (_streamItems[client.ClientId].TranscoderObject != null)
            _streamItems[client.ClientId].TranscoderObject.StopStreaming();

          if (_streamItems[client.ClientId].StreamContext != null)
            _streamItems[client.ClientId].StreamContext.InUse = false;
        }
      }
    }

    internal static IMediaConverter MediaConverter
    {
      get { return ServiceRegistration.Get<IMediaConverter>(); }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
