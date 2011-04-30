#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.Services.ThumbnailGenerator;
using MediaPortal.Core.Settings;
using MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Settings;
using MediaPortal.Utilities;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for movie files. Supports several formats.
  /// </summary>
  public class MovieMetadataExtractor : IMetadataExtractor
  {
    #region Public constants

    /// <summary>
    /// GUID string for the movie metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "5DE08B57-7D0A-4577-A117-4CB8D6F0C825";

    /// <summary>
    /// Movie metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    #region Protected fields and classes

    protected static IList<string> SHARE_CATEGORIES = new List<string>();
    protected static IList<string> MOVIE_EXTENSIONS = new List<string>();

    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static MovieMetadataExtractor()
    {
      SHARE_CATEGORIES.Add(DefaultMediaCategory.Video.ToString());
      MovieMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<MovieMetadataExtractorSettings>();
      InitializeExtensions(settings);
    }

    /// <summary>
    /// (Re)initializes the movie extensions for which this <see cref="MovieMetadataExtractorSettings"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(MovieMetadataExtractorSettings settings)
    {
      MOVIE_EXTENSIONS = new List<string>(settings.MovieExtensions.Select(e => e.ToLowerInvariant()));
    }

    public MovieMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Movie metadata extractor", true,
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
    protected static bool HasMovieExtension(string fileName)
    {
      string ext = Path.GetExtension(fileName);
      if (ext != null)
        ext = ext.ToLower();
      return MOVIE_EXTENSIONS.Contains(ext);
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

    public class MovieResult
    {
      protected bool _isDVD;
      protected string _title;
      protected string _mimeType;

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

      public MovieResult(string movieTitle, MediaInfoWrapper mainInfo)
      {
        _title = movieTitle;
        AddMediaInfo(mainInfo);
      }

      public static MovieResult CreateDVDInfo(string dvdTitle, MediaInfoWrapper videoTsInfo)
      {
        MovieResult result = new MovieResult(dvdTitle, videoTsInfo) {IsDVD = true, MimeType = "video/dvd"};
        return result;
      }

      public static MovieResult CreateFileInfo(string fileName, MediaInfoWrapper fileInfo)
      {
        return new MovieResult(fileName, fileInfo);
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
            _playTime = mediaInfo.GetPlaytime(i);
          if (!_vidBitRate.HasValue)
            _vidBitRate = mediaInfo.GetVidBitrate(i);
          string vidCodec = mediaInfo.GetVidCodec(i);
          if (!string.IsNullOrEmpty(vidCodec))
            _vidCodecs.Add(vidCodec);
        }
        _audioStreamCount = mediaInfo.GetAudioCount();
        for (int i = 0; i < _audioStreamCount; i++)
        {
          if (!_audBitRate.HasValue)
            _audBitRate = mediaInfo.GetAudioBitrate(i);
          string audCodec = mediaInfo.GetAudioCodec(i);
          if (!string.IsNullOrEmpty(audCodec))
            _audCodecs.Add(audCodec);
        }
      }

      public void UpdateMetadata(MediaItemAspect mediaAspect, MediaItemAspect videoAspect, string localFsResourcePath)
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

        if (localFsResourcePath != null)
        {
          // Thumbnail extraction
          IThumbnailGenerator generator = ServiceRegistration.Get<IThumbnailGenerator>();
          byte[] thumbData;
          ImageType imageType;
          if (generator.GetThumbnail(localFsResourcePath, 32, 32, out thumbData, out imageType))
            mediaAspect.SetAttribute(MediaAspect.ATTR_THUMB_SMALL, thumbData);
          if (generator.GetThumbnail(localFsResourcePath, 96, 96, out thumbData, out imageType))
            mediaAspect.SetAttribute(MediaAspect.ATTR_THUMB_MEDIUM, thumbData);
          if (generator.GetThumbnail(localFsResourcePath, 256, 256, out thumbData, out imageType))
            mediaAspect.SetAttribute(MediaAspect.ATTR_THUMB_LARGE, thumbData);
          if (generator.GetThumbnail(localFsResourcePath, 1024, 1024, out thumbData, out imageType))
            mediaAspect.SetAttribute(MediaAspect.ATTR_THUMB_XLARGE, thumbData);
        }
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

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      try
      {
        MovieResult result = null;
        IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        if (fsra != null && fsra.IsDirectory && fsra.Exists("VIDEO_TS"))
        {
          IFileSystemResourceAccessor fsraVideoTs = fsra.GetResource("VIDEO_TS") as IFileSystemResourceAccessor;
          if (fsraVideoTs != null && fsraVideoTs.Exists("VIDEO_TS.IFO"))
          { // Video DVD
            using (MediaInfoWrapper videoTsInfo = ReadMediaInfo(fsraVideoTs.GetResource("VIDEO_TS.IFO")))
            {
              if (!videoTsInfo.IsValid || videoTsInfo.GetVideoCount() == 0)
                return false; // Invalid video_ts.ifo file
              result = MovieResult.CreateDVDInfo(fsra.ResourceName, videoTsInfo);
            }
            // Iterate over all video files; MediaInfo finds different audio/video metadata for each .ifo file
            foreach (IFileSystemResourceAccessor file in fsraVideoTs.GetFiles())
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
          if (!HasMovieExtension(filePath))
            return false;
          using (MediaInfoWrapper fileInfo = ReadMediaInfo(mediaItemAccessor))
          {
            // Before we start evaluating the file, check if it is a video at all
            if (fileInfo.IsValid && fileInfo.GetVideoCount() == 0)
              return false;
            result = MovieResult.CreateFileInfo(Path.GetFileNameWithoutExtension(mediaItemAccessor.ResourceName), fileInfo);
          }
          using (Stream stream = mediaItemAccessor.OpenRead())
            result.MimeType = MimeTypeDetector.GetMimeType(stream);
        }
        if (result != null)
        {
          // TODO: The creation of new media item aspects could be moved to a general method
          MediaItemAspect mediaAspect;
          if (!extractedAspectData.TryGetValue(MediaAspect.ASPECT_ID, out mediaAspect))
            extractedAspectData[MediaAspect.ASPECT_ID] = mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
          MediaItemAspect videoAspect;
          if (!extractedAspectData.TryGetValue(VideoAspect.ASPECT_ID, out videoAspect))
            extractedAspectData[VideoAspect.ASPECT_ID] = videoAspect = new MediaItemAspect(VideoAspect.Metadata);

          ILocalFsResourceAccessor lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(mediaItemAccessor);
          result.UpdateMetadata(mediaAspect, videoAspect, lfsra == null ? null : lfsra.LocalFileSystemPath);
          return true;
        }
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here
        ServiceRegistration.Get<ILogger>().Info("MovieMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.LocalResourcePath, e.Message);
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