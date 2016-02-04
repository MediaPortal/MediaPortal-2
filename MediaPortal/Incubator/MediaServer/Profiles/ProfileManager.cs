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
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using System.Xml;
using System.IO;
using MediaPortal.Common.Logging;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Plugins.MediaServer.DIDL;
using MediaPortal.Plugins.MediaServer.DLNA;
using MediaPortal.Plugins.MediaServer.Protocols;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Plugins.MediaServer.Filters;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.MediaServer.Settings;
using MediaPortal.Plugins.Transcoding.Service.Profiles;

namespace MediaPortal.Plugins.MediaServer.Profiles
{
  public class ProfileManager
  {
    private const string DLNA_DEFAULT_PROFILE_ID = "DLNADefault";
    private const string PROFILE_FILE = "DLNAProfiles.xml";

    public const string TRANSCODE_PROFILE_SECTION = "DLNA";

    public static Dictionary<IPAddress, EndPointSettings> ProfileLinks = new Dictionary<IPAddress, EndPointSettings>();
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

    public static EndPointSettings DetectProfile(NameValueCollection headers)
    {
      // overwrite the automatic profile detection
      if (headers["remote_addr"] != null && ProfileLinks.ContainsKey(IPAddress.Parse(headers["remote_addr"])))
      {
        IPAddress ip = ResolveIpAddress(headers["remote_addr"]);
        if (ProfileLinks[ip].Profile != null)
        {
          //Logger.Info("DetectProfile: overwrite automatic profile detection for IP: {0}, using: {1}", ip, ProfileLinks[ip].Profile.ID);
          return ProfileLinks[ip];
        }
        else
        {
          //Logger.Info("DetectProfile: overwrite automatic profile detection for IP: {0}, using: None", ip);
          return null;
        }
      }

      if (headers["remote_addr"] == null)
      {
        Logger.Warn("DetectProfile: Couldn't find Header 'remote_addr'!");
      }

      foreach (KeyValuePair<string, EndPointProfile> profile in Profiles)
      {
        var match = false;
        foreach (Detection detection in profile.Value.Detections)
        {
          // check if HTTP header matches
          if (detection.HttpHeaders.Count > 0) match = true;
          foreach (KeyValuePair<string, string> header in detection.HttpHeaders)
          {
            if (headers[header.Key] == null)
            {
              match = false;
              break;
            }
            if (header.Value == null || Regex.IsMatch(headers[header.Key], header.Value, RegexOptions.IgnoreCase) == false)
            {
              match = false;
              break;
            }
          }

          // If there are Http Header conditions, but match = false, we can't fulfill the requirements anymore
          if (detection.HttpHeaders.Count > 0 && !match) break;

          // check UPnP Fields
          if (match && detection.UPnPSearch.Count() > 0)
          {
            Logger.Info("DetectProfile: Matching UPnP Fields");
            match = true;

            if (headers["remote_addr"] == null)
            {
              break;
            }
            List<TrackedDevice> trackedDevices = MediaServerPlugin.Tracker.GeTrackedDevicesByIp(IPAddress.Parse(headers["remote_addr"]));
            if (trackedDevices == null || trackedDevices.Count == 0)
            {
              Logger.Warn("DetectProfile: No matching Devices");
              break;
            }

            foreach (TrackedDevice trackedDevice in trackedDevices)
            {
              if (trackedDevice.FriendlyName != null && (detection.UPnPSearch.FriendlyName != null && !Regex.IsMatch(trackedDevice.FriendlyName, detection.UPnPSearch.FriendlyName, RegexOptions.IgnoreCase)))
              {
                match = false;
#if DEBUG
                Logger.Debug("DetectProfile: No FriendlyName Tracked: {0}, Search: {1}", trackedDevice.FriendlyName, detection.UPnPSearch.FriendlyName);
#endif
                break;
              }

              if (trackedDevice.ModelName != null && (detection.UPnPSearch.ModelName != null && !Regex.IsMatch(trackedDevice.ModelName, detection.UPnPSearch.ModelName, RegexOptions.IgnoreCase)))
              {
                match = false;
#if DEBUG
                Logger.Debug("DetectProfile: No ModelName Tracked: {0}, Search: {1}", trackedDevice.ModelName, detection.UPnPSearch.ModelName);
#endif
                break;
              }

              if (trackedDevice.ModelNumber != null && (detection.UPnPSearch.ModelNumber != null && !Regex.IsMatch(trackedDevice.ModelNumber, detection.UPnPSearch.ModelNumber, RegexOptions.IgnoreCase)))
              {
                match = false;
#if DEBUG
                Logger.Debug("DetectProfile: No ModelNumber Tracked: {0}, Search: {1}", trackedDevice.ModelNumber, detection.UPnPSearch.ModelNumber);
#endif
                break;
              }

              if (trackedDevice.ProductNumber != null && (detection.UPnPSearch.ProductNumber != null && !Regex.IsMatch(trackedDevice.ProductNumber, detection.UPnPSearch.ProductNumber, RegexOptions.IgnoreCase)))
              {
                match = false;
#if DEBUG
                Logger.Debug("DetectProfile: No ProductNumber Tracked: {0}, Search: {1}", trackedDevice.ProductNumber, detection.UPnPSearch.ProductNumber);
#endif
                break;
              }

              if (trackedDevice.Server != null && (detection.UPnPSearch.Server != null && !Regex.IsMatch(trackedDevice.Server, detection.UPnPSearch.Server, RegexOptions.IgnoreCase)))
              {
                match = false;
#if DEBUG
                Logger.Debug("DetectProfile: No Server Tracked: {0}, Search: {1}", trackedDevice.Server, detection.UPnPSearch.Server);
#endif
                break;
              }

              if (trackedDevice.Manufacturer != null && (detection.UPnPSearch.Manufacturer != null && !Regex.IsMatch(trackedDevice.Manufacturer, detection.UPnPSearch.Manufacturer, RegexOptions.IgnoreCase)))
              {
                match = false;
#if DEBUG
                Logger.Info("DetectProfile: No Manufacturer Tracked: {0}, Search: {1}", trackedDevice.Manufacturer, detection.UPnPSearch.Manufacturer);
#endif
                break;
              }
            }
          }

          if (match)
          {
            Logger.Info("DetectProfile: Profile found => using {0}, headers={1}", profile.Value.ID, string.Join(", ", headers.AllKeys.Select(key => key + ": " + headers[key]).ToArray()));
            if (headers["remote_addr"] != null)
            {
              IPAddress ip = ResolveIpAddress(headers["remote_addr"]);
              if (ProfileLinks.ContainsKey(ip) == false) ProfileLinks.Add(ip, GetEndPointSettings(ip.ToString(), profile.Value.ID));
              return ProfileLinks[ip];
            }
            return GetEndPointSettings(profile.Key, profile.Value.ID);
          }
        }
      }

      // no match => return Defaul Profile
      Logger.Info("DetectProfile: No profile found => using {0}, headers={1}", DLNA_DEFAULT_PROFILE_ID, string.Join(", ", headers.AllKeys.Select(key => key + ": " + headers[key]).ToArray()));
      if (headers["remote_addr"] != null)
      {
        IPAddress ip = ResolveIpAddress(headers["remote_addr"]);
        if (ProfileLinks.ContainsKey(ip) == false) ProfileLinks.Add(ip, GetEndPointSettings(ip.ToString(), DLNA_DEFAULT_PROFILE_ID));
        return ProfileLinks[ip];
      }
      return GetEndPointSettings(headers["remote_addr"], DLNA_DEFAULT_PROFILE_ID);
    }

