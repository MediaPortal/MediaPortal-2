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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.Transcoding.Interfaces.Aspects;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata.Streams;
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Extensions.MetadataExtractors.TranscodingService.MetadataExtractor.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.TranscodingService.MetadataExtractor
{
  public class TranscodeVideoMetadataExtractor : IMetadataExtractor
  {
    /// <summary>
    /// Image metadata extractor GUID.
    /// </summary>
    public static Guid MetadataExtractorId = new Guid("40302A55-BC21-436C-9544-03AF95F4F7A4");

    protected static List<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory> { DefaultMediaCategories.Video };
    protected static ICollection<string> VIDEO_FILE_EXTENSIONS = new HashSet<string>();

    static TranscodeVideoMetadataExtractor()
    {
      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(TranscodeItemVideoAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(TranscodeItemVideoAudioAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectType(TranscodeItemVideoEmbeddedAspect.Metadata);
      TranscodeVideoMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TranscodeVideoMetadataExtractorSettings>();
      InitializeExtensions(settings);
    }

    /// <summary>
    /// (Re)initializes the video extensions for which this <see cref="TranscodeVideoMetadataExtractorSettings"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(TranscodeVideoMetadataExtractorSettings settings)
    {
      VIDEO_FILE_EXTENSIONS = new HashSet<string>(settings.VideoFileExtensions.Select(e => e.ToLowerInvariant()));
    }

    public TranscodeVideoMetadataExtractor()
    {
      Metadata = new MetadataExtractorMetadata(
        MetadataExtractorId,
        "Transcode video metadata extractor",
        MetadataExtractorPriority.Core,
        true,
        MEDIA_CATEGORIES,
        new[]
          {
            MediaAspect.Metadata,
            TranscodeItemVideoAspect.Metadata
          });
    }

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata { get; private set; }

    private bool HasVideoExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return VIDEO_FILE_EXTENSIONS.Contains(ext);
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        MetadataContainer metadata = MediaAnalyzer.ParseMediaStream(mediaItemAccessor);
        if (metadata == null)
        {
          Logger.Info("TranscodeAudioMetadataExtractor: Error analyzing stream '{0}'", mediaItemAccessor.CanonicalLocalResourcePath);
        }
        else if (metadata.IsVideo)
        {
          ConvertMetadataToAspectData(metadata, extractedAspectData);
          return true;
        }
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        Logger.Error("TranscodeAudioMetadataExtractor: Exception reading resource '{0}'", e, mediaItemAccessor.CanonicalLocalResourcePath);
      }
      return false;
    }

    private void ConvertMetadataToAspectData(MetadataContainer info, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_CONTAINER, info.Metadata.VideoContainerType.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_STREAM, info.Video.StreamIndex);
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_CODEC, info.Video.Codec.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_FOURCC, StringUtils.TrimToNull(info.Video.FourCC));
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_BRAND, StringUtils.TrimToNull(info.Metadata.MajorBrand));
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_PIXEL_FORMAT, info.Video.PixelFormatType.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_PIXEL_ASPECTRATIO, info.Video.PixelAspectRatio);
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_H264_PROFILE, info.Video.ProfileType.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_H264_HEADER_LEVEL, info.Video.HeaderLevel);
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_H264_REF_LEVEL, info.Video.RefLevel);
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_TS_TIMESTAMP, info.Video.TimestampType.ToString());

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
        MediaItemAspect.AddOrUpdateAspect(extractedAspectData, aspect);
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
          MediaItemAspect.AddOrUpdateAspect(extractedAspectData, aspect);
        }
      }
    }

    #endregion

    private static IMediaAnalyzer MediaAnalyzer
    {
      get { return ServiceRegistration.Get<IMediaAnalyzer>(); }
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
