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
using MediaPortal.Plugins.MediaServer.Objects;
using MediaPortal.Plugins.MediaServer.Objects.MediaLibrary;

namespace MediaPortal.Plugins.MediaServer.DIDL
{
  public class PacketVideoDidlMessageBuilder : GenericDidlMessageBuilder
  {
    //<DIDL-Lite xmlns:dc="http://purl.org/dc/elements/1.1/"
    //xmlns:upnp="urn:schemas-upnp-org:metadata-1-0/upnp/"
    //xmlns="urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/"
    //xmlns:pv="http://www.pv.com/pvns/"> 

    //<item id="64$1$4$0" parentID="64$1$4" restricted="1">
    //<dc:title>Cube (1997)</dc:title>
    //<upnp:class>object.item.videoItem</upnp:class>
    //<dc:date>2011-07-05T16:23:42</dc:date>

    //<res protocolInfo="http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_TN">
    //http://192.168.5.251:8200/AlbumArt/7-35.jpg
    //</res>

    //<res size="397 2493312" duration="1:30:17.120" resolution="1280x704"
    //pv:subtitleFileType="SRT"
    //pv:subtitleFileUri="http://192.168.5.251:8200/Captions/35.srt"
    //protocolInfo="http-get:*:video/x-matroska:DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01700000000000000000000000000000">
    //http://192.168.5.251:8200/MediaItems/35.mkv
    //</res>

    //<res duration="1:30:17.120" resolution="720x480"
    //protocolInfo="http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_NTSC;DLNA.ORG_OP=10;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=01500000000000000000000000000000">
    //http://192.168.5.251:8200/MediaItems/TranscodeVideo/NTSC/35.mkv
    //</res>

    //<res duration="1:30:17.120" resolution="720x576"
    //protocolInfo="http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_PS_PAL;DLNA.ORG_OP=10;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=01500000000000000000000000000000">
    //http://192.168.5.251:8200/MediaItems/TranscodeVideo/PAL/35.mkv
    //</res>
    //</item>

    //</DIDL-Lite>

    public PacketVideoDidlMessageBuilder()
    {
      AddMessageAttribute("xmlns", "pv", null, "http://www.pv.com/pvns/");
    }

    protected override void BuildInternal(object directoryPropertyObject)
    {
      if (!_hasCompleted)
      {
        if (directoryPropertyObject is IDirectoryContainer)
        {
          _xml.WriteStartElement("container");
        }
        else if (directoryPropertyObject is IDirectoryItem)
        {
          _xml.WriteStartElement("item");
        }
        else
        {
          throw new ArgumentException("directoryPropertyObject isn't either an IDirectoryContainer or IDirectoryItem");
        }

        if (directoryPropertyObject is MediaLibraryVideoItem)
        {
          try
          {
            MediaLibraryVideoItem res = (MediaLibraryVideoItem)directoryPropertyObject;
            IDirectoryResource subRes = null;
            IDirectoryResource movieRes = null;
            foreach (var embeddedRes in res.Resources)
            {
              if (embeddedRes is MediaLibrarySubtitleResource)
              {
                subRes = embeddedRes;
              }
              else if (embeddedRes is MediaLibraryResource)
              {
                movieRes = embeddedRes;
              }
            }
            if (subRes != null && movieRes != null)
            {
              movieRes.PacketVideoSubtitleType = "SRT";
              movieRes.PacketVideoSubtitleUri = subRes.Uri;
            }
          }
          catch (Exception ex)
          {
            Logger.Warn("DlnaMediaServer: Cannot resolve resource elements for injection of subtitles information:\n {0}", ex.Message);
          }
        }

        ObjectRenderer.Render(_filter, directoryPropertyObject, _xml);
        _xml.WriteEndElement();
      }
    }
  }
}
