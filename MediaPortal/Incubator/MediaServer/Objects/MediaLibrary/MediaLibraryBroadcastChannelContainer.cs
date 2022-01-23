#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryBroadcastChannelContainer : BasicContainer
  {
    public MediaLibraryBroadcastChannelContainer(string id, string title, EndPointSettings client)
      : base(id, client)
    {
      Title = title;
    }

    public override void Initialise(string sortCriteria, uint? offset = null, uint? count = null)
    {
      base.Initialise(sortCriteria, offset, count);

      if (ServiceRegistration.IsRegistered<ITvProvider>())
      {
        IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
        var res = channelAndGroupInfo?.GetChannelAsync(ChannelId).Result;
        if (res?.Success ?? false)
        {
          var dlnaItem = Client.GetLiveDlnaItem(ChannelId);
          if (dlnaItem == null)
          {
            var mediaItem = Client.StoreLiveDlnaItem(ChannelId);
            if (mediaItem == null)
            {
              Logger.Error("MediaServer: Error analyzing channel {0} stream", ChannelId);
              return;
            }
          }

          IChannel channel = res.Result;
          try
          {
            if (channel.MediaType == MediaType.TV)
            {
              Add(new MediaLibraryVideoBroadcastItem(channel.Name, channel.ChannelId, Client));
            }
            else if (channel.MediaType == MediaType.Radio)
            {
              Add(new MediaLibraryAudioBroadcastItem(channel.Name, channel.ChannelId, Client));
            }
          }
          catch (Exception ex)
          {
            Logger.Error("MediaServer: Error analyzing channel {0}", ex, ChannelId);
          }
        }
      }
    }

    public int ChannelId { get; set; }
  }
}
