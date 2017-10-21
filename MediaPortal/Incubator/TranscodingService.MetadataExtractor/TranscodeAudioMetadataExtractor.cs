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
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.Transcoding.Interfaces.Aspects;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Extensions.MetadataExtractors.TranscodingService.MetadataExtractor.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.TranscodingService.MetadataExtractor
{
  public class TranscodeAudioMetadataExtractor : IMetadataExtractor
  {
    /// <summary>
    /// Image metadata extractor GUID.
    /// </summary>
    public static Guid MetadataExtractorId = new Guid("D03E1343-A2DD-4C77-83F3-08791E85ABD9");

    protected static List<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory> { DefaultMediaCategories.Audio };

    protected static List<string> AUDIO_EXTENSIONS;

    static TranscodeAudioMetadataExtractor()
    {
      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(TranscodeItemAudioAspect.Metadata);

      TranscodeAudioMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TranscodeAudioMetadataExtractorSettings>();
      InitializeExtensions(settings);
    }

    protected static bool HasAudioExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return AUDIO_EXTENSIONS.Contains(ext);
    }

    /// <summary>
    /// (Re)initializes the audio extensions for which this <see cref="TranscodeAudioMetadataExtractorSettings"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(TranscodeAudioMetadataExtractorSettings settings)
    {
      AUDIO_EXTENSIONS = settings.AudioFileExtensions;
    }

    public TranscodeAudioMetadataExtractor()
    {
      Metadata = new MetadataExtractorMetadata(
        MetadataExtractorId,
        "Transcode audio metadata extractor",
        MetadataExtractorPriority.Core,
        true,
        MEDIA_CATEGORIES,
        new[]
          {
            MediaAspect.Metadata,
            TranscodeItemAudioAspect.Metadata
          });
    }

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata { get; private set; }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly, bool forceQuickMode)
    {
      //Logger.Debug("Extracing {0}", mediaItemAccessor.CanonicalLocalResourcePath);
      IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
      if (fsra == null)
        return false;
      if (!fsra.IsFile)
        return false;
      string fileName = fsra.ResourceName;
      if (!HasAudioExtension(fileName))
        return false;

      try
      {
        MetadataContainer metadata = MediaAnalyzer.ParseMediaStream(mediaItemAccessor);
        //Logger.Debug("Metadata for {0} -> {1} {2}", mediaItemAccessor.CanonicalLocalResourcePath, metadata, metadata?.IsAudio);
        if (metadata == null)
        {
          Logger.Info("TranscodeAudioMetadataExtractor: Error analyzing stream '{0}'", mediaItemAccessor.CanonicalLocalResourcePath);
        }
        else if (metadata.IsAudio)
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
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemAudioAspect.ATTR_CONTAINER, info.Metadata.AudioContainerType.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemAudioAspect.ATTR_STREAM, info.Audio[0].StreamIndex);
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemAudioAspect.ATTR_CODEC, info.Audio[0].Codec.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemAudioAspect.ATTR_CHANNELS, info.Audio[0].Channels);
      MediaItemAspect.SetAttribute(extractedAspectData, TranscodeItemAudioAspect.ATTR_FREQUENCY, info.Audio[0].Frequency);
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
