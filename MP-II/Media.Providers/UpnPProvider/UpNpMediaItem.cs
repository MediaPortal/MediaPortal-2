#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using Intel.UPNP.AV.CdsMetadata;
using Intel.UPNP.AV.MediaServer.CP;
using MediaPortal.Media.MetaData;
using IRootContainer = MediaPortal.Media.MediaManager.IRootContainer;
using IMediaItem = MediaPortal.Media.MediaManager.IMediaItem;

namespace Media.Providers.UpNpProvider
{
  internal class UpNpMediaItem : IMediaItem
  {
    #region IMediaItem Members

    private readonly CpMediaItem _item;
    private  IRootContainer _parent;
    private Dictionary<string, object> _metaData;

    public UpNpMediaItem(CpMediaItem item, IRootContainer parent)
    {
      _item = item;
      _parent = parent;
    }
    public IMetaDataMappingCollection Mapping
    {
      get
      {
        if (_parent != null)
          return _parent.Mapping;
        return null;
      }
      set
      {
      }
    }

    /// <summary>
    /// Gets a value indicating whether this item is located locally or remote
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this item is located locally; otherwise, <c>false</c>.
    /// </value>
    public bool IsLocal
    {
      get
      {
        return false;
      }
    }
    public IRootContainer Parent
    {
      get { return _parent; }
      set
      {
        _parent = value;
      }
    }

    public string FullPath
    {
      get
      {
        return String.Format(@"{0}/{1}", Parent.FullPath, Title);
      }
      set { }
    }
    public IDictionary<string, object> MetaData
    {
      get
      {
        if (_metaData == null)
        {
          _metaData = LoadMetaData();
        }
        return _metaData;
      }
    }

