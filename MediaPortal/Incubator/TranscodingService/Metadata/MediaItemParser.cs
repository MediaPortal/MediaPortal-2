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
        oValue = item.Aspects[TranscodeItemAudioAspect.ASPECT_ID].GetAttributeValue(TranscodeItemAudioAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.AudioContainerType = (AudioContainer)Enum.Parse(typeof(AudioContainer), oValue.ToString());
        }
        AudioStream audio = new AudioStream();
        oValue = item.Aspects[TranscodeItemAudioAspect.ASPECT_ID].GetAttributeValue(TranscodeItemAudioAspect.ATTR_STREAM);
        if (oValue != null)
        {
          audio.StreamIndex = Convert.ToInt32(oValue);
          oValue = (string)item.Aspects[TranscodeItemAudioAspect.ASPECT_ID].GetAttributeValue(TranscodeItemAudioAspect.ATTR_CODEC);
          if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
          {
            audio.Codec = (AudioCodec)Enum.Parse(typeof(AudioCodec), oValue.ToString());
          }
          oValue = item.Aspects[TranscodeItemAudioAspect.ASPECT_ID].GetAttributeValue(TranscodeItemAudioAspect.ATTR_CHANNELS);
          if (oValue != null)
          {
            audio.Channels = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[TranscodeItemAudioAspect.ASPECT_ID].GetAttributeValue(TranscodeItemAudioAspect.ATTR_FREQUENCY);
          if (oValue != null)
          {
            audio.Frequency = Convert.ToInt64(oValue);
          }
          if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID) == true)
          {
            oValue = item.Aspects[AudioAspect.ASPECT_ID].GetAttributeValue(AudioAspect.ATTR_BITRATE);
            if (oValue != null)
            {
              audio.Bitrate = Convert.ToInt64(oValue);
            }
            oValue = item.Aspects[AudioAspect.ASPECT_ID].GetAttributeValue(AudioAspect.ATTR_DURATION);
            if (oValue != null)
            {
              info.Metadata.Duration = Convert.ToDouble(oValue);
            }
          }
          if (item.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
          {
            oValue = item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_MIME_TYPE);
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
        oValue = item.Aspects[TranscodeItemImageAspect.ASPECT_ID].GetAttributeValue(TranscodeItemImageAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.ImageContainerType = (ImageContainer)Enum.Parse(typeof(ImageContainer), oValue.ToString());
        }
        oValue = item.Aspects[TranscodeItemImageAspect.ASPECT_ID].GetAttributeValue(TranscodeItemImageAspect.ATTR_PIXEL_FORMAT);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Image.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), oValue.ToString());
        }
        if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID) == true)
        {
          oValue = item.Aspects[ImageAspect.ASPECT_ID].GetAttributeValue(ImageAspect.ATTR_HEIGHT);
          if (oValue != null)
          {
            info.Image.Height = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[ImageAspect.ASPECT_ID].GetAttributeValue(ImageAspect.ATTR_WIDTH);
          if (oValue != null)
          {
            info.Image.Width = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[ImageAspect.ASPECT_ID].GetAttributeValue(ImageAspect.ATTR_ORIENTATION);
          if (oValue != null)
          {
            info.Image.Orientation = Convert.ToInt32(oValue);
          }
        }
        if (item.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
        {
          oValue = item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_MIME_TYPE);
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
        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetAttributeValue(TranscodeItemVideoAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.VideoContainerType = (VideoContainer)Enum.Parse(typeof(VideoContainer), oValue.ToString());
        }
        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetAttributeValue(TranscodeItemVideoAspect.ATTR_PIXEL_FORMAT);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), oValue.ToString());
        }
        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetAttributeValue(TranscodeItemVideoAspect.ATTR_BRAND);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.MajorBrand = oValue.ToString();
        }
        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetAttributeValue(TranscodeItemVideoAspect.ATTR_CODEC);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.Codec = (VideoCodec)Enum.Parse(typeof(VideoCodec), oValue.ToString());
        }
        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetAttributeValue(TranscodeItemVideoAspect.ATTR_FOURCC);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.FourCC = oValue.ToString();
        }
        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetAttributeValue(TranscodeItemVideoAspect.ATTR_H264_PROFILE);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.ProfileType = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), oValue.ToString());
        }
        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetAttributeValue(TranscodeItemVideoAspect.ATTR_H264_HEADER_LEVEL);
        if (oValue != null)
        {
          info.Video.HeaderLevel = Convert.ToSingle(oValue);
        }
        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetAttributeValue(TranscodeItemVideoAspect.ATTR_H264_REF_LEVEL);
        if (oValue != null)
        {
          info.Video.RefLevel = Convert.ToSingle(oValue);
        }
        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetAttributeValue(TranscodeItemVideoAspect.ATTR_PIXEL_ASPECTRATIO);
        if (oValue != null)
        {
          info.Video.PixelAspectRatio = Convert.ToSingle(oValue);
        }
        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetAttributeValue(TranscodeItemVideoAspect.ATTR_STREAM);
        if (oValue != null)
        {
          info.Video.StreamIndex = Convert.ToInt32(oValue);
        }
        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetAttributeValue(TranscodeItemVideoAspect.ATTR_TS_TIMESTAMP);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.TimestampType = (Timestamp)Enum.Parse(typeof(Timestamp), oValue.ToString());
        }

        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_AUDIOBITRATES);
        if (oValue != null)
        {
          List<object> valuesBitrates = new List<object>(item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_AUDIOBITRATES));
          List<object> valuesChannels = new List<object>(item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_AUDIOCHANNELS));
          List<object> valuesCodecs = new List<object>(item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_AUDIOCODECS));
          List<object> valuesFrequencies = new List<object>(item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_AUDIOFREQUENCIES));
          List<object> valuesLangs = new List<object>(item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_AUDIOLANGUAGES));
          List<object> valuesStreams = new List<object>(item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_AUDIOSTREAMS));
          List<object> valuesDefaults = new List<object>(item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_AUDIODEFAULTS));
          for (int iAudio = 0; iAudio < valuesStreams.Count; iAudio++)
          {
            AudioStream audio = new AudioStream();
            if (valuesBitrates.ElementAtOrDefault(iAudio) != null)
            {
              audio.Bitrate = Convert.ToInt64(valuesBitrates[iAudio]);
            }
            if (valuesChannels.ElementAtOrDefault(iAudio) != null)
            {
              audio.Channels = Convert.ToInt32(valuesChannels[iAudio]);
            }
            if (valuesCodecs.ElementAtOrDefault(iAudio) != null && string.IsNullOrEmpty(valuesCodecs[iAudio].ToString()) == false)
            {
              audio.Codec = (AudioCodec)Enum.Parse(typeof(AudioCodec), valuesCodecs[iAudio].ToString());
            }
            if (valuesFrequencies.ElementAtOrDefault(iAudio) != null)
            {
              audio.Frequency = Convert.ToInt64(valuesFrequencies[iAudio]);
            }
            if (valuesLangs.ElementAtOrDefault(iAudio) != null && string.IsNullOrEmpty(valuesLangs[iAudio].ToString()) == false)
            {
              audio.Language = valuesLangs[iAudio].ToString();
            }
            if (valuesStreams.ElementAtOrDefault(iAudio) != null)
            {
              audio.StreamIndex = Convert.ToInt32(valuesStreams[iAudio]);
            }
            if (valuesDefaults.ElementAtOrDefault(iAudio) != null)
            {
              audio.Default = Convert.ToInt32(valuesDefaults[iAudio]) > 0;
            }
            info.Audio.Add(audio);
          }
        }

        oValue = item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_EMBEDDED_SUBCODECS);
        if (oValue != null)
        {
          List<object> valuesEmSubCodecs = new List<object>(item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_EMBEDDED_SUBCODECS));
          List<object> valuesEmSubDefaults = new List<object>(item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_EMBEDDED_SUBDEFAULTS));
          List<object> valuesEmSubLangs = new List<object>(item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_EMBEDDED_SUBLANGUAGES));
          List<object> valuesEmSubStreams = new List<object>(item.Aspects[TranscodeItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(TranscodeItemVideoAspect.ATTR_EMBEDDED_SUBSTREAMS));
          for (int iSub = 0; iSub < valuesEmSubStreams.Count; iSub++)
          {
            SubtitleStream sub = new SubtitleStream();
            if (valuesEmSubCodecs.ElementAtOrDefault(iSub) != null && string.IsNullOrEmpty(valuesEmSubCodecs[iSub].ToString()) == false)
            {
              sub.Codec = (SubtitleCodec)Enum.Parse(typeof(SubtitleCodec), valuesEmSubCodecs[iSub].ToString());
            }
            if (valuesEmSubLangs.ElementAtOrDefault(iSub) != null && string.IsNullOrEmpty(valuesEmSubLangs[iSub].ToString()) == false)
            {
              sub.Language = valuesEmSubLangs[iSub].ToString();
            }
            if (valuesEmSubStreams.ElementAtOrDefault(iSub) != null)
            {
              sub.StreamIndex = Convert.ToInt32(valuesEmSubStreams[iSub]);
            }
            if (valuesEmSubDefaults.ElementAtOrDefault(iSub) != null)
            {
              sub.Default = Convert.ToInt32(valuesEmSubDefaults[iSub]) > 0;
            }
            info.Subtitles.Add(sub);
          }
        }

        if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID) == true)
        {
          oValue = item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_HEIGHT);
          if (oValue != null)
          {
            info.Video.Height = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_WIDTH);
          if (oValue != null)
          {
            info.Video.Width = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_ASPECTRATIO);
          if (oValue != null)
          {
            info.Video.AspectRatio = Convert.ToSingle(oValue);
          }
          oValue = item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_DURATION);
          if (oValue != null)
          {
            info.Metadata.Duration = Convert.ToDouble(oValue);
          }
          oValue = item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_FPS);
          if (oValue != null)
          {
            info.Video.Framerate = Convert.ToSingle(oValue);
          }
          oValue = item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_VIDEOBITRATE);
          if (oValue != null)
          {
            info.Video.Bitrate = Convert.ToInt64(oValue);
          }
        }
        if (item.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
        {
          oValue = item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_MIME_TYPE);
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
      string resourcePathStr = (string)item.Aspects[ProviderResourceAspect.ASPECT_ID][ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH];
      var resourcePath = ResourcePath.Deserialize(resourcePathStr);
      IResourceAccessor stra = SlimTvResourceAccessor.GetResourceAccessor(resourcePath.BasePathSegment.Path);

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
      string resourcePathStr = (string)item.Aspects[ProviderResourceAspect.ASPECT_ID][ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH];
      var resourcePath = ResourcePath.Deserialize(resourcePathStr);
      IResourceAccessor stra = SlimTvResourceAccessor.GetResourceAccessor(resourcePath.BasePathSegment.Path);

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
