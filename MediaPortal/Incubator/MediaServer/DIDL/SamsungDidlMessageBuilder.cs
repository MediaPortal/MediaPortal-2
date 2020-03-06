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
using System.IO;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MediaServer.Objects;
using MediaPortal.Extensions.MediaServer.Objects.MediaLibrary;
using System.Collections.Generic;

namespace MediaPortal.Extensions.MediaServer.DIDL
{
  public class SamsungDidlMessageBuilder : GenericDidlMessageBuilder
  {
    //<DIDL-Lite xmlns="urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/" 
    //xmlns:dc="http://purl.org/dc/elements/1.1/" 
    //xmlns:dlna="urn:schemas-dlna-org:metadata-1-0/" 
    //xmlns:sec="http://www.sec.co.kr/" 
    //xmlns:upnp="urn:schemas-upnp-org:metadata-1-0/upnp/">

    //<item id="V_F#FOL_R3$MI138" parentID="V_F#FOL_R3" restricted="1">
    //<dc:title>Avatar.Featurette Creating.the.World.of.Pandora.1080P.H264.AAC2.0</dc:title>
    //<upnp:class>object.item.videoItem</upnp:class>
    //<dc:date>2010-01-24</dc:date>
    //<upnp:genre>Unknown</upnp:genre>
    //<res duration="0:22:55.000" protocolInfo="http-get:*:video/x-mkv:DLNA.ORG_PN=MATROSKA;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=81100000000000000000000000000000" 
    //resolution="1920x1080" size="1649087264">http://192.168.79.150:8895/resource/138/MEDIA_ITEM</res>
    //<res protocolInfo="http-get:*:smi/caption:*">http://192.168.79.150:8895/resource/138/SUBTITLE</res>
    //<sec:CaptionInfoEx sec:type="srt">http://192.168.79.150:8895/resource/138/SUBTITLE</sec:CaptionInfoEx>
    //</item>

    //object.item.imageItem:
    //<item id="828" parentID="800" restricted="0">
    //<res resolution="3008x2000" size="2770167" protocolInfo="http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_LRG;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=00D00000000000000000000000000000">
    //http://192.168.0.5:17679/FileProvider/M$0/O$1/S$/P$JPEG_LRG/I$image/jpeg/828
    //</res> 
    //<res resolution="160x106" size="5159" protocolInfo="http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_TN;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=00D00000000000000000000000000000">
    //http://192.168.0.5:17679/FileProvider/M$0/O$0/S$/P$JPEG_TN/I$image/jpeg/830
    //</res> 
    //<res resolution="640x425" size="52401" protocolInfo="http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_SM;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=00D00000000000000000000000000000">
    //http://192.168.0.5:17679/FileProvider/M$0/O$0/S$/P$JPEG_SM/I$image/jpeg/829
    //</res> 
    //<sec:manufacturer>NIKON CORPORATION</sec:manufacturer> 
    //<sec:fvalue>10.0</sec:fvalue> 
    //<sec:exposureTime>0.0025</sec:exposureTime> 
    //<sec:iso>200</sec:iso> 
    //<sec:model>NIKON D40</sec:model> 
    //<upnp:playbackCount>0</upnp:playbackCount> 
    //<sec:preference>0</sec:preference> 
    //<sec:dcmInfo>WIDTH=3008,HEIGHT=2000,COMPOSCORE=0,COMPOID=0,COLORSCORE=0,COLORID=0,MONTHLY=05,ORT=1,CREATIONDATE=1336233107,FOLDER=Img0003</sec:dcmInfo> 
    //<dc:date>2012-05-05T17:51:47</dc:date> 
    //<sec:composition>0</sec:composition> 
    //<sec:modifiationDate>2012-05-05T16:51:47</sec:modifiationDate> 
    //<sec:color>0</sec:color> 
    //<upnp:class>object.item.imageItem</upnp:class> 
    //<dc:title>DSC_0010</dc:title> 
    //<upnp:objectUpdateID>501</upnp:objectUpdateID> 
    //<sec:initUpdateID>501</sec:initUpdateID> 
    //</item> 

