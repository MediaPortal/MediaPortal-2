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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Xml;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.MediaServer.DIDL;
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Extensions.MediaServer.Filters;
using MediaPortal.Extensions.MediaServer.Protocols;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Utilities.FileSystem;

//Thanks goes to the Serviio team over at http://www.serviio.org/
//Their profile structure was inspiring and the community driven DLNA profiling is very effective 

namespace MediaPortal.Extensions.MediaServer.Profiles
{
  public class ProfileManager
  {
    private const string DLNA_DEFAULT_PROFILE_ID = "DLNADefault";
    private const string PROFILE_FILE = "DLNAProfiles.xml";
    private const string PROFILE_LINK_FILE = "MediaPortal.Extensions.MediaServer.Links.xml";
    private const string LANGUAGE_FILE = "MediaPortal.Extensions.MediaServer.PreferredLanguages.xml";

    public static Dictionary<IPAddress, EndPointSettings> ProfileLinks = new Dictionary<IPAddress, EndPointSettings>();
    private static EndPointSettings PreferredLanguages;
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
            if (headers["remote_addr"] == null)
            {
              IPAddress ip = ResolveIpAddress(headers["remote_addr"]);
              ProfileLinks.Add(ip, GetEndPointSettings(profile.Value.ID));
              return ProfileLinks[ip];
            }
            return GetEndPointSettings(profile.Value.ID);
          }
        }
      }

      // nop match => return Defaul Profile
      Logger.Info("DetectProfile: No profile found => using {0}, headers={1}", DLNA_DEFAULT_PROFILE_ID, string.Join(", ", headers.AllKeys.Select(key => key + ": " + headers[key]).ToArray()));
      if (headers["remote_addr"] == null)
      {
        IPAddress ip = ResolveIpAddress(headers["remote_addr"]);
        ProfileLinks.Add(ip, GetEndPointSettings(DLNA_DEFAULT_PROFILE_ID));
        return ProfileLinks[ip];
      }
      return GetEndPointSettings(DLNA_DEFAULT_PROFILE_ID);
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

                    profile.Settings.Video.MaxHeight = Profiles[parentProfileId].Settings.Video.MaxHeight;
                    profile.Settings.Video.H262QualityFactor = Profiles[parentProfileId].Settings.Video.H262QualityFactor;
                    profile.Settings.Video.H262TargetPreset = Profiles[parentProfileId].Settings.Video.H262TargetPreset;
                    profile.Settings.Video.H262TargetProfile = Profiles[parentProfileId].Settings.Video.H262TargetProfile;
                    profile.Settings.Video.H264LevelCheckMethod = Profiles[parentProfileId].Settings.Video.H264LevelCheckMethod;
                    profile.Settings.Video.H264Level = Profiles[parentProfileId].Settings.Video.H264Level;
                    profile.Settings.Video.H264QualityFactor = Profiles[parentProfileId].Settings.Video.H264QualityFactor;
                    profile.Settings.Video.H264TargetPreset = Profiles[parentProfileId].Settings.Video.H264TargetPreset;
                    profile.Settings.Video.H264TargetProfile = Profiles[parentProfileId].Settings.Video.H264TargetProfile;
                    profile.Settings.Video.H265Level = Profiles[parentProfileId].Settings.Video.H265Level;
                    profile.Settings.Video.H265QualityFactor = Profiles[parentProfileId].Settings.Video.H265QualityFactor;
                    profile.Settings.Video.H265TargetPreset = Profiles[parentProfileId].Settings.Video.H265TargetPreset;
                    profile.Settings.Video.H265TargetProfile = Profiles[parentProfileId].Settings.Video.H265TargetProfile;
                    profile.Settings.Video.Quality = Profiles[parentProfileId].Settings.Video.Quality;
                    profile.Settings.Video.QualityFactor = Profiles[parentProfileId].Settings.Video.QualityFactor;
                    profile.Settings.Video.CoderType = Profiles[parentProfileId].Settings.Video.CoderType;

                    profile.Settings.Images.AutoRotate = Profiles[parentProfileId].Settings.Images.AutoRotate;
                    profile.Settings.Images.MaxHeight = Profiles[parentProfileId].Settings.Images.MaxHeight;
                    profile.Settings.Images.MaxWidth = Profiles[parentProfileId].Settings.Images.MaxWidth;
                    profile.Settings.Images.CoderType = Profiles[parentProfileId].Settings.Images.CoderType;

                    profile.Settings.Audio.DefaultBitrate = Profiles[parentProfileId].Settings.Audio.DefaultBitrate;
                    profile.Settings.Audio.DefaultStereo = Profiles[parentProfileId].Settings.Audio.DefaultStereo;
                    profile.Settings.Audio.CoderType = Profiles[parentProfileId].Settings.Audio.CoderType;

                    profile.Settings.Communication.AllowChunckedTransfer = Profiles[parentProfileId].Settings.Communication.AllowChunckedTransfer;
                    profile.Settings.Communication.DefaultBufferSize = Profiles[parentProfileId].Settings.Communication.DefaultBufferSize;
                    profile.Settings.Communication.InitialBufferSize = Profiles[parentProfileId].Settings.Communication.InitialBufferSize;

                    profile.Settings.Metadata.Delivery = Profiles[parentProfileId].Settings.Metadata.Delivery;

                    profile.Settings.Subtitles.SubtitleMode = Profiles[parentProfileId].Settings.Subtitles.SubtitleMode;
                    profile.Settings.Subtitles.SubtitlesSupported = new List<ProfileSubtitle>(Profiles[parentProfileId].Settings.Subtitles.SubtitlesSupported);

                    profile.MediaTranscoding.Audio = new List<AudioTranscodingTarget>();
                    foreach (AudioTranscodingTarget aTrans in Profiles[parentProfileId].MediaTranscoding.Audio)
                    {
                      if (aTrans.Target.ForceInheritance == true)
                      {
                        profile.MediaTranscoding.Audio.Add(aTrans);
                      }
                    }
                    profile.MediaTranscoding.Images = new List<ImageTranscodingTarget>();
                    foreach (ImageTranscodingTarget iTrans in Profiles[parentProfileId].MediaTranscoding.Images)
                    {
                      if (iTrans.Target.ForceInheritance == true)
                      {
                        profile.MediaTranscoding.Images.Add(iTrans);
                      }
                    }
                    profile.MediaTranscoding.Video = new List<VideoTranscodingTarget>();
                    foreach (VideoTranscodingTarget vTrans in Profiles[parentProfileId].MediaTranscoding.Video)
                    {
                      if (vTrans.Target.ForceInheritance == true)
                      {
                        profile.MediaTranscoding.Video.Add(vTrans);
                      }
                    }
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
            else if (nodeName == "MediaTranscoding" && reader.NodeType == XmlNodeType.Element)
            {
              ReadTranscoding(reader, reader.Name, ref profile.MediaTranscoding.Video, ref profile.MediaTranscoding.Audio, ref profile.MediaTranscoding.Images);
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
                else if (reader.Name == "Video" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "maxHeight")
                    {
                      profile.Settings.Video.MaxHeight = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "qualityMode")
                    {
                      profile.Settings.Video.Quality = (QualityMode)Enum.Parse(typeof(QualityMode), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "qualityFactor")
                    {
                      profile.Settings.Video.QualityFactor = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "coder")
                    {
                      profile.Settings.Video.CoderType = (Coder)Enum.Parse(typeof(Coder), reader.ReadContentAsString(), true);
                    }
                  }
                }
                else if (reader.Name == "H262Video" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "qualityFactor")
                    {
                      profile.Settings.Video.QualityFactor = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "preset")
                    {
                      profile.Settings.Video.H262TargetPreset = (EncodingPreset)Enum.Parse(typeof(EncodingPreset), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "profile")
                    {
                      profile.Settings.Video.H262TargetProfile = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), reader.ReadContentAsString(), true);
                    }
                  }
                }
                else if (reader.Name == "H264Video" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "levelCheck")
                    {
                      profile.Settings.Video.H264LevelCheckMethod = (LevelCheck)Enum.Parse(typeof(LevelCheck), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "qualityFactor")
                    {
                      profile.Settings.Video.H264QualityFactor = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "preset")
                    {
                      profile.Settings.Video.H264TargetPreset = (EncodingPreset)Enum.Parse(typeof(EncodingPreset), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "profile")
                    {
                      profile.Settings.Video.H264TargetProfile = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "level")
                    {
                      profile.Settings.Video.H264Level = Convert.ToSingle(reader.ReadContentAsString(), CultureInfo.InvariantCulture);
                    }
                  }
                }
                else if (reader.Name == "H265Video" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "qualityFactor")
                    {
                      profile.Settings.Video.H265QualityFactor = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "preset")
                    {
                      profile.Settings.Video.H265TargetPreset = (EncodingPreset)Enum.Parse(typeof(EncodingPreset), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "profile")
                    {
                      profile.Settings.Video.H265TargetProfile = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "level")
                    {
                      profile.Settings.Video.H265Level = Convert.ToSingle(reader.ReadContentAsString(), CultureInfo.InvariantCulture);
                    }
                  }
                }
                else if (reader.Name == "Images" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "autoRotate")
                    {
                      profile.Settings.Images.AutoRotate = reader.ReadContentAsBoolean();
                    }
                    else if (reader.Name == "maxWidth")
                    {
                      profile.Settings.Images.MaxWidth = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "maxHeight")
                    {
                      profile.Settings.Images.MaxHeight = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "qualityMode")
                    {
                      profile.Settings.Images.Quality = (QualityMode)Enum.Parse(typeof(QualityMode), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "coder")
                    {
                      profile.Settings.Images.CoderType = (Coder)Enum.Parse(typeof(Coder), reader.ReadContentAsString(), true);
                    }
                  }
                }
                else if (reader.Name == "Audio" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "defaultStereo")
                    {
                      profile.Settings.Audio.DefaultStereo = reader.ReadContentAsBoolean();
                    }
                    else if (reader.Name == "defaultBitrate")
                    {
                      profile.Settings.Audio.DefaultBitrate = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "coder")
                    {
                      profile.Settings.Audio.CoderType = (Coder)Enum.Parse(typeof(Coder), reader.ReadContentAsString(), true);
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
                else if (reader.Name == "Subtitles" && reader.NodeType == XmlNodeType.Element)
                {
                  XmlReader subReader = reader.ReadSubtree();
                  profile.Settings.Subtitles.SubtitlesSupported.Clear();
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "support")
                    {
                      profile.Settings.Subtitles.SubtitleMode = (SubtitleSupport)Enum.Parse(typeof(SubtitleSupport), reader.ReadContentAsString(), true);
                    }
                  }
                  while (subReader.Read())
                  {
                    if (subReader.Name == "Subtitle" && subReader.NodeType == XmlNodeType.Element)
                    {
                      ProfileSubtitle newSub = new ProfileSubtitle();
                      while (subReader.MoveToNextAttribute()) // Read the attributes.
                      {
                        if (subReader.Name == "format")
                        {
                          newSub.Format = (SubtitleCodec)Enum.Parse(typeof(SubtitleCodec), subReader.ReadContentAsString(), true);
                        }
                        else if (subReader.Name == "mime")
                        {
                          newSub.Mime = subReader.ReadContentAsString();
                        }
                      }
                      profile.Settings.Subtitles.SubtitlesSupported.Add(newSub);
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
        }
      }
      catch (Exception e)
      {
        Logger.Info("DlnaMediaServer: Exception reading profiles (Text: '{0}')", e.Message);
      }
    }

    private static void ReadTranscoding(XmlTextReader reader, string elementName, ref List<VideoTranscodingTarget> vTrans, ref List<AudioTranscodingTarget> aTrans, ref List<ImageTranscodingTarget> iTrans)
    {
      if (vTrans == null)
      {
        vTrans = new List<VideoTranscodingTarget>();
      }
      if (aTrans == null)
      {
        aTrans = new List<AudioTranscodingTarget>();
      }
      if (iTrans == null)
      {
        iTrans = new List<ImageTranscodingTarget>();
      }

      List<VideoTranscodingTarget> vList = new List<VideoTranscodingTarget>();
      List<AudioTranscodingTarget> aList = new List<AudioTranscodingTarget>();
      List<ImageTranscodingTarget> iList = new List<ImageTranscodingTarget>();

      VideoTranscodingTarget vTranscoding = new VideoTranscodingTarget();
      AudioTranscodingTarget aTranscoding = new AudioTranscodingTarget();
      ImageTranscodingTarget iTranscoding = new ImageTranscodingTarget();

      while (reader.Read())
      {
        if (reader.Name == "VideoTarget" && reader.NodeType == XmlNodeType.Element)
        {
          vTranscoding = new VideoTranscodingTarget();
          vTranscoding.Target = new VideoInfo();
          while (reader.MoveToNextAttribute()) // Read the attributes.
          {
            if (reader.Name == "container")
            {
              vTranscoding.Target.VideoContainerType = (VideoContainer)Enum.Parse(typeof(VideoContainer), reader.ReadContentAsString(), true);
            }
            else if (reader.Name == "movflags")
            {
              vTranscoding.Target.Movflags = reader.ReadContentAsString();
            }
            else if (reader.Name == "videoCodec")
            {
              vTranscoding.Target.VideoCodecType = (VideoCodec)Enum.Parse(typeof(VideoCodec), reader.ReadContentAsString(), true);
            }
            else if (reader.Name == "videoFourCC")
            {
              vTranscoding.Target.FourCC = reader.ReadContentAsString();
            }
            else if (reader.Name == "videoAR")
            {
              vTranscoding.Target.AspectRatio = Convert.ToSingle(reader.ReadContentAsString(), CultureInfo.InvariantCulture);
            }
            else if (reader.Name == "videoProfile")
            {
              vTranscoding.Target.EncodingProfileType = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), reader.ReadContentAsString(), true);
            }
            else if (reader.Name == "videoLevel")
            {
              vTranscoding.Target.LevelMinimum = Convert.ToSingle(reader.ReadContentAsString(), CultureInfo.InvariantCulture);
            }
            else if (reader.Name == "videoPreset")
            {
              vTranscoding.Target.TargetPresetType = (EncodingPreset)Enum.Parse(typeof(EncodingPreset), reader.ReadContentAsString(), true);
            }
            else if (reader.Name == "qualityMode")
            {
              vTranscoding.Target.QualityType = (QualityMode)Enum.Parse(typeof(QualityMode), reader.ReadContentAsString(), true);
            }
            else if (reader.Name == "videoBrandExclusion")
            {
              vTranscoding.Target.BrandExclusion = reader.ReadContentAsString();
            }
            else if (reader.Name == "videoMaxBitrate")
            {
              vTranscoding.Target.MaxVideoBitrate = reader.ReadContentAsLong();
            }
            else if (reader.Name == "videoMaxHeight")
            {
              vTranscoding.Target.MaxVideoHeight = reader.ReadContentAsInt();
            }
            else if (reader.Name == "videoSquarePixels")
            {
              vTranscoding.Target.SquarePixels = reader.ReadContentAsBoolean();
            }
            else if (reader.Name == "videoPixelFormat")
            {
              vTranscoding.Target.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), reader.ReadContentAsString(), true);
            }
            else if (reader.Name == "audioCodec")
            {
              vTranscoding.Target.AudioCodecType = (AudioCodec)Enum.Parse(typeof(AudioCodec), reader.ReadContentAsString(), true);
            }
            else if (reader.Name == "audioBitrate")
            {
              vTranscoding.Target.AudioBitrate = reader.ReadContentAsLong();
            }
            else if (reader.Name == "audioFrequency")
            {
              vTranscoding.Target.AudioFrequency = reader.ReadContentAsLong();
            }
            else if (reader.Name == "audioMultiChannel")
            {
              vTranscoding.Target.AudioMultiChannel = reader.ReadContentAsBoolean();
            }
            else if (reader.Name == "forceTranscoding")
            {
              vTranscoding.Target.ForceVideoTranscoding = reader.ReadContentAsBoolean();
            }
            else if (reader.Name == "forceStereo")
            {
              vTranscoding.Target.ForceStereo = reader.ReadContentAsBoolean();
            }
            else if (reader.Name == "forceInheritance")
            {
              vTranscoding.Target.ForceInheritance = reader.ReadContentAsBoolean();
            }
            else if (reader.Name == "transcoder")
            {
              vTranscoding.TranscoderBinPath = reader.ReadContentAsString();
            }
            else if (reader.Name == "transcoderArguments")
            {
              vTranscoding.TranscoderArguments = reader.ReadContentAsString();
            }
          }
          while (reader.Read())
          {
            if (reader.Name == "VideoTarget" && reader.NodeType == XmlNodeType.EndElement)
            {
              vList.Add(vTranscoding);
              break;
            }
            if (reader.Name == "VideoSource" && reader.NodeType == XmlNodeType.Element)
            {
              if (vTranscoding.Sources == null)
              {
                vTranscoding.Sources = new List<VideoInfo>();
              }
              VideoInfo src = new VideoInfo();
              while (reader.MoveToNextAttribute()) // Read the attributes.
              {
                if (reader.Name == "container")
                {
                  src.VideoContainerType = (VideoContainer)Enum.Parse(typeof(VideoContainer), reader.ReadContentAsString(), true);
                }
                else if (reader.Name == "videoCodec")
                {
                  src.VideoCodecType = (VideoCodec)Enum.Parse(typeof(VideoCodec), reader.ReadContentAsString(), true);
                }
                else if (reader.Name == "videoFourCC")
                {
                  src.FourCC = reader.ReadContentAsString();
                }
                else if (reader.Name == "videoAR")
                {
                  src.AspectRatio = Convert.ToSingle(reader.ReadContentAsString(), CultureInfo.InvariantCulture);
                }
                else if (reader.Name == "videoProfile")
                {
                  src.EncodingProfileType = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), reader.ReadContentAsString(), true);
                }
                else if (reader.Name == "videoLevel")
                {
                  src.LevelMinimum = Convert.ToSingle(reader.ReadContentAsString(), CultureInfo.InvariantCulture);
                }
                else if (reader.Name == "videoBrandExclusion")
                {
                  src.BrandExclusion = reader.ReadContentAsString();
                }
                else if (reader.Name == "videoMaxBitrate")
                {
                  src.MaxVideoBitrate = reader.ReadContentAsLong();
                }
                else if (reader.Name == "videoMaxHeight")
                {
                  src.MaxVideoHeight = reader.ReadContentAsInt();
                }
                else if (reader.Name == "videoSquarePixels")
                {
                  src.SquarePixels = reader.ReadContentAsBoolean();
                }
                else if (reader.Name == "videoPixelFormat")
                {
                  src.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), reader.ReadContentAsString(), true);
                }
                else if (reader.Name == "audioCodec")
                {
                  src.AudioCodecType = (AudioCodec)Enum.Parse(typeof(AudioCodec), reader.ReadContentAsString(), true);
                }
                else if (reader.Name == "audioBitrate")
                {
                  src.AudioBitrate = reader.ReadContentAsLong();
                }
                else if (reader.Name == "audioFrequency")
                {
                  src.AudioFrequency = reader.ReadContentAsLong();
                }
                else if (reader.Name == "audioMultiChannel")
                {
                  src.AudioMultiChannel = reader.ReadContentAsBoolean();
                }
              }
              vTranscoding.Sources.Add(src);
            }
          }
        }
        else if (reader.Name == "AudioTarget" && reader.NodeType == XmlNodeType.Element)
        {
          aTranscoding = new AudioTranscodingTarget();
          aTranscoding.Target = new AudioInfo();
          while (reader.MoveToNextAttribute()) // Read the attributes.
          {
            if (reader.Name == "container")
            {
              aTranscoding.Target.AudioContainerType = (AudioContainer)Enum.Parse(typeof(AudioContainer), reader.ReadContentAsString(), true);
            }
            else if (reader.Name == "audioBitrate")
            {
              aTranscoding.Target.Bitrate = reader.ReadContentAsLong();
            }
            else if (reader.Name == "audioFrequency")
            {
              aTranscoding.Target.Frequency = reader.ReadContentAsLong();
            }
            else if (reader.Name == "forceStereo")
            {
              aTranscoding.Target.ForceStereo = reader.ReadContentAsBoolean();
            }
            else if (reader.Name == "forceInheritance")
            {
              aTranscoding.Target.ForceInheritance = reader.ReadContentAsBoolean();
            }
            else if (reader.Name == "transcoder")
            {
              aTranscoding.TranscoderBinPath = reader.ReadContentAsString();
            }
            else if (reader.Name == "transcoderArguments")
            {
              aTranscoding.TranscoderArguments = reader.ReadContentAsString();
            }
          }
          while (reader.Read())
          {
            if (reader.Name == "AudioTarget" && reader.NodeType == XmlNodeType.EndElement)
            {
              aList.Add(aTranscoding);
              break;
            }
            if (reader.Name == "AudioSource" && reader.NodeType == XmlNodeType.Element)
            {
              if (aTranscoding.Sources == null)
              {
                aTranscoding.Sources = new List<AudioInfo>();
              }
              AudioInfo src = new AudioInfo();
              while (reader.MoveToNextAttribute()) // Read the attributes.
              {
                if (reader.Name == "container")
                {
                  src.AudioContainerType = (AudioContainer)Enum.Parse(typeof(AudioContainer), reader.ReadContentAsString(), true);
                }
                else if (reader.Name == "audioBitrate")
                {
                  src.Bitrate = reader.ReadContentAsLong();
                }
                else if (reader.Name == "audioFrequency")
                {
                  src.Frequency = reader.ReadContentAsLong();
                }
              }
              aTranscoding.Sources.Add(src);
            }
          }
        }
        else if (reader.Name == "ImageTarget" && reader.NodeType == XmlNodeType.Element)
        {
          iTranscoding = new ImageTranscodingTarget();
          iTranscoding.Target = new ImageInfo();
          while (reader.MoveToNextAttribute()) // Read the attributes.
          {
            if (reader.Name == "container")
            {
              iTranscoding.Target.ImageContainerType = (ImageContainer)Enum.Parse(typeof(ImageContainer), reader.ReadContentAsString(), true);
            }
            else if (reader.Name == "pixelFormat")
            {
              iTranscoding.Target.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), reader.ReadContentAsString(), true);
            }
            else if (reader.Name == "qualityMode")
            {
              iTranscoding.Target.QualityType = (QualityMode)Enum.Parse(typeof(QualityMode), reader.ReadContentAsString(), true);
            }
            else if (reader.Name == "forceInheritance")
            {
              iTranscoding.Target.ForceInheritance = reader.ReadContentAsBoolean();
            }
            else if (reader.Name == "transcoder")
            {
              iTranscoding.TranscoderBinPath = reader.ReadContentAsString();
            }
            else if (reader.Name == "transcoderOptions")
            {
              iTranscoding.TranscoderArguments = reader.ReadContentAsString();
            }
          }
          while (reader.Read())
          {
            if (reader.Name == "ImageTarget" && reader.NodeType == XmlNodeType.EndElement)
            {
              iList.Add(iTranscoding);
              break;
            }
            if (reader.Name == "ImageSource" && reader.NodeType == XmlNodeType.Element)
            {
              if (iTranscoding.Sources == null)
              {
                iTranscoding.Sources = new List<ImageInfo>();
              }
              ImageInfo src = new ImageInfo();
              while (reader.MoveToNextAttribute()) // Read the attributes.
              {
                if (reader.Name == "container")
                {
                  src.ImageContainerType = (ImageContainer)Enum.Parse(typeof(ImageContainer), reader.ReadContentAsString(), true);
                }
                else if (reader.Name == "pixelFormat")
                {
                  src.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), reader.ReadContentAsString(), true);
                }
              }
              iTranscoding.Sources.Add(src);
            }
          }
        }
        if (reader.Name == elementName && reader.NodeType == XmlNodeType.EndElement)
        {
          break;
        }
      }

      //Own transcoding profiles should have higher priority than inherited ones
      vList.AddRange(vTrans);
      aList.AddRange(aTrans);
      iList.AddRange(iTrans);
      vTrans = vList;
      aTrans = aList;
      iTrans = iList;
    }

    public static EndPointSettings GetEndPointSettings(string profileId)
    {
      EndPointSettings settings = new EndPointSettings
      {
        PreferredSubtitleLanguages = "EN",
        PreferredAudioLanguages = "EN",
        DefaultSubtitleEncodings = ""
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

        if (PreferredLanguages != null)
        {
          settings.PreferredSubtitleLanguages = PreferredLanguages.PreferredSubtitleLanguages;
          settings.DefaultSubtitleEncodings = PreferredLanguages.DefaultSubtitleEncodings;
          settings.PreferredAudioLanguages = PreferredLanguages.PreferredAudioLanguages;
        }
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
        IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
        string dataPath = pathManager.GetPath("<CONFIG>");
        string linkFile = Path.Combine(dataPath, PROFILE_LINK_FILE);
        if (File.Exists(linkFile) == true)
        {
          XmlDocument document = new XmlDocument();
          document.Load(linkFile);
          XmlNode configNode = document.SelectSingleNode("Configuration");
          XmlNode node = null;
          if (configNode != null)
          {
            node = configNode.SelectSingleNode("ProfileLinks");
          }
          if (node != null)
          {
            foreach (XmlNode childNode in node.ChildNodes)
            {
              IPAddress ip = null;
              foreach (XmlAttribute attribute in childNode.Attributes)
              {
                if (attribute.Name == "IPv4")
                {
                  ip = IPAddress.Parse(attribute.InnerText);
                }
                else if (attribute.Name == "IPv6")
                {
                  ip = IPAddress.Parse(attribute.InnerText);
                }
              }

              EndPointSettings settings = new EndPointSettings();
              settings.PreferredSubtitleLanguages = "EN";
              settings.PreferredAudioLanguages = "EN";
              settings.DefaultSubtitleEncodings = "";
              foreach (XmlNode subChildNode in childNode.ChildNodes)
              {
                if (subChildNode.Name == "Profile")
                {
                  string profileId = Convert.ToString(childNode.InnerText);
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
                }
                else if (subChildNode.Name == "Subtitles")
                {
                  foreach (XmlAttribute attribute in childNode.Attributes)
                  {
                    if (attribute.Name == "PreferredLanguages")
                    {
                      settings.PreferredSubtitleLanguages = attribute.InnerText;
                    }
                    else if (attribute.Name == "DefaultEncodings")
                    {
                      settings.DefaultSubtitleEncodings = attribute.InnerText;
                    }
                  }
                }
                else if (subChildNode.Name == "Audio")
                {
                  foreach (XmlAttribute attribute in childNode.Attributes)
                  {
                    if (attribute.Name == "PreferredLanguages")
                    {
                      settings.PreferredAudioLanguages = attribute.InnerText;
                    }
                  }
                }
              }
              settings.InitialiseContainerTree();
              if (settings.Profile == null)
                Logger.Info("DlnaMediaServer: IP: {0}, using profile: None", ip);
              else
                Logger.Info("DlnaMediaServer: IP: {0}, using profile: {1}", ip, settings.Profile.ID);
              ProfileLinks.Add(ip, settings);
            }
          }
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
        IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
        string dataPath = pathManager.GetPath("<CONFIG>");
        string linkFile = Path.Combine(dataPath, PROFILE_LINK_FILE);
        if (Profiles.Count == 0) return; //Avoid overwriting of exisitng links if no profiles.xml found
        XmlDocument document = new XmlDocument();
        if (File.Exists(linkFile) == true)
        {
          document.Load(linkFile);
        }
        XmlNode configNode = document.SelectSingleNode("Configuration");
        XmlNode node = null;
        if (configNode != null)
        {
          node = configNode.SelectSingleNode("ProfileLinks");
          if (node == null)
          {
            node = document.CreateElement("ProfileLinks");
            configNode.AppendChild(node);
          }
        }
        else
        {
          configNode = document.CreateElement("Configuration");
          document.AppendChild(configNode);
          node = document.CreateElement("ProfileLinks");
          configNode.AppendChild(node);
        }
        if (node != null)
        {
          node.RemoveAll();
          foreach (KeyValuePair<IPAddress, EndPointSettings> pair in ProfileLinks)
          {
            XmlNode attr;
            XmlElement ipElem = document.CreateElement("IP");
            if (pair.Key.AddressFamily == AddressFamily.InterNetwork)
            {
              attr = document.CreateNode(XmlNodeType.Attribute, "IPv4", null);
              attr.InnerText = pair.Key.ToString();
              ipElem.Attributes.SetNamedItem(attr);
            }
            else if (pair.Key.AddressFamily == AddressFamily.InterNetworkV6)
            {
              attr = document.CreateNode(XmlNodeType.Attribute, "IPv6", null);
              attr.InnerText = pair.Key.ToString();
              ipElem.Attributes.SetNamedItem(attr);
            }

            XmlElement profileElem = document.CreateElement("Profile");
            if (pair.Value.Profile == null)
            {
              profileElem.InnerText = "None";
            }
            else
            {
              profileElem.InnerText = pair.Value.Profile.ID;
            }
            ipElem.AppendChild(profileElem);

            XmlElement subtitleElem = document.CreateElement("Subtitles");
            attr = document.CreateNode(XmlNodeType.Attribute, "PreferredLanguages", null);
            attr.InnerText = pair.Value.PreferredSubtitleLanguages;
            subtitleElem.Attributes.SetNamedItem(attr);
            attr = document.CreateNode(XmlNodeType.Attribute, "DefaultEncodings", null);
            attr.InnerText = pair.Value.DefaultSubtitleEncodings;
            subtitleElem.Attributes.SetNamedItem(attr);
            ipElem.AppendChild(subtitleElem);

            XmlElement audioElem = document.CreateElement("Audio");
            attr = document.CreateNode(XmlNodeType.Attribute, "PreferredLanguages", null);
            attr.InnerText = pair.Value.PreferredAudioLanguages;
            audioElem.Attributes.SetNamedItem(attr);
            ipElem.AppendChild(audioElem);

            node.AppendChild(ipElem);
          }
        }

        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Indent = true;
        settings.IndentChars = "\t";
        settings.NewLineChars = Environment.NewLine;
        settings.NewLineHandling = NewLineHandling.Replace;
        using (XmlWriter writer = XmlWriter.Create(linkFile, settings))
        {
          document.Save(writer);
        }
      }
      catch (Exception e)
      {
        Logger.Info("DlnaMediaServer: Exception saving profile links (Text: '{0}')", e.Message);
      }
    }

    public static void LoadPreferredLanguages()
    {
      try
      {
        IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
        string dataPath = pathManager.GetPath("<CONFIG>");
        string linkFile = Path.Combine(dataPath, LANGUAGE_FILE);
        if (File.Exists(linkFile))
        {
          XmlDocument document = new XmlDocument();
          document.Load(linkFile);
          XmlNode configNode = document.SelectSingleNode("Configuration");
          XmlNode node = null;
          if (configNode != null)
          {
            node = configNode.SelectSingleNode("PreferredLanguages");
          }
          if (node != null)
          {
            EndPointSettings settings = new EndPointSettings();
            settings.PreferredSubtitleLanguages = "EN";
            settings.PreferredAudioLanguages = "EN";
            settings.DefaultSubtitleEncodings = "";
            foreach (XmlNode childNode in node.ChildNodes)
            {
              if (childNode.Name == "Subtitles")
              {
                foreach (XmlAttribute attribute in childNode.Attributes)
                {
                  if (attribute.Name == "PreferredLanguages")
                  {
                    settings.PreferredSubtitleLanguages = attribute.InnerText;
                  }
                  else if (attribute.Name == "DefaultEncodings")
                  {
                    settings.DefaultSubtitleEncodings = attribute.InnerText;
                  }
                }
              }
              else if (childNode.Name == "Audio")
              {
                foreach (XmlAttribute attribute in childNode.Attributes)
                {
                  if (attribute.Name == "PreferredLanguages")
                  {
                    settings.PreferredAudioLanguages = attribute.InnerText;
                  }
                }
              }
            }
            settings.InitialiseContainerTree();
            PreferredLanguages = settings;
            Logger.Info("DlnaMediaServer: Loaded preferred languages.");
          }
        }
      }
      catch (Exception e)
      {
        Logger.Info("DlnaMediaServer: Exception reading preferred languages (Text: '{0}')", e.Message);
      }
    }

    public static void SavePreferredLanguages()
    {
      try
      {
        IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
        string dataPath = pathManager.GetPath("<CONFIG>");
        string linkFile = Path.Combine(dataPath, LANGUAGE_FILE);
        XmlDocument document = new XmlDocument();
        if (File.Exists(linkFile))
        {
          document.Load(linkFile);
        }

        // setting default values
        if (PreferredLanguages == null)
        {
          PreferredLanguages = new EndPointSettings();
          PreferredLanguages.PreferredSubtitleLanguages = "EN";
          PreferredLanguages.PreferredAudioLanguages = "EN";
          PreferredLanguages.DefaultSubtitleEncodings = "";
        }

        XmlNode configNode = document.SelectSingleNode("Configuration");
        XmlNode node;
        if (configNode != null)
        {
          node = configNode.SelectSingleNode("PreferredLanguages");
          if (node == null)
          {
            node = document.CreateElement("PreferredLanguages");
            configNode.AppendChild(node);
          }
        }
        else
        {
          configNode = document.CreateElement("Configuration");
          document.AppendChild(configNode);
          node = document.CreateElement("PreferredLanguages");
          configNode.AppendChild(node);
        }
        if (node != null)
        {
          node.RemoveAll();

          XmlNode attr;

          XmlElement subtitleElem = document.CreateElement("Subtitles");
          attr = document.CreateNode(XmlNodeType.Attribute, "PreferredLanguages", null);
          attr.InnerText = PreferredLanguages.PreferredSubtitleLanguages;
          subtitleElem.Attributes.SetNamedItem(attr);
          attr = document.CreateNode(XmlNodeType.Attribute, "DefaultEncodings", null);
          attr.InnerText = PreferredLanguages.DefaultSubtitleEncodings;
          subtitleElem.Attributes.SetNamedItem(attr);
          node.AppendChild(subtitleElem);

          XmlElement audioElem = document.CreateElement("Audio");
          attr = document.CreateNode(XmlNodeType.Attribute, "PreferredLanguages", null);
          attr.InnerText = PreferredLanguages.PreferredAudioLanguages;
          audioElem.Attributes.SetNamedItem(attr);
          node.AppendChild(audioElem);
        }

        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Indent = true;
        settings.IndentChars = "\t";
        settings.NewLineChars = Environment.NewLine;
        settings.NewLineHandling = NewLineHandling.Replace;
        using (XmlWriter writer = XmlWriter.Create(linkFile, settings))
        {
          document.Save(writer);
        }
      }
      catch (Exception e)
      {
        Logger.Info("DlnaMediaServer: Exception saving preferred languages (Text: '{0}')", e.Message);
      }
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
