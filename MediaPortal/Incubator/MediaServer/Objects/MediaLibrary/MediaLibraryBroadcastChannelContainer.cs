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
using MediaPortal.Common;
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
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
        MediaItem mediaItem = null;
        if (ServiceRegistration.IsRegistered<IMediaAnalyzer>())
        {
          IMediaAnalyzer analyzer = ServiceRegistration.Get<IMediaAnalyzer>() as IMediaAnalyzer;
          if(analyzer.ParseChannelStream(ChannelId, out mediaItem) == null)
          {
            Logger.Error("MediaServer: Error analyzing channel {0} stream", ChannelId);
            return;
          }
        }

        IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
        IChannel channel;
        if (channelAndGroupInfo.GetChannel(ChannelId, out channel))
        {
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
