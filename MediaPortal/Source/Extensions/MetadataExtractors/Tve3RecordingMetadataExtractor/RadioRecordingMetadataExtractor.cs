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
using System.IO;
using MediaInfoLib;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Extensions.MetadataExtractors
{
  /// <summary>
  /// Metadata extractor for radio recordings done by our TvEngine.
  /// </summary>
  public class RadioRecordingMetadataExtractor : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the audio metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "A35FC618-AE13-4F91-B9AB-FBA4CB2E7AD4";

    /// <summary>
    /// Audio metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected MetadataExtractorMetadata _metadata;

    static RadioRecordingMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Audio);
    }

    public RadioRecordingMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Radio recording metadata extractor", MetadataExtractorPriority.Core, false,
          MEDIA_CATEGORIES, new MediaItemAspectMetadata[]
              {
                MediaAspect.Metadata,
                ProviderResourceAspect.Metadata,
                AudioAspect.Metadata,
              });
    }

    public MetadataExtractorMetadata Metadata { get { return _metadata; } }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly)
    {
      IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
      if (fsra == null || !fsra.IsFile)
        return false;
      if (extractedAspectData.ContainsKey(AudioAspect.ASPECT_ID))
        return false;

      try
      {
        var extension = DosPathHelper.GetExtension(fsra.ResourceName).ToLowerInvariant();
        if (extension != ".ts")
          return false;
        if (extractedAspectData.ContainsKey(AudioAspect.ASPECT_ID))
          return false;

        using (MediaInfoWrapper mediaInfo = ReadMediaInfo(fsra))
        {
          // Before we start evaluating the file, check if it is not a video file (
          if (mediaInfo.IsValid && (mediaInfo.GetVideoCount() != 0 || mediaInfo.GetAudioCount() == 0))
            return false;
          string fileName = ProviderPathHelper.GetFileNameWithoutExtension(fsra.Path) ?? string.Empty;
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, fileName);
          MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, ProviderResourceAspect.Metadata);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, fsra.Size);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "slimtv/radio");

          var audioBitrate = mediaInfo.GetAudioBitrate(0);
          if (audioBitrate.HasValue)
            MediaItemAspect.SetAttribute(extractedAspectData, AudioAspect.ATTR_BITRATE, (int)(audioBitrate.Value / 1000)); // We store kbit/s;
          var audioChannels = mediaInfo.GetAudioChannels(0);
          if (audioChannels.HasValue)
            MediaItemAspect.SetAttribute(extractedAspectData, AudioAspect.ATTR_CHANNELS, audioChannels.Value);
          var audioSampleRate = mediaInfo.GetAudioSampleRate(0);
          if (audioSampleRate.HasValue)
            MediaItemAspect.SetAttribute(extractedAspectData, AudioAspect.ATTR_SAMPLERATE, audioSampleRate.Value);

          MediaItemAspect.SetAttribute(extractedAspectData, AudioAspect.ATTR_ENCODING, mediaInfo.GetAudioCodec(0));
          // MediaInfo returns milliseconds, we need seconds
          long? time = mediaInfo.GetPlaytime(0);
          if (time.HasValue && time > 1000)
            MediaItemAspect.SetAttribute(extractedAspectData, AudioAspect.ATTR_DURATION, time.Value / 1000);
        }
        return true;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here
        ServiceRegistration.Get<ILogger>().Info("RadioRecordingMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", fsra.CanonicalLocalResourcePath, e.Message);
        return false;
      }
    }

    protected MediaInfoWrapper ReadMediaInfo(IFileSystemResourceAccessor mediaItemAccessor)
    {
      MediaInfoWrapper result = new MediaInfoWrapper();

      ILocalFsResourceAccessor localFsResourceAccessor = mediaItemAccessor as ILocalFsResourceAccessor;
      if (ReferenceEquals(localFsResourceAccessor, null))
      {
        Stream stream = null;
        try
        {
          stream = mediaItemAccessor.OpenRead();
          if (stream != null)
            result.Open(stream);
        }
        finally
        {
          if (stream != null)
            stream.Close();
        }
      }
      else
      {
        using (localFsResourceAccessor.EnsureLocalFileSystemAccess())
          result.Open(localFsResourceAccessor.LocalFileSystemPath);
      }
      return result;
    }
  }
}
