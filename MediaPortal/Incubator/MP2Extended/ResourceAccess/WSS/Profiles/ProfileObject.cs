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
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.Setup;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS
{
  #region Profile

  public class MediaMimeMapping
  {
    public string MIME = null;
    public string MIMEName = null;
    public string MappedMediaFormat = null;
  }

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

  public class EndPointSettings
  {
    public EndPointProfile Profile { get; set; } = null;
    public IEnumerable<string> PreferredSubtitleLanguages { get; set; } = null;
    public IEnumerable<string> PreferredAudioLanguages { get; set; } = null;
    public Guid ClientId { get; set; } = Guid.Empty;
    public Guid? UserId { get; set; } = null;
    public bool EstimateTransodedSize { get; set; } = true;

    public static string GetClientName(IPAddress ip)
    {
      return $"MP2E ({ip.ToString()})";
    }
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

    private ITranscodeProfileManager TranscodeProfileManager
    {
      get { return ServiceRegistration.Get<ITranscodeProfileManager>(); }
    }
  }

  #endregion
}
