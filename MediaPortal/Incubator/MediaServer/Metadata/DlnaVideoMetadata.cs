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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.Plugins.Transcoding.Aspects;
using MediaPortal.Plugins.Transcoding.Service;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Extensions.MediaServer.Metadata
{
  public class DlnaVideoMetadata
  {
    public static MetadataContainer ParseMediaItem(MediaItem item)
    {
      MetadataContainer info = new MetadataContainer();
      IResourceAccessor mediaItemAccessor = item.GetResourceLocator().CreateAccessor();
      if (mediaItemAccessor is IFileSystemResourceAccessor)
      {
        using (var fsra = (IFileSystemResourceAccessor)mediaItemAccessor.Clone())
        {
          if (!fsra.IsFile)
            return null;
          using (var lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra))
          {
            info.Metadata.Source = lfsra;
            info.Metadata.Size = lfsra.Size;
          }
        }
      }
      else if (mediaItemAccessor is INetworkResourceAccessor)
      {
        using (var nra = (INetworkResourceAccessor)mediaItemAccessor.Clone())
        {
          info.Metadata.Source = nra;
        }
        info.Metadata.Size = 0;
      }

      if (item.Aspects.ContainsKey(TranscodeItemVideoAspect.ASPECT_ID) == true)
      {
        SingleMediaItemAspect transcodeVideoAspect = MediaItemAspect.GetAspect(item.Aspects, TranscodeItemVideoAspect.Metadata);

        object oValue = null;
        oValue = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.VideoContainerType = (VideoContainer)Enum.Parse(typeof(VideoContainer), oValue.ToString());
        }
        oValue = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAspect.ATTR_PIXEL_FORMAT);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), oValue.ToString());
        }
        oValue = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAspect.ATTR_BRAND);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.MajorBrand = oValue.ToString();
        }
        oValue = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAspect.ATTR_CODEC);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.Codec = (VideoCodec)Enum.Parse(typeof(VideoCodec), oValue.ToString());
        }
        oValue = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAspect.ATTR_FOURCC);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.FourCC = oValue.ToString();
        }
        oValue = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAspect.ATTR_H264_PROFILE);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.ProfileType = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), oValue.ToString());
        }
        oValue = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAspect.ATTR_H264_HEADER_LEVEL);
        if (oValue != null)
        {
          info.Video.HeaderLevel = Convert.ToSingle(oValue);
        }
        oValue = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAspect.ATTR_H264_REF_LEVEL);
        if (oValue != null)
        {
          info.Video.RefLevel = Convert.ToSingle(oValue);
        }
        oValue = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAspect.ATTR_PIXEL_ASPECTRATIO);
        if (oValue != null)
        {
          info.Video.PixelAspectRatio = Convert.ToSingle(oValue);
        }
        oValue = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAspect.ATTR_STREAM);
        if (oValue != null)
        {
          info.Video.StreamIndex = Convert.ToInt32(oValue);
        }
        oValue = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAspect.ATTR_TS_TIMESTAMP);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.TimestampType = (Timestamp)Enum.Parse(typeof(Timestamp), oValue.ToString());
        }

        IList<MultipleMediaItemAspect> transcodeItemVideoAudioAspects;
        if (MediaItemAspect.TryGetAspects(item.Aspects, TranscodeItemVideoAudioAspect.Metadata, out transcodeItemVideoAudioAspects))
        {
          object valueBitrate = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIOBITRATE);
          object valueChannel = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIOCHANNEL);
          object valueCodec = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIOCODEC);
          object valueFrequency = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIOFREQUENCY);
          object valueLang = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIOLANGUAGE);
          object valueStream = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIOSTREAM);
          object valueDefault = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIODEFAULT);

          for (int iAudio = 0; iAudio < transcodeItemVideoAudioAspects.Count; iAudio++)
          {
            AudioStream audio = new AudioStream();
            if (valueBitrate != null)
            {
              audio.Bitrate = Convert.ToInt64(valueBitrate);
            }
            if (valueChannel != null)
            {
              audio.Channels = Convert.ToInt32(valueChannel);
            }
            if (valueCodec != null && string.IsNullOrEmpty(valueCodec.ToString()) == false)
            {
              audio.Codec = (AudioCodec)Enum.Parse(typeof(AudioCodec), valueCodec.ToString());
            }
            if (valueFrequency != null)
            {
              audio.Frequency = Convert.ToInt64(valueFrequency);
            }
            if (valueLang != null && string.IsNullOrEmpty(valueLang.ToString()) == false)
            {
              audio.Language = valueLang.ToString();
            }
            if (valueStream != null)
            {
              audio.StreamIndex = Convert.ToInt32(valueStream);
            }
            if (valueDefault != null)
            {
              audio.Default = Convert.ToInt32(valueDefault) > 0;
            }
            info.Audio.Add(audio);
          }
        }

        IList<MultipleMediaItemAspect> transcodeItemVideoEmbeddedAspects;
        if (MediaItemAspect.TryGetAspects(item.Aspects, TranscodeItemVideoEmbeddedAspect.Metadata, out transcodeItemVideoEmbeddedAspects))
        {
          object valueEmSubCodec = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBCODEC);
          object valueEmSubDefault = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBDEFAULT);
          object valueEmSubLang = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBLANGUAGE);
          object valueEmSubStream = transcodeVideoAspect.GetAttributeValue(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBSTREAM);

          SubtitleStream sub = new SubtitleStream();
          if (valueEmSubCodec != null && string.IsNullOrEmpty(valueEmSubCodec.ToString()) == false)
          {
            sub.Codec = (SubtitleCodec)Enum.Parse(typeof(SubtitleCodec), valueEmSubCodec.ToString());
          }
          if (valueEmSubLang != null && string.IsNullOrEmpty(valueEmSubLang.ToString()) == false)
          {
            sub.Language = valueEmSubLang.ToString();
          }
          if (valueEmSubStream != null)
          {
            sub.StreamIndex = Convert.ToInt32(valueEmSubStream);
          }
          if (valueEmSubDefault != null)
          {
            sub.Default = Convert.ToInt32(valueEmSubDefault) > 0;
          }
          info.Subtitles.Add(sub);
        }

        if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID) == true)
        {
          SingleMediaItemAspect videoAspect = MediaItemAspect.GetAspect(item.Aspects, VideoAspect.Metadata);

          oValue = videoAspect.GetAttributeValue(VideoAspect.ATTR_HEIGHT);
          if (oValue != null)
          {
            info.Video.Height = Convert.ToInt32(oValue);
          }
          oValue = videoAspect.GetAttributeValue(VideoAspect.ATTR_WIDTH);
          if (oValue != null)
          {
            info.Video.Width = Convert.ToInt32(oValue);
          }
          oValue = videoAspect.GetAttributeValue(VideoAspect.ATTR_ASPECTRATIO);
          if (oValue != null)
          {
            info.Video.AspectRatio = Convert.ToSingle(oValue);
          }
          oValue = videoAspect.GetAttributeValue(VideoAspect.ATTR_DURATION);
          if (oValue != null)
          {
            info.Metadata.Duration = Convert.ToDouble(oValue);
          }
          oValue = videoAspect.GetAttributeValue(VideoAspect.ATTR_FPS);
          if (oValue != null)
          {
            info.Video.Framerate = Convert.ToSingle(oValue);
          }
          oValue = videoAspect.GetAttributeValue(VideoAspect.ATTR_VIDEOBITRATE);
          if (oValue != null)
          {
            info.Video.Bitrate = Convert.ToInt64(oValue);
          }
        }
        if (item.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
        {
          SingleMediaItemAspect providerResourceAspect = MediaItemAspect.GetAspect(item.Aspects, ProviderResourceAspect.Metadata);
          oValue = providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE);
          if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
          {
            info.Metadata.Mime = oValue.ToString();
          }
        }
        if (info.Audio.Count > 0 && info.Audio[0].Bitrate > 0 && info.Video.Bitrate > 0)
        {
          info.Metadata.Bitrate = info.Audio[0].Bitrate + info.Video.Bitrate;
        }
      }
      return info;
    }
  }
}