    //object.container:
    //<container id="800" parentID="106" childCount="25" searchable="0" restricted="1">
    //<upnp:class>object.container</upnp:class> 
    //<dc:title>Img0003</dc:title> 
    //<upnp:objectUpdateID>532</upnp:objectUpdateID> 
    //<sec:initUpdateID>481</sec:initUpdateID> 
    //<sec:classCount class="object.container">0</sec:classCount> 
    //<sec:classCount class="object.item.imageItem">25</sec:classCount> 
    //<sec:classCount class="object.item.audioItem">0</sec:classCount> 
    //<sec:classCount class="object.item.videoItem">0</sec:classCount> 
    //</container> 

    //object.item.audioItem:
    //<item id="A_M_0005_340" parentID="A_M_0005" restricted="1">
    //<res bitrate="320000" duration="00:07:40" size="18385144" protocolInfo="http-get:*:audio/mpeg:DLNA.ORG_PN=MP3;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000">
    //http://192.168.0.5:17679/FileProvider/M$460000/O$1/S$/P$MP3/I$audio/mpeg/1067
    //</res> 
    //<upnp:originalTrackNumber>9</upnp:originalTrackNumber> 
    //<upnp:album>Abigail [Bonus Tracks]</upnp:album> 
    //<upnp:genre>Metal</upnp:genre> 
    //<upnp:playbackCount>0</upnp:playbackCount> 
    //<upnp:artist>King Diamond</upnp:artist> 
    //<sec:dcmInfo>MOODSCORE=0,MOODID=5,CREATIONDATE=1323206029,YEAR=2011</sec:dcmInfo> 
    //<sec:modifiationDate>2011-12-06T22:13:49</sec:modifiationDate> 
    //<sec:preference>0</sec:preference> 
    //<sec:composer>King Diamond</sec:composer> 
    //<dc:date>2011-12-06T22:13:49</dc:date> 
    //<upnp:class>object.item.audioItem</upnp:class> 
    //<dc:title>Black Horsemen</dc:title> 
    //</item>

    //</DIDL-Lite>

    public SamsungDidlMessageBuilder()
    {
      AddMessageAttribute("xmlns", "sec", null, "http://www.sec.co.kr/");
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
          throw new ArgumentException(
              "directoryPropertyObject isn't either an IDirectoryContainer or IDirectoryItem");
        }

        ObjectRenderer.Render(_filter, directoryPropertyObject, _xml);