    public static void LoadProfiles(bool userProfiles)
    {
      try
      {
        string profileFile = FileUtils.BuildAssemblyRelativePath(PROFILE_FILE);
        if (userProfiles)
        {
          IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
          string dataPath = pathManager.GetPath("<CONFIG>");
          profileFile = Path.Combine(dataPath, PROFILE_FILE);
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
                    profile.UpnpDevice.DeviceInformation.FriendlyName = Profiles[parentProfileId].UpnpDevice.DeviceInformation.FriendlyName;
                    profile.UpnpDevice.DeviceInformation.Manufacturer = Profiles[parentProfileId].UpnpDevice.DeviceInformation.Manufacturer;
                    profile.UpnpDevice.DeviceInformation.ManufacturerURL = Profiles[parentProfileId].UpnpDevice.DeviceInformation.ManufacturerURL;
                    profile.UpnpDevice.DeviceInformation.ModelDescription = Profiles[parentProfileId].UpnpDevice.DeviceInformation.ModelDescription;
                    profile.UpnpDevice.DeviceInformation.ModelName = Profiles[parentProfileId].UpnpDevice.DeviceInformation.ModelName;
                    profile.UpnpDevice.DeviceInformation.ModelNumber = Profiles[parentProfileId].UpnpDevice.DeviceInformation.ModelNumber;
                    profile.UpnpDevice.DeviceInformation.ModelURL = Profiles[parentProfileId].UpnpDevice.DeviceInformation.ModelURL;
                    profile.UpnpDevice.DeviceInformation.SerialNumber = Profiles[parentProfileId].UpnpDevice.DeviceInformation.SerialNumber;
                    profile.UpnpDevice.DeviceInformation.UPC = Profiles[parentProfileId].UpnpDevice.DeviceInformation.UPC;
                    profile.UpnpDevice.AdditionalElements = Profiles[parentProfileId].UpnpDevice.AdditionalElements;

                    profile.DirectoryContentBuilder = Profiles[parentProfileId].DirectoryContentBuilder;
                    profile.ResourceAccessHandler = Profiles[parentProfileId].ResourceAccessHandler;
                    profile.DirectoryContentFilter = Profiles[parentProfileId].DirectoryContentFilter;
                    profile.ProtocolInfo = Profiles[parentProfileId].ProtocolInfo;

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
            else if (nodeName == "DLNAProtocolInfo" && reader.NodeType == XmlNodeType.Element)
            {
              profile.ProtocolInfo = (ProtocolInfoFormat)Enum.Parse(typeof(ProtocolInfoFormat), reader.ReadElementContentAsString(), true);
            }
            #region Detections
            else if (nodeName == "Detections" && reader.NodeType == XmlNodeType.Element)
            {
              while (reader.Read())
              {
                if (reader.Name == "Detections" && reader.NodeType == XmlNodeType.EndElement)
                {
                  break;
                }
                if (reader.Name == "Detection" && reader.NodeType == XmlNodeType.Element)
                {
                  Detection detection = new Detection();
                  while (reader.Read())
                  {
                    if (reader.Name == "Detection" && reader.NodeType == XmlNodeType.EndElement)
                    {
                      break;
                    }
                    if (reader.Name == "UPnPSearch")
                    {
                      while (reader.Read())
                      {
                        if (reader.Name == "UPnPSearch" && reader.NodeType == XmlNodeType.EndElement)
                        {
                          break;
                        }
                        if (reader.Name == "FriendlyName" && reader.NodeType == XmlNodeType.Element)
                        {
                          detection.UPnPSearch.FriendlyName = reader.ReadElementContentAsString();
                        }
                        else if (reader.Name == "ModelName" && reader.NodeType == XmlNodeType.Element)
                        {
                          detection.UPnPSearch.ModelName = reader.ReadElementContentAsString();
                        }
                        else if (reader.Name == "ModelNumber" && reader.NodeType == XmlNodeType.Element)
                        {
                          detection.UPnPSearch.ModelNumber = reader.ReadElementContentAsString();
                        }
                        else if (reader.Name == "ProductNumber" && reader.NodeType == XmlNodeType.Element)
                        {
                          detection.UPnPSearch.ProductNumber = reader.ReadElementContentAsString();
                        }
                        else if (reader.Name == "Server" && reader.NodeType == XmlNodeType.Element)
                        {
                          detection.UPnPSearch.Server = reader.ReadElementContentAsString();
                        }
                        else if (reader.Name == "Manufacturer" && reader.NodeType == XmlNodeType.Element)
                        {
                          detection.UPnPSearch.Manufacturer = reader.ReadElementContentAsString();
                        }
                      }
                    }
                    else if (reader.Name == "HttpSearch")
                    {
                      while (reader.Read())
                      {
                        if (reader.Name == "HttpSearch" && reader.NodeType == XmlNodeType.EndElement)
                        {
                          break;
                        }
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                          detection.HttpHeaders.Add(reader.Name, reader.ReadElementContentAsString());
                        }
                      }
                    }
                  }
                  profile.Detections.Add(detection);
                }
              }
            }
            #endregion Detections
            else if (nodeName == "DirectoryContentBuilder" && reader.NodeType == XmlNodeType.Element)
            {
              profile.DirectoryContentBuilder = (GenericDidlMessageBuilder.ContentBuilder)Enum.Parse(typeof(GenericDidlMessageBuilder.ContentBuilder), reader.ReadElementContentAsString(), true);
            }
            else if (nodeName == "ResourceAccessProtocol" && reader.NodeType == XmlNodeType.Element)
            {
              profile.ResourceAccessHandler = (GenericAccessProtocol.ResourceAccessProtocol)Enum.Parse(typeof(GenericAccessProtocol.ResourceAccessProtocol), reader.ReadElementContentAsString(), true);
            }
            else if (nodeName == "DirectoryContentFilter" && reader.NodeType == XmlNodeType.Element)
            {
              profile.DirectoryContentFilter = (GenericContentDirectoryFilter.ContentFilter)Enum.Parse(typeof(GenericContentDirectoryFilter.ContentFilter), reader.ReadElementContentAsString(), true);
            }
            #region UPNPDeviceDescription
            else if (nodeName == "UPNPDeviceDescription" && reader.NodeType == XmlNodeType.Element)
            {
              while (reader.Read())
              {
                if (reader.Name == "UPNPDeviceDescription" && reader.NodeType == XmlNodeType.EndElement)
                {
                  break;
                }
                if (reader.Name == "FriendlyName")
                {
                  profile.UpnpDevice.DeviceInformation.FriendlyName = reader.ReadElementContentAsString().Replace("{computerName}", Dns.GetHostName());
                }
                else if (reader.Name == "Manufacturer")
                {
                  profile.UpnpDevice.DeviceInformation.Manufacturer = reader.ReadElementContentAsString();
                }
                else if (reader.Name == "ManufacturerURL")
                {
                  profile.UpnpDevice.DeviceInformation.ManufacturerURL = reader.ReadElementContentAsString();
                }
                else if (reader.Name == "ModelDescription")
                {
                  profile.UpnpDevice.DeviceInformation.ModelDescription = reader.ReadElementContentAsString();
                }
                else if (reader.Name == "ModelName")
                {
                  profile.UpnpDevice.DeviceInformation.ModelName = reader.ReadElementContentAsString();
                }
                else if (reader.Name == "ModelNumber")
                {
                  profile.UpnpDevice.DeviceInformation.ModelNumber = reader.ReadElementContentAsString();
                }
                else if (reader.Name == "ModelURL")
                {
                  profile.UpnpDevice.DeviceInformation.ModelURL = reader.ReadElementContentAsString();
                }
                else if (reader.Name == "AdditionalElements")
                {
                  profile.UpnpDevice.AdditionalElements = reader.ReadElementContentAsString().Replace("\t", "").Replace("\r", "").Replace("\n", "").Replace("  ", "").Trim();
                }
              }
            }
            #endregion UPNPDeviceDescription
            else if (nodeName == "DLNAMediaFormats" && reader.NodeType == XmlNodeType.Element)
            {
              while (reader.Read())
              {
                if (reader.Name == "DLNAMediaFormats" && reader.NodeType == XmlNodeType.EndElement)
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
                if(userProfiles == true)
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
        Logger.Info("DlnaMediaServer: Exception reading profiles (Text: '{0}')", e.Message);
      }
    }

    public static EndPointSettings GetEndPointSettings(string clientIp, string profileId)
    {
      EndPointSettings settings = new EndPointSettings
      {
        PreferredSubtitleLanguages = MediaServerPlugin.Settings.PreferredSubtitleLanguages,
        PreferredAudioLanguages = MediaServerPlugin.Settings.PreferredAudioLanguages,
        DefaultSubtitleEncodings = MediaServerPlugin.Settings.DefaultSubtitleEncodings
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
        else if (Profiles.ContainsKey(DLNA_DEFAULT_PROFILE_ID) == true)
        {
          settings.Profile = Profiles[DLNA_DEFAULT_PROFILE_ID];
        }

        settings.ClientId = "DLNA_" + clientIp;
        settings.InitialiseContainerTree();
      }
      catch (Exception e)
      {
        Logger.Info("DlnaMediaServer: Exception reading profile links (Text: '{0}')", e.Message);
      }
      return settings;
    }

    public static void LoadProfileLinks()
    {
      try
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        ProfileLinks profileLinks = settingsManager.Load<ProfileLinks>();

        foreach (ProfileLink link in profileLinks.Links)
        {
          IPAddress ip = IPAddress.Parse(link.IP);

          EndPointSettings settings = new EndPointSettings();
          if (string.IsNullOrEmpty(link.PreferredAudioLanguages) == false)
          {
            settings.PreferredAudioLanguages = link.PreferredAudioLanguages;
          }
          else
          {
            settings.PreferredAudioLanguages = MediaServerPlugin.Settings.PreferredAudioLanguages;
          }
          if (string.IsNullOrEmpty(link.PreferredSubtitleLanguages) == false)
          {
            settings.PreferredSubtitleLanguages = link.PreferredSubtitleLanguages;
          }
          else
          {
            settings.PreferredSubtitleLanguages = MediaServerPlugin.Settings.PreferredSubtitleLanguages;
          }
          if (string.IsNullOrEmpty(link.DefaultSubtitleEncodings) == false)
          {
            settings.DefaultSubtitleEncodings = link.DefaultSubtitleEncodings;
          }
          else
          {
            settings.DefaultSubtitleEncodings = MediaServerPlugin.Settings.DefaultSubtitleEncodings;
          }

          if (Profiles.ContainsKey(link.Profile) == true)
          {
            settings.Profile = Profiles[link.Profile];
          }
          else if (link.Profile == "None")
          {
            settings.Profile = null;
          }
          else if (Profiles.ContainsKey(DLNA_DEFAULT_PROFILE_ID) == true)
          {
            settings.Profile = Profiles[DLNA_DEFAULT_PROFILE_ID];
          }

          settings.ClientId = "DLNA_" + ip.ToString();
          settings.InitialiseContainerTree();
          if (settings.Profile == null)
            Logger.Info("DlnaMediaServer: IP: {0}, using profile: None", ip);
          else
            Logger.Info("DlnaMediaServer: IP: {0}, using profile: {1}", ip, settings.Profile.ID);
          ProfileLinks.Add(ip, settings);
        }
      }
      catch (Exception e)
      {
        Logger.Info("DlnaMediaServer: Exception reading profile links (Text: '{0}')", e.Message);
      }
    }

    public static void SaveProfileLinks()
    {
      try
      {
        ProfileLinks profileLinks = new ProfileLinks();
        foreach (KeyValuePair<IPAddress, EndPointSettings> pair in ProfileLinks)
        {
          ProfileLink link = new ProfileLink();
          link.IP = pair.Key.ToString();
          if (pair.Value.Profile == null)
          {
            link.Profile = "None";
          }
          else
          {
            link.Profile = pair.Value.Profile.ID;
          }
          link.PreferredAudioLanguages = pair.Value.PreferredAudioLanguages;
          link.PreferredSubtitleLanguages = pair.Value.PreferredSubtitleLanguages;
          link.DefaultSubtitleEncodings = pair.Value.DefaultSubtitleEncodings;
          profileLinks.Links.Add(link);
        }

        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        settingsManager.Save(profileLinks);
      }
      catch (Exception e)
      {
        Logger.Info("DlnaMediaServer: Exception saving profile links (Text: '{0}')", e.Message);
      }
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
