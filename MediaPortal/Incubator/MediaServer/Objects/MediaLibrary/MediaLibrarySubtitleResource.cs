﻿#region Copyright (C) 2007-2017 Team MediaPortal

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

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibrarySubtitleResource : IDirectoryResource
  {
    public MediaLibrarySubtitle Item { get; set; }
    public string MimeType { get; set; }
    public string SubtitleType { get; set; }

    public MediaLibrarySubtitleResource(MediaLibrarySubtitle item)
    {
      Item = item;
      MimeType = item.MimeType;
      SubtitleType = item.SubtitleType;
    }

    public void Initialise()
    {
      if (string.IsNullOrEmpty(MimeType) == false)
      {
        Uri = Item.Uri;
        ProtocolInfo = "http-get:*:" + MimeType + ":*";
      }

      Size = null;
      BitRate = null;
      SampleFrequency = null;
      BitsPerSample = null;
      NumberOfAudioChannels = null;
      ColorDepth = null;
    }

    public string Uri { get; set; }

    public string ProtocolInfo { get; set; }

    public ulong? Size { get; set; }

    public string Duration { get; set; }

    public uint? BitRate { get; set; }

    public uint? SampleFrequency { get; set; }

    public uint? BitsPerSample { get; set; }

    public uint? NumberOfAudioChannels { get; set; }

    public string Resolution { get; set; }

    public uint? ColorDepth { get; set; }

    public string Protection { get; set; }

    public string ImportUri { get; set; }

    public string DlnaIfoFileUrl { get; set; }

    public string PacketVideoSubtitleType { get; set; }

    public string PacketVideoSubtitleUri { get; set; }
  }
}
