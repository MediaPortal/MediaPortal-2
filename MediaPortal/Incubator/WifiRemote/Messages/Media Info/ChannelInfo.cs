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

using MediaPortal.Common.FanArt;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.WifiRemote.Messages.MediaInfo;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class ChannelInfo : IAdditionalMediaInfo
  {
    public string MediaType { get; }
    public string Id => ChannelId.ToString();
    public int MpMediaType => (int)MpMediaTypes.Tv;
    public int MpProviderId => (int)MpProviders.MPTvServer;

    /// <summary>
    /// ID of the channel
    /// </summary>
    public int ChannelId { get; set; }
    /// <summary>
    /// Name of the channel
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Channel thumb
    /// </summary>
    public string ImageName { get; set; }

    public ChannelInfo(IChannel channel)
    {
      ChannelId = channel.ChannelId;
      Name = channel.Name;
      if (channel.MediaType == SlimTv.Interfaces.Items.MediaType.TV)
      {
        MediaType = "tv";
        ImageName = Helper.GetImageBaseURL(channel, FanArtMediaTypes.ChannelTv);
      }
      else
      {
        MediaType = "radio";
        ImageName = Helper.GetImageBaseURL(channel, FanArtMediaTypes.ChannelRadio);
      }
    }
  }
}
