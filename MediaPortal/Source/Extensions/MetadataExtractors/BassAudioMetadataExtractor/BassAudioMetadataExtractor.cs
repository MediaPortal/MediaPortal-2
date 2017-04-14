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
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Matchers;
using MediaPortal.Extensions.OnlineLibraries;
using System.Linq;

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
    public new const string METADATAEXTRACTOR_ID_STR = "EBE04C71-AB3A-4418-B544-FCE3553A7687";

    /// <summary>
    /// Audio metadata extractor GUID.
    /// </summary>
    public new static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

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

    public new bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly)
    {
      // If the base AudioMDE already extracted metadata, don't try here again to avoid conflicts.
      if (extractedAspectData.ContainsKey(AudioAspect.ASPECT_ID))
        return false;

      ILocalFsResourceAccessor fsra = mediaItemAccessor as ILocalFsResourceAccessor;
      if (fsra == null)
        return false;
      if (!fsra.IsFile)
        return false;
      string fileName = fsra.ResourceName;
      if (!HasAudioExtension(fileName))
        return false;

      try
      {
        TAG_INFO tags;
        using (fsra.EnsureLocalFileSystemAccess())
          tags = BassTags.BASS_TAG_GetFromFile(fsra.LocalFileSystemPath);
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

        TrackInfo trackInfo = new TrackInfo();
        if (extractedAspectData.ContainsKey(AudioAspect.ASPECT_ID))
        {
          trackInfo.FromMetadata(extractedAspectData);
        }
        else
        {
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, title);
          IList<MultipleMediaItemAspect> providerResourceAspect;
          if (MediaItemAspect.TryGetAspects(extractedAspectData, ProviderResourceAspect.Metadata, out providerResourceAspect))
          {
            providerResourceAspect[0].SetAttribute(ProviderResourceAspect.ATTR_SIZE, fsra.Size);
            // Calling EnsureLocalFileSystemAccess not necessary; only string operation
            providerResourceAspect[0].SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "audio/" + Path.GetExtension(fsra.LocalFileSystemPath).Substring(1));
          }
          MediaItemAspect.SetAttribute(extractedAspectData, AudioAspect.ATTR_BITRATE, tags.bitrate);
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_COMMENT, StringUtils.TrimToNull(tags.comment));
          MediaItemAspect.SetAttribute(extractedAspectData, AudioAspect.ATTR_DURATION, (long)tags.duration);
        }

        if (!trackInfo.IsBaseInfoPresent)
        {
          trackInfo.TrackName = title;
          trackInfo.Album = StringUtils.TrimToNull(tags.album);
          if (trackNo.HasValue)
            trackInfo.TrackNum = (int)trackNo.Value;

          trackInfo.Artists = new List<PersonInfo>();
          foreach (string artistName in ApplyAdditionalSeparator(artists))
          {
            trackInfo.Artists.Add(new PersonInfo()
            {
              Name = artistName,
              Occupation = PersonAspect.OCCUPATION_ARTIST,
              ParentMediaName = trackInfo.Album,
              MediaName = trackInfo.TrackName
            });
          }

          IEnumerable<string> albumArtists = SplitTagEnum(tags.albumartist);
          albumArtists = PatchID3v23Enumeration(albumArtists);
          trackInfo.AlbumArtists = new List<PersonInfo>();
          foreach (string artistName in ApplyAdditionalSeparator(albumArtists))
          {
            trackInfo.AlbumArtists.Add(new PersonInfo()
            {
              Name = artistName,
              Occupation = PersonAspect.OCCUPATION_ARTIST,
              ParentMediaName = trackInfo.Album,
              MediaName = trackInfo.TrackName
            });
          }

          IEnumerable<string> composers = SplitTagEnum(tags.composer);
          composers = PatchID3v23Enumeration(composers);
          trackInfo.Composers = new List<PersonInfo>();
          foreach (string composerName in ApplyAdditionalSeparator(composers))
          {
            trackInfo.Composers.Add(new PersonInfo()
            {
              Name = composerName,
              Occupation = PersonAspect.OCCUPATION_COMPOSER,
              ParentMediaName = trackInfo.Album,
              MediaName = trackInfo.TrackName
            });
          }

          IEnumerable<string> genres = SplitTagEnum(tags.genre);
          genres = PatchID3v23Enumeration(genres);
          trackInfo.Genres = ApplyAdditionalSeparator(genres).Select(s => new GenreInfo { Name = s }).ToList();
          OnlineMatcherService.Instance.AssignMissingMusicGenreIds(trackInfo.Genres);

          int year;
          if (int.TryParse(tags.year, out year))
          {
            if (year >= 30 && year <= 99)
              year += 1900;
            if (year >= 1930 && year <= 2030)
              trackInfo.ReleaseDate = new DateTime(year, 1, 1);
          }

          if (!trackInfo.HasThumbnail)
          {
            // The following code gets cover art images from file (embedded) or from windows explorer cache (supports folder.jpg).
            if (tags.PictureCount > 0)
            {
              try
              {
                using (Image cover = tags.PictureGetImage(0))
                using (MemoryStream result = new MemoryStream())
                {
                  cover.Save(result, ImageFormat.Jpeg);
                  trackInfo.Thumbnail = result.ToArray();
                  trackInfo.HasChanged = true;
                }
              }
              // Decoding of invalid image data can fail, but main MediaItem is correct.
              catch { }
            }
            else
            {
              // In quick mode only allow thumbs taken from cache.
              bool cachedOnly = importOnly;

              // Thumbnail extraction
              fileName = mediaItemAccessor.ResourcePathName;
              IThumbnailGenerator generator = ServiceRegistration.Get<IThumbnailGenerator>();
              byte[] thumbData;
              ImageType imageType;
              if (generator.GetThumbnail(fileName, cachedOnly, out thumbData, out imageType))
              {
                trackInfo.Thumbnail = thumbData;
                trackInfo.HasChanged = true;
              }
            }
          }
        }

        if(!SkipOnlineSearches)
          OnlineMatcherService.Instance.FindAndUpdateTrack(trackInfo, importOnly);

        if (!trackInfo.HasChanged && !importOnly)
          return false;

        trackInfo.SetMetadata(extractedAspectData);

        return trackInfo.IsBaseInfoPresent;
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
