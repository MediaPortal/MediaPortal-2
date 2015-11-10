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
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Utilities.Process;
using System.Globalization;
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Plugins.Transcoding.Service.Interfaces;
using MediaPortal.Utilities;
using MediaPortal.Common.Settings;
using System.Linq;
using MediaPortal.Plugins.Transcoding.Aspects;
using MediaPortal.Plugins.Transcoding.MetadataExtractors.Settings;

namespace MediaPortal.Plugins.Transcoding.MetadataExtractors
{
  public class TranscodeAudioMetadataExtractor : IMetadataExtractor
  {
    /// <summary>
    /// Image metadata extractor GUID.
    /// </summary>
    public static Guid MetadataExtractorId = new Guid("D03E1343-A2DD-4C77-83F3-08791E85ABD9");

    protected static List<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory> { DefaultMediaCategories.Audio };
    protected static ICollection<string> AUDIO_EXTENSIONS = new List<string>();

    private static readonly IMediaAnalyzer _analyzer = new MediaAnalyzer();

    static TranscodeAudioMetadataExtractor()
    {
      // Initialize analyzer
      _analyzer.Logger = Logger;
      _analyzer.AnalyzerMaximumThreads = TranscodingServicePlugin.Settings.TranscoderMaximumThreads;

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(TranscodeItemAudioAspect.Metadata);

      TranscodeAudioMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TranscodeAudioMetadataExtractorSettings>();
      InitializeExtensions(settings);
    }

    /// <summary>
    /// (Re)initializes the audio extensions for which this <see cref="TranscodeAudioMetadataExtractorSettings"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(TranscodeAudioMetadataExtractorSettings settings)
    {
      AUDIO_EXTENSIONS = new List<string>(settings.AudioExtensions.Select(e => e.ToLowerInvariant()));
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

    private bool HasAudioExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return AUDIO_EXTENSIONS.Contains(ext);
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        if (mediaItemAccessor is IFileSystemResourceAccessor)
        {
          using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          {
            if (!rah.LocalFsResourceAccessor.IsFile)
              return false;
            string fileName = rah.LocalFsResourceAccessor.ResourceName;
            if (!HasAudioExtension(fileName))
              return false;
            MetadataContainer metadata = _analyzer.ParseAudioFile(rah.LocalFsResourceAccessor);
            if (metadata.IsAudio)
            {
              ConvertMetadataToAspectData(metadata, extractedAspectData);
              return true;
            }
          }
        }
        //else if (mediaItemAccessor is INetworkResourceAccessor)
        //{
        //  using (var nra = (INetworkResourceAccessor)mediaItemAccessor.Clone())
        //  {
        //    MetadataContainer metadata = _analyzer.ParseStream(nra);
        //    if (metadata.IsAudio)
        //    {
        //      ConvertMetadataToAspectData(metadata, extractedAspectData);
        //      return true;
        //    }
        //  }
        //}
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        Logger.Info("TranscodeMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
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

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
