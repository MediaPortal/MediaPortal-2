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
using System.IO;
using System.Linq;
using MediaInfoLib;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using MediaPortal.Extensions.MetadataExtractors.VideoMetadataExtractor.Settings;
using MediaPortal.Utilities;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Extensions.MetadataExtractors.VideoMetadataExtractor
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for video files. Supports several formats.
  /// </summary>
  public class VideoMetadataExtractor : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the video metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "F2D86BE4-07E6-40F2-9D12-C0076861CAB8";

    /// <summary>
    /// Video metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected static ICollection<string> VIDEO_FILE_EXTENSIONS = new List<string>();

    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static VideoMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);
      VideoMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<VideoMetadataExtractorSettings>();
      InitializeExtensions(settings);
    }

    /// <summary>
    /// (Re)initializes the video extensions for which this <see cref="VideoMetadataExtractorSettings"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(VideoMetadataExtractorSettings settings)
    {
      VIDEO_FILE_EXTENSIONS = new List<string>(settings.VideoFileExtensions.Select(e => e.ToLowerInvariant()));
    }

    public VideoMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Video metadata extractor", MetadataExtractorPriority.Core, true,
          MEDIA_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                VideoAspect.Metadata,
                SeriesAspect.Metadata
              });
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Returns the information if the specified file name (or path) has a file extension which is
    /// supposed to be supported by this metadata extractor.
    /// </summary>
    /// <param name="fileName">Relative or absolute file path to check.</param>
    /// <returns><c>true</c>, if the file's extension is supposed to be supported, else <c>false</c>.</returns>
    protected static bool HasVideoExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return VIDEO_FILE_EXTENSIONS.Contains(ext);
    }

    protected MediaInfoWrapper ReadMediaInfo(IFileSystemResourceAccessor mediaItemAccessor)
    {
      MediaInfoWrapper result = new MediaInfoWrapper();
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
      return result;
    }

    public class VideoResult
    {
      protected bool _isDVD;
      protected string _title;
      protected string _mimeType;
      protected DateTime? _mediaDate;

      protected float? _ar;
      protected int? _frameRate;
      protected int? _width;
      protected int? _height;
      protected long? _playTime;
      protected long? _vidBitRate;
      protected long? _audBitRate;
      protected int _audioStreamCount;
      protected ICollection<string> _vidCodecs = new List<string>();
      protected ICollection<string> _audCodecs = new List<string>();
      protected ICollection<string> _audioLanguages = new List<string>();

      public VideoResult(string videoTitle, MediaInfoWrapper mainInfo)
      {
        _title = videoTitle;
        AddMediaInfo(mainInfo);
      }

      public static VideoResult CreateDVDInfo(string dvdTitle, MediaInfoWrapper videoTsInfo)
      {
        VideoResult result = new VideoResult(dvdTitle, videoTsInfo) { IsDVD = true, MimeType = "video/dvd" };
        return result;
      }

      public static VideoResult CreateFileInfo(string fileName, MediaInfoWrapper fileInfo)
      {
        return new VideoResult(fileName, fileInfo);
      }

      public void AddMediaInfo(MediaInfoWrapper mediaInfo)
      {
        // This method will be called at least one time, for video DVDs it will be called multiple times for the different
        // .ifo files. The first time this method is called, the given media info instance is the "major" instance, i.e.
        // in case of a video DVD, it is the video_ts.ifo file.
        // We will collect most of our interesting attributes by taking the first one which is available. All others will then be
        // ignored. Only for some attributes, all values will be collected.
        for (int i = 0; i < mediaInfo.GetVideoCount(); i++)
        {
          if (!_ar.HasValue)
            _ar = mediaInfo.GetAR(i);
          if (!_frameRate.HasValue)
            _frameRate = mediaInfo.GetFramerate(i);
          if (!_width.HasValue)
            _width = mediaInfo.GetWidth(i);
          if (!_height.HasValue)
            _height = mediaInfo.GetHeight(i);
          if (!_playTime.HasValue)
          {
            long? time = mediaInfo.GetPlaytime(i);
            if (time.HasValue && time > 1000)
              _playTime = time.Value;
          }
          if (!_vidBitRate.HasValue)
            _vidBitRate = mediaInfo.GetVidBitrate(i);
          string vidCodec = mediaInfo.GetVidCodec(i);
          if (!string.IsNullOrEmpty(vidCodec) && !_vidCodecs.Contains(vidCodec))
            _vidCodecs.Add(vidCodec);
        }
        _audioStreamCount = mediaInfo.GetAudioCount();
        for (int i = 0; i < _audioStreamCount; i++)
        {
          if (!_audBitRate.HasValue)
            _audBitRate = mediaInfo.GetAudioBitrate(i);
          string audCodec = mediaInfo.GetAudioCodec(i);
          if (!string.IsNullOrEmpty(audCodec) && !_audCodecs.Contains(audCodec))
            _audCodecs.Add(audCodec);

          string audLang = mediaInfo.GetAudioLanguage(i);
          if (!string.IsNullOrEmpty(audLang) && !_audioLanguages.Contains(audLang))
            _audioLanguages.Add(audLang);
        }
      }

      public void UpdateMetadata(IDictionary<Guid, MediaItemAspect> extractedAspectData)
      {
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, _title);
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_MIME_TYPE, _mimeType);
        if (_ar.HasValue)
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_ASPECTRATIO, _ar.Value);
        if (_frameRate.HasValue)
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_FPS, _frameRate.Value);
        if (_width.HasValue)
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_WIDTH, _width.Value);
        if (_height.HasValue)
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_HEIGHT, _height.Value);
        // MediaInfo returns milliseconds, we need seconds
        if (_playTime.HasValue)
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_DURATION, _playTime.Value / 1000);
        if (_vidBitRate.HasValue)
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_VIDEOBITRATE, _vidBitRate.Value);

        MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_VIDEOENCODING, StringUtils.Join(", ", _vidCodecs));

        MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_AUDIOSTREAMCOUNT, _audioStreamCount);
        if (_audBitRate.HasValue)
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_AUDIOBITRATE, _audBitRate.Value);

        MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_AUDIOENCODING, StringUtils.Join(", ", _audCodecs));

        MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_AUDIOLANGUAGES, _audioLanguages);
        // TODO: extract cover art (see Mantis #1977)
      }

      public bool IsDVD
      {
        get { return _isDVD; }
        set { _isDVD = value; }
      }

      public string MimeType
      {
        get { return _mimeType; }
        set { _mimeType = value; }
      }
    }

    protected void ExtractMatroskaTags(string localFsResourcePath, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      string extensionUpper = StringUtils.TrimToEmpty(Path.GetExtension(localFsResourcePath)).ToUpper();

      // Try to get extended information out of matroska files)
      if (extensionUpper == ".MKV" || extensionUpper == ".MK3D")
      {
        MatroskaInfoReader mkvReader = new MatroskaInfoReader(localFsResourcePath);
        // Add keys to be extracted to tags dictionary, matching results will returned as value
        Dictionary<string, IList<string>> tagsToExtract = MatroskaConsts.DefaultTags;
        mkvReader.ReadTags(tagsToExtract);

        string title = string.Empty;
        IList<string> tags = tagsToExtract[MatroskaConsts.TAG_SIMPLE_TITLE];
        if (tags != null)
          title = tags.FirstOrDefault();

        int year;
        if (!string.IsNullOrEmpty(title))
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, title);

        string yearCandidate = null;
        tags = tagsToExtract[MatroskaConsts.TAG_EPISODE_YEAR] ?? tagsToExtract[MatroskaConsts.TAG_SEASON_YEAR];
        if (tags != null)
          yearCandidate = (tags.FirstOrDefault() ?? string.Empty).Substring(0, 4);

        if (int.TryParse(yearCandidate, out year))
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, new DateTime(year, 1, 1));

        tags = tagsToExtract[MatroskaConsts.TAG_SERIES_GENRE];
        if (tags != null)
          MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_GENRES, tags);

        IEnumerable<string> actors;
        // Combine series actors and episode actors if both are available
        var tagSeriesActors = tagsToExtract[MatroskaConsts.TAG_SERIES_ACTORS];
        var tagActors = tagsToExtract[MatroskaConsts.TAG_ACTORS];
        if (tagSeriesActors != null && tagActors != null)
          actors = tagSeriesActors.Union(tagActors);
        else
          actors = tagSeriesActors ?? tagActors;

        if (actors != null)
          MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_ACTORS, actors);
      }

      // Movie handling
      //if (!string.IsNullOrEmpty(title) && !forceQuickMode)
      //{
      //  // TODO: online information can overwrite information read out of mkv tags, how to handle this?
      //  MovieInfo movieInfo = new MovieInfo
      //  {
      //    MovieName = title,
      //    Year = year
      //  };
      //  if (MovieTheMovieDbMatcher.Instance.FindAndUpdateMovie(movieInfo))
      //    movieInfo.SetMetadata(extractedAspectData);
      //}
    }

    protected void ExtractThumbnailData(string localFsResourcePath, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      // In quick mode only allow thumbs taken from cache.
      bool cachedOnly = forceQuickMode;

      // Thumbnail extraction
      IThumbnailGenerator generator = ServiceRegistration.Get<IThumbnailGenerator>();
      byte[] thumbData;
      ImageType imageType;
      if (generator.GetThumbnail(localFsResourcePath, 96, 96, cachedOnly, out thumbData, out imageType))
        MediaItemAspect.SetAttribute(extractedAspectData, ThumbnailSmallAspect.ATTR_THUMBNAIL, thumbData);
      if (generator.GetThumbnail(localFsResourcePath, 256, 256, cachedOnly, out thumbData, out imageType))
        MediaItemAspect.SetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, thumbData);
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        VideoResult result = null;
        IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        if (fsra == null)
          return false;
        if (!fsra.IsFile && fsra.ResourceExists("VIDEO_TS"))
        {
          IFileSystemResourceAccessor fsraVideoTs = fsra.GetResource("VIDEO_TS");
          if (fsraVideoTs != null && fsraVideoTs.ResourceExists("VIDEO_TS.IFO"))
          {
            // Video DVD
            using (MediaInfoWrapper videoTsInfo = ReadMediaInfo(fsraVideoTs.GetResource("VIDEO_TS.IFO")))
            {
              if (!videoTsInfo.IsValid || videoTsInfo.GetVideoCount() == 0)
                return false; // Invalid video_ts.ifo file
              result = VideoResult.CreateDVDInfo(fsra.ResourceName, videoTsInfo);
            }
            // Iterate over all video files; MediaInfo finds different audio/video metadata for each .ifo file
            ICollection<IFileSystemResourceAccessor> files = fsraVideoTs.GetFiles();
            if (files != null)
              foreach (IFileSystemResourceAccessor file in files)
              {
                string lowerPath = (file.ResourcePathName ?? string.Empty).ToLowerInvariant();
                if (!lowerPath.EndsWith(".ifo") || lowerPath.EndsWith("video_ts.ifo"))
                  continue;
                using (MediaInfoWrapper mediaInfo = ReadMediaInfo(file))
                {
                  // Before we start evaluating the file, check if it is a video at all
                  if (mediaInfo.IsValid && mediaInfo.GetVideoCount() == 0)
                    continue;
                  result.AddMediaInfo(mediaInfo);
                }
              }
          }
        }
        else if (fsra.IsFile)
        {
          string filePath = fsra.ResourcePathName;
          if (!HasVideoExtension(filePath))
            return false;
          using (MediaInfoWrapper fileInfo = ReadMediaInfo(fsra))
          {
            // Before we start evaluating the file, check if it is a video at all
            if (!fileInfo.IsValid || (fileInfo.GetVideoCount() == 0 && !IsWorkaroundRequired(filePath)))
              return false;

            string mediaTitle = DosPathHelper.GetFileNameWithoutExtension(fsra.ResourceName);
            result = VideoResult.CreateFileInfo(mediaTitle, fileInfo);
          }
          using (Stream stream = fsra.OpenRead())
            result.MimeType = MimeTypeDetector.GetMimeType(stream);
        }
        if (result != null)
        {
          result.UpdateMetadata(extractedAspectData);

          ILocalFsResourceAccessor disposeLfsra = null;
          try
          {
            ILocalFsResourceAccessor lfsra = fsra as ILocalFsResourceAccessor;
            if (lfsra == null && !forceQuickMode)
            { // In case forceQuickMode, we only want local browsing
              IFileSystemResourceAccessor localFsra = (IFileSystemResourceAccessor) fsra.Clone();
              try
              {
                lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(localFsra);
                disposeLfsra = lfsra; // Remember to dispose the extra resource accessor instance
              }
              catch (Exception)
              {
                localFsra.Dispose();
              }
            }
            if (lfsra != null)
            {
              string localFsPath = lfsra.LocalFileSystemPath;
              ExtractMatroskaTags(localFsPath, extractedAspectData, forceQuickMode);
              ExtractThumbnailData(localFsPath, extractedAspectData, forceQuickMode);
            }
          }
          finally
          {
            if (disposeLfsra != null)
              disposeLfsra.Dispose();
          }
          return true;
        }
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("VideoMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    /// <summary>
    /// Helper method that contains overrides for video detection for certain formats, because video count information is not always correct.
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <returns><c>true</c> if this file should be treated as video.</returns>
    protected bool IsWorkaroundRequired(string filePath)
    {
      return filePath.ToLowerInvariant().EndsWith(".wtv");
    }

    // The following code should be used in the slow batch mode (see Mantis #1977)
    //#region code testing the xbmc scraper
    //if (scraper.IsLoaded)
    //{
    //  scraper.CreateSearchUrl((string)movie["title"]);
    //  ServiceRegistration.Get<ILogger>().Info("MovieImporter: Getting online info from: {0} ", scraper.SearchUrl);
    //  scraper.GetSearchResults();
    //  ServiceRegistration.Get<ILogger>().Info("MovieImporter: Result found {0} ", scraper.SearchResults.Count);
    //  if (scraper.SearchResults.Count > 0)
    //  {

    //    SystemMessage msgc = new SystemMessage();
    //    msgc.MessageData["action"] = "imdbchoiceneeded";
    //    msgc.MessageData["file"] = filePath;
    //    msgc.MessageData["title"] = (string)movie["title"];
    //    List<string> urlList = new List<string>();
    //    List<string> idList = new List<string>();
    //    List<string> titleList = new List<string>();
    //    foreach (ScraperSearchResult res in scraper.SearchResults)
    //    {
    //      urlList.Add(res.Url);
    //      idList.Add(res.Id);
    //      titleList.Add(res.Title);
    //    }
    //    msgc.MessageData["urls"] = urlList;
    //    msgc.MessageData["ids"] = idList;
    //    msgc.MessageData["titles"] = titleList;
    //    SendMessage(msgc);

    //    ServiceRegistration.Get<ILogger>().Info("MovieImporter: Getting online info for: {0}", scraper.SearchResults[0].Title);
    //    scraper.GetDetails(scraper.SearchResults[0].Url, scraper.SearchResults[0].Id);
    //    if (scraper.Metadata.ContainsKey("genre"))
    //    {
    //      movie["title"] = scraper.Metadata["title"];
    //      movie["genre"] = scraper.Metadata["genre"];
    //      if (scraper.Metadata.ContainsKey("thumb"))
    //        movie["CoverArt"] = scraper.Metadata["thumb"];
    //      if (scraper.Metadata.ContainsKey("actors"))
    //        movie["actors"] = scraper.Metadata["actors"];
    //      if (scraper.Metadata.ContainsKey("year"))
    //        movie["year"] = scraper.Metadata["year"];

    //    }
    //  }
    //}
    //else
    //{
    //  ServiceRegistration.Get<ILogger>().Info("MovieImporter: No online scrapers are loaded ");
    //}

    //#endregion

    #endregion
  }
}