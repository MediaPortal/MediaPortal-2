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
using System.Net;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MediaServer.DLNA;
using MediaPortal.Plugins.MediaServer.ResourceAccess;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.Transcoding.Service;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryResource : IDirectoryResource
  {
    public MediaItem Item { get; private set; }
    public EndPointSettings Client { get; private set; }

    public MediaLibraryResource(MediaItem item, EndPointSettings client)
    {
      Item = item;
      Client = client;
    }

    public void Initialise()
    {
      DlnaMediaItem dlnaItem = Client.GetDlnaItem(Item, false);
      var url = DlnaResourceAccessUtils.GetBaseResourceURL() + DlnaResourceAccessUtils.GetResourceUrl(Item.MediaItemId);
      if (dlnaItem.IsSegmented == true)
      {
        url += "/playlist.m3u8";
      }

      BitRate = uint.MinValue;
      SampleFrequency = uint.MinValue;
      NumberOfAudioChannels = uint.MinValue;
      Size = ulong.MinValue;
      BitsPerSample = uint.MinValue;
      ColorDepth = uint.MinValue;
      Duration = null;
      var dlnaProtocolInfo = DlnaProtocolInfoFactory.GetProfileInfo(dlnaItem, Client.Profile.ProtocolInfo).ToString();
      if (dlnaProtocolInfo != null)
        ProtocolInfo = dlnaProtocolInfo.ToString();
      if (dlnaItem.DlnaMetadata.Metadata.Size > 0)
      {
        Size = Convert.ToUInt64(dlnaItem.DlnaMetadata.Metadata.Size);
      }
      if (dlnaItem.IsImage == false)
      {
        if (dlnaItem.DlnaMetadata.Metadata.Bitrate > 0)
          BitRate = Convert.ToUInt32((double)dlnaItem.DlnaMetadata.Metadata.Bitrate / 8.0);
        if (dlnaItem.DlnaMetadata.Audio[0].Frequency > 0)
          SampleFrequency = Convert.ToUInt32(dlnaItem.DlnaMetadata.Audio[0].Frequency);
        if (dlnaItem.DlnaMetadata.Audio[0].Channels > 0)
          NumberOfAudioChannels = Convert.ToUInt32(dlnaItem.DlnaMetadata.Audio[0].Channels);
        if(dlnaItem.DlnaMetadata.Metadata.Duration > 0)
          Duration = TimeSpan.FromSeconds(dlnaItem.DlnaMetadata.Metadata.Duration).ToString(@"hh\:mm\:ss\.fff");
      }
      if(dlnaItem.IsVideo == true)
      {
        Resolution = dlnaItem.DlnaMetadata.Video.Width + "x" + dlnaItem.DlnaMetadata.Video.Height;
      }
      else if (dlnaItem.IsImage == true)
      {
        Resolution = dlnaItem.DlnaMetadata.Image.Width + "x" + dlnaItem.DlnaMetadata.Image.Height;
      }

      Uri = url;
    }

    public string Uri { get; set; }

    public ulong Size { get; set; }

    public string Duration { get; set; }

    public uint BitRate { get; set; }

    public uint SampleFrequency { get; set; }

    public uint BitsPerSample { get; set; }

    public uint NumberOfAudioChannels { get; set; }

    public string Resolution { get; set; }

    public uint ColorDepth { get; set; }

    public string ProtocolInfo { get; set; }

    public string Protection { get; set; }

    public string ImportUri { get; set; }

    public string DlnaIfoFileUrl { get; set; }

    public string PacketVideoSubtitleType { get; set; }

    public string PacketVideoSubtitleUri { get; set; }
  }
}
