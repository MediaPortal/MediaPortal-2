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
using System.Globalization;
using System.IO;
using System.Xml;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.Transcoding.Interfaces.Profiles.Setup;
using MediaPortal.Plugins.Transcoding.Interfaces.Profiles.Setup.Settings;
using MediaPortal.Plugins.Transcoding.Interfaces.Profiles.Setup.Targets;
using MediaPortal.Plugins.Transcoding.Interfaces.Profiles.MediaInfo;
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces.Profiles.MediaMatch;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata.Streams;

//Thanks goes to the Serviio team over at http://www.serviio.org/
//Their profile structure was inspiring and the community driven DLNA profiling is very effective 

namespace MediaPortal.Plugins.Transcoding.Interfaces.Profiles
{
  public class TranscodeProfileManager
  {
    public const string INPUT_FILE_TOKEN = "{input}";
    public const string OUTPUT_FILE_TOKEN = "{output}";
    public const string SUBTITLE_FILE_TOKEN = "{subtitle}";

    private static Dictionary<string, Dictionary<string, TranscodingSetup>> _profiles = new Dictionary<string, Dictionary<string, TranscodingSetup>>();

    public static void ClearTranscodeProfiles(string section)
    {
      if (_profiles.ContainsKey(section) == true)
        _profiles[section].Clear();
    }

    public static void AddTranscodingProfile(string section, string profileName, TranscodingSetup profile)
    {
      if (_profiles.ContainsKey(section) == false)
        _profiles.Add(section, new Dictionary<string, TranscodingSetup>());

      if (_profiles[section].ContainsKey(profileName))
      {
        //User profiles can override defaults
        _profiles[section][profileName] = profile;
      }
      else
      {
        _profiles[section].Add(profileName, profile);
      }
    }

