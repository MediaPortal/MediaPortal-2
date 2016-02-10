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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.Plugins.Transcoding.Aspects;
using MediaPortal.Plugins.Transcoding.Service.Metadata.Streams;
using MediaPortal.Plugins.Transcoding.Service.Analyzers;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;

namespace MediaPortal.Plugins.Transcoding.Service.Metadata
{
  public class MediaItemParser
  {
    public static MetadataContainer ParseAudioItem(MediaItem item)
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
      if (item.Aspects.ContainsKey(TranscodeItemAudioAspect.ASPECT_ID) == true)
      {
        object oValue = null;
        oValue = item[TranscodeItemAudioAspect.Metadata].GetAttributeValue(TranscodeItemAudioAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.AudioContainerType = (AudioContainer)Enum.Parse(typeof(AudioContainer), oValue.ToString());
        }
        AudioStream audio = new AudioStream();
        oValue = item[TranscodeItemAudioAspect.Metadata].GetAttributeValue(TranscodeItemAudioAspect.ATTR_STREAM);
        if (oValue != null)
        {
          audio.StreamIndex = Convert.ToInt32(oValue);
          oValue = (string)item[TranscodeItemAudioAspect.Metadata].GetAttributeValue(TranscodeItemAudioAspect.ATTR_CODEC);
          if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
          {
            audio.Codec = (AudioCodec)Enum.Parse(typeof(AudioCodec), oValue.ToString());
          }
          oValue = item[TranscodeItemAudioAspect.Metadata].GetAttributeValue(TranscodeItemAudioAspect.ATTR_CHANNELS);
          if (oValue != null)
          {
            audio.Channels = Convert.ToInt32(oValue);
          }
          oValue = item[TranscodeItemAudioAspect.Metadata].GetAttributeValue(TranscodeItemAudioAspect.ATTR_FREQUENCY);
          if (oValue != null)
          {
            audio.Frequency = Convert.ToInt64(oValue);
          }
          if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID) == true)
          {
            oValue = item[AudioAspect.Metadata].GetAttributeValue(AudioAspect.ATTR_BITRATE);
            if (oValue != null)
            {
              audio.Bitrate = Convert.ToInt64(oValue);
            }
            oValue = item[AudioAspect.Metadata].GetAttributeValue(AudioAspect.ATTR_DURATION);
            if (oValue != null)
            {
              info.Metadata.Duration = Convert.ToDouble(oValue);
            }
          }
          if (item.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
          {
            oValue = item[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE);
            if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
            {
              info.Metadata.Mime = oValue.ToString();
            }
          }
        }
        info.Audio.Add(audio);
        if (info.Audio.Count > 0 && info.Audio[0].Bitrate > 0)
        {
          info.Metadata.Bitrate = info.Audio[0].Bitrate;
        }
      }

