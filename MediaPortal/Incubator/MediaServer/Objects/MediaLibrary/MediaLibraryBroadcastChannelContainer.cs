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

using System;
using MediaPortal.Common;
using MediaPortal.Extensions.MediaServer.Objects.Basic;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryBroadcastChannelContainer : BasicContainer
  {
    public MediaLibraryBroadcastChannelContainer(string id, string title, EndPointSettings client)
      : base(id, client)
    {
      Title = title;
    }

    public override void Initialise()
    {
      if (ServiceRegistration.IsRegistered<ITvProvider>())
      {
        LiveTvMediaItem mediaItem = null;
        if (ServiceRegistration.IsRegistered<IMediaAnalyzer>())
        {
          IMediaAnalyzer analyzer = ServiceRegistration.Get<IMediaAnalyzer>() as IMediaAnalyzer;
          var analysis = analyzer.ParseChannelStreamAsync(ChannelId, mediaItem).Result;
          if (analysis == null)
          {
            Logger.Error("MediaServer: Error analyzing channel {0} stream", ChannelId);
            return;
          }
        }

        IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
        var res = channelAndGroupInfo.GetChannelAsync(ChannelId).Result;
        if (res.Success)
        {
          IChannel channel = res.Result;
          try
          {
            if (channel.MediaType == MediaType.TV)
            {
              if (mediaItem != null)
              {
                Add(new MediaLibraryVideoBroadcastItem(mediaItem, channel.Name, channel.ChannelId, Client));
              }
            }
            else if (channel.MediaType == MediaType.Radio)
            {
              if (mediaItem != null)
              {
                Add(new MediaLibraryAudioBroadcastItem(mediaItem, channel.Name, channel.ChannelId, Client));
              }
            }
          }
          catch (Exception ex)
          {
            Logger.Error("MediaServer: Error analyzing channel {0}", ex, ChannelId);
            return;
          }
        }
      }
    }

    public int ChannelId { get; set; }
  }
}
