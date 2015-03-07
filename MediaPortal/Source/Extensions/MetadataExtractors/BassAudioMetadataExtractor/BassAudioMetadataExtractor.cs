#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Graphics;
using Un4seen.Bass.AddOn.Tags;

namespace MediaPortal.Extensions.MetadataExtractors.BassAudioMetadataExtractor
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for audio files. Supports several formats.
  /// </summary>
  public class BassAudioMetadataExtractor : AudioMetadataExtractor.AudioMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the audio metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "EBE04C71-AB3A-4418-B544-FCE3553A7687";

    /// <summary>
    /// Audio metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    #region Fields

    protected BassLibraryManager _lib;

    #endregion

    #region Ctor

    static BassAudioMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Audio);
      AUDIO_EXTENSIONS.Add(".dff");
      AUDIO_EXTENSIONS.Add(".dsf");
    }

    public BassAudioMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Bass Audio metadata extractor", MetadataExtractorPriority.Core, false,
          MEDIA_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                AudioAspect.Metadata,
                ThumbnailLargeAspect.Metadata
              });
      _lib = BassLibraryManager.Get();
    }

    #endregion

    #region IMetadataExtractor implementation

    protected IEnumerable<string> SplitTagEnum(string tags)
    {
      if (string.IsNullOrWhiteSpace(tags))
        return new List<string>();
      return tags.Split(';', '/');
    }

    public override bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      // If the base AudioMDE already extracted metadata, don't try here again to avoid conflicts.
      if (extractedAspectData.ContainsKey(AudioAspect.ASPECT_ID))
        return false;

      ILocalFsResourceAccessor fsra = mediaItemAccessor as ILocalFsResourceAccessor;
      if (fsra == null)
        return false;
      if (!fsra.IsFile)
        return false;
      string filePath = fsra.LocalFileSystemPath;
      string fileName = fsra.ResourceName;
      if (!HasAudioExtension(fileName))
        return false;

      try
      {
        var tags = BassTags.BASS_TAG_GetFromFile(filePath);
        if (tags == null)
          return false;

        fileName = ProviderPathHelper.GetFileNameWithoutExtension(fileName) ?? string.Empty;
        string title;
        string artist;
        uint? trackNo;
        GuessMetadataFromFileName(fileName, out title, out artist, out trackNo);
        if (!string.IsNullOrWhiteSpace(tags.title))
          title = tags.title;
        IEnumerable<string> artists;
        if (!string.IsNullOrWhiteSpace(tags.artist))
        {
          artists = SplitTagEnum(tags.artist);
          artists = PatchID3v23Enumeration(artists);
        }
        else
          artists = artist == null ? null : new string[] { artist };
        if (!string.IsNullOrWhiteSpace(tags.track) && tags.track != "0")
        {
          int iTrackNo;
          if (int.TryParse(tags.track, out iTrackNo))
            trackNo = (uint?)iTrackNo;
          else
            trackNo = null;
        }

        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, title);
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SIZE, fsra.Size);
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_MIME_TYPE, "audio/" + Path.GetExtension(filePath).Substring(1));
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, AudioAspect.ATTR_ARTISTS, ApplyAdditionalSeparator(artists));
        MediaItemAspect.SetAttribute(extractedAspectData, AudioAspect.ATTR_ALBUM, StringUtils.TrimToNull(tags.album));
        IEnumerable<string> albumArtists = SplitTagEnum(tags.albumartist);
        albumArtists = PatchID3v23Enumeration(albumArtists);

        MediaItemAspect.SetCollectionAttribute(extractedAspectData, AudioAspect.ATTR_ALBUMARTISTS, ApplyAdditionalSeparator(albumArtists));
        MediaItemAspect.SetAttribute(extractedAspectData, AudioAspect.ATTR_BITRATE, tags.bitrate);
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_COMMENT, StringUtils.TrimToNull(tags.comment));
        IEnumerable<string> composers = SplitTagEnum(tags.composer);
        composers = PatchID3v23Enumeration(composers);
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, AudioAspect.ATTR_COMPOSERS, ApplyAdditionalSeparator(composers));

        MediaItemAspect.SetAttribute(extractedAspectData, AudioAspect.ATTR_DURATION, (long)tags.duration);

        IEnumerable<string> genres = SplitTagEnum(tags.genre);
        genres = PatchID3v23Enumeration(genres);
        MediaItemAspect.SetCollectionAttribute(extractedAspectData, AudioAspect.ATTR_GENRES, ApplyAdditionalSeparator(genres));

        if (trackNo.HasValue)
          MediaItemAspect.SetAttribute(extractedAspectData, AudioAspect.ATTR_TRACK, (int)trackNo.Value);

        int year;
        if (int.TryParse(tags.year, out year))
        {
          if (year >= 30 && year <= 99)
            year += 1900;
          if (year >= 1930 && year <= 2030)
            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, new DateTime(year, 1, 1));
        }

        // The following code gets cover art images from file (embedded) or from windows explorer cache (supports folder.jpg).
        if (tags.PictureCount > 0)
        {
          try
          {
            using (Image cover = tags.PictureGetImage(0))
            using (Image resized = ImageUtilities.ResizeImage(cover, MAX_COVER_WIDTH, MAX_COVER_HEIGHT))
            using (MemoryStream result = new MemoryStream())
            {
              resized.Save(result, ImageFormat.Jpeg);
              MediaItemAspect.SetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, result.ToArray());
            }
          }
          // Decoding of invalid image data can fail, but main MediaItem is correct.
          catch { }
        }
        else
        {
          // In quick mode only allow thumbs taken from cache.
          bool cachedOnly = forceQuickMode;

          // Thumbnail extraction
          fileName = mediaItemAccessor.ResourcePathName;
          IThumbnailGenerator generator = ServiceRegistration.Get<IThumbnailGenerator>();
          byte[] thumbData;
          ImageType imageType;
          if (generator.GetThumbnail(fileName, 256, 256, cachedOnly, out thumbData, out imageType))
            MediaItemAspect.SetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, thumbData);
        }
        return true;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here
        ServiceRegistration.Get<ILogger>().Info("BassAudioMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", fsra.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    #endregion
  }
}
