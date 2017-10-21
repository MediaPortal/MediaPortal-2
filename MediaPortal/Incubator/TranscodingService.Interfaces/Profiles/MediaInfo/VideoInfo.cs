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
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;

namespace MediaPortal.Plugins.Transcoding.Interfaces.Profiles.MediaInfo
{
  public class VideoInfo
  {
    public VideoContainer VideoContainerType = VideoContainer.Unknown;
    public VideoCodec VideoCodecType = VideoCodec.Unknown;
    public AudioCodec AudioCodecType = AudioCodec.Unknown;
    public PixelFormat PixelFormatType = PixelFormat.Unknown;
    public QualityMode QualityType = QualityMode.Default;
    public string BrandExclusion = null;
    public float AspectRatio = 0;
    public long MaxVideoBitrate = 0;
    public int MaxVideoHeight = 0;
    public long AudioBitrate = 0;
    public long AudioFrequency = 0;
    public bool? AudioMultiChannel = null;
    public string FourCC = null;
    public bool? SquarePixels = null;
    public bool ForceVideoTranscoding = false;
    public bool ForceStereo = false;
    public bool ForceInheritance = false;
    public string Movflags = null;
    public EncodingPreset TargetPresetType = EncodingPreset.Default;
    public EncodingProfile EncodingProfileType = EncodingProfile.Unknown;
    public float LevelMinimum = 0;

    public bool Matches(MetadataContainer info, int audioStreamIndex, LevelCheck levelCheckType)
    {
      bool bPass = true;
      bPass &= (VideoContainerType == VideoContainer.Unknown || VideoContainerType == info.Metadata.VideoContainerType);
      bPass &= (VideoCodecType == VideoCodec.Unknown || VideoCodecType == info.Video.Codec);
      bPass &= (AudioCodecType == AudioCodec.Unknown || AudioCodecType == info.Audio[audioStreamIndex].Codec);
      if (SquarePixels == true)
      {
        bPass &= (info.Video.HasSquarePixels == true);
      }
      else if (SquarePixels == false)
      {
        bPass &= (info.Video.HasSquarePixels == false);
      }
      bPass &= (PixelFormatType == PixelFormat.Unknown || PixelFormatType == info.Video.PixelFormatType);
      if (AudioMultiChannel == true)
      {
        bPass &= (info.Audio[audioStreamIndex].Channels == 0 || info.Audio[audioStreamIndex].Channels > 2);
      }
      else if (AudioMultiChannel == false)
      {
        bPass &= (info.Audio[audioStreamIndex].Channels <= 2);
      }

      List<string> brandExclusions = new List<string>();
      if (BrandExclusion != null)
      {
        brandExclusions.AddRange(BrandExclusion.Split(','));
      }
      bPass &= (BrandExclusion == null || (info.Metadata.MajorBrand != null && !brandExclusions.Contains(info.Metadata.MajorBrand)));

      List<string> fourcc = new List<string>();
      if (FourCC != null)
      {
        fourcc.AddRange(FourCC.Split(','));
      }
      bPass &= (FourCC == null || (info.Video.FourCC != null && fourcc.Contains(info.Video.FourCC)));

      if (info.Video.Codec == VideoCodec.H264)
      {
        if (EncodingProfileType != EncodingProfile.Unknown)
        {
          bPass &= (info.Video.ProfileType != EncodingProfile.Unknown && EncodingProfileType == info.Video.ProfileType);
          if (LevelMinimum > 0)
          {
            float videoLevel = 0;
            if (levelCheckType == LevelCheck.RefFramesLevel)
            {
              videoLevel = info.Video.RefLevel;
            }
            else if (levelCheckType == LevelCheck.HeaderLevel)
            {
              videoLevel = info.Video.HeaderLevel;
            }
            else
            {
              if (info.Video.HeaderLevel <= 0)
              {
                videoLevel = info.Video.RefLevel;
              }
              if (info.Video.RefLevel <= 0)
              {
                videoLevel = info.Video.HeaderLevel;
              }
              if (info.Video.HeaderLevel > info.Video.RefLevel)
              {
                videoLevel = info.Video.HeaderLevel;
              }
              else
              {
                videoLevel = info.Video.RefLevel;
              }
            }
            bPass &= (videoLevel > 0 && videoLevel >= LevelMinimum);
          }
        }
      }
      else if (info.Video.Codec == VideoCodec.H265)
      {
        if (EncodingProfileType != EncodingProfile.Unknown)
        {
          bPass &= (info.Video.ProfileType != EncodingProfile.Unknown && EncodingProfileType == info.Video.ProfileType);
          if (LevelMinimum > 0)
          {
            bPass &= (info.Video.HeaderLevel > 0 && info.Video.HeaderLevel >= LevelMinimum);
          }
        }
      }

      return bPass;
    }

    public bool Matches(VideoInfo videoItem)
    {
      return VideoContainerType == videoItem.VideoContainerType &&
        VideoCodecType == videoItem.VideoCodecType &&
        AudioCodecType == videoItem.AudioCodecType &&
        EncodingProfileType == videoItem.EncodingProfileType &&
        BrandExclusion == videoItem.BrandExclusion &&
        AspectRatio == videoItem.AspectRatio &&
        LevelMinimum == videoItem.LevelMinimum &&
        MaxVideoBitrate == videoItem.MaxVideoBitrate &&
        MaxVideoHeight == videoItem.MaxVideoHeight &&
        AudioBitrate == videoItem.AudioBitrate &&
        AudioFrequency == videoItem.AudioFrequency &&
        SquarePixels == videoItem.SquarePixels &&
        FourCC == videoItem.FourCC &&
        ForceVideoTranscoding == videoItem.ForceVideoTranscoding &&
        ForceStereo == videoItem.ForceStereo;
    }
  }
}
