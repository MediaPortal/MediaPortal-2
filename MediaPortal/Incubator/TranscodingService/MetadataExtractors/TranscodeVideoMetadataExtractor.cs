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
using MediaPortal.Plugins.Transcoding.Aspects;
using MediaPortal.Plugins.Transcoding.MetadataExtractors.Settings;
using MediaPortal.Plugins.Transcoding.Service.Metadata;
using MediaPortal.Plugins.Transcoding.Service.Metadata.Streams;
using MediaPortal.Plugins.Transcoding.Service.Analyzers;

namespace MediaPortal.Plugins.Transcoding.MetadataExtractors
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

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        if (mediaItemAccessor is IFileSystemResourceAccessor)
        {
          using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          {
            if (!rah.LocalFsResourceAccessor.IsFile)
              return false;
            string filePath = rah.LocalFsResourceAccessor.ResourcePathName;
            if (!HasVideoExtension(filePath))
              return false;
            MetadataContainer metadata = MediaAnalyzer.ParseVideoFile(rah.LocalFsResourceAccessor);
            if (metadata.IsVideo)
            {
              ConvertMetadataToAspectData(metadata, extractedAspectData);
              return true;
            }
          }
        }
        else if (mediaItemAccessor is INetworkResourceAccessor)
        {
          using (var nra = (INetworkResourceAccessor)mediaItemAccessor.Clone())
          {
            MetadataContainer metadata = MediaAnalyzer.ParseVideoStream(nra);
            if (metadata.IsVideo)
            {
              ConvertMetadataToAspectData(metadata, extractedAspectData);
              return true;
            }
          }
        }
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        Logger.Info("TranscodeMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    private void ConvertMetadataToAspectData(MetadataContainer info, IDictionary<Guid, MediaItemAspect> extractedAspectData)
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

      List<string> valuesLangs = new List<string>();
      List<string> valuesCodecs = new List<string>();
      List<string> valuesStreams = new List<string>();
      List<string> valuesBitrates = new List<string>();
      List<string> valuesChannels = new List<string>();
      List<string> valuesFrequencies = new List<string>();
      List<string> valuesDefaults = new List<string>();
      foreach (AudioStream audio in info.Audio)
      {
        valuesStreams.Add(audio.StreamIndex.ToString());
        valuesCodecs.Add(audio.Codec.ToString());
        if (audio.Language == null)
        {
          valuesLangs.Add("");
        }
        else
        {
          valuesLangs.Add(audio.Language);
        }
        valuesBitrates.Add(audio.Bitrate.ToString());
        valuesChannels.Add(audio.Channels.ToString());
        valuesFrequencies.Add(audio.Frequency.ToString());
        valuesDefaults.Add(audio.Default ? "1" : "0");
      }
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_AUDIOLANGUAGES, valuesLangs);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_AUDIOCODECS, valuesCodecs);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_AUDIOSTREAMS, valuesStreams);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_AUDIOBITRATES, valuesBitrates);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_AUDIOCHANNELS, valuesChannels);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_AUDIOFREQUENCIES, valuesFrequencies);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_AUDIODEFAULTS, valuesDefaults);

      List<string> valuesEmSubStreams = new List<string>();
      List<string> valuesEmSubCodecs = new List<string>();
      List<string> valuesEmSubLangs = new List<string>();
      List<string> valuesEmSubDefaults = new List<string>();
      foreach (SubtitleStream sub in info.Subtitles)
      {
        if (sub.IsEmbedded)
        {
          valuesEmSubStreams.Add(sub.StreamIndex.ToString());
          valuesEmSubCodecs.Add(sub.Codec.ToString());
          if (sub.Language == null)
          {
            valuesEmSubLangs.Add("");
          }
          else
          {
            valuesEmSubLangs.Add(sub.Language);
          }
          valuesEmSubDefaults.Add(sub.Default ? "1" : "0");
        }
      }
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_EMBEDDED_SUBSTREAMS, valuesEmSubStreams);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_EMBEDDED_SUBCODECS, valuesEmSubCodecs);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_EMBEDDED_SUBLANGUAGES, valuesEmSubLangs);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, TranscodeItemVideoAspect.ATTR_EMBEDDED_SUBDEFAULTS, valuesEmSubDefaults);
    }

    #endregion

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