    public static void LoadTranscodeProfiles(string section, string profileFile)
    {
      try
      {
        if (File.Exists(profileFile) == true)
        {
          if (_profiles.ContainsKey(section) == false)
            _profiles.Add(section, new Dictionary<string, TranscodingSetup>());

          string profileName = null;
          TranscodingSetup profile = null;
          XmlTextReader reader = new XmlTextReader(profileFile);
          while (reader.Read())
          {
            if (reader.NodeType != XmlNodeType.Element && reader.NodeType != XmlNodeType.EndElement)
            {
              continue;
            }
            string nodeName = reader.Name;

            if (nodeName == "Profile" && reader.NodeType == XmlNodeType.Element)
            {
              profileName = null;
              profile = new TranscodingSetup();
              while (reader.MoveToNextAttribute()) // Read the attributes.
              {
                if (reader.Name == "id")
                {
                  profileName = reader.ReadContentAsString();
                }
                else if (reader.Name == "baseProfile")
                {
                  string parentProfileId = reader.ReadContentAsString();

                  profile.VideoSettings.MaxHeight = _profiles[section][parentProfileId].VideoSettings.MaxHeight;
                  profile.VideoSettings.H262QualityFactor = _profiles[section][parentProfileId].VideoSettings.H262QualityFactor;
                  profile.VideoSettings.H262TargetPreset = _profiles[section][parentProfileId].VideoSettings.H262TargetPreset;
                  profile.VideoSettings.H262TargetProfile = _profiles[section][parentProfileId].VideoSettings.H262TargetProfile;
                  profile.VideoSettings.H264LevelCheckMethod = _profiles[section][parentProfileId].VideoSettings.H264LevelCheckMethod;
                  profile.VideoSettings.H264Level = _profiles[section][parentProfileId].VideoSettings.H264Level;
                  profile.VideoSettings.H264QualityFactor = _profiles[section][parentProfileId].VideoSettings.H264QualityFactor;
                  profile.VideoSettings.H264TargetPreset = _profiles[section][parentProfileId].VideoSettings.H264TargetPreset;
                  profile.VideoSettings.H264TargetProfile = _profiles[section][parentProfileId].VideoSettings.H264TargetProfile;
                  profile.VideoSettings.H265Level = _profiles[section][parentProfileId].VideoSettings.H265Level;
                  profile.VideoSettings.H265QualityFactor = _profiles[section][parentProfileId].VideoSettings.H265QualityFactor;
                  profile.VideoSettings.H265TargetPreset = _profiles[section][parentProfileId].VideoSettings.H265TargetPreset;
                  profile.VideoSettings.H265TargetProfile = _profiles[section][parentProfileId].VideoSettings.H265TargetProfile;
                  profile.VideoSettings.Quality = _profiles[section][parentProfileId].VideoSettings.Quality;
                  profile.VideoSettings.QualityFactor = _profiles[section][parentProfileId].VideoSettings.QualityFactor;
                  profile.VideoSettings.CoderType = _profiles[section][parentProfileId].VideoSettings.CoderType;

                  profile.ImageSettings.AutoRotate = _profiles[section][parentProfileId].ImageSettings.AutoRotate;
                  profile.ImageSettings.MaxHeight = _profiles[section][parentProfileId].ImageSettings.MaxHeight;
                  profile.ImageSettings.MaxWidth = _profiles[section][parentProfileId].ImageSettings.MaxWidth;
                  profile.ImageSettings.CoderType = _profiles[section][parentProfileId].ImageSettings.CoderType;

                  profile.AudioSettings.DefaultBitrate = _profiles[section][parentProfileId].AudioSettings.DefaultBitrate;
                  profile.AudioSettings.DefaultStereo = _profiles[section][parentProfileId].AudioSettings.DefaultStereo;
                  profile.AudioSettings.CoderType = _profiles[section][parentProfileId].AudioSettings.CoderType;

                  profile.SubtitleSettings.SubtitleMode = _profiles[section][parentProfileId].SubtitleSettings.SubtitleMode;
                  profile.SubtitleSettings.SubtitlesSupported = new List<ProfileSubtitle>(_profiles[section][parentProfileId].SubtitleSettings.SubtitlesSupported);

                  if (_profiles[section].ContainsKey(parentProfileId) == true)
                  {
                    profile.AudioTargets = new List<AudioTranscodingTarget>();
                    foreach (AudioTranscodingTarget aTrans in _profiles[section][parentProfileId].AudioTargets)
                    {
                      if (aTrans.Target.ForceInheritance == true)
                      {
                        profile.AudioTargets.Add(aTrans);
                      }
                    }
                    profile.ImageTargets = new List<ImageTranscodingTarget>();
                    foreach (ImageTranscodingTarget iTrans in _profiles[section][parentProfileId].ImageTargets)
                    {
                      if (iTrans.Target.ForceInheritance == true)
                      {
                        profile.ImageTargets.Add(iTrans);
                      }
                    }
                    profile.VideoTargets = new List<VideoTranscodingTarget>();
                    foreach (VideoTranscodingTarget vTrans in _profiles[section][parentProfileId].VideoTargets)
                    {
                      if (vTrans.Target.ForceInheritance == true)
                      {
                        profile.VideoTargets.Add(vTrans);
                      }
                    }
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
                else if (reader.Name == "Video" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "maxHeight")
                    {
                      profile.VideoSettings.MaxHeight = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "qualityMode")
                    {
                      profile.VideoSettings.Quality = (QualityMode)Enum.Parse(typeof(QualityMode), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "qualityFactor")
                    {
                      profile.VideoSettings.QualityFactor = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "coder")
                    {
                      profile.VideoSettings.CoderType = (Coder)Enum.Parse(typeof(Coder), reader.ReadContentAsString(), true);
                    }
                  }
                }
                else if (reader.Name == "H262Video" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "qualityFactor")
                    {
                      profile.VideoSettings.QualityFactor = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "preset")
                    {
                      profile.VideoSettings.H262TargetPreset = (EncodingPreset)Enum.Parse(typeof(EncodingPreset), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "profile")
                    {
                      profile.VideoSettings.H262TargetProfile = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), reader.ReadContentAsString(), true);
                    }
                  }
                }
                else if (reader.Name == "H264Video" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "levelCheck")
                    {
                      profile.VideoSettings.H264LevelCheckMethod = (LevelCheck)Enum.Parse(typeof(LevelCheck), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "qualityFactor")
                    {
                      profile.VideoSettings.H264QualityFactor = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "preset")
                    {
                      profile.VideoSettings.H264TargetPreset = (EncodingPreset)Enum.Parse(typeof(EncodingPreset), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "profile")
                    {
                      profile.VideoSettings.H264TargetProfile = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "level")
                    {
                      profile.VideoSettings.H264Level = Convert.ToSingle(reader.ReadContentAsString(), CultureInfo.InvariantCulture);
                    }
                  }
                }
                else if (reader.Name == "H265Video" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "qualityFactor")
                    {
                      profile.VideoSettings.H265QualityFactor = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "preset")
                    {
                      profile.VideoSettings.H265TargetPreset = (EncodingPreset)Enum.Parse(typeof(EncodingPreset), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "profile")
                    {
                      profile.VideoSettings.H265TargetProfile = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "level")
                    {
                      profile.VideoSettings.H265Level = Convert.ToSingle(reader.ReadContentAsString(), CultureInfo.InvariantCulture);
                    }
                  }
                }
                else if (reader.Name == "Images" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "autoRotate")
                    {
                      profile.ImageSettings.AutoRotate = reader.ReadContentAsBoolean();
                    }
                    else if (reader.Name == "maxWidth")
                    {
                      profile.ImageSettings.MaxWidth = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "maxHeight")
                    {
                      profile.ImageSettings.MaxHeight = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "qualityMode")
                    {
                      profile.ImageSettings.Quality = (QualityMode)Enum.Parse(typeof(QualityMode), reader.ReadContentAsString(), true);
                    }
                    else if (reader.Name == "coder")
                    {
                      profile.ImageSettings.CoderType = (Coder)Enum.Parse(typeof(Coder), reader.ReadContentAsString(), true);
                    }
                  }
                }
                else if (reader.Name == "Audio" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "defaultStereo")
                    {
                      profile.AudioSettings.DefaultStereo = reader.ReadContentAsBoolean();
                    }
                    else if (reader.Name == "defaultBitrate")
                    {
                      profile.AudioSettings.DefaultBitrate = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "coder")
                    {
                      profile.AudioSettings.CoderType = (Coder)Enum.Parse(typeof(Coder), reader.ReadContentAsString(), true);
                    }
                  }
                }
                else if (reader.Name == "Subtitles" && reader.NodeType == XmlNodeType.Element)
                {
                  XmlReader subReader = reader.ReadSubtree();
                  profile.SubtitleSettings.SubtitlesSupported.Clear();
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "support")
                    {
                      profile.SubtitleSettings.SubtitleMode = (SubtitleSupport)Enum.Parse(typeof(SubtitleSupport), reader.ReadContentAsString(), true);
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
                      profile.SubtitleSettings.SubtitlesSupported.Add(newSub);
                    }
                  }
                }
              }
            }
            else if (nodeName == "MediaTranscoding" && reader.NodeType == XmlNodeType.Element)
            {
              ReadTranscoding(reader, reader.Name, ref profile.VideoTargets, ref profile.AudioTargets, ref profile.ImageTargets);
            }
            else if (nodeName == "Profile" && reader.NodeType == XmlNodeType.EndElement)
            {
              AddTranscodingProfile(section, profileName, profile);
            }
          }
          reader.Close();
        }
      }
      catch (Exception ex)
      {
        Logger.Error("TranscodeProfileManager: Exception reading transcoding profiles", ex);
      }
    }

    private static void ReadTranscoding(XmlTextReader reader, string elementName,
      ref List<VideoTranscodingTarget> vTrans, ref List<AudioTranscodingTarget> aTrans, ref List<ImageTranscodingTarget> iTrans)
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

    public static TranscodingSetup GetTranscodeProfile(string section, string profile)
    {
      if (ValidateProfile(section, profile))
      {
        return _profiles[section][profile];
      }
      return null;
    }

    public static bool ValidateProfile(string section, string profile)
    {
      if (_profiles.ContainsKey(section) == true && _profiles[section].ContainsKey(profile) == true)
      {
        return true;
      }
      return false;
    }

    public static VideoTranscoding GetVideoTranscoding(string section, string profile, MetadataContainer info, string preferedAudioLanguages, bool liveStreaming, string transcodeId)
    {
      if (info == null) return null;
      if (ValidateProfile(section, profile) == false) return null;

      TranscodingSetup transSetup = _profiles[section][profile];
      VideoMatch srcVideo;
      VideoTranscodingTarget dstVideo = _profiles[section][profile].GetMatchingVideoTranscoding(info, preferedAudioLanguages, out srcVideo);
      if (dstVideo != null && srcVideo.MatchedVideoSource.Matches(dstVideo.Target) == false)
      {
        VideoTranscoding video = new VideoTranscoding();
        video.SourceAudioStreamIndex = info.Audio[srcVideo.MatchedAudioStream].StreamIndex;
        video.SourceVideoStreamIndex = info.Video.StreamIndex;
        if (info.Metadata.VideoContainerType != VideoContainer.Unknown)
        {
          video.SourceVideoContainer = info.Metadata.VideoContainerType;
        }
        if (info.Audio[srcVideo.MatchedAudioStream].Bitrate > 0)
        {
          video.SourceAudioBitrate = info.Audio[srcVideo.MatchedAudioStream].Bitrate;
        }
        if (info.Audio[srcVideo.MatchedAudioStream].Frequency > 0)
        {
          video.SourceAudioFrequency = info.Audio[srcVideo.MatchedAudioStream].Frequency;
        }
        if (info.Audio[srcVideo.MatchedAudioStream].Channels > 0)
        {
          video.SourceAudioChannels = info.Audio[srcVideo.MatchedAudioStream].Channels;
        }
        if (info.Audio[srcVideo.MatchedAudioStream].Codec != AudioCodec.Unknown)
        {
          video.SourceAudioCodec = info.Audio[srcVideo.MatchedAudioStream].Codec;
        }
        video.SourceSubtitles = new List<SubtitleStream>(info.Subtitles);

        if (info.Video.Bitrate > 0)
        {
          video.SourceVideoBitrate = info.Video.Bitrate;
        }
        if (info.Video.Framerate > 0)
        {
          video.SourceFrameRate = info.Video.Framerate;
        }
        if (info.Video.PixelFormatType != PixelFormat.Unknown)
        {
          video.SourcePixelFormat = info.Video.PixelFormatType;
        }
        if (info.Video.AspectRatio > 0)
        {
          video.SourceVideoAspectRatio = info.Video.AspectRatio;
        }
        if (info.Video.Codec != VideoCodec.Unknown)
        {
          video.SourceVideoCodec = info.Video.Codec;
        }
        if (info.Video.Height > 0)
        {
          video.SourceVideoHeight = info.Video.Height;
        }
        if (info.Video.Width > 0)
        {
          video.SourceVideoWidth = info.Video.Width;
        }
        if (info.Video.PixelAspectRatio > 0)
        {
          video.SourceVideoPixelAspectRatio = info.Video.PixelAspectRatio;
        }

        if (info.Metadata.Duration > 0)
        {
          video.SourceDuration = TimeSpan.FromSeconds(info.Metadata.Duration);
        }
        if (info.Metadata.Source != null)
        {
          video.SourceMedia = info.Metadata.Source;
        }

        if (dstVideo.Target.VideoContainerType != VideoContainer.Unknown)
        {
          video.TargetVideoContainer = dstVideo.Target.VideoContainerType;
        }

        if (dstVideo.Target.Movflags != null)
        {
          video.Movflags = dstVideo.Target.Movflags;
        }

        video.TargetAudioBitrate = transSetup.AudioSettings.DefaultBitrate;
        if (dstVideo.Target.AudioBitrate > 0)
        {
          video.TargetAudioBitrate = dstVideo.Target.AudioBitrate;
        }
        if (dstVideo.Target.AudioFrequency > 0)
        {
          video.TargetAudioFrequency = dstVideo.Target.AudioFrequency;
        }
        if (dstVideo.Target.AudioCodecType != AudioCodec.Unknown)
        {
          video.TargetAudioCodec = dstVideo.Target.AudioCodecType;
        }
        video.TargetForceAudioStereo = transSetup.AudioSettings.DefaultStereo;
        if (dstVideo.Target.ForceStereo)
        {
          video.TargetForceAudioStereo = dstVideo.Target.ForceStereo;
        }

        video.TargetVideoQuality = transSetup.VideoSettings.Quality;
        if (dstVideo.Target.QualityType != QualityMode.Default)
        {
          video.TargetVideoQuality = dstVideo.Target.QualityType;
        }
        if (dstVideo.Target.PixelFormatType != PixelFormat.Unknown)
        {
          video.TargetPixelFormat = dstVideo.Target.PixelFormatType;
        }
        if (dstVideo.Target.AspectRatio > 0)
        {
          video.TargetVideoAspectRatio = dstVideo.Target.AspectRatio;
        }
        if (dstVideo.Target.MaxVideoBitrate > 0)
        {
          video.TargetVideoBitrate = dstVideo.Target.MaxVideoBitrate;
        }
        if (dstVideo.Target.VideoCodecType != VideoCodec.Unknown)
        {
          video.TargetVideoCodec = dstVideo.Target.VideoCodecType;
        }
        video.TargetVideoMaxHeight = transSetup.VideoSettings.MaxHeight;
        if (dstVideo.Target.MaxVideoHeight > 0)
        {
          video.TargetVideoMaxHeight = dstVideo.Target.MaxVideoHeight;
        }
        video.TargetForceVideoTranscoding = dstVideo.Target.ForceVideoTranscoding;

        if (dstVideo.Target.VideoCodecType == VideoCodec.Mpeg2)
        {
          video.TargetQualityFactor = transSetup.VideoSettings.H262QualityFactor;
          video.TargetProfile = transSetup.VideoSettings.H262TargetProfile;
          video.TargetPreset = transSetup.VideoSettings.H262TargetPreset;
        }
        else if (dstVideo.Target.VideoCodecType == VideoCodec.H264)
        {
          video.TargetQualityFactor = transSetup.VideoSettings.H264QualityFactor;
          video.TargetLevel = transSetup.VideoSettings.H264Level;
          if (dstVideo.Target.LevelMinimum > 0)
          {
            video.TargetLevel = dstVideo.Target.LevelMinimum;
          }
          video.TargetProfile = transSetup.VideoSettings.H264TargetProfile;
          if (dstVideo.Target.EncodingProfileType != EncodingProfile.Unknown)
          {
            video.TargetProfile = dstVideo.Target.EncodingProfileType;
          }
          video.TargetPreset = transSetup.VideoSettings.H264TargetPreset;
          if (dstVideo.Target.TargetPresetType != EncodingPreset.Default)
          {
            video.TargetPreset = dstVideo.Target.TargetPresetType;
          }
        }
        else if (dstVideo.Target.VideoCodecType == VideoCodec.H265)
        {
          video.TargetQualityFactor = transSetup.VideoSettings.H265QualityFactor;
          video.TargetLevel = transSetup.VideoSettings.H265Level;
          if (dstVideo.Target.LevelMinimum > 0)
          {
            video.TargetLevel = dstVideo.Target.LevelMinimum;
          }
          video.TargetProfile = transSetup.VideoSettings.H265TargetProfile;
          if (dstVideo.Target.EncodingProfileType != EncodingProfile.Unknown)
          {
            video.TargetProfile = dstVideo.Target.EncodingProfileType;
          }
          video.TargetPreset = transSetup.VideoSettings.H265TargetPreset;
          if (dstVideo.Target.TargetPresetType != EncodingPreset.Default)
          {
            video.TargetPreset = dstVideo.Target.TargetPresetType;
          }
        }

        video.TargetVideoQualityFactor = transSetup.VideoSettings.QualityFactor;
        video.TargetCoder = transSetup.VideoSettings.CoderType;
        video.TargetIsLive = liveStreaming;

        video.TargetSubtitleSupport = transSetup.SubtitleSettings.SubtitleMode;
        if (transSetup.SubtitleSettings.SubtitleMode == SubtitleSupport.HardCoded)
        {
          video.TargetSubtitleSupport = SubtitleSupport.None;
        }

        video.TranscoderBinPath = dstVideo.TranscoderBinPath;
        video.TranscoderArguments = dstVideo.TranscoderArguments;
        video.TranscodeId = transcodeId;

        return video;
      }
      return null;
    }

    public static AudioTranscoding GetAudioTranscoding(string section, string profile, MetadataContainer info, bool liveStreaming, string transcodeId)
    {
      if (info == null) return null;
      if (ValidateProfile(section, profile) == false) return null;

      TranscodingSetup transSetup = _profiles[section][profile];
      AudioMatch srcAudio;
      AudioTranscodingTarget dstAudio = transSetup.GetMatchingAudioTranscoding(info, out srcAudio);
      if (dstAudio != null && srcAudio.MatchedAudioSource.Matches(dstAudio.Target) == false)
      {
        AudioTranscoding audio = new AudioTranscoding();
        if (info.Metadata.AudioContainerType != AudioContainer.Unknown)
        {
          audio.SourceAudioContainer = info.Metadata.AudioContainerType;
        }
        if (info.Audio[srcAudio.MatchedAudioStream].Bitrate > 0)
        {
          audio.SourceAudioBitrate = info.Audio[srcAudio.MatchedAudioStream].Bitrate;
        }
        if (info.Audio[srcAudio.MatchedAudioStream].Frequency > 0)
        {
          audio.SourceAudioFrequency = info.Audio[srcAudio.MatchedAudioStream].Frequency;
        }
        if (info.Audio[srcAudio.MatchedAudioStream].Channels > 0)
        {
          audio.SourceAudioChannels = info.Audio[srcAudio.MatchedAudioStream].Channels;
        }
        if (info.Audio[srcAudio.MatchedAudioStream].Codec != AudioCodec.Unknown)
        {
          audio.SourceAudioCodec = info.Audio[srcAudio.MatchedAudioStream].Codec;
        }
        if (info.Metadata.Duration > 0)
        {
          audio.SourceDuration = TimeSpan.FromSeconds(info.Metadata.Duration);
        }
        if (info.Metadata.Source != null)
        {
          audio.SourceMedia = info.Metadata.Source;
        }

        audio.TargetAudioBitrate = transSetup.AudioSettings.DefaultBitrate;
        if (dstAudio.Target.Bitrate > 0)
        {
          audio.TargetAudioBitrate = dstAudio.Target.Bitrate;
        }
        if (dstAudio.Target.AudioContainerType != AudioContainer.Unknown)
        {
          audio.TargetAudioContainer = dstAudio.Target.AudioContainerType;
        }
        if (dstAudio.Target.Frequency > 0)
        {
          audio.TargetAudioFrequency = dstAudio.Target.Frequency;
        }
        audio.TargetForceAudioStereo = transSetup.AudioSettings.DefaultStereo;
        if (dstAudio.Target.ForceStereo)
        {
          audio.TargetForceAudioStereo = dstAudio.Target.ForceStereo;
        }

        audio.TargetCoder = transSetup.AudioSettings.CoderType;
        audio.TargetIsLive = liveStreaming;

        audio.TranscoderBinPath = dstAudio.TranscoderBinPath;
        audio.TranscoderArguments = dstAudio.TranscoderArguments;
        audio.TranscodeId = transcodeId;
        return audio;
      }
      return null;
    }

    public static ImageTranscoding GetImageTranscoding(string section, string profile, MetadataContainer info, string transcodeId)
    {
      if (info == null) return null;
      if (ValidateProfile(section, profile) == false) return null;

      TranscodingSetup transSetup = _profiles[section][profile];
      ImageMatch srcImage;
      ImageTranscodingTarget dstImage = transSetup.GetMatchingImageTranscoding(info, out srcImage);
      if (dstImage != null && srcImage.MatchedImageSource.Matches(dstImage.Target) == false)
      {
        ImageTranscoding image = new ImageTranscoding();
        if (info.Metadata.ImageContainerType != ImageContainer.Unknown)
        {
          image.SourceImageCodec = info.Metadata.ImageContainerType;
        }
        if (info.Image.Height > 0)
        {
          image.SourceHeight = info.Image.Height;
        }
        if (info.Image.Width > 0)
        {
          image.SourceWidth = info.Image.Width;
        }
        if (info.Image.Orientation > 0)
        {
          image.SourceOrientation = info.Image.Orientation;
        }
        if (info.Image.PixelFormatType != PixelFormat.Unknown)
        {
          image.SourcePixelFormat = info.Image.PixelFormatType;
        }
        if (info.Metadata.Source != null)
        {
          image.SourceMedia = info.Metadata.Source;
        }

        if (dstImage.Target.PixelFormatType > 0)
        {
          image.TargetPixelFormat = dstImage.Target.PixelFormatType;
        }
        if (dstImage.Target.ImageContainerType != ImageContainer.Unknown)
        {
          image.TargetImageCodec = dstImage.Target.ImageContainerType;
        }
        image.TargetImageQuality = transSetup.ImageSettings.Quality;
        if (dstImage.Target.QualityType != QualityMode.Default)
        {
          image.TargetImageQuality = dstImage.Target.QualityType;
        }

        image.TargetImageQualityFactor = transSetup.VideoSettings.QualityFactor;

        image.TargetAutoRotate = transSetup.ImageSettings.AutoRotate;
        image.TargetCoder = transSetup.ImageSettings.CoderType;
        image.TargetHeight = transSetup.ImageSettings.MaxHeight;
        image.TargetWidth = transSetup.ImageSettings.MaxWidth;

        image.TranscoderBinPath = dstImage.TranscoderBinPath;
        image.TranscoderArguments = dstImage.TranscoderArguments;

        image.TranscodeId = transcodeId;
        return image;
      }
      return null;
    }

    public static VideoTranscoding GetVideoSubtitleTranscoding(string section, string profile, MetadataContainer info, bool live, string transcodeId)
    {
      if (info == null) return null;
      if (ValidateProfile(section, profile) == false) return null;
      if (info.Audio.Count == 0) return null;

      int iMatchedAudioStream = 0;
      VideoTranscoding video = new VideoTranscoding();
      video.SourceAudioStreamIndex = info.Audio[iMatchedAudioStream].StreamIndex;
      video.SourceVideoStreamIndex = info.Video.StreamIndex;
      if (info.Metadata.VideoContainerType != VideoContainer.Unknown)
      {
        video.SourceVideoContainer = info.Metadata.VideoContainerType;
      }
      if (info.Audio[iMatchedAudioStream].Bitrate > 0)
      {
        video.SourceAudioBitrate = info.Audio[iMatchedAudioStream].Bitrate;
      }
      if (info.Audio[iMatchedAudioStream].Frequency > 0)
      {
        video.SourceAudioFrequency = info.Audio[iMatchedAudioStream].Frequency;
      }
      if (info.Audio[iMatchedAudioStream].Channels > 0)
      {
        video.SourceAudioChannels = info.Audio[iMatchedAudioStream].Channels;
      }
      if (info.Audio[iMatchedAudioStream].Codec != AudioCodec.Unknown)
      {
        video.SourceAudioCodec = info.Audio[iMatchedAudioStream].Codec;
      }
      video.SourceSubtitles = new List<SubtitleStream>(info.Subtitles);

      if (info.Video.Bitrate > 0)
      {
        video.SourceVideoBitrate = info.Video.Bitrate;
      }
      if (info.Video.Framerate > 0)
      {
        video.SourceFrameRate = info.Video.Framerate;
      }
      if (info.Video.PixelFormatType != PixelFormat.Unknown)
      {
        video.SourcePixelFormat = info.Video.PixelFormatType;
      }
      if (info.Video.AspectRatio > 0)
      {
        video.SourceVideoAspectRatio = info.Video.AspectRatio;
      }
      if (info.Video.Codec != VideoCodec.Unknown)
      {
        video.SourceVideoCodec = info.Video.Codec;
      }
      if (info.Video.Height > 0)
      {
        video.SourceVideoHeight = info.Video.Height;
      }
      if (info.Video.Width > 0)
      {
        video.SourceVideoWidth = info.Video.Width;
      }
      if (info.Video.PixelAspectRatio > 0)
      {
        video.SourceVideoPixelAspectRatio = info.Video.PixelAspectRatio;
      }
      if (info.Metadata.Duration > 0)
      {
        video.SourceDuration = TimeSpan.FromSeconds(info.Metadata.Duration);
      }
      if (info.Metadata.Source != null)
      {
        video.SourceMedia = info.Metadata.Source;
      }

      video.TargetVideoContainer = video.SourceVideoContainer;
      video.TargetAudioCodec = video.SourceAudioCodec;
      video.TargetVideoCodec = video.SourceVideoCodec;
      video.TargetLevel = info.Video.HeaderLevel;
      video.TargetProfile = info.Video.ProfileType;
      video.TargetForceAudioCopy = true;
      video.TargetForceVideoCopy = true;

      TranscodingSetup transSetup = _profiles[section][profile];
      video.TargetSubtitleSupport = transSetup.SubtitleSettings.SubtitleMode;
      video.SourceSubtitles.AddRange(info.Subtitles);
      if (transSetup.SubtitleSettings.SubtitleMode == SubtitleSupport.HardCoded)
      {
        video.TargetSubtitleSupport = SubtitleSupport.None;
      }
      video.TargetIsLive = live;
      video.TranscodeId = transcodeId;
      return video;
    }

    public static VideoTranscoding GetLiveVideoTranscoding(MetadataContainer info, string preferedAudioLanguages, string transcodeId)
    {
      if (info == null) return null;

      int iMatchedAudioStream = 0;
      if (string.IsNullOrEmpty(preferedAudioLanguages) == false)
      {
        List<string> valuesLangs = new List<string>(preferedAudioLanguages.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        int currentPriority = -1;
        for (int iAudio = 0; iAudio < info.Audio.Count; iAudio++)
        {
          for (int iPriority = 0; iPriority < valuesLangs.Count; iPriority++)
          {
            if (valuesLangs[iPriority].Equals(info.Audio[iAudio].Language, StringComparison.InvariantCultureIgnoreCase) == true)
            {
              if (currentPriority == -1 || iPriority < currentPriority)
              {
                currentPriority = iPriority;
                iMatchedAudioStream = iAudio;
              }
            }
          }
        }
      }

      VideoTranscoding video = new VideoTranscoding();
      video.SourceAudioStreamIndex = info.Audio[iMatchedAudioStream].StreamIndex;
      video.SourceVideoStreamIndex = info.Video.StreamIndex;
      if (info.Metadata.VideoContainerType != VideoContainer.Unknown)
      {
        video.SourceVideoContainer = info.Metadata.VideoContainerType;
      }
      if (info.Audio[iMatchedAudioStream].Bitrate > 0)
      {
        video.SourceAudioBitrate = info.Audio[iMatchedAudioStream].Bitrate;
      }
      if (info.Audio[iMatchedAudioStream].Frequency > 0)
      {
        video.SourceAudioFrequency = info.Audio[iMatchedAudioStream].Frequency;
      }
      if (info.Audio[iMatchedAudioStream].Channels > 0)
      {
        video.SourceAudioChannels = info.Audio[iMatchedAudioStream].Channels;
      }
      if (info.Audio[iMatchedAudioStream].Codec != AudioCodec.Unknown)
      {
        video.SourceAudioCodec = info.Audio[iMatchedAudioStream].Codec;
      }
      video.SourceSubtitles = new List<SubtitleStream>(info.Subtitles);

      if (info.Video.Bitrate > 0)
      {
        video.SourceVideoBitrate = info.Video.Bitrate;
      }
      if (info.Video.Framerate > 0)
      {
        video.SourceFrameRate = info.Video.Framerate;
      }
      if (info.Video.PixelFormatType != PixelFormat.Unknown)
      {
        video.SourcePixelFormat = info.Video.PixelFormatType;
      }
      if (info.Video.AspectRatio > 0)
      {
        video.SourceVideoAspectRatio = info.Video.AspectRatio;
      }
      if (info.Video.Codec != VideoCodec.Unknown)
      {
        video.SourceVideoCodec = info.Video.Codec;
      }
      if (info.Video.Height > 0)
      {
        video.SourceVideoHeight = info.Video.Height;
      }
      if (info.Video.Width > 0)
      {
        video.SourceVideoWidth = info.Video.Width;
      }
      if (info.Video.PixelAspectRatio > 0)
      {
        video.SourceVideoPixelAspectRatio = info.Video.PixelAspectRatio;
      }
      //if (info.Metadata.Duration > 0)
      //{
      //  video.SourceDuration = TimeSpan.FromSeconds(info.Metadata.Duration);
      //}
      if (info.Metadata.Source != null)
      {
        video.SourceMedia = info.Metadata.Source;
      }

      video.TargetVideoContainer = video.SourceVideoContainer;
      video.TargetAudioCodec = video.SourceAudioCodec;
      video.TargetVideoCodec = video.SourceVideoCodec;
      video.TargetLevel = info.Video.HeaderLevel;
      video.TargetProfile = info.Video.ProfileType;
      video.TargetForceVideoCopy = true;
      video.TargetForceAudioCopy = true;

      video.TargetIsLive = true;
      video.TargetSubtitleSupport = SubtitleSupport.None;
      video.TranscodeId = transcodeId;
      return video;
    }

    public static AudioTranscoding GetLiveAudioTranscoding(MetadataContainer info, string transcodeId)
    {
      if (info == null) return null;

      int iMatchedAudioStream = 0;
      AudioTranscoding audio = new AudioTranscoding();
      if (info.Metadata.AudioContainerType != AudioContainer.Unknown)
      {
        audio.SourceAudioContainer = info.Metadata.AudioContainerType;
      }
      if (info.Audio[iMatchedAudioStream].Bitrate > 0)
      {
        audio.SourceAudioBitrate = info.Audio[iMatchedAudioStream].Bitrate;
      }
      if (info.Audio[iMatchedAudioStream].Frequency > 0)
      {
        audio.SourceAudioFrequency = info.Audio[iMatchedAudioStream].Frequency;
      }
      if (info.Audio[iMatchedAudioStream].Channels > 0)
      {
        audio.SourceAudioChannels = info.Audio[iMatchedAudioStream].Channels;
      }
      if (info.Audio[iMatchedAudioStream].Codec != AudioCodec.Unknown)
      {
        audio.SourceAudioCodec = info.Audio[iMatchedAudioStream].Codec;
      }
      //if (info.Metadata.Duration > 0)
      //{
      //  audio.SourceDuration = TimeSpan.FromSeconds(info.Metadata.Duration);
      //}
      if (info.Metadata.Source != null)
      {
        audio.SourceMedia = info.Metadata.Source;
      }

      audio.TargetAudioContainer = audio.SourceAudioContainer;
      audio.TargetAudioCodec = audio.SourceAudioCodec;
      audio.TargetForceCopy = true;

      audio.TargetIsLive = true;
      audio.TranscodeId = transcodeId;
      return audio;
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
