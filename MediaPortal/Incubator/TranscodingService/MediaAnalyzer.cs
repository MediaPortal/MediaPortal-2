#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Process;
using MediaPortal.Extensions.MetadataExtractors.FFMpegLib;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers;
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces.Helpers;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Settings;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.Transcoding.Interfaces.Aspects;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata.Streams;
using MediaPortal.Plugins.Transcoding.Interfaces.SlimTv;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml;
using MediaPortal.Common.PathManager;
using System.IO;
using MediaPortal.Utilities;

namespace MediaPortal.Plugins.Transcoding.Service
{
  public class MediaAnalyzer : IMediaAnalyzer
  {
    #region Constants

    /// <summary>
    /// Maximum duration for analyzing H264 stream.
    /// </summary>
    private const int H264_TIMEOUT_MS = 3000;

    #endregion

    private readonly object FFPROBE_THROTTLE_LOCK = new object();
    private int _analyzerMaximumThreads;
    private int _analyzerTimeout;
    private long _analyzerStreamTimeout;
    private ILogger _logger = null;
    private readonly Dictionary<string, CultureInfo> _countryCodesMapping = new Dictionary<string, CultureInfo>();
    private readonly Dictionary<float, long> _h264MaxDpbMbs = new Dictionary<float, long>();
    private SlimTvHandler _slimTvHandler = new SlimTvHandler();
    private ICollection<string> _audioExtensions = new List<string>();
    private ICollection<string> _videoExtensions = new List<string>();
    private ICollection<string> _imageExtensions = new List<string>();
    private readonly string[] DEFAULT_AUDIO_FILE_EXTENSIONS = new string[]
      {
          ".ape",
          ".flac",
          ".mp3",
          ".ogg",
          ".wv",
          ".wav",
          ".wma",
          ".mp4",
          ".m4a",
          ".m4p",
          ".mpc",
          ".mp+",
          ".mpp",
          ".dsf",
          ".dff",
      };
    private readonly string[] DEFAULT_IMAGE_FILE_EXTENSIONS = new string[]
      {
          ".jpg",
          ".jpeg",
          ".png",
          ".bmp",
          ".gif",
          ".tga",
          ".tiff",
          ".tif",
      };
    private readonly string[] DEFAULT_VIDEO_FILE_EXTENSIONS = new string[]
      {
          ".mkv",
          ".mk3d",
          ".ogm",
          ".avi",
          ".wmv",
          ".mpg",
          ".mp4",
          ".m4v",
          ".ts",
          ".flv",
          ".m2ts",
          ".mts",
          ".mov",
          ".wtv",
          ".dvr-ms",
      };