    private Dictionary<string, object> LoadMetaData()
    {
      Dictionary<string, object> data = new Dictionary<string, object>();
      for (int i = 0; i < _item.Resources.Length; ++i)
      {
        IMediaResource resource = _item.Resources[i];
        if (resource.ProtocolInfo != null)
        {
          if (resource.ProtocolInfo.MimeType != null)
          {
            data["MimeType"] = resource.ProtocolInfo.MimeType;
          }
        }
        if (resource.HasBitrate)
        {
          data["Bitrate"] = resource.Bitrate.Value;
        }
        if (resource.HasBitsPerSample)
        {
          data["BitsPerSample"] = resource.BitsPerSample.Value;
        }
        if (resource.HasColorDepth)
        {
          data["ColorDepth"] = resource.ColorDepth.Value;
        }
        if (resource.HasDuration)
        {
          data["Duration"] = resource.Duration.Value;
        }
        if (resource.HasNrAudioChannels)
        {
          data["nrAudioChannels"] = resource.nrAudioChannels.Value;
        }
        if (resource.HasProtection)
        {
          data["Protection"] = resource.Protection;
        }
        if (resource.HasResolution)
        {
          data["Resolution"] = resource.Resolution.Value;
        }
        if (resource.HasSampleFrequency)
        {
          data["SampleFrequency"] = resource.SampleFrequency.Value;
        }
        if (resource.HasSize)
        {
          data["size"] = (long)((UInt64)resource.Size.Value);
        }
      }
      if (_item.Properties != null)
      {
        for (int i = 0; i < _item.Properties.PropertyNames.Count; ++i)
        {
          string propName = _item.Properties.PropertyNames[i].ToString();
          ICdsElement[] elements = (ICdsElement[])_item.Properties[propName];
          for (int x = 0; x < elements.Length; ++x)
          {
            ICdsElement element = elements[x];
            if (propName == "dc:date")
            {
              data["date"] = element.Value;
            }
            if (propName == "dc:creator")
            {
              data["creator"] = element.Value.ToString();
            }
            if (propName == "dc:title")
            {
              data["title"] = element.Value.ToString();
            }
            ;
            if (propName == "upnp:genre")
            {
              data["genre"] = element.Value.ToString();
            }
            ;
            if (propName == "upnp:artist")
            {
              data["artist"] = element.Value.ToString();
            }
            ;
            if (propName == "upnp:album")
            {
              data["album"] = element.Value.ToString();
            }
            ;
            if (propName == "upnp:actor")
            {
              data["actor"] = element.Value.ToString();
            }
            ;
            if (propName == "upnp:albumArtURI")
            {
              data["CoverArt"] = element.Value;
            }
          }
        }
        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.actor))
        {
          data["actor"] = _item.Properties[CommonPropertyNames.actor].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.album))
        {
          data["album"] = _item.Properties[CommonPropertyNames.album].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.albumArtURI))
        {
          data["albumArtURI"] = _item.Properties[CommonPropertyNames.albumArtURI];
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.artist))
        {
          data["artist"] = _item.Properties[CommonPropertyNames.artist].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.artistDiscographyURI))
        {
          data["artistDiscographyURI"] = _item.Properties[CommonPropertyNames.artistDiscographyURI];
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.author))
        {
          data["author"] = _item.Properties[CommonPropertyNames.author].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.channelName))
        {
          data["channelName"] = _item.Properties[CommonPropertyNames.channelName].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.channelNr))
        {
          data["channelNr"] = _item.Properties[CommonPropertyNames.channelNr].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.contributor))
        {
          data["contributor"] = _item.Properties[CommonPropertyNames.contributor].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.creator))
        {
          data["creator"] = _item.Properties[CommonPropertyNames.creator].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.date))
        {
          data["date"] = _item.Properties[CommonPropertyNames.date];
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.description))
        {
          data["description"] = _item.Properties[CommonPropertyNames.description].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.director))
        {
          data["director"] = _item.Properties[CommonPropertyNames.director].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.DVDRegionCode))
        {
          data["DVDRegionCode"] = _item.Properties[CommonPropertyNames.DVDRegionCode].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.genre))
        {
          data["genre"] = _item.Properties[CommonPropertyNames.genre].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.icon))
        {
          data["CoverArt"] = _item.Properties[CommonPropertyNames.icon].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.language))
        {
          data["language"] = _item.Properties[CommonPropertyNames.language].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.longDescription))
        {
          data["longDescription"] = _item.Properties[CommonPropertyNames.longDescription].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.lyricsURI))
        {
          data["lyricsURI"] = _item.Properties[CommonPropertyNames.lyricsURI];
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.originalTrackNumber))
        {
          data["lyricsURI"] = _item.Properties[CommonPropertyNames.originalTrackNumber].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.playlist))
        {
          data["playlist"] = _item.Properties[CommonPropertyNames.playlist].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.producer))
        {
          data["producer"] = _item.Properties[CommonPropertyNames.producer].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.publisher))
        {
          data["publisher"] = _item.Properties[CommonPropertyNames.publisher].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.radioBand))
        {
          data["radioBand"] = _item.Properties[CommonPropertyNames.radioBand].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.radioCallSign))
        {
          data["radioCallSign"] = _item.Properties[CommonPropertyNames.radioCallSign].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.radioStationID))
        {
          data["radioStationID"] = _item.Properties[CommonPropertyNames.radioStationID].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.rating))
        {
          data["rating"] = _item.Properties[CommonPropertyNames.rating].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.region))
        {
          data["region"] = _item.Properties[CommonPropertyNames.region].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.relation))
        {
          data["relation"] = _item.Properties[CommonPropertyNames.relation].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.rights))
        {
          data["rights"] = _item.Properties[CommonPropertyNames.rights].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.role))
        {
          data["role"] = _item.Properties[CommonPropertyNames.role].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.scheduledEndTime))
        {
          data["scheduledEndTime"] = _item.Properties[CommonPropertyNames.scheduledEndTime];
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.scheduledStartTime))
        {
          data["scheduledStartTime"] = _item.Properties[CommonPropertyNames.scheduledStartTime];
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.title))
        {
          data["title"] = _item.Properties[CommonPropertyNames.title].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.toc))
        {
          data["toc"] = _item.Properties[CommonPropertyNames.toc].ToString();
        }

        if (_item.Properties.PropertyNames.Contains(CommonPropertyNames.userAnnotation))
        {
          data["userAnnotation"] = _item.Properties[CommonPropertyNames.userAnnotation].ToString();
        }
      }
      return data;
    }

    public string Title
    {
      get { return _item.Title; }
      set
      {
      }
    }

    public Uri ContentUri
    {
      get
      {
        if (_item.Resources == null)
        {
          return null;
        }
        if (_item.Resources.Length == 0)
        {
          return null;
        }
        return new Uri(_item.Resources[0].ContentUri);
      }
    }

    #endregion
 
  }
}
