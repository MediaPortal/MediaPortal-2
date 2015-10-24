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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Plugins.MediaServer.DLNA;
using MediaPortal.Plugins.MediaServer.ResourceAccess;
using UPnP.Infrastructure.Utils;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Utilities.Network;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using System.IO;
using MediaPortal.Plugins.MediaServer.Profiles;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
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

      Size = ulong.MinValue;
      BitRate = uint.MinValue;
      SampleFrequency = uint.MinValue;
      BitsPerSample = uint.MinValue;
      NumberOfAudioChannels = uint.MinValue;
      ColorDepth = uint.MinValue;
    }

    public string Uri { get; set; }

    public string ProtocolInfo { get; set; }

    public ulong Size { get; set; }

    public string Duration { get; set; }

    public uint BitRate { get; set; }

    public uint SampleFrequency { get; set; }

    public uint BitsPerSample { get; set; }

    public uint NumberOfAudioChannels { get; set; }

    public string Resolution { get; set; }

    public uint ColorDepth { get; set; }

    public string Protection { get; set; }

    public string ImportUri { get; set; }

    public string DlnaIfoFileUrl { get; set; }

    public string PacketVideoSubtitleType { get; set; }

    public string PacketVideoSubtitleUri { get; set; }
  }
}