    public MediaAnalyzer()
    {
      _analyzerMaximumThreads = TranscodingServicePlugin.Settings.AnalyzerMaximumThreads;
      _analyzerTimeout = TranscodingServicePlugin.Settings.AnalyzerTimeout * 2;
      _analyzerStreamTimeout = TranscodingServicePlugin.Settings.AnalyzerStreamTimeout;
      _logger = ServiceRegistration.Get<ILogger>();

      _audioExtensions = new List<string>(
        ReadExtensions("MediaPortal.Extensions.MetadataExtractors.TranscodingService.MetadataExtractor.Settings.TranscodeAudioMetadataExtractorSettings.xml",
        "AudioFileExtensions", DEFAULT_AUDIO_FILE_EXTENSIONS).Select(e => e.ToLowerInvariant()));

      _videoExtensions = new List<string>(
        ReadExtensions("MediaPortal.Extensions.MetadataExtractors.TranscodingService.MetadataExtractor.Settings.TranscodeVideoMetadataExtractorSettings.xml",
        "VideoFileExtensions", DEFAULT_VIDEO_FILE_EXTENSIONS).Select(e => e.ToLowerInvariant()));

      _imageExtensions = new List<string>(
        ReadExtensions("MediaPortal.Extensions.MetadataExtractors.TranscodingService.MetadataExtractor.Settings.TranscodeImageMetadataExtractorSettings.xml",
        "ImageFileExtensions", DEFAULT_IMAGE_FILE_EXTENSIONS).Select(e => e.ToLowerInvariant()));

      _h264MaxDpbMbs.Add(1F, 396);
      _h264MaxDpbMbs.Add(1.1F, 396);
      _h264MaxDpbMbs.Add(1.2F, 900);
      _h264MaxDpbMbs.Add(1.3F, 2376);
      _h264MaxDpbMbs.Add(2F, 2376);
      _h264MaxDpbMbs.Add(2.1F, 4752);
      _h264MaxDpbMbs.Add(2.2F, 8100);
      _h264MaxDpbMbs.Add(3F, 8100);
      _h264MaxDpbMbs.Add(3.1F, 18000);
      _h264MaxDpbMbs.Add(3.2F, 20480);
      _h264MaxDpbMbs.Add(4F, 32768);
      _h264MaxDpbMbs.Add(4.1F, 32768);
      _h264MaxDpbMbs.Add(4.2F, 34816);
      _h264MaxDpbMbs.Add(5F, 110400);
      _h264MaxDpbMbs.Add(5.1F, 184320);
      _h264MaxDpbMbs.Add(5.2F, 184320);

      CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
      foreach (CultureInfo culture in cultures)
      {
        try
        {
          _countryCodesMapping[culture.ThreeLetterISOLanguageName.ToUpperInvariant()] = culture;
        }
        catch { }
      }
    }

    private bool HasAudioExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return _audioExtensions.Contains(ext);
    }

