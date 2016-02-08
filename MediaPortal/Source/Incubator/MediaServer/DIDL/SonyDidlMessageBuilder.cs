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
using MediaPortal.Plugins.MediaServer.Objects.Basic;

namespace MediaPortal.Plugins.MediaServer.DIDL
{
  public class SonyDidlMessageBuilder : GenericDidlMessageBuilder
  {
    //object.item.audioItem.musicTrack:
    //<item id="121000000191711100000000001200000000010107" restricted="1" parentID="11100000000001200000000010107" refID="1210000001917108">
    //<dc:title>Clocks (feat. Coldplay)</dc:title> 
    //<upnp:class>object.item.audioItem.musicTrack</upnp:class> 
    //<dc:date>2007-01-01</dc:date> 
    //<upnp:genre>Latin Jazz</upnp:genre> 
    //<upnp:album>Rhythms del Mundo Cuba</upnp:album> 
    //<upnp:artist>Ibrahim Ferrer & Omara Portuondo</upnp:artist> 
    //<res protocolInfo="http-get:*:audio/mpeg:DLNA.ORG_PN=MP3;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=01700000000000000000000000000000" size="5076992">
    //http://10.20.20.77:60151/card/Music/Ibrahim%20Ferrer%20%26%20Omara%20Portuondo%20-%20Clocks%20(feat.%20Coldplay).mp3?%21121000000191711100000000001200000000010107%21audio%2fmpeg%21%21%21%21%21
    //</res> 
    //</item> 

    //object.item.imageItem.photo:
    //<item id="12200000012351132008-05-04103" restricted="1" parentID="1132008-05-04103" refID="1220000001235104">
    //<dc:title>P1000394.JPG</dc:title> 
    //<upnp:class>object.item.imageItem.photo</upnp:class> 
    //<dc:date>2008-05-04</dc:date> 
    //<res protocolInfo="http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_LRG;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=00F00000000000000000000000000000" size="1596355" resolution="2048x1536">
    //http://10.20.20.77:60151/card/Picture/P1000394.JPG?%2112200000012351132008%2d05%2d04103%21image%2fjpeg%3aJPEG%5fLRG%21%21%21%21%21
    //</res> 
    //<res protocolInfo="http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_SM;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=00F00000000000000000000000000000" resolution="640x480">
    //http://10.20.20.77:60151/card/Picture/P1000394.JPG?%2112200000012351132008%2d05%2d04103%21image%2fjpeg%3aJPEG%5fSM%21%21%21%21%21
    //</res> 
    //<res protocolInfo="http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_TN;DLNA.ORG_OP=01;DLNA.ORG_FLAGS=00F00000000000000000000000000000" resolution="160x160">
    //http://10.20.20.77:60151/card/Picture/P1000394.JPG?%2112200000012351132008%2d05%2d04103%21image%2fjpeg%3aJPEG%5fTN%21%21%21%21%21
    //</res> 
    //</item> 

    //object.container:
    //<container id="11100000000001200000000010107" restricted="1" parentID="1200000000010107">
    //<dc:title>Alle</dc:title> 
    //<upnp:class>object.container</upnp:class> 
    //<av:mediaClass>M</av:mediaClass> 
    //<av:containerClass>musicAllItems</av:containerClass> 
    //</container>

    //MediaClass:
    //M = music/audio
    //P = Picture/image
    //V = Video

    public SonyDidlMessageBuilder()
    {
      AddMessageAttribute("xmlns", "av", null, "urn:schemas-sony-com:av");
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
        ObjectRenderer.Render(_filter, directoryPropertyObject, _xml);
        if (directoryPropertyObject is BasicContainer)
        {
          if ((directoryPropertyObject as BasicContainer).Id == "A") //Audio
          {
            _xml.WriteStartElement("av:mediaClass");
            _xml.WriteValue("M");
            _xml.WriteEndElement();
          }
          else if ((directoryPropertyObject as BasicContainer).Id == "I") //Image
          {
            _xml.WriteStartElement("av:mediaClass");
            _xml.WriteValue("P");
            _xml.WriteEndElement();
          }
          else if ((directoryPropertyObject as BasicContainer).Id == "V") //Video
          {
            _xml.WriteStartElement("av:mediaClass");
            _xml.WriteValue("V");
            _xml.WriteEndElement();
          }
        }

        _xml.WriteEndElement();
      }
    }
  }
}