      return info;
    }

    public static MetadataContainer ParseImageItem(MediaItem item)
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

      if (item.Aspects.ContainsKey(TranscodeItemImageAspect.ASPECT_ID) == true)
      {
        object oValue = null;
        oValue = item[TranscodeItemImageAspect.Metadata].GetAttributeValue(TranscodeItemImageAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.ImageContainerType = (ImageContainer)Enum.Parse(typeof(ImageContainer), oValue.ToString());
        }
        oValue = item[TranscodeItemImageAspect.Metadata].GetAttributeValue(TranscodeItemImageAspect.ATTR_PIXEL_FORMAT);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Image.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), oValue.ToString());
        }
        if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID) == true)
        {
          oValue = item[ImageAspect.Metadata].GetAttributeValue(ImageAspect.ATTR_HEIGHT);
          if (oValue != null)
          {
            info.Image.Height = Convert.ToInt32(oValue);
          }
          oValue = item[ImageAspect.Metadata].GetAttributeValue(ImageAspect.ATTR_WIDTH);
          if (oValue != null)
          {
            info.Image.Width = Convert.ToInt32(oValue);
          }
          oValue = item[ImageAspect.Metadata].GetAttributeValue(ImageAspect.ATTR_ORIENTATION);
          if (oValue != null)
          {
            info.Image.Orientation = Convert.ToInt32(oValue);
          }
        }
        if (item.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
        {
          oValue = item[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE);
          if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
          {
            info.Metadata.Mime = oValue.ToString();
          }
        }
      }
      return info;
    }

    public static MetadataContainer ParseVideoItem(MediaItem item)
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
        object oValue = null;
        oValue = item[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.VideoContainerType = (VideoContainer)Enum.Parse(typeof(VideoContainer), oValue.ToString());
        }
        oValue = item[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_PIXEL_FORMAT);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), oValue.ToString());
        }
        oValue = item[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_BRAND);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.MajorBrand = oValue.ToString();
        }
        oValue = item[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_CODEC);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.Codec = (VideoCodec)Enum.Parse(typeof(VideoCodec), oValue.ToString());
        }
        oValue = item[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_FOURCC);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.FourCC = oValue.ToString();
        }
        oValue = item[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_H264_PROFILE);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.ProfileType = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), oValue.ToString());
        }
        oValue = item[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_H264_HEADER_LEVEL);
        if (oValue != null)
        {
          info.Video.HeaderLevel = Convert.ToSingle(oValue);
        }
        oValue = item[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_H264_REF_LEVEL);
        if (oValue != null)
        {
          info.Video.RefLevel = Convert.ToSingle(oValue);
        }
        oValue = item[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_PIXEL_ASPECTRATIO);
        if (oValue != null)
        {
          info.Video.PixelAspectRatio = Convert.ToSingle(oValue);
        }
        oValue = item[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_STREAM);
        if (oValue != null)
        {
          info.Video.StreamIndex = Convert.ToInt32(oValue);
        }
        oValue = item[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_TS_TIMESTAMP);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.TimestampType = (Timestamp)Enum.Parse(typeof(Timestamp), oValue.ToString());
        }

        IList<MultipleMediaItemAspect> transcodeItemVideoAudioAspects;
        if (MediaItemAspect.TryGetAspects(item.Aspects, TranscodeItemVideoAudioAspect.Metadata, out transcodeItemVideoAudioAspects))
        {
          for (int iAudio = 0; iAudio < transcodeItemVideoAudioAspects.Count; iAudio++)
          {
            object valueBitrate = transcodeItemVideoAudioAspects[iAudio].GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIOBITRATE);
            object valueChannel = transcodeItemVideoAudioAspects[iAudio].GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIOCHANNEL);
            object valueCodec = transcodeItemVideoAudioAspects[iAudio].GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIOCODEC);
            object valueFrequency = transcodeItemVideoAudioAspects[iAudio].GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIOFREQUENCY);
            object valueLang = transcodeItemVideoAudioAspects[iAudio].GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIOLANGUAGE);
            object valueStream = transcodeItemVideoAudioAspects[iAudio].GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIOSTREAM);
            object valueDefault = transcodeItemVideoAudioAspects[iAudio].GetAttributeValue(TranscodeItemVideoAudioAspect.ATTR_AUDIODEFAULT);

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
          for (int iSub = 0; iSub < transcodeItemVideoEmbeddedAspects.Count; iSub++)
          {
            object valueEmSubCodec = transcodeItemVideoEmbeddedAspects[iSub].GetAttributeValue(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBCODEC);
            object valueEmSubDefault = transcodeItemVideoEmbeddedAspects[iSub].GetAttributeValue(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBDEFAULT);
            object valueEmSubLang = transcodeItemVideoEmbeddedAspects[iSub].GetAttributeValue(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBLANGUAGE);
            object valueEmSubStream = transcodeItemVideoEmbeddedAspects[iSub].GetAttributeValue(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBSTREAM);

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
        }

        if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID) == true)
        {
          oValue = item[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_HEIGHT);
          if (oValue != null)
          {
            info.Video.Height = Convert.ToInt32(oValue);
          }
          oValue = item[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_WIDTH);
          if (oValue != null)
          {
            info.Video.Width = Convert.ToInt32(oValue);
          }
          oValue = item[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_ASPECTRATIO);
          if (oValue != null)
          {
            info.Video.AspectRatio = Convert.ToSingle(oValue);
          }
          oValue = item[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_DURATION);
          if (oValue != null)
          {
            info.Metadata.Duration = Convert.ToDouble(oValue);
          }
          oValue = item[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_FPS);
          if (oValue != null)
          {
            info.Video.Framerate = Convert.ToSingle(oValue);
          }
          oValue = item[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_VIDEOBITRATE);
          if (oValue != null)
          {
            info.Video.Bitrate = Convert.ToInt64(oValue);
          }
        }
        if (item.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
        {
          oValue = item[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE);
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

    public static MetadataContainer ParseLiveVideoItem(MediaItem item)
    {
      string resourcePathStr = (string)item[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      var resourcePath = ResourcePath.Deserialize(resourcePathStr);
      IResourceAccessor stra = SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path);
      if (stra is ILocalFsResourceAccessor)
      {
        return MediaAnalyzer.ParseVideoFile((ILocalFsResourceAccessor)stra);
      }
      else
      {
        return MediaAnalyzer.ParseVideoStream((INetworkResourceAccessor)stra);
      }
    }

    public static MetadataContainer ParseLiveAudioItem(MediaItem item)
    {
      string resourcePathStr = (string)item[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      var resourcePath = ResourcePath.Deserialize(resourcePathStr);
      IResourceAccessor stra = SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path);

      if (stra is ILocalFsResourceAccessor)
      {
        return MediaAnalyzer.ParseAudioFile((ILocalFsResourceAccessor)stra);
      }
      else
      {
        return MediaAnalyzer.ParseAudioStream((INetworkResourceAccessor)stra);
      }
    }
  }
}