        if (directoryPropertyObject is MediaLibraryVideoItem)
        {
          try
          {
            MediaLibraryVideoItem res = (MediaLibraryVideoItem)directoryPropertyObject;
            foreach (var embeddedRes in res.Resources)
            {
              if (embeddedRes is MediaLibrarySubtitleResource)
              {
                _xml.WriteStartElement("sec", "CaptionInfoEx", null);
                _xml.WriteAttributeString("sec", "type", null, ((MediaLibrarySubtitleResource)embeddedRes).SubtitleType);
                _xml.WriteValue(embeddedRes.Uri);
                _xml.WriteEndElement();
                break;
              }
            }

            var dlnaItem = res.Client.GetDlnaItem(res.MediaItemId);
            long creationDateSecs = 0;
            if (dlnaItem?.MediaItemAddedDate != null)
            {
              creationDateSecs = Convert.ToInt64((dlnaItem.MediaItemAddedDate.Value - new DateTime(1970, 1, 1)).TotalSeconds);
            }
            string dcm = "CREATIONDATE=" + creationDateSecs;
            if (dlnaItem?.MediaItemWidth != null)
            {
              dcm += ",WIDTH=" + Convert.ToString(dlnaItem.MediaItemWidth.Value);
            }
            if (dlnaItem?.MediaItemHeight != null)
            {
              dcm += ",HEIGHT=" + Convert.ToString(dlnaItem.MediaItemHeight.Value);
            }
            if (dlnaItem?.MediaItemRecordingDate != null)
            {
              dcm += ",YEAR=" + dlnaItem.MediaItemRecordingDate.Value.Year;
            }

            //TODO: Support resumed playback of videos?
            //dcm += string.Format(",BM={0}", numberOfSecondsToResumeFrom)

            _xml.WriteStartElement("sec", "dcmInfo", null);
            _xml.WriteValue(dcm);
            _xml.WriteEndElement();
          }
          catch (Exception ex)
          {
            Logger.Warn("DlnaMediaServer: Cannot resolve resource elements for injection of information:\n {0}", ex.Message);
          }
        }
        else if (directoryPropertyObject is MediaLibraryImageItem)
        {
          try
          {
            MediaLibraryImageItem res = (MediaLibraryImageItem)directoryPropertyObject;
            var dlnaItem = res.Client.GetDlnaItem(res.MediaItemId);
            if (dlnaItem?.MediaItemImageMake != null)
            {
              _xml.WriteStartElement("sec", "manufacturer", null);
              _xml.WriteValue(dlnaItem.MediaItemImageMake);
              _xml.WriteEndElement();
            }
            if (dlnaItem?.MediaItemImageModel != null)
            {
              _xml.WriteStartElement("sec", "model", null);
              _xml.WriteValue(dlnaItem.MediaItemImageModel);
              _xml.WriteEndElement();
            }
            if (dlnaItem?.MediaItemImageFNumber != null)
            {
              _xml.WriteStartElement("sec", "fvalue", null);
              _xml.WriteValue(dlnaItem.MediaItemImageFNumber);
              _xml.WriteEndElement();
            }
            if (dlnaItem?.MediaItemImageIsoSpeed != null)
            {
              _xml.WriteStartElement("sec", "iso", null);
              _xml.WriteValue(dlnaItem.MediaItemImageIsoSpeed);
              _xml.WriteEndElement();
            }
            if (dlnaItem?.MediaItemImageExposureTime != null)
            {
              _xml.WriteStartElement("sec", "exposureTime", null);
              _xml.WriteValue(dlnaItem.MediaItemImageExposureTime);
              _xml.WriteEndElement();
            }

            long creationDateSecs = 0;
            if (dlnaItem?.MediaItemAddedDate != null)
            {
              creationDateSecs = Convert.ToInt64((dlnaItem.MediaItemAddedDate.Value - new DateTime(1970, 1, 1)).TotalSeconds);
            }
            string dcm = "CREATIONDATE=" + creationDateSecs;
            if (dlnaItem?.MediaItemWidth != null)
            {
              dcm += ",WIDTH=" + Convert.ToString(dlnaItem.MediaItemWidth.Value);
            }
            if (dlnaItem?.MediaItemHeight != null)
            {
              dcm += ",HEIGHT=" + Convert.ToString(dlnaItem.MediaItemHeight.Value);
            }
            if (dlnaItem?.MediaItemImageOrientation != null)
            {
              dcm += ",ORT=" + Convert.ToString(dlnaItem.MediaItemImageOrientation.Value);
            }
            if (dlnaItem?.MediaItemRecordingDate != null)
            {
              dcm += ",YEAR=" + dlnaItem.MediaItemRecordingDate.Value.Year;
            }
            
            _xml.WriteStartElement("sec", "dcmInfo", null);
            _xml.WriteValue(dcm);
            _xml.WriteEndElement();
          }
          catch (Exception ex)
          {
            Logger.Warn("DlnaMediaServer: Cannot resolve resource elements for injection of information:\n {0}", ex.Message);
          }
        }
        _xml.WriteEndElement();
      }
    }
  }
}
