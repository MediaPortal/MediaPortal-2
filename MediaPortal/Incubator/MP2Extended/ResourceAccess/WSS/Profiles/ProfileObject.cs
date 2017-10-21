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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.Transcoding.Interfaces.Profiles.Setup;
using MediaPortal.Plugins.Transcoding.Interfaces.Profiles;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS
{
  #region Profile

  public class MediaMimeMapping
  {
    public string MIME = null;
    public string MIMEName = null;
    public string MappedMediaFormat = null;
  }

  /*public class UpnpDeviceInformation
  {
    public MediaServerUpnPDeviceInformation DeviceInformation = new MediaServerUpnPDeviceInformation();
    public string AdditionalElements = null;
  }*/

  #endregion

  #region Client settings

  public enum ThumbnailDelivery
  {
    None,
    All,
    Resource,
    AlbumArt,
    Icon
  }

  public enum MetadataDelivery
  {
    All,
    Required
  }

  public class ThumbnailSettings
  {
    public int MaxWidth = 160;
    public int MaxHeight = 160;
    public ThumbnailDelivery Delivery = ThumbnailDelivery.All;
  }

  public class CommunicationSettings
  {
    public bool AllowChunckedTransfer = true;
    public int DefaultBufferSize = 1500;
    public long InitialBufferSize = 0;
  }

  public class MetadataSettings
  {
    public MetadataDelivery Delivery = MetadataDelivery.All;
  }

  public class ProfileSettings
  {
    public ThumbnailSettings Thumbnails = new ThumbnailSettings();
    public CommunicationSettings Communication = new CommunicationSettings();
    public MetadataSettings Metadata = new MetadataSettings();
  }

  public class UpnpSearch
  {
    public UpnpSearch()
    {
      FriendlyName = null;
      ModelName = null;
      ModelNumber = null;
      ProductNumber = null;
      Server = null;
      Manufacturer = null;
    }

    public string FriendlyName { get; set; }
    public string ModelName { get; set; }
    public string ModelNumber { get; set; }
    public string ProductNumber { get; set; }
    public string Server { get; set; }
    public string Manufacturer { get; set; }

    public int Count()
    {
      PropertyInfo[] properties = typeof(UpnpSearch).GetProperties();
      return properties.Count(property => property.GetValue(this) != null);
    }
  }

  public class EndPointSettings
  {
    public EndPointProfile Profile = null;
    public string PreferredAudioLanguages = null;
    public bool EstimateTransodedSize = true;
  }

  public class EndPointProfile
  {
    public bool Active = false;
    public string ID = "";
    public string Name = "?";

    public ProfileSettings Settings = new ProfileSettings();
    public TranscodingSetup MediaTranscoding
    {
      get
      {
        return TranscodeProfileManager.GetTranscodeProfile(ProfileManager.TRANSCODE_PROFILE_SECTION, ID);
      }
    }
    public Dictionary<string, MediaMimeMapping> MediaMimeMap = new Dictionary<string, MediaMimeMapping>();
    public List<string> Targets = new List<string>();

    public override string ToString()
    {
      return ID + " - " + Name;
    }
  }

  #endregion
}
