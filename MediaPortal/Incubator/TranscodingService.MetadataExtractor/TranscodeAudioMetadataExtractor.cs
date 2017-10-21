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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata;
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Plugins.Transcoding.Service.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.TranscodingService.MetadataExtractor
{
  public class TranscodeAudioMetadataExtractor : IMetadataExtractor
  {
    /// <summary>
    /// Image metadata extractor GUID.
    /// </summary>
    public static Guid MetadataExtractorId = new Guid("D03E1343-A2DD-4C77-83F3-08791E85ABD9");

    protected static List<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory> { DefaultMediaCategories.Audio };
    protected static ICollection<string> AUDIO_EXTENSIONS = new List<string>();

    static TranscodeAudioMetadataExtractor()
    {
      TranscodingServiceSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TranscodingServiceSettings>();
      InitializeExtensions(settings);
    }

    /// <summary>
    /// (Re)initializes the audio extensions for which this <see cref="TranscodeAudioMetadataExtractorSettings"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(TranscodingServiceSettings settings)
    {
      AUDIO_EXTENSIONS = new List<string>(settings.AudioFileExtensions.Select(e => e.ToLowerInvariant()));
    }

    public TranscodeAudioMetadataExtractor()
    {
      Metadata = new MetadataExtractorMetadata(
        MetadataExtractorId,
        "Transcode audio metadata extractor",
        MetadataExtractorPriority.Extended,
        true,
        MEDIA_CATEGORIES,
        new[]
          {
            MediaAspect.Metadata
          });
    }

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata { get; private set; }

    private bool HasAudioExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return AUDIO_EXTENSIONS.Contains(ext);
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly, bool forceQuickMode)
    {
      try
      {
        if (forceQuickMode)
          return false;

        if (!importOnly)
          return false;

        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
          return false;

        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
        {
          if (!HasAudioExtension(rah.LocalFsResourceAccessor.LocalFileSystemPath))
            return false;
        }
        
        MetadataContainer metadata = MediaAnalyzer.ParseMediaStream(mediaItemAccessor);
        if (metadata == null)
        {
          Logger.Info("TranscodeAudioMetadataExtractor: Error analyzing stream '{0}'", mediaItemAccessor.CanonicalLocalResourcePath);
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

    public bool IsSingleResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      return false;
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
