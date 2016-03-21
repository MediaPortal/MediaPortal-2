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
using System.IO;
using System.Net;
using System.Xml;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Plugins.Transcoding.Interfaces.Profiles;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles
{
  public class ProfileManager
  {
    private const string DEFAULT_PROFILE_ID = "WebDefault";
    private const string PROFILE_FILE_NAME = "StreamingProfiles.xml";

    public  const string TRANSCODE_PROFILE_SECTION = "MP2EXT";

    public static Dictionary<string, EndPointProfile> Profiles = new Dictionary<string, EndPointProfile>();

    public static IPAddress ResolveIpAddress(string address)
    {
      try
      { 
        // Get host IP addresses
        IPAddress[] hostIPs = Dns.GetHostAddresses(address);
        // Get local IP addresses
        IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

        // Test if host IP equals to local IP or localhost
        foreach (IPAddress hostIP in hostIPs)
        {
          // Is localhost
          if (IPAddress.IsLoopback(hostIP)) 
            return IPAddress.Loopback;
          // Is local address
          foreach (IPAddress localIP in localIPs)
          {
            if (hostIP.Equals(localIP))
              return IPAddress.Loopback;
          }
        }
      }
      catch { }
      return IPAddress.Parse(address);
    }

    public static void LoadProfiles(bool userProfiles)
    {
      try
      {
        string profileFile = FileUtils.BuildAssemblyRelativePath(PROFILE_FILE_NAME);
        if (userProfiles)
        {
          IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
          string dataPath = pathManager.GetPath("<CONFIG>");
          profileFile = Path.Combine(dataPath, PROFILE_FILE_NAME);
        }
        else
        {
          TranscodeProfileManager.ClearTranscodeProfiles(TRANSCODE_PROFILE_SECTION);
        }

        if (File.Exists(profileFile) == true)
        {
          XmlTextReader reader = new XmlTextReader(profileFile);
          EndPointProfile profile = null;
          while (reader.Read())
          {
            if (reader.NodeType != XmlNodeType.Element && reader.NodeType != XmlNodeType.EndElement)
            {
              continue;
            }
            string nodeName = reader.Name;
            #region Profile
            if (nodeName == "Profile" && reader.NodeType == XmlNodeType.Element)
            {
              profile = new EndPointProfile();
              while (reader.MoveToNextAttribute()) // Read the attributes.
              {
                if (reader.Name == "id")
                {
                  profile.ID = reader.ReadContentAsString();
                }
                else if (reader.Name == "name")
                {
                  profile.Name = reader.ReadContentAsString();
                }
                else if (reader.Name == "active")
                {
                  profile.Active = reader.ReadContentAsBoolean();
                }
                else if (reader.Name == "baseProfile")
                {
                  string parentProfileId = reader.ReadContentAsString();
                  if (Profiles.ContainsKey(parentProfileId) == true)
                  {
                    Logger.Info("ProfileManager: Profile: {0}, ParentProfile: {1}, ParentTargets: {2}", profile.Name, parentProfileId, string.Join(", ", Profiles[parentProfileId].Targets));
                    profile.Targets = new List<string>(Profiles[parentProfileId].Targets);
                    
                    profile.Settings.Thumbnails.MaxHeight = Profiles[parentProfileId].Settings.Thumbnails.MaxHeight;
                    profile.Settings.Thumbnails.MaxWidth = Profiles[parentProfileId].Settings.Thumbnails.MaxWidth;
                    profile.Settings.Thumbnails.Delivery = Profiles[parentProfileId].Settings.Thumbnails.Delivery;

                    profile.Settings.Communication.AllowChunckedTransfer = Profiles[parentProfileId].Settings.Communication.AllowChunckedTransfer;
                    profile.Settings.Communication.DefaultBufferSize = Profiles[parentProfileId].Settings.Communication.DefaultBufferSize;
                    profile.Settings.Communication.InitialBufferSize = Profiles[parentProfileId].Settings.Communication.InitialBufferSize;

                    profile.Settings.Metadata.Delivery = Profiles[parentProfileId].Settings.Metadata.Delivery;

                    profile.MediaMimeMap = new Dictionary<string, MediaMimeMapping>(Profiles[parentProfileId].MediaMimeMap);
                  }
                }
              }
            }
            #endregion Profile
            #region Targets
            else if (nodeName == "Targets" && reader.NodeType == XmlNodeType.Element)
            {
              while (reader.Read())
              {
                if (reader.Name == "Targets" && reader.NodeType == XmlNodeType.EndElement)
                {
                  break;
                }
                if (reader.Name == "Target" && reader.NodeType == XmlNodeType.Element)
                {
                  profile.Targets.Add(reader.ReadElementContentAsString().Trim());
                }
              }
            }
            #endregion Targets
            
            else if (nodeName == "WebMediaFormats" && reader.NodeType == XmlNodeType.Element)
            {
              while (reader.Read())
              {
                if (reader.Name == "WebMediaFormats" && reader.NodeType == XmlNodeType.EndElement)
                {
                  break;
                }
                if (reader.Name == "MediaFormat" && reader.NodeType == XmlNodeType.Element)
                {
                  MediaMimeMapping map = new MediaMimeMapping();
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "mime")
                    {
                      map.MIME = reader.ReadContentAsString();
                    }
                    else if (reader.Name == "name")
                    {
                      map.MIMEName = reader.ReadContentAsString();
                    }
                  }
                  reader.Read();
                  map.MappedMediaFormat = reader.Value;
                  //Overwrite any inherited media map
                  if (profile.MediaMimeMap.ContainsKey(map.MappedMediaFormat))
                  {
                    profile.MediaMimeMap[map.MappedMediaFormat] = map;
                  }
                  else
                  {
                    profile.MediaMimeMap.Add(map.MappedMediaFormat, map);
                  }
                }
              }
            }
            else if (nodeName == "Settings" && reader.NodeType == XmlNodeType.Element)
            {
              while (reader.Read())
              {
                if (reader.Name == "Settings" && reader.NodeType == XmlNodeType.EndElement)
                {
                  break;
                }
                else if (reader.Name == "Thumbnails" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "maxWidth")
                    {
                      profile.Settings.Thumbnails.MaxWidth = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "maxHeight")
                    {
                      profile.Settings.Thumbnails.MaxHeight = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "delivery")
                    {
                      profile.Settings.Thumbnails.Delivery = (ThumbnailDelivery)Enum.Parse(typeof(ThumbnailDelivery), reader.ReadContentAsString(), true);
                    }
                  }
                }
                else if (reader.Name == "Communication" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "allowChunckedTransfer")
                    {
                      profile.Settings.Communication.AllowChunckedTransfer = reader.ReadContentAsBoolean();
                    }
                    else if (reader.Name == "initialBufferSize")
                    {
                      profile.Settings.Communication.InitialBufferSize = reader.ReadContentAsLong();
                    }
                    else if (reader.Name == "defaultBufferSize")
                    {
                      profile.Settings.Communication.DefaultBufferSize = reader.ReadContentAsInt();
                    }
                  }
                }
                else if (reader.Name == "Metadata" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "delivery")
                    {
                      profile.Settings.Metadata.Delivery = (MetadataDelivery)Enum.Parse(typeof(MetadataDelivery), reader.ReadContentAsString(), true);
                    }
                  }
                }
                    }
                  }
            else if (nodeName == "Profile" && reader.NodeType == XmlNodeType.EndElement)
            {
              if (Profiles.ContainsKey(profile.ID))
              {
                //User profiles can override defaults
                if (userProfiles == true)
                {
                  profile.Name = profile.Name + " [User]";
                }
                Profiles[profile.ID] = profile;
              }
              else
              {
                Profiles.Add(profile.ID, profile);
              }
            }
          }
          reader.Close();

          TranscodeProfileManager.LoadTranscodeProfiles(TRANSCODE_PROFILE_SECTION, profileFile);
        }
      }
      catch (Exception e)
      {
        Logger.Info("MP2Extended: Exception reading profiles (Text: '{0}')", e.Message);
      }
    }

    public static EndPointSettings GetEndPointSettings(string profileId)
    {
      EndPointSettings settings = new EndPointSettings
      {
        PreferredAudioLanguages = MP2Extended.Settings.PreferredAudioLanguages
      };
      try
      {
        if (Profiles.ContainsKey(profileId) == true)
        {
          settings.Profile = Profiles[profileId];
        }
        else if (profileId == "None")
        {
          settings.Profile = null;
        }
        else if (Profiles.ContainsKey(DEFAULT_PROFILE_ID) == true)
        {
          settings.Profile = Profiles[DEFAULT_PROFILE_ID];
        }
      }
      catch (Exception e)
      {
        Logger.Info("MP2Extended: Exception reading profile links (Text: '{0}')", e.Message);
      }
      return settings;
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