    private bool HasVideoExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return _videoExtensions.Contains(ext);
    }

    private bool HasImageExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return _imageExtensions.Contains(ext);
    }

    private string[] ReadExtensions(string settingsFile, string propertyName, string[] defaults)
    {
      object array = null;
      try
      {
        IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
        string dataPath = pathManager.GetPath("<CONFIG>");
        settingsFile = Path.Combine(dataPath, settingsFile);
        if (File.Exists(settingsFile))
        {
          XDocument document = XDocument.Load(settingsFile);
          XmlSerializer ser = new XmlSerializer(typeof(string[]));
          IEnumerable<XElement> properties =
            from element in document.Root.Elements("Property")
            where (string)element.Attribute("Name") == propertyName
            select element;
          foreach (XElement prop in properties)
          {
            array = ser.Deserialize(prop.FirstNode.CreateReader());
            break;
          }
        }
      }
      catch(Exception ex)
      {
        _logger.Error("MediaAnalyzer: Could not load {0}, using defaults", ex, propertyName);
      }
      if(array != null) return (string[])array;
      return defaults;
    }

    private ProcessExecutionResult ParseFile(ILocalFsResourceAccessor lfsra, string arguments)
    {
      ProcessExecutionResult executionResult;
      lock (FFPROBE_THROTTLE_LOCK)
        executionResult = FFMpegBinary.FFProbeExecuteWithResourceAccessAsync(lfsra, arguments, ProcessPriorityClass.Idle, _analyzerTimeout).Result;

      // My guess (agree with dtb's comment): AFAIK ffmpeg uses stdout to pipe out binary data(multimedia, snapshots, etc.)
      // and stderr is used for logging purposes. In your example you use stdout.
      // http://stackoverflow.com/questions/4246758/why-doesnt-this-method-redirect-my-output-from-exe-ffmpeg
      return executionResult;
    }

    public MetadataContainer ParseMediaStream(IResourceAccessor MediaResource)
    {
      if (MediaResource is ILocalFsResourceAccessor)
      {
        ILocalFsResourceAccessor fileResource = (ILocalFsResourceAccessor)MediaResource;
        string fileName = fileResource.LocalFileSystemPath;
        string arguments = "";
        if (HasImageExtension(fileName))
        {
          //Default image decoder (image2) fails if file name contains å, ø, ö etc., so force format to image2pipe
          arguments = string.Format("-threads {0} -f image2pipe -i \"{1}\"", _analyzerMaximumThreads, fileName);
        }
        else
        {
          arguments = string.Format("-threads {0} -i \"{1}\"", _analyzerMaximumThreads, fileName);
        }

        ProcessExecutionResult executionResult = ParseFile(fileResource, arguments);
        if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
        {
          //_logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
          MetadataContainer info = new MetadataContainer { Metadata = { Source = MediaResource } };
          info.Metadata.Size = fileResource.Size;
          FFMpegParseFFMpegOutput.ParseFFMpegOutput(executionResult.StandardError, ref info, _countryCodesMapping);
          if (info.IsImage || HasImageExtension(fileName))
          {
            info.Metadata.Mime = MimeDetector.GetFileMime(fileResource, "image/unknown");
          }
          else if (info.IsVideo|| HasVideoExtension(fileName))
          {
            info.Metadata.Mime = MimeDetector.GetFileMime(fileResource, "video/unknown");
            FFMpegParseH264Info.ParseH264Info(ref info, _h264MaxDpbMbs, H264_TIMEOUT_MS);
            FFMpegParseMPEG2TSInfo.ParseMPEG2TSInfo(ref info);
          }
          else if (info.IsAudio || HasAudioExtension(fileName))
          {
            info.Metadata.Mime = MimeDetector.GetFileMime(fileResource, "audio/unknown");
          }
          else
          {
            info.Metadata.Mime = MimeDetector.GetFileMime(fileResource, "unknown/unknown");
          }
          return info;
        }

        if (executionResult != null)
          _logger.Error("MediaAnalyzer: Failed to extract media type information for resource '{0}', Result: {1}, ExitCode: {2}, Success: {3}", fileName, executionResult.StandardError, executionResult.ExitCode, executionResult.Success);
        else
          _logger.Error("MediaAnalyzer: Failed to extract media type information for resource '{0}', executionResult=null", fileName);
      }
      else if (MediaResource is INetworkResourceAccessor)
      {
        string url = ((INetworkResourceAccessor)MediaResource).URL;
        string arguments = "";
        if (url.StartsWith("rtsp://", StringComparison.InvariantCultureIgnoreCase) == true)
        {
          arguments += "-rtsp_transport +tcp+udp ";
        }
        arguments += "-analyzeduration " + _analyzerStreamTimeout + " ";
        arguments += string.Format("-i \"{0}\"", url);

        ProcessExecutionResult executionResult;
        lock (FFPROBE_THROTTLE_LOCK)
          executionResult = FFMpegBinary.FFProbeExecuteAsync(arguments, ProcessPriorityClass.Idle, _analyzerTimeout).Result;

        if (executionResult != null && executionResult.Success && executionResult.ExitCode == 0 && !string.IsNullOrEmpty(executionResult.StandardError))
        {
          //_logger.Debug("MediaAnalyzer: Successfully ran FFProbe:\n {0}", executionResult.StandardError);
          MetadataContainer info = new MetadataContainer { Metadata = { Source = MediaResource } };
          info.Metadata.Size = 0;
          FFMpegParseFFMpegOutput.ParseFFMpegOutput(executionResult.StandardError, ref info, _countryCodesMapping);
          if (info.IsVideo)
          {
            info.Metadata.Mime = MimeDetector.GetUrlMime(url, "video/unknown");
            FFMpegParseH264Info.ParseH264Info(ref info, _h264MaxDpbMbs, H264_TIMEOUT_MS);
            FFMpegParseMPEG2TSInfo.ParseMPEG2TSInfo(ref info);
          }
          else if (info.IsAudio)
          {
            info.Metadata.Mime = MimeDetector.GetUrlMime(url, "audio/unknown");
          }
          else
          {
            info.Metadata.Mime = MimeDetector.GetUrlMime(url, "unknown/unknown");
          }
          return info;
        }

        if (executionResult != null)
          _logger.Error("MediaAnalyzer: Failed to extract media type information for resource '{0}', Result: {1}, ExitCode: {2}, Success: {3}", url, executionResult.StandardError, executionResult.ExitCode, executionResult.Success);
        else
          _logger.Error("MediaAnalyzer: Failed to extract media type information for resource '{0}', executionResult=null", url);
      }
      return null;
    }

    private MetadataContainer ParseAudioItem(MediaItem Media)
    {
      MetadataContainer info = new MetadataContainer();
      IResourceAccessor mia = null;
      if (Media is LiveTvMediaItem)
      {
        string resourcePathStr = (string)Media[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
        var resourcePath = ResourcePath.Deserialize(resourcePathStr);
        mia = SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path);

        info.Metadata.Size = 0;
      }
      else
      {
        mia = Media.GetResourceLocator().CreateAccessor();
        if (mia is ILocalFsResourceAccessor)
        {
          info.Metadata.Size = ((ILocalFsResourceAccessor)mia).Size;
        }
        else if (mia is INetworkResourceAccessor)
        {
          info.Metadata.Size = 0;
        }
      }
      info.Metadata.Source = mia;

      if (Media.Aspects.ContainsKey(TranscodeItemAudioAspect.ASPECT_ID) == true)
      {
        object oValue = null;
        oValue = Media[TranscodeItemAudioAspect.Metadata].GetAttributeValue(TranscodeItemAudioAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.AudioContainerType = (AudioContainer)Enum.Parse(typeof(AudioContainer), oValue.ToString());
        }
        AudioStream audio = new AudioStream();
        oValue = Media[TranscodeItemAudioAspect.Metadata].GetAttributeValue(TranscodeItemAudioAspect.ATTR_STREAM);
        if (oValue != null)
        {
          audio.StreamIndex = Convert.ToInt32(oValue);
          oValue = (string)Media[TranscodeItemAudioAspect.Metadata].GetAttributeValue(TranscodeItemAudioAspect.ATTR_CODEC);
          if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
          {
            audio.Codec = (AudioCodec)Enum.Parse(typeof(AudioCodec), oValue.ToString());
          }
          oValue = Media[TranscodeItemAudioAspect.Metadata].GetAttributeValue(TranscodeItemAudioAspect.ATTR_CHANNELS);
          if (oValue != null)
          {
            audio.Channels = Convert.ToInt32(oValue);
          }
          oValue = Media[TranscodeItemAudioAspect.Metadata].GetAttributeValue(TranscodeItemAudioAspect.ATTR_FREQUENCY);
          if (oValue != null)
          {
            audio.Frequency = Convert.ToInt64(oValue);
          }
          if (Media.Aspects.ContainsKey(AudioAspect.ASPECT_ID) == true)
          {
            oValue = Media[AudioAspect.Metadata].GetAttributeValue(AudioAspect.ATTR_BITRATE);
            if (oValue != null)
            {
              audio.Bitrate = Convert.ToInt64(oValue);
            }
            oValue = Media[AudioAspect.Metadata].GetAttributeValue(AudioAspect.ATTR_DURATION);
            if (oValue != null)
            {
              info.Metadata.Duration = Convert.ToDouble(oValue);
            }
          }
          if (Media.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
          {
            oValue = Media[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE);
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

    private MetadataContainer ParseImageItem(MediaItem Media)
    {
      MetadataContainer info = new MetadataContainer();
      IResourceAccessor mia = Media.GetResourceLocator().CreateAccessor();
      info.Metadata.Source = mia;
      if (mia is ILocalFsResourceAccessor)
      {
        info.Metadata.Size = ((ILocalFsResourceAccessor)mia).Size;
      }
      else if (mia is INetworkResourceAccessor)
      {
        info.Metadata.Size = 0;
      }

      if (Media.Aspects.ContainsKey(TranscodeItemImageAspect.ASPECT_ID) == true)
      {
        object oValue = null;
        oValue = Media[TranscodeItemImageAspect.Metadata].GetAttributeValue(TranscodeItemImageAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.ImageContainerType = (ImageContainer)Enum.Parse(typeof(ImageContainer), oValue.ToString());
        }
        oValue = Media[TranscodeItemImageAspect.Metadata].GetAttributeValue(TranscodeItemImageAspect.ATTR_PIXEL_FORMAT);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Image.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), oValue.ToString());
        }
        if (Media.Aspects.ContainsKey(ImageAspect.ASPECT_ID) == true)
        {
          oValue = Media[ImageAspect.Metadata].GetAttributeValue(ImageAspect.ATTR_HEIGHT);
          if (oValue != null)
          {
            info.Image.Height = Convert.ToInt32(oValue);
          }
          oValue = Media[ImageAspect.Metadata].GetAttributeValue(ImageAspect.ATTR_WIDTH);
          if (oValue != null)
          {
            info.Image.Width = Convert.ToInt32(oValue);
          }
          oValue = Media[ImageAspect.Metadata].GetAttributeValue(ImageAspect.ATTR_ORIENTATION);
          if (oValue != null)
          {
            info.Image.Orientation = Convert.ToInt32(oValue);
          }
        }
        if (Media.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
        {
          oValue = Media[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE);
          if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
          {
            info.Metadata.Mime = oValue.ToString();
          }
        }
      }
      return info;
    }

    private MetadataContainer ParseVideoItem(MediaItem Media)
    {
      MetadataContainer info = new MetadataContainer();
      IResourceAccessor mia = null;
      if (Media is LiveTvMediaItem)
      {
        string resourcePathStr = (string)Media[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
        var resourcePath = ResourcePath.Deserialize(resourcePathStr);
        mia = SlimTvResourceProvider.GetResourceAccessor(resourcePath.BasePathSegment.Path);

        info.Metadata.Size = 0;
      }
      else
      {
        mia = Media.GetResourceLocator().CreateAccessor();
        if (mia is ILocalFsResourceAccessor)
        {
          info.Metadata.Size = ((ILocalFsResourceAccessor)mia).Size;
        }
        else if (mia is INetworkResourceAccessor)
        {
          info.Metadata.Size = 0;
        }
      }
      info.Metadata.Source = mia;

      if (Media.Aspects.ContainsKey(TranscodeItemVideoAspect.ASPECT_ID) == true)
      {
        object oValue = null;
        oValue = Media[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.VideoContainerType = (VideoContainer)Enum.Parse(typeof(VideoContainer), oValue.ToString());
        }
        oValue = Media[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_PIXEL_FORMAT);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), oValue.ToString());
        }
        oValue = Media[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_BRAND);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.MajorBrand = oValue.ToString();
        }
        oValue = Media[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_CODEC);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.Codec = (VideoCodec)Enum.Parse(typeof(VideoCodec), oValue.ToString());
        }
        oValue = Media[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_FOURCC);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.FourCC = oValue.ToString();
        }
        oValue = Media[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_H264_PROFILE);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.ProfileType = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), oValue.ToString());
        }
        oValue = Media[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_H264_HEADER_LEVEL);
        if (oValue != null)
        {
          info.Video.HeaderLevel = Convert.ToSingle(oValue);
        }
        oValue = Media[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_H264_REF_LEVEL);
        if (oValue != null)
        {
          info.Video.RefLevel = Convert.ToSingle(oValue);
        }
        oValue = Media[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_PIXEL_ASPECTRATIO);
        if (oValue != null)
        {
          info.Video.PixelAspectRatio = Convert.ToSingle(oValue);
        }
        oValue = Media[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_STREAM);
        if (oValue != null)
        {
          info.Video.StreamIndex = Convert.ToInt32(oValue);
        }
        oValue = Media[TranscodeItemVideoAspect.Metadata].GetAttributeValue(TranscodeItemVideoAspect.ATTR_TS_TIMESTAMP);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.TimestampType = (Timestamp)Enum.Parse(typeof(Timestamp), oValue.ToString());
        }

        IList<MultipleMediaItemAspect> transcodeItemVideoAudioAspects;
        if (MediaItemAspect.TryGetAspects(Media.Aspects, TranscodeItemVideoAudioAspect.Metadata, out transcodeItemVideoAudioAspects))
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
        if (MediaItemAspect.TryGetAspects(Media.Aspects, TranscodeItemVideoEmbeddedAspect.Metadata, out transcodeItemVideoEmbeddedAspects))
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

        if (Media.Aspects.ContainsKey(VideoAspect.ASPECT_ID) == true)
        {
          oValue = Media[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_HEIGHT);
          if (oValue != null)
          {
            info.Video.Height = Convert.ToInt32(oValue);
          }
          oValue = Media[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_WIDTH);
          if (oValue != null)
          {
            info.Video.Width = Convert.ToInt32(oValue);
          }
          oValue = Media[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_ASPECTRATIO);
          if (oValue != null)
          {
            info.Video.AspectRatio = Convert.ToSingle(oValue);
          }
          oValue = Media[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_DURATION);
          if (oValue != null)
          {
            info.Metadata.Duration = Convert.ToDouble(oValue);
          }
          oValue = Media[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_FPS);
          if (oValue != null)
          {
            info.Video.Framerate = Convert.ToSingle(oValue);
          }
          oValue = Media[VideoAspect.Metadata].GetAttributeValue(VideoAspect.ATTR_VIDEOBITRATE);
          if (oValue != null)
          {
            info.Video.Bitrate = Convert.ToInt64(oValue);
          }
        }
        if (Media.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
        {
          oValue = Media[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE);
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

    private void CopyAspects(MediaItem SourceMediaItem, MediaItem DestinationMediaItem)
    {
      foreach (IList<MediaItemAspect> aspectList in SourceMediaItem.Aspects.Values.ToList())
      {
        foreach (MediaItemAspect aspectData in aspectList.ToList())
        {
          if (aspectData is SingleMediaItemAspect)
          {
            MediaItemAspect.SetAspect(DestinationMediaItem.Aspects, (SingleMediaItemAspect)aspectData);
          }
          else if (aspectData is MultipleMediaItemAspect)
          {
            MediaItemAspect.AddOrUpdateAspect(DestinationMediaItem.Aspects, (MultipleMediaItemAspect)aspectData);
          }
        }
      }
    }

    private MetadataContainer ParseSlimTvItem(LiveTvMediaItem LiveMedia)
    {
      if (LiveMedia.Aspects.ContainsKey(TranscodeItemVideoAspect.ASPECT_ID))
      {
        return ParseVideoItem(LiveMedia);
      }
      else if (LiveMedia.Aspects.ContainsKey(TranscodeItemAudioAspect.ASPECT_ID))
      {
        return ParseAudioItem(LiveMedia);
      }
      else //Not been tuned for transcode aspects yet
      {
        MediaItem liveMediaItem = new MediaItem(LiveMedia.MediaItemId, LiveMedia.Aspects); //Preserve current aspects
        IChannel channel = (IChannel)LiveMedia.AdditionalProperties[LiveTvMediaItem.CHANNEL];
        MetadataContainer container = ParseChannelStream(channel.ChannelId, out liveMediaItem);
        if (container == null) return null;
        CopyAspects(liveMediaItem, LiveMedia);
        return container;
      }
    }

    public MetadataContainer ParseMediaItem(MediaItem Media)
    {
      MetadataContainer info = null;
      if (Media.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        if (Media.Aspects.ContainsKey(TranscodeItemAudioAspect.ASPECT_ID) == false)
        {
          if (Media[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE).ToString() == LiveTvMediaItem.MIME_TYPE_RADIO)
          {
            info = ParseSlimTvItem((LiveTvMediaItem)Media);
            if(info != null) info.Metadata.Live = true;
          }
          else
          {
            _logger.Warn("MediaAnalyzer: Mediaitem {0} contains no transcoding audio information", Media.MediaItemId);
            return info;
          }
        }
        else
        {
          info = ParseAudioItem(Media);
        }
      }
      else if (Media.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
      {
        if (Media.Aspects.ContainsKey(TranscodeItemImageAspect.ASPECT_ID) == false)
        {
          _logger.Warn("MediaAnalyzer: Mediaitem {0} contains no transcoding image information", Media.MediaItemId);
          return info;
        }
        else
        {
          info = ParseImageItem(Media);
        }
      }
      else if (Media.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      {
        if (Media.Aspects.ContainsKey(TranscodeItemVideoAspect.ASPECT_ID) == false)
        {
          if (Media[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE).ToString() == LiveTvMediaItem.MIME_TYPE_TV)
          {
            info = ParseSlimTvItem((LiveTvMediaItem)Media);
            if (info != null) info.Metadata.Live = true;
          }
          else
          {
            _logger.Warn("MediaAnalyzer: Mediaitem {0} contains no transcoding video information", Media.MediaItemId);
            return info;
          }
        }
        else
        {
          info = ParseVideoItem(Media);
        }
      }
      else
      {
        _logger.Warn("MediaAnalyzer: Mediaitem {0} contains no required aspect information", Media.MediaItemId);
      }
      return info;
    }

    public MetadataContainer ParseChannelStream(int ChannelId, out MediaItem ChannelMediaItem)
    {
      MetadataContainer info = null;
      ChannelMediaItem = null;
      string identifier = "MediaAnalyzer_" + ChannelId;
      if (_slimTvHandler.StartTuning(identifier, ChannelId, out ChannelMediaItem))
      {
        try
        {
          if (ChannelMediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
          {
            //Create media item with channel GUID
            string channelGuid = "{54560000-0000-0000-0000-" + ChannelId.ToString("000000000000") + "}";
            LiveTvMediaItem liveTvMediaItem = new LiveTvMediaItem(new Guid(channelGuid), ChannelMediaItem.Aspects);
            foreach (KeyValuePair<string, object> props in ((LiveTvMediaItem)ChannelMediaItem).AdditionalProperties)
            {
              liveTvMediaItem.AdditionalProperties.Add(props.Key, props.Value);
            }
            ChannelMediaItem = liveTvMediaItem;
          }
          else if (ChannelMediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
          {
            //Create media item with channel GUID
            string channelGuid = "{5244494F-0000-0000-0000-" + ChannelId.ToString("000000000000") + "}";
            LiveTvMediaItem liveRadioMediaItem = new LiveTvMediaItem(new Guid(channelGuid), ChannelMediaItem.Aspects);
            foreach (KeyValuePair<string, object> props in ((LiveTvMediaItem)ChannelMediaItem).AdditionalProperties)
            {
              liveRadioMediaItem.AdditionalProperties.Add(props.Key, props.Value);
            }
            ChannelMediaItem = liveRadioMediaItem;
          }

          IResourceAccessor ra = _slimTvHandler.GetAnalysisAccessor(ChannelId);
          info = ParseMediaStream(ra);
          if (info == null) return null;

          if (info.IsAudio || ChannelMediaItem[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE).ToString() == LiveTvMediaItem.MIME_TYPE_RADIO)
          {
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemAudioAspect.ATTR_CONTAINER, info.Metadata.AudioContainerType.ToString());
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemAudioAspect.ATTR_STREAM, info.Audio[0].StreamIndex);
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemAudioAspect.ATTR_CODEC, info.Audio[0].Codec.ToString());
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemAudioAspect.ATTR_CHANNELS, info.Audio[0].Channels);
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemAudioAspect.ATTR_FREQUENCY, info.Audio[0].Frequency);

            if (info.Metadata.Bitrate > 0) MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, AudioAspect.ATTR_BITRATE, info.Metadata.Bitrate);
            //MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, AudioAspect.ATTR_DURATION, 0);
          }
          else if (info.IsVideo || ChannelMediaItem[ProviderResourceAspect.Metadata].GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE).ToString() == LiveTvMediaItem.MIME_TYPE_TV)
          {
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemVideoAspect.ATTR_CONTAINER, info.Metadata.VideoContainerType.ToString());
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemVideoAspect.ATTR_STREAM, info.Video.StreamIndex);
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemVideoAspect.ATTR_CODEC, info.Video.Codec.ToString());
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemVideoAspect.ATTR_FOURCC, StringUtils.TrimToNull(info.Video.FourCC));
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemVideoAspect.ATTR_BRAND, StringUtils.TrimToNull(info.Metadata.MajorBrand));
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemVideoAspect.ATTR_PIXEL_FORMAT, info.Video.PixelFormatType.ToString());
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemVideoAspect.ATTR_PIXEL_ASPECTRATIO, info.Video.PixelAspectRatio);
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemVideoAspect.ATTR_H264_PROFILE, info.Video.ProfileType.ToString());
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemVideoAspect.ATTR_H264_HEADER_LEVEL, info.Video.HeaderLevel);
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemVideoAspect.ATTR_H264_REF_LEVEL, info.Video.RefLevel);
            MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, TranscodeItemVideoAspect.ATTR_TS_TIMESTAMP, info.Video.TimestampType.ToString());

            foreach (AudioStream audio in info.Audio)
            {
              MultipleMediaItemAspect aspect = new MultipleMediaItemAspect(TranscodeItemVideoAudioAspect.Metadata);
              aspect.SetAttribute(TranscodeItemVideoAudioAspect.ATTR_AUDIOSTREAM, audio.StreamIndex.ToString());
              aspect.SetAttribute(TranscodeItemVideoAudioAspect.ATTR_AUDIOCODEC, audio.Codec.ToString());
              if (audio.Language == null)
              {
                aspect.SetAttribute(TranscodeItemVideoAudioAspect.ATTR_AUDIOLANGUAGE, "");
              }
              else
              {
                aspect.SetAttribute(TranscodeItemVideoAudioAspect.ATTR_AUDIOLANGUAGE, audio.Language);
              }
              aspect.SetAttribute(TranscodeItemVideoAudioAspect.ATTR_AUDIOBITRATE, audio.Bitrate.ToString());
              aspect.SetAttribute(TranscodeItemVideoAudioAspect.ATTR_AUDIOCHANNEL, audio.Channels.ToString());
              aspect.SetAttribute(TranscodeItemVideoAudioAspect.ATTR_AUDIOFREQUENCY, audio.Frequency.ToString());
              aspect.SetAttribute(TranscodeItemVideoAudioAspect.ATTR_AUDIODEFAULT, audio.Default ? "1" : "0");
              MediaItemAspect.AddOrUpdateAspect(ChannelMediaItem.Aspects, aspect);
            }

            foreach (SubtitleStream sub in info.Subtitles)
            {
              MultipleMediaItemAspect aspect = new MultipleMediaItemAspect(TranscodeItemVideoEmbeddedAspect.Metadata);
              if (sub.IsEmbedded)
              {
                aspect.SetAttribute(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBSTREAM, sub.StreamIndex.ToString());
                aspect.SetAttribute(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBCODEC, sub.Codec.ToString());

                if (sub.Language == null)
                {
                  aspect.SetAttribute(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBLANGUAGE, "");
                }
                else
                {
                  aspect.SetAttribute(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBLANGUAGE, sub.Language);
                }

                aspect.SetAttribute(TranscodeItemVideoEmbeddedAspect.ATTR_EMBEDDED_SUBDEFAULT, sub.Default ? "1" : "0");
                MediaItemAspect.AddOrUpdateAspect(ChannelMediaItem.Aspects, aspect);
              }
            }

            if (info.Video.Height > 0) MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, VideoAspect.ATTR_HEIGHT, info.Video.Height);
            if (info.Video.Width > 0) MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, VideoAspect.ATTR_WIDTH, info.Video.Width);
            if (info.Video.AspectRatio > 0) MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, VideoAspect.ATTR_ASPECTRATIO, info.Video.AspectRatio);
            if (info.Video.Framerate > 0) MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, VideoAspect.ATTR_FPS, Convert.ToInt32(info.Video.Framerate));
            if (info.Video.Bitrate > 0) MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, VideoAspect.ATTR_VIDEOBITRATE, info.Video.Bitrate);
            //MediaItemAspect.SetAttribute(ChannelMediaItem.Aspects, VideoAspect.ATTR_DURATION, 0);
          }
        }
        finally
        {
          _slimTvHandler.EndTuning(identifier);
        }
      }
      return info;
    }
  }
}
