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
using MediaPortal.Extensions.MetadataExtractors.VideoMetadataExtractor.Matroska;
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
    #region Public constants

    /// <summary>
    /// GUID string for the video metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "F2D86BE4-07E6-40F2-9D12-C0076861CAB8";

    #region Matroska reader tags

    // Tags are constructed by using TargetTypeValue (i.e. 70) and the name of the <Simple> tag (i.e. TITLE).
    private const string TAG_SERIES_TITLE = "70.TITLE";
    private const string TAG_SERIES_GENRE = "70.GENRE";
    private const string TAG_SEASON_YEAR = "60.DATE_RELEASE";
    private const string TAG_SEASON_TITLE = "60.TITLE";
    private const string TAG_EPISODE_TITLE = "50.TITLE";
    private const string TAG_EPISODE_SUMMARY = "50.SUMMARY";
    private const string TAG_ACTORS = "50.ACTOR";
    private const string TAG_SEASON_NUMBER = "60.PART_NUMBER";
    private const string TAG_EPISODE_YEAR = "50.DATE_RELEASED";
    private const string TAG_EPISODE_NUMBER = "50.PART_NUMBER";
    private const string TAG_SIMPLE_TITLE = "TITLE";

    #endregion

    /// <summary>
    /// Video metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    #region Protected fields and classes

    protected static IList<string> SHARE_CATEGORIES = new List<string>();
    protected static IList<string> VIDEO_FILE_EXTENSIONS = new List<string>();

    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static VideoMetadataExtractor()
    {
      SHARE_CATEGORIES.Add(DefaultMediaCategory.Video.ToString());
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
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Video metadata extractor", true,
          SHARE_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                VideoAspect.Metadata
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

    protected MediaInfoWrapper ReadMediaInfo(IResourceAccessor mediaItemAccessor)
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

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
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
        }
      }

      public void UpdateMetadata(MediaItemAspect mediaAspect, MediaItemAspect videoAspect, MediaItemAspect thumbnailSmallAspect, MediaItemAspect thumbnailLargeAspect, string localFsResourcePath, bool forceQuickMode)
      {
        mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, _title);
        mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, _mimeType);
        if (_ar.HasValue)
          videoAspect.SetAttribute(VideoAspect.ATTR_ASPECTRATIO, _ar.Value);
        if (_frameRate.HasValue)
          videoAspect.SetAttribute(VideoAspect.ATTR_FPS, _frameRate.Value);
        if (_width.HasValue)
          videoAspect.SetAttribute(VideoAspect.ATTR_WIDTH, _width.Value);
        if (_height.HasValue)
          videoAspect.SetAttribute(VideoAspect.ATTR_HEIGHT, _height.Value);
        // MediaInfo returns milliseconds, we need seconds
        if (_playTime.HasValue)
          videoAspect.SetAttribute(VideoAspect.ATTR_DURATION, _playTime.Value / 1000);
        if (_vidBitRate.HasValue)
          videoAspect.SetAttribute(VideoAspect.ATTR_VIDEOBITRATE, _vidBitRate.Value);
        videoAspect.SetAttribute(VideoAspect.ATTR_VIDEOENCODING, StringUtils.Join(", ", _vidCodecs));

        videoAspect.SetAttribute(VideoAspect.ATTR_AUDIOSTREAMCOUNT, _audioStreamCount);
        if (_audBitRate.HasValue)
          videoAspect.SetAttribute(VideoAspect.ATTR_AUDIOBITRATE, _audBitRate.Value);
        videoAspect.SetAttribute(VideoAspect.ATTR_AUDIOENCODING, StringUtils.Join(", ", _audCodecs));
        // TODO: extract cover art (see Mantis #1977)


        // Try to get extended information out of matroska files
        if (localFsResourcePath.EndsWith(".mkv"))
        {
          MatroskaInfoReader mkvReader = new MatroskaInfoReader(localFsResourcePath);
          // Add keys to be extracted to tags dictionary, matching results will returned as value
          Dictionary<string, IList<string>> tagsToExtract = new Dictionary<string, IList<string>>
                                                   {
                                                     {TAG_SERIES_TITLE, null}, // Series title
                                                     {TAG_SERIES_GENRE, null}, // Series genre(s)
                                                     {TAG_SEASON_NUMBER, null}, // Season number
                                                     {TAG_SEASON_YEAR, null}, // Season year
                                                     {TAG_SEASON_TITLE, null}, // Season title
                                                     {TAG_EPISODE_TITLE, null}, // Episode title
                                                     {TAG_EPISODE_SUMMARY, null}, // Episode summary
                                                     {TAG_EPISODE_YEAR, null}, // Episode year
                                                     {TAG_EPISODE_NUMBER, null}, // Episode number
                                                     {TAG_ACTORS, null}, // Actor(s)
                                                     {TAG_SIMPLE_TITLE, null} // File title
                                                   };
          mkvReader.ReadTags(tagsToExtract);

          string title = string.Empty;
          if (tagsToExtract[TAG_SIMPLE_TITLE] != null)
            title = tagsToExtract[TAG_SIMPLE_TITLE].FirstOrDefault();

          if (tagsToExtract[TAG_EPISODE_TITLE] != null)
          {
            string episodeName = tagsToExtract[TAG_EPISODE_TITLE].FirstOrDefault();
            string seriesName = string.Empty;
            string seasonNum = string.Empty;
            string episodeNum = string.Empty;

            if (tagsToExtract[TAG_SERIES_TITLE] != null)
             seriesName = tagsToExtract[TAG_SERIES_TITLE].FirstOrDefault();
            if (tagsToExtract[TAG_SEASON_NUMBER] != null)
             seasonNum = tagsToExtract[TAG_SEASON_NUMBER].FirstOrDefault();
            if (tagsToExtract[TAG_EPISODE_NUMBER] != null)
              episodeNum = tagsToExtract[TAG_EPISODE_NUMBER].FirstOrDefault();

            if (!string.IsNullOrEmpty(seriesName) && !string.IsNullOrEmpty(seasonNum) && !string.IsNullOrEmpty(episodeNum))
              title = string.Format("{0} S{1}E{2} - {3}", seriesName, seasonNum.PadLeft(2, '0'), episodeNum.PadLeft(2, '0'), episodeName);
          }
          if (!string.IsNullOrEmpty(title))
            mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, title);

          string yearCandidate = null;
          if (tagsToExtract[TAG_EPISODE_YEAR] != null)
            yearCandidate = tagsToExtract[TAG_EPISODE_YEAR].FirstOrDefault().Substring(0, 4);
          else
            if (tagsToExtract[TAG_SEASON_YEAR] != null)
              yearCandidate = tagsToExtract[TAG_SEASON_YEAR].FirstOrDefault().Substring(0, 4);

          int year;
          if (int.TryParse(yearCandidate, out year))
            mediaAspect.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, new DateTime(year, 1, 1));

          if (tagsToExtract[TAG_SERIES_GENRE] != null)
            videoAspect.SetCollectionAttribute(VideoAspect.ATTR_GENRES, tagsToExtract[TAG_SERIES_GENRE]);

          if (tagsToExtract[TAG_ACTORS] != null)
            videoAspect.SetCollectionAttribute(VideoAspect.ATTR_ACTORS, tagsToExtract[TAG_ACTORS]);

          if (tagsToExtract[TAG_EPISODE_SUMMARY] != null)
            videoAspect.SetAttribute(VideoAspect.ATTR_STORYPLOT, tagsToExtract[TAG_EPISODE_SUMMARY].FirstOrDefault());
        }


        // In quick mode only allow thumbs taken from cache.
        bool cachedOnly = forceQuickMode;

        // Thumbnail extraction
        IThumbnailGenerator generator = ServiceRegistration.Get<IThumbnailGenerator>();
        byte[] thumbData;
        ImageType imageType;
        if (generator.GetThumbnail(localFsResourcePath, 96, 96, cachedOnly, out thumbData, out imageType))
          thumbnailSmallAspect.SetAttribute(ThumbnailSmallAspect.ATTR_THUMBNAIL, thumbData);
        if (generator.GetThumbnail(localFsResourcePath, 256, 256, cachedOnly, out thumbData, out imageType))
          thumbnailLargeAspect.SetAttribute(ThumbnailLargeAspect.ATTR_THUMBNAIL, thumbData);
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

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        VideoResult result = null;
        IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        if (fsra != null && fsra.IsDirectory && fsra.ResourceExists("VIDEO_TS"))
        {
          IFileSystemResourceAccessor fsraVideoTs = fsra.GetResource("VIDEO_TS");
          if (fsraVideoTs != null && fsraVideoTs.ResourceExists("VIDEO_TS.IFO"))
          { // Video DVD
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
                string lowerPath = file.ResourcePathName.ToLowerInvariant();
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
        else if (mediaItemAccessor.IsFile)
        {
          string filePath = mediaItemAccessor.ResourcePathName;
          if (!HasVideoExtension(filePath))
            return false;
          using (MediaInfoWrapper fileInfo = ReadMediaInfo(mediaItemAccessor))
          {
            // Before we start evaluating the file, check if it is a video at all
            if (fileInfo.IsValid && fileInfo.GetVideoCount() == 0)
              return false;

            string mediaTitle = DosPathHelper.GetFileNameWithoutExtension(mediaItemAccessor.ResourceName);
            result = VideoResult.CreateFileInfo(mediaTitle, fileInfo);
          }
          using (Stream stream = mediaItemAccessor.OpenRead())
            result.MimeType = MimeTypeDetector.GetMimeType(stream);
        }
        if (result != null)
        {
          MediaItemAspect mediaAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, MediaAspect.ASPECT_ID, MediaAspect.Metadata);
          MediaItemAspect videoAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, VideoAspect.ASPECT_ID, VideoAspect.Metadata);
          MediaItemAspect thumbnailSmallAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, ThumbnailSmallAspect.ASPECT_ID, ThumbnailSmallAspect.Metadata);
          MediaItemAspect thumbnailLargeAspect = MediaItemAspect.GetOrCreateAspect(extractedAspectData, ThumbnailLargeAspect.ASPECT_ID, ThumbnailLargeAspect.Metadata);

          IResourceAccessor ra = mediaItemAccessor.Clone();
          try
          {
            using (ILocalFsResourceAccessor lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(ra))
              result.UpdateMetadata(mediaAspect, videoAspect, thumbnailSmallAspect, thumbnailLargeAspect, lfsra.LocalFileSystemPath, forceQuickMode);
          }
          catch
          {
            ra.Dispose();
            throw;
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