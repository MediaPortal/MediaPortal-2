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
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Extensions.MediaServer.ResourceAccess;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryLiveResource : IDirectoryResource
  {
    public int ChannelId { get; private set; }
    public string Key { get; private set; }
    public EndPointSettings Client { get; private set; }

    public MediaLibraryLiveResource(string key, int channelId, EndPointSettings client)
    {
      Key = key;
      ChannelId = channelId;
      Client = client;
    }

    public void Initialise()
    {
      DlnaMediaItem dlnaItem = Client.GetLiveDlnaItem(ChannelId);
      var url = DlnaResourceAccessUtils.GetBaseResourceURL() + DlnaResourceAccessUtils.GetResourceUrl(Key + (dlnaItem.IsSegmented ? "/playlist.m3u8" : ""), Client.ClientId);

      BitRate = null;
      SampleFrequency = null;
      NumberOfAudioChannels = null;
      Size = null;
      BitsPerSample = null;
      ColorDepth = null;
      Duration = null;
      var dlnaProtocolInfo = DlnaProtocolInfoFactory.GetProfileInfo(dlnaItem, Client.Profile.ProtocolInfo);
      if (dlnaProtocolInfo != null)
        ProtocolInfo = dlnaProtocolInfo.ToString();
      if (dlnaItem.Metadata == null)
      {
        throw new DlnaAspectMissingException("No DLNA metadata found for MediaItem " + dlnaItem.MediaItemId);
      }
      if (dlnaItem.IsImage == false)
      {
        if (dlnaItem.Metadata.Bitrate > 0)
          BitRate = Convert.ToUInt32((double)dlnaItem.Metadata.Bitrate / 8.0);
        if (dlnaItem.Audio[0].Frequency > 0)
          SampleFrequency = Convert.ToUInt32(dlnaItem.Audio[0].Frequency);
        if (dlnaItem.Audio[0].Channels > 0)
          NumberOfAudioChannels = Convert.ToUInt32(dlnaItem.Audio[0].Channels);
      }
      if(dlnaItem.IsVideo == true)
      {
        Resolution = dlnaItem.Video.Width + "x" + dlnaItem.Video.Height;
      }
      else if (dlnaItem.IsImage == true)
      {
        Resolution = dlnaItem.Image.Width + "x" + dlnaItem.Image.Height;
      }

      Uri = url;
    }

    public string Uri { get; set; }

    public ulong? Size { get; set; }

    public string Duration { get; set; }

    public uint? BitRate { get; set; }

    public uint? SampleFrequency { get; set; }

    public uint? BitsPerSample { get; set; }

    public uint? NumberOfAudioChannels { get; set; }

    public string Resolution { get; set; }

    public uint? ColorDepth { get; set; }

    public string ProtocolInfo { get; set; }

    public string Protection { get; set; }

    public string ImportUri { get; set; }

    public string DlnaIfoFileUrl { get; set; }

    public string PacketVideoSubtitleType { get; set; }

    public string PacketVideoSubtitleUri { get; set; }
  }
}
