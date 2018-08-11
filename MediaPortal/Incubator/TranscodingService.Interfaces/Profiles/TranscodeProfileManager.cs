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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.Setup;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.Setup.Settings;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.Setup.Targets;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.MediaInfo;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.MediaMatch;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;
using System.Linq;
using MediaPortal.Extensions.TranscodingService.Interfaces.Analyzers;
using System.Threading.Tasks;

//Thanks goes to the Serviio team over at http://www.serviio.org/
//Their profile structure was inspiring and the community driven DLNA profiling is very effective 

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Profiles
{
  public class TranscodeProfileManager : ITranscodeProfileManager
  {
    public const string INPUT_FILE_TOKEN = "{input}";
    public const string OUTPUT_FILE_TOKEN = "{output}";
    public const string SUBTITLE_FILE_TOKEN = "{subtitle}";

    private Dictionary<string, Dictionary<string, TranscodingSetup>> _profiles = new Dictionary<string, Dictionary<string, TranscodingSetup>>();
    private string _subtitleFont = null;
    private string _subtitleFontSize = null;
    private string _subtitleColor = null;
    private bool _subtitleBox = false;
    private bool _forceSubtitles = true;

    public string SubtitleFont { get => _subtitleFont; set => _subtitleFont = value; }
    public string SubtitleFontSize { get => _subtitleFontSize; set => _subtitleFontSize = value; }
    public string SubtitleColor { get => _subtitleColor; set => _subtitleColor = value; }
    public bool SubtitleBox { get => _subtitleBox; set => _subtitleBox = value; }
    public bool ForceSubtitles { get => _forceSubtitles; set => _forceSubtitles = value; }

    public void ClearTranscodeProfiles(string section)
    {
      if (_profiles.ContainsKey(section) == true)
        _profiles[section].Clear();
    }

    public void AddTranscodingProfile(string section, string profileName, TranscodingSetup profile)
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

    public async Task LoadTranscodeProfilesAsync(string section, string profileFile)
    {
      try
      {
        if (File.Exists(profileFile) == true)
        {
          if (_profiles.ContainsKey(section) == false)
            _profiles.Add(section, new Dictionary<string, TranscodingSetup>());

          string profileName = null;
          TranscodingSetup profile = null;
          XmlReader reader = XmlReader.Create(profileFile, new XmlReaderSettings { Async = true });
          while (await reader.ReadAsync())
          {
            if (reader.NodeType != XmlNodeType.Element && reader.NodeType != XmlNodeType.EndElement)
              continue;

            string nodeName = reader.Name;
            if (nodeName == "Profile" && reader.NodeType == XmlNodeType.Element)
            {
              profileName = null;
              profile = new TranscodingSetup();
              while (reader.MoveToNextAttribute()) // Read the attributes.
              {
                if (reader.Name == "id")
                {
                  profileName = await reader.ReadContentAsStringAsync();
                }
                else if (reader.Name == "baseProfile")
                {
                  string parentProfileId = await reader.ReadContentAsStringAsync();

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
                  profile.VideoSettings.MultipleAudioTracksSupported = _profiles[section][parentProfileId].VideoSettings.MultipleAudioTracksSupported;

                  profile.ImageSettings.AutoRotate = _profiles[section][parentProfileId].ImageSettings.AutoRotate;
                  profile.ImageSettings.MaxHeight = _profiles[section][parentProfileId].ImageSettings.MaxHeight;
                  profile.ImageSettings.MaxWidth = _profiles[section][parentProfileId].ImageSettings.MaxWidth;
                  profile.ImageSettings.CoderType = _profiles[section][parentProfileId].ImageSettings.CoderType;

                  profile.AudioSettings.DefaultBitrate = _profiles[section][parentProfileId].AudioSettings.DefaultBitrate;
                  profile.AudioSettings.DefaultStereo = _profiles[section][parentProfileId].AudioSettings.DefaultStereo;
                  profile.AudioSettings.CoderType = _profiles[section][parentProfileId].AudioSettings.CoderType;

                  profile.SubtitleSettings.SubtitleMode = _profiles[section][parentProfileId].SubtitleSettings.SubtitleMode;
                  profile.SubtitleSettings.TetxBasedSupported = _profiles[section][parentProfileId].SubtitleSettings.TetxBasedSupported;
                  profile.SubtitleSettings.ImageBasedSupported = _profiles[section][parentProfileId].SubtitleSettings.ImageBasedSupported;
                  profile.SubtitleSettings.SubtitlesSupported = new List<ProfileSubtitle>(_profiles[section][parentProfileId].SubtitleSettings.SubtitlesSupported);

                  if (_profiles[section].ContainsKey(parentProfileId) == true)
                  {
                    profile.AudioTargets = _profiles[section][parentProfileId].AudioTargets.Where(a => a.Target.ForceInheritance).ToList();
                    profile.ImageTargets = _profiles[section][parentProfileId].ImageTargets.Where(i => i.Target.ForceInheritance).ToList();
                    profile.VideoTargets = _profiles[section][parentProfileId].VideoTargets.Where(v => v.Target.ForceInheritance).ToList();

                    profile.GenericAudioTargets = _profiles[section][parentProfileId].GenericAudioTargets.ToList();
                    profile.GenericImageTargets = _profiles[section][parentProfileId].GenericImageTargets.ToList();
                    profile.GenericVideoTargets = _profiles[section][parentProfileId].GenericVideoTargets.ToList();
                  }
                }
              }
            }
            else if (nodeName == "Settings" && reader.NodeType == XmlNodeType.Element)
            {
              while (await reader.ReadAsync())
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
                      profile.VideoSettings.Quality = (QualityMode)Enum.Parse(typeof(QualityMode), await reader.ReadContentAsStringAsync(), true);
                    }
                    else if (reader.Name == "qualityFactor")
                    {
                      profile.VideoSettings.QualityFactor = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "coder")
                    {
                      profile.VideoSettings.CoderType = (Coder)Enum.Parse(typeof(Coder), await reader.ReadContentAsStringAsync(), true);
                    }
                    else if (reader.Name == "multipleAudioTracksSupported")
                    {
                      profile.VideoSettings.MultipleAudioTracksSupported = reader.ReadContentAsBoolean();
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
                      profile.VideoSettings.H262TargetPreset = (EncodingPreset)Enum.Parse(typeof(EncodingPreset), await reader.ReadContentAsStringAsync(), true);
                    }
                    else if (reader.Name == "profile")
                    {
                      profile.VideoSettings.H262TargetProfile = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), await reader.ReadContentAsStringAsync(), true);
                    }
                  }
                }
                else if (reader.Name == "H264Video" && reader.NodeType == XmlNodeType.Element)
                {
                  while (reader.MoveToNextAttribute()) // Read the attributes.
                  {
                    if (reader.Name == "levelCheck")
                    {
                      profile.VideoSettings.H264LevelCheckMethod = (LevelCheck)Enum.Parse(typeof(LevelCheck), await reader.ReadContentAsStringAsync(), true);
                    }
                    else if (reader.Name == "qualityFactor")
                    {
                      profile.VideoSettings.H264QualityFactor = reader.ReadContentAsInt();
                    }
                    else if (reader.Name == "preset")
                    {
                      profile.VideoSettings.H264TargetPreset = (EncodingPreset)Enum.Parse(typeof(EncodingPreset), await reader.ReadContentAsStringAsync(), true);
                    }
                    else if (reader.Name == "profile")
                    {
                      profile.VideoSettings.H264TargetProfile = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), await reader.ReadContentAsStringAsync(), true);
                    }
                    else if (reader.Name == "level")
                    {
                      profile.VideoSettings.H264Level = Convert.ToSingle(await reader.ReadContentAsStringAsync(), CultureInfo.InvariantCulture);
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
                      profile.VideoSettings.H265TargetPreset = (EncodingPreset)Enum.Parse(typeof(EncodingPreset), await reader.ReadContentAsStringAsync(), true);
                    }
                    else if (reader.Name == "profile")
                    {
                      profile.VideoSettings.H265TargetProfile = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), await reader.ReadContentAsStringAsync(), true);
                    }
                    else if (reader.Name == "level")
                    {
                      profile.VideoSettings.H265Level = Convert.ToSingle(await reader.ReadContentAsStringAsync(), CultureInfo.InvariantCulture);
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
                      profile.ImageSettings.Quality = (QualityMode)Enum.Parse(typeof(QualityMode), await reader.ReadContentAsStringAsync(), true);
                    }
                    else if (reader.Name == "coder")
                    {
                      profile.ImageSettings.CoderType = (Coder)Enum.Parse(typeof(Coder), await reader.ReadContentAsStringAsync(), true);
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
                      profile.AudioSettings.CoderType = (Coder)Enum.Parse(typeof(Coder), await reader.ReadContentAsStringAsync(), true);
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
                      profile.SubtitleSettings.SubtitleMode = (SubtitleSupport)Enum.Parse(typeof(SubtitleSupport), await reader.ReadContentAsStringAsync(), true);
                    }
                    else if (reader.Name == "imageBasedSupported")
                    {
                      profile.SubtitleSettings.ImageBasedSupported = reader.ReadContentAsBoolean();
                    }
                    else if (reader.Name == "textBasedSupported")
                    {
                      profile.SubtitleSettings.TetxBasedSupported = reader.ReadContentAsBoolean();
                    }
                  }
                  while (await subReader.ReadAsync())
                  {
                    if (subReader.Name == "Subtitle" && subReader.NodeType == XmlNodeType.Element)
                    {
                      ProfileSubtitle newSub = new ProfileSubtitle();
                      while (subReader.MoveToNextAttribute()) // Read the attributes.
                      {
                        if (subReader.Name == "format")
                        {
                          newSub.Format = (SubtitleCodec)Enum.Parse(typeof(SubtitleCodec), await subReader.ReadContentAsStringAsync(), true);
                        }
                        else if (subReader.Name == "mime")
                        {
                          newSub.Mime = await subReader.ReadContentAsStringAsync();
                        }
                        else if (subReader.Name == "encoding")
                        {
                          newSub.Encoding = await subReader.ReadContentAsStringAsync();
                        }
                      }
                      profile.SubtitleSettings.SubtitlesSupported.Add(newSub);
                    }
                  }
                }
              }
            }
            else if (nodeName == "GenericTranscoding" && reader.NodeType == XmlNodeType.Element)
            {
              profile.GenericVideoTargets.Clear();
              profile.GenericAudioTargets.Clear();
              profile.GenericImageTargets.Clear();

              await ReadTranscodingAsync(reader, reader.Name, profile.GenericVideoTargets, profile.GenericAudioTargets, profile.GenericImageTargets);
            }
            else if (nodeName == "MediaTranscoding" && reader.NodeType == XmlNodeType.Element)
            {
              await ReadTranscodingAsync(reader, reader.Name, profile.VideoTargets, profile.AudioTargets, profile.ImageTargets);
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

    private async Task ReadTranscodingAsync(XmlReader reader, string elementName,
      List<VideoTranscodingTarget> vTrans, List<AudioTranscodingTarget> aTrans, List<ImageTranscodingTarget> iTrans)
    {
      List<VideoTranscodingTarget> vList = new List<VideoTranscodingTarget>();
      List<AudioTranscodingTarget> aList = new List<AudioTranscodingTarget>();
      List<ImageTranscodingTarget> iList = new List<ImageTranscodingTarget>();

      VideoTranscodingTarget vTranscoding = new VideoTranscodingTarget();
      AudioTranscodingTarget aTranscoding = new AudioTranscodingTarget();
      ImageTranscodingTarget iTranscoding = new ImageTranscodingTarget();

      while (await reader.ReadAsync())
      {
        if (reader.Name == "VideoTarget" && reader.NodeType == XmlNodeType.Element)
        {
          vTranscoding = new VideoTranscodingTarget();
          vTranscoding.Target = new VideoInfo();
          while (reader.MoveToNextAttribute()) // Read the attributes.
          {
            if (reader.Name == "container")
            {
              vTranscoding.Target.VideoContainerType = (VideoContainer)Enum.Parse(typeof(VideoContainer), await reader.ReadContentAsStringAsync(), true);
            }
            else if (reader.Name == "movflags")
            {
              vTranscoding.Target.Movflags = await reader.ReadContentAsStringAsync();
            }
            else if (reader.Name == "videoCodec")
            {
              vTranscoding.Target.VideoCodecType = (VideoCodec)Enum.Parse(typeof(VideoCodec), await reader.ReadContentAsStringAsync(), true);
            }
            else if (reader.Name == "videoFourCC")
            {
              vTranscoding.Target.FourCC = await reader.ReadContentAsStringAsync();
            }
            else if (reader.Name == "videoAR")
            {
              vTranscoding.Target.AspectRatio = Convert.ToSingle(await reader.ReadContentAsStringAsync(), CultureInfo.InvariantCulture);
            }
            else if (reader.Name == "videoProfile")
            {
              vTranscoding.Target.EncodingProfileType = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), await reader.ReadContentAsStringAsync(), true);
            }
            else if (reader.Name == "videoLevel")
            {
              vTranscoding.Target.LevelMinimum = Convert.ToSingle(await reader.ReadContentAsStringAsync(), CultureInfo.InvariantCulture);
            }
            else if (reader.Name == "videoPreset")
            {
              vTranscoding.Target.TargetPresetType = (EncodingPreset)Enum.Parse(typeof(EncodingPreset), await reader.ReadContentAsStringAsync(), true);
            }
            else if (reader.Name == "qualityMode")
            {
              vTranscoding.Target.QualityType = (QualityMode)Enum.Parse(typeof(QualityMode), await reader.ReadContentAsStringAsync(), true);
            }
            else if (reader.Name == "videoBrandExclusion")
            {
              vTranscoding.Target.BrandExclusion = await reader.ReadContentAsStringAsync();
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
              vTranscoding.Target.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), await reader.ReadContentAsStringAsync(), true);
            }
            else if (reader.Name == "audioCodec")
            {
              vTranscoding.Target.AudioCodecType = (AudioCodec)Enum.Parse(typeof(AudioCodec), await reader.ReadContentAsStringAsync(), true);
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
              vTranscoding.TranscoderBinPath = await reader.ReadContentAsStringAsync();
            }
            else if (reader.Name == "transcoderArguments")
            {
              vTranscoding.TranscoderArguments = await reader.ReadContentAsStringAsync();
            }
          }
          while (await reader.ReadAsync())
          {
            if (reader.Name == elementName && reader.NodeType == XmlNodeType.EndElement)
              break;
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
                  src.VideoContainerType = (VideoContainer)Enum.Parse(typeof(VideoContainer), await reader.ReadContentAsStringAsync(), true);
                }
                else if (reader.Name == "videoCodec")
                {
                  src.VideoCodecType = (VideoCodec)Enum.Parse(typeof(VideoCodec), await reader.ReadContentAsStringAsync(), true);
                }
                else if (reader.Name == "videoFourCC")
                {
                  src.FourCC = await reader.ReadContentAsStringAsync();
                }
                else if (reader.Name == "videoAR")
                {
                  src.AspectRatio = Convert.ToSingle(reader.ReadContentAsStringAsync(), CultureInfo.InvariantCulture);
                }
                else if (reader.Name == "videoProfile")
                {
                  src.EncodingProfileType = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), await reader.ReadContentAsStringAsync(), true);
                }
                else if (reader.Name == "videoLevel")
                {
                  src.LevelMinimum = Convert.ToSingle(await reader.ReadContentAsStringAsync(), CultureInfo.InvariantCulture);
                }
                else if (reader.Name == "videoBrandExclusion")
                {
                  src.BrandExclusion = await reader.ReadContentAsStringAsync();
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
                  src.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), await reader.ReadContentAsStringAsync(), true);
                }
                else if (reader.Name == "audioCodec")
                {
                  src.AudioCodecType = (AudioCodec)Enum.Parse(typeof(AudioCodec), await reader.ReadContentAsStringAsync(), true);
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
              aTranscoding.Target.AudioContainerType = (AudioContainer)Enum.Parse(typeof(AudioContainer), await reader.ReadContentAsStringAsync(), true);
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
              aTranscoding.TranscoderBinPath = await reader.ReadContentAsStringAsync();
            }
            else if (reader.Name == "transcoderArguments")
            {
              aTranscoding.TranscoderArguments = await reader.ReadContentAsStringAsync();
            }
          }
          while (await reader.ReadAsync())
          {
            if (reader.Name == elementName && reader.NodeType == XmlNodeType.EndElement)
              break;
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
                  src.AudioContainerType = (AudioContainer)Enum.Parse(typeof(AudioContainer), await reader.ReadContentAsStringAsync(), true);
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
              iTranscoding.Target.ImageContainerType = (ImageContainer)Enum.Parse(typeof(ImageContainer), await reader.ReadContentAsStringAsync(), true);
            }
            else if (reader.Name == "pixelFormat")
            {
              iTranscoding.Target.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), await reader.ReadContentAsStringAsync(), true);
            }
            else if (reader.Name == "qualityMode")
            {
              iTranscoding.Target.QualityType = (QualityMode)Enum.Parse(typeof(QualityMode), await reader.ReadContentAsStringAsync(), true);
            }
            else if (reader.Name == "forceInheritance")
            {
              iTranscoding.Target.ForceInheritance = reader.ReadContentAsBoolean();
            }
            else if (reader.Name == "transcoder")
            {
              iTranscoding.TranscoderBinPath = await reader.ReadContentAsStringAsync();
            }
            else if (reader.Name == "transcoderOptions")
            {
              iTranscoding.TranscoderArguments = await reader.ReadContentAsStringAsync();
            }
          }
          while (await reader.ReadAsync())
          {
            if (reader.Name == elementName && reader.NodeType == XmlNodeType.EndElement)
              break;
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
                  src.ImageContainerType = (ImageContainer)Enum.Parse(typeof(ImageContainer), await reader.ReadContentAsStringAsync(), true);
                }
                else if (reader.Name == "pixelFormat")
                {
                  src.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), await reader.ReadContentAsStringAsync(), true);
                }
              }
              iTranscoding.Sources.Add(src);
            }
          }
        }
        if (reader.Name == elementName && reader.NodeType == XmlNodeType.EndElement)
          break;
      }

      //Own transcoding profiles should have higher priority than inherited ones
      vList.AddRange(vTrans);
      vTrans.Clear();
      vTrans.AddRange(vList);

      aList.AddRange(aTrans);
      aTrans.Clear();
      aTrans.AddRange(aList);

      iList.AddRange(iTrans);
      iTrans.Clear();
      iTrans.AddRange(iList);
    }

    public TranscodingSetup GetTranscodeProfile(string section, string profile)
    {
      if (ValidateProfile(section, profile))
        return _profiles[section][profile];

      return null;
    }

    private bool ValidateProfile(string section, string profile)
    {
      if (_profiles.ContainsKey(section) == true && _profiles[section].ContainsKey(profile) == true)
        return true;

      return false;
    }

    private bool AreEmbeddedSubsSupported(TranscodingSetup transSetup, VideoTranscodingTarget target)
    {
      if (transSetup.SubtitleSettings.SubtitleMode == SubtitleSupport.Embedded)
      {
        if (target == null)
          return false;
        else if (target.Target.VideoContainerType == VideoContainer.Matroska)
          return true;
        else if (target.Target.VideoContainerType == VideoContainer.Mp4)
          return true;
        else if (target.Target.VideoContainerType == VideoContainer.Hls)
          return true;
        else if (target.Target.VideoContainerType == VideoContainer.Avi)
          return true;
        //else if (target.Target.VideoContainerType == VideoContainer.Mpeg2Ts)
        //  return true;
      }
      return false;
    }

    private int GetPreferredAudioStream(MetadataContainer info, IEnumerable<string> preferredAudioLanguages)
    {
      int matchedAudioStream = info.FirstAudioStream?.StreamIndex ?? -1;
      if (preferredAudioLanguages.Any())
      {
        List<string> valuesLangs = preferredAudioLanguages.ToList();
        int currentPriority = -1;
        for (int idx = 0; idx < info.Audio.Count; idx++)
        {
          for (int priority = 0; priority < valuesLangs.Count; priority++)
          {
            if (valuesLangs[priority].Equals(info.Audio[idx].Language, StringComparison.InvariantCultureIgnoreCase) == true)
            {
              if (currentPriority == -1 || priority < currentPriority)
              {
                currentPriority = priority;
                matchedAudioStream = info.Audio[idx].StreamIndex;
              }
            }
          }
        }
      }
      return matchedAudioStream;
    }

    /// <summary>
    /// Get the video transcoding profile that best matches the source video.
    /// </summary>
    public VideoTranscoding GetVideoTranscoding(string section, string profile, IEnumerable<MetadataContainer> infos, IEnumerable<string> preferedAudioLanguages, bool liveStreaming, string transcodeId)
    {
      if (infos == null)
        return null;

      TranscodingSetup transSetup = GetTranscodeProfile(section, profile);
      if (transSetup == null)
        return null;

      VideoMatch srcVideo = null;
      int matchedAudioStream = GetPreferredAudioStream(infos.First(), preferedAudioLanguages);
      VideoTranscodingTarget dstVideo = transSetup.GetMatchingVideoTranscoding(infos.First(), matchedAudioStream, out srcVideo);
      SubtitleSupport subMode = transSetup.SubtitleSettings.SubtitleMode;
      if (subMode != SubtitleSupport.HardCoded)
      {
        if (!infos.Any(i => i.Subtitles.Any()))
        {
          //No subtitles
          subMode = SubtitleSupport.None;
        }
        else if (subMode != SubtitleSupport.None && transSetup.SubtitleSettings.TetxBasedSupported && !transSetup.SubtitleSettings.ImageBasedSupported && !infos.Any(i => i.Subtitles.Any(s => !SubtitleAnalyzer.IsImageBasedSubtitle(s.Codec))))
        {
          //No matching text subtitles supported
          subMode = SubtitleSupport.None;
        }
        else if (subMode != SubtitleSupport.None && !transSetup.SubtitleSettings.TetxBasedSupported && transSetup.SubtitleSettings.ImageBasedSupported && !infos.Any(i => i.Subtitles.Any(s => SubtitleAnalyzer.IsImageBasedSubtitle(s.Codec))))
        {
          //No matching image subtitles supported
          subMode = SubtitleSupport.None;
        }

        if (subMode == SubtitleSupport.Embedded && !AreEmbeddedSubsSupported(transSetup, dstVideo))
        {
          //Embedding subtitles not supported
          subMode = SubtitleSupport.None;
        }
        if (ForceSubtitles && subMode == SubtitleSupport.None && infos.Any(i => i.Subtitles.Any()))
        {
          //Force subtitles
          subMode = SubtitleSupport.HardCoded;
        }
      }

      if ((infos.Any() || subMode == SubtitleSupport.HardCoded) && transSetup.GenericVideoTargets.Any())
      {
        //Stacked files or hardcoded subs need generic transcoding
        srcVideo = new VideoMatch();
        srcVideo.MatchedAudioStream = matchedAudioStream;
        dstVideo = transSetup.GenericVideoTargets.First();
        srcVideo.MatchedVideoSource = dstVideo.Target;
      }
      else if (dstVideo == null)
      {
        return null;
      }

      if (srcVideo.MatchedVideoSource.Matches(dstVideo.Target) == false)
      {
        VideoTranscoding video = new VideoTranscoding();
        List<MetadataContainer> infoList = infos.ToList();
        for (int infoIndex = 0; infoIndex < infoList.Count; infoIndex++)
        {
          MetadataContainer info = infoList[infoIndex];

          video.SourceVideoStreams.Add(infoIndex, info.Video);
          video.SourceVideoContainers.Add(infoIndex, info.Metadata.VideoContainerType);

          if(info.Metadata.Duration.HasValue)
            video.SourceMediaDurations.Add(infoIndex, TimeSpan.FromSeconds(info.Metadata.Duration.Value));
          if (info.Metadata.Source != null)
            video.SourceMedia.Add(infoIndex, info.Metadata.Source);

          var audioStream = info.Audio.FirstOrDefault(s => s.StreamIndex == srcVideo.MatchedAudioStream);
          if (audioStream != null)
            video.SourceAudioStreams.Add(infoIndex, new List<AudioStream>() { audioStream });
          if (transSetup.VideoSettings.MultipleAudioTracksSupported)
          {
            video.TargetAudioMultiTrackSupport = true;
            for (int idx = 0; idx < info.Audio.Count; idx++)
            {
              if (info.Audio[idx].StreamIndex == srcVideo.MatchedAudioStream)
                continue;

              video.SourceAudioStreams[infoIndex].Add(info.Audio[idx]);
            }
          }

          video.SourceSubtitles.Add(infoIndex, new List<SubtitleStream>(info.Subtitles));
        }      

        if (dstVideo.Target.VideoContainerType != VideoContainer.Unknown)
          video.TargetVideoContainer = dstVideo.Target.VideoContainerType;

        if (dstVideo.Target.Movflags != null)
          video.Movflags = dstVideo.Target.Movflags;

        video.TargetAudioBitrate = transSetup.AudioSettings.DefaultBitrate;
        if (dstVideo.Target.AudioBitrate > 0)
          video.TargetAudioBitrate = dstVideo.Target.AudioBitrate;

        if (dstVideo.Target.AudioFrequency > 0)
          video.TargetAudioFrequency = dstVideo.Target.AudioFrequency;

        if (dstVideo.Target.AudioCodecType != AudioCodec.Unknown)
          video.TargetAudioCodec = dstVideo.Target.AudioCodecType;
 
        video.TargetForceAudioStereo = transSetup.AudioSettings.DefaultStereo;
        if (dstVideo.Target.ForceStereo)
          video.TargetForceAudioStereo = dstVideo.Target.ForceStereo;

        video.TargetVideoQuality = transSetup.VideoSettings.Quality;
        if (dstVideo.Target.QualityType != QualityMode.Default)
          video.TargetVideoQuality = dstVideo.Target.QualityType;

        if (dstVideo.Target.PixelFormatType != PixelFormat.Unknown)
          video.TargetPixelFormat = dstVideo.Target.PixelFormatType;

        if (dstVideo.Target.AspectRatio > 0)
          video.TargetVideoAspectRatio = dstVideo.Target.AspectRatio;

        if (dstVideo.Target.MaxVideoBitrate > 0)
          video.TargetVideoBitrate = dstVideo.Target.MaxVideoBitrate;

        if (dstVideo.Target.VideoCodecType != VideoCodec.Unknown)
          video.TargetVideoCodec = dstVideo.Target.VideoCodecType;

        video.TargetVideoMaxHeight = transSetup.VideoSettings.MaxHeight;
        if (dstVideo.Target.MaxVideoHeight > 0)
          video.TargetVideoMaxHeight = dstVideo.Target.MaxVideoHeight;

        video.TargetForceVideoTranscoding = dstVideo.Target.ForceVideoTranscoding;

        video.TargetSubtitleCharacterEncoding = transSetup.SubtitleSettings.SubtitlesSupported.FirstOrDefault()?.Encoding;
        video.TargetSubtitleBox = _subtitleBox;
        video.TargetSubtitleColor = _subtitleColor;
        video.TargetSubtitleFont = _subtitleFont;
        video.TargetSubtitleFontSize = _subtitleFontSize;

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
            video.TargetLevel = dstVideo.Target.LevelMinimum;

          video.TargetProfile = transSetup.VideoSettings.H264TargetProfile;
          if (dstVideo.Target.EncodingProfileType != EncodingProfile.Unknown)
            video.TargetProfile = dstVideo.Target.EncodingProfileType;

          video.TargetPreset = transSetup.VideoSettings.H264TargetPreset;
          if (dstVideo.Target.TargetPresetType != EncodingPreset.Default)
            video.TargetPreset = dstVideo.Target.TargetPresetType;
        }
        else if (dstVideo.Target.VideoCodecType == VideoCodec.H265)
        {
          video.TargetQualityFactor = transSetup.VideoSettings.H265QualityFactor;
          video.TargetLevel = transSetup.VideoSettings.H265Level;
          if (dstVideo.Target.LevelMinimum > 0)
            video.TargetLevel = dstVideo.Target.LevelMinimum;
 
          video.TargetProfile = transSetup.VideoSettings.H265TargetProfile;
          if (dstVideo.Target.EncodingProfileType != EncodingProfile.Unknown)
            video.TargetProfile = dstVideo.Target.EncodingProfileType;
      
          video.TargetPreset = transSetup.VideoSettings.H265TargetPreset;
          if (dstVideo.Target.TargetPresetType != EncodingPreset.Default)
            video.TargetPreset = dstVideo.Target.TargetPresetType;
        }

        video.TargetVideoQualityFactor = transSetup.VideoSettings.QualityFactor;
        video.TargetCoder = transSetup.VideoSettings.CoderType;
        video.TargetIsLive = liveStreaming;

        video.TargetSubtitleSupport = transSetup.SubtitleSettings.SubtitleMode;
        video.TargetSubtitleSupport = subMode;

        video.TranscoderBinPath = dstVideo.TranscoderBinPath;
        video.TranscoderArguments = dstVideo.TranscoderArguments;
        video.TranscodeId = transcodeId;

        return video;
      }
      return null;
    }

    /// <summary>
    /// Get the audio transcoding profile that best matches the source audio.
    /// </summary>
    public AudioTranscoding GetAudioTranscoding(string section, string profile, MetadataContainer info, bool liveStreaming, string transcodeId)
    {
      if (info == null)
        return null;

      TranscodingSetup transSetup = GetTranscodeProfile(section, profile);
      if (transSetup == null)
        return null;

      AudioMatch srcAudio;
      AudioTranscodingTarget dstAudio = transSetup.GetMatchingAudioTranscoding(info, out srcAudio);
      if (dstAudio != null && srcAudio.MatchedAudioSource.Matches(dstAudio.Target) == false)
      {
        AudioTranscoding audio = new AudioTranscoding();
        if (info.Metadata.AudioContainerType != AudioContainer.Unknown)
          audio.SourceAudioContainer = info.Metadata.AudioContainerType;

        if (info.Audio.First(s => s.StreamIndex == srcAudio.MatchedAudioStream).Bitrate > 0)
          audio.SourceAudioBitrate = info.Audio.First(s => s.StreamIndex == srcAudio.MatchedAudioStream).Bitrate;
       
        if (info.Audio.First(s => s.StreamIndex == srcAudio.MatchedAudioStream).Frequency > 0)
          audio.SourceAudioFrequency = info.Audio.First(s => s.StreamIndex == srcAudio.MatchedAudioStream).Frequency;
        
        if (info.Audio.First(s => s.StreamIndex == srcAudio.MatchedAudioStream).Channels > 0)
          audio.SourceAudioChannels = info.Audio.First(s => s.StreamIndex == srcAudio.MatchedAudioStream).Channels;
        
        if (info.Audio.First(s => s.StreamIndex == srcAudio.MatchedAudioStream).Codec != AudioCodec.Unknown)
          audio.SourceAudioCodec = info.Audio.First(s => s.StreamIndex == srcAudio.MatchedAudioStream).Codec;
      
        if (info.Metadata.Duration.HasValue)
          audio.SourceDuration = TimeSpan.FromSeconds(info.Metadata.Duration.Value);
      
        if (info.Metadata.Source != null)
          audio.SourceMedia.Add(0, info.Metadata.Source);

        audio.TargetAudioBitrate = transSetup.AudioSettings.DefaultBitrate;
        if (dstAudio.Target.Bitrate > 0)
          audio.TargetAudioBitrate = dstAudio.Target.Bitrate;
   
        if (dstAudio.Target.AudioContainerType != AudioContainer.Unknown)
          audio.TargetAudioContainer = dstAudio.Target.AudioContainerType;
      
        if (dstAudio.Target.Frequency > 0)
          audio.TargetAudioFrequency = dstAudio.Target.Frequency;
       
        audio.TargetForceAudioStereo = transSetup.AudioSettings.DefaultStereo;
        if (dstAudio.Target.ForceStereo)
          audio.TargetForceAudioStereo = dstAudio.Target.ForceStereo;

        audio.TargetCoder = transSetup.AudioSettings.CoderType;
        audio.TargetIsLive = liveStreaming;

        audio.TranscoderBinPath = dstAudio.TranscoderBinPath;
        audio.TranscoderArguments = dstAudio.TranscoderArguments;
        audio.TranscodeId = transcodeId;
        return audio;
      }
      return null;
    }

    /// <summary>
    /// Get the image transcoding profile that best matches the source image.
    /// </summary>
    public ImageTranscoding GetImageTranscoding(string section, string profile, MetadataContainer info, string transcodeId)
    {
      if (info == null)
        return null;

      TranscodingSetup transSetup = GetTranscodeProfile(section, profile);
      if (transSetup == null)
        return null;

      ImageMatch srcImage;
      ImageTranscodingTarget dstImage = transSetup.GetMatchingImageTranscoding(info, out srcImage);
      if (dstImage != null && srcImage.MatchedImageSource.Matches(dstImage.Target) == false)
      {
        ImageTranscoding image = new ImageTranscoding();
        if (info.Metadata.ImageContainerType != ImageContainer.Unknown)
          image.SourceImageCodec = info.Metadata.ImageContainerType;
   
        if (info.Image.Height.HasValue)
          image.SourceHeight = info.Image.Height.Value;
    
        if (info.Image.Width.HasValue)
          image.SourceWidth = info.Image.Width.Value;
    
        if (info.Image.Orientation.HasValue)
          image.SourceOrientation = info.Image.Orientation.Value;

        if (info.Image.PixelFormatType != PixelFormat.Unknown)
          image.SourcePixelFormat = info.Image.PixelFormatType;
 
        if (info.Metadata.Source != null)
          image.SourceMedia.Add(0, info.Metadata.Source);

        if (dstImage.Target.PixelFormatType > 0)
          image.TargetPixelFormat = dstImage.Target.PixelFormatType;
  
        if (dstImage.Target.ImageContainerType != ImageContainer.Unknown)
          image.TargetImageCodec = dstImage.Target.ImageContainerType;
   
        image.TargetImageQuality = transSetup.ImageSettings.Quality;
        if (dstImage.Target.QualityType != QualityMode.Default)
          image.TargetImageQuality = dstImage.Target.QualityType;

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

    /// <summary>
    /// Get a video transcoding profile that adds subtitles the source video.
    /// </summary>
    public VideoTranscoding GetVideoSubtitleTranscoding(string section, string profile, IEnumerable<MetadataContainer> infos, bool live, string transcodeId)
    {
      if (infos == null)
        return null;

      TranscodingSetup transSetup = GetTranscodeProfile(section, profile);
      if (transSetup == null)
        return null;

      if (!infos.Any(i => i.Audio.Count > 0))
        return null;

      int matchedAudioStream = 0;
      VideoTranscoding video = new VideoTranscoding();
      List<MetadataContainer> infoList = infos.ToList();
      for (int infoIndex = 0; infoIndex < infoList.Count; infoIndex++)
      {
        MetadataContainer info = infoList[infoIndex];

        video.SourceVideoStreams.Add(infoIndex, info.Video);
        video.SourceVideoContainers.Add(infoIndex, info.Metadata.VideoContainerType);

        if (info.Metadata.Duration.HasValue)
          video.SourceMediaDurations.Add(infoIndex, TimeSpan.FromSeconds(info.Metadata.Duration.Value));

        if (info.Metadata.Source != null)
          video.SourceMedia.Add(infoIndex, info.Metadata.Source);

        video.SourceAudioStreams.Add(infoIndex, new List<AudioStream>() { info.Audio[matchedAudioStream] });
        for (int idx = 0; idx < info.Audio.Count; idx++)
        {
          if (idx == matchedAudioStream)
            continue;

          video.SourceAudioStreams[infoIndex].Add(info.Audio[idx]);
        }

        video.SourceSubtitles.Add(infoIndex, new List<SubtitleStream>(info.Subtitles));
      }

      video.TargetVideoContainer = video.FirstSourceVideoContainer;
      video.TargetAudioCodec = video.FirstSourceAudioStream.Codec;
      video.TargetVideoCodec = video.FirstSourceVideoStream.Codec;
      video.TargetLevel = video.FirstSourceVideoStream.HeaderLevel;
      video.TargetProfile = video.FirstSourceVideoStream.ProfileType;
      video.TargetForceAudioCopy = true;
      video.TargetForceVideoCopy = true;

      video.TargetSubtitleSupport = transSetup.SubtitleSettings.SubtitleMode;
      if (transSetup.SubtitleSettings.SubtitleMode == SubtitleSupport.HardCoded)
        video.TargetSubtitleSupport = SubtitleSupport.None;
  
      video.TargetIsLive = live;
      video.TranscodeId = transcodeId;
      return video;
    }

    /// <summary>
    /// Get a video transcoding profile that streams the live source video.
    /// </summary>
    public VideoTranscoding GetLiveVideoTranscoding(MetadataContainer info, IEnumerable<string> preferedAudioLanguages, string transcodeId)
    {
      if (info == null)
        return null;

      int matchedAudioStream = GetPreferredAudioStream(info, preferedAudioLanguages);

      VideoTranscoding video = new VideoTranscoding();
      video.SourceVideoStreams.Add(0, info.Video);
      video.SourceVideoContainers.Add(0, info.Metadata.VideoContainerType);

      if (info.Metadata.Source != null)
        video.SourceMedia.Add(0, info.Metadata.Source);

      video.SourceAudioStreams.Add(0, new List<AudioStream>() { info.Audio.First(s => s.StreamIndex == matchedAudioStream) });
      for (int idx = 0; idx < info.Audio.Count; idx++)
      {
        if (info.Audio[idx].StreamIndex == matchedAudioStream)
          continue;

        video.SourceAudioStreams[0].Add(info.Audio[idx]);
      }

      video.SourceSubtitles.Add(0, new List<SubtitleStream>(info.Subtitles));

      video.TargetSubtitleBox = _subtitleBox;
      video.TargetSubtitleColor = _subtitleColor;
      video.TargetSubtitleFont = _subtitleFont;
      video.TargetSubtitleFontSize = _subtitleFontSize;

      video.TargetVideoContainer = video.FirstSourceVideoContainer;
      video.TargetAudioCodec = video.FirstSourceAudioStream.Codec;
      video.TargetVideoCodec = video.FirstSourceVideoStream.Codec;
      video.TargetLevel = video.FirstSourceVideoStream.HeaderLevel;
      video.TargetProfile = video.FirstSourceVideoStream.ProfileType;
      video.TargetForceVideoCopy = true;
      video.TargetForceAudioCopy = true;

      video.TargetIsLive = true;
      video.TargetSubtitleSupport = SubtitleSupport.None;
      video.TranscodeId = transcodeId;
      return video;
    }

    /// <summary>
    /// Get an audio transcoding profile that streams the live source audio.
    /// </summary>
    public AudioTranscoding GetLiveAudioTranscoding(MetadataContainer info, string transcodeId)
    {
      if (info == null)
        return null;

      int matchedAudioStream = 0;

      AudioTranscoding audio = new AudioTranscoding();
      if (info.Metadata.AudioContainerType != AudioContainer.Unknown)
        audio.SourceAudioContainer = info.Metadata.AudioContainerType;
    
      if (info.Audio[matchedAudioStream].Bitrate > 0)
        audio.SourceAudioBitrate = info.Audio[matchedAudioStream].Bitrate;
    
      if (info.Audio[matchedAudioStream].Frequency > 0)
        audio.SourceAudioFrequency = info.Audio[matchedAudioStream].Frequency;
     
      if (info.Audio[matchedAudioStream].Channels > 0)
        audio.SourceAudioChannels = info.Audio[matchedAudioStream].Channels;
  
      if (info.Audio[matchedAudioStream].Codec != AudioCodec.Unknown)
        audio.SourceAudioCodec = info.Audio[matchedAudioStream].Codec;
   
      if (info.Metadata.Source != null)
        audio.SourceMedia.Add(0, info.Metadata.Source);

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
