#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using MediaInfoLib;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MediaProviders;

namespace MediaPortal.Media.MetadataExtractors.MovieMetadataExtractor
{
  /// <summary>
  /// MediaPortal-II metadata extractor implementation for movie files. Supports several formats.
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

      // TODO: Validate those formats - can they correctly be processed by our MediaInfo library?
      MOVIE_EXTENSIONS.Add(".mkv"); // Not confirmed yet
      MOVIE_EXTENSIONS.Add(".ogm"); // Not confirmed yet
      MOVIE_EXTENSIONS.Add(".avi"); // Not confirmed yet
      MOVIE_EXTENSIONS.Add(".wmv"); // Not confirmed yet
      MOVIE_EXTENSIONS.Add(".mpg"); // Not confirmed yet
      MOVIE_EXTENSIONS.Add(".mp4"); // Not confirmed yet
    }

    public MovieMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Movie metadata extractor",
          SHARE_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                MovieAspect.Metadata
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
      string ext = Path.GetExtension(fileName).ToLower();
      return MOVIE_EXTENSIONS.Contains(ext);
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IMediaProvider provider, string path, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      string filePath = provider.GetResourcePath(path);
      if (!HasMovieExtension(filePath))
        return false;
      try
      {
        string ext = Path.GetExtension(filePath).ToLower();
        bool isDvd = (ext == ".ifo");
        string dvdFolder = null;
        if (isDvd && !filePath.ToLower().EndsWith("video_ts.ifo")) return false;
        if (isDvd)
        {
          string video_ts_folderPath = Path.GetDirectoryName(filePath);
          dvdFolder = Path.GetDirectoryName(video_ts_folderPath);
        }
        FileInfo info = new FileInfo(filePath);
        using (MediaInfoWrapper mediaInfo = new MediaInfoWrapper())
        {
          Stream stream = null;
          try
          {
            stream = provider.OpenRead(path);
            mediaInfo.Open(stream);
          }
          finally
          {
            if (stream != null)
              stream.Close();
          }
          // Before we start evaluating the file, check if it is a video at all
          if (mediaInfo.IsOpened && mediaInfo.GetVideoCount() == 0)
            return false;

          MediaItemAspect mediaAspect = extractedAspectData[MediaAspect.ASPECT_ID];
          MediaItemAspect movieAspect = extractedAspectData[MovieAspect.ASPECT_ID];

          movieAspect.SetAttribute(MovieAspect.ATTR_DURATION, info.Length);
          if (isDvd)
          {
            mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, Path.GetFileName(dvdFolder));
            string parentDirectory = Path.GetDirectoryName(dvdFolder); // Directory where the DVD folder is located
            // If DVD is located at root level (e.g. a DVD in the DVD drive), we don't have a dvd folder
            mediaAspect.SetAttribute(MediaAspect.ATTR_RECTIFIED_PATH,
                string.IsNullOrEmpty(parentDirectory) ? dvdFolder : parentDirectory);
          }
          else
          {
            mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, Path.GetFileNameWithoutExtension(filePath));
            mediaAspect.SetAttribute(MediaAspect.ATTR_RECTIFIED_PATH, filePath);
            // TODO: extract cover art, store it in media library (see Mantis #1977)
          }
          mediaAspect.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, info.CreationTime);
          movieAspect.SetAttribute(MovieAspect.ATTR_ISDVD, isDvd);
          if (mediaInfo.IsOpened)
          {
            int? i;
            long? l;
            string s;
            float? f;
            s = mediaInfo.GetVidCodec();
            if (s != null)
              movieAspect.SetAttribute(MovieAspect.ATTR_VIDEOENCODING, s);
            l = mediaInfo.GetVidBitrate();
            if (l.HasValue)
              movieAspect.SetAttribute(MovieAspect.ATTR_VIDEOBITRATE, l.Value);
            i = mediaInfo.GetWidth();
            if (i.HasValue)
              movieAspect.SetAttribute(MovieAspect.ATTR_WIDTH, i.Value);
            i = mediaInfo.GetHeight();
            if (i.HasValue)
              movieAspect.SetAttribute(MovieAspect.ATTR_HEIGHT, i.Value);
            i = mediaInfo.GetFPS();
            if (i.HasValue)
              movieAspect.SetAttribute(MovieAspect.ATTR_FPS, i.Value);
            movieAspect.SetAttribute(MovieAspect.ATTR_AUDIOSTREAMCOUNT, mediaInfo.GetAudioCount());
            s = mediaInfo.GetAudioCodec();
            if (s != null)
              movieAspect.SetAttribute(MovieAspect.ATTR_AUDIOENCODING, s);
            l = mediaInfo.GetAudioBitrate();
            if (l.HasValue)
              movieAspect.SetAttribute(MovieAspect.ATTR_AUDIOBITRATE, l.Value);
            f = mediaInfo.GetAR();
            if (f.HasValue)
              movieAspect.SetAttribute(MovieAspect.ATTR_ASPECTRATIO, f.Value);
            l = mediaInfo.GetPlaytime();
            if (l.HasValue)
              movieAspect.SetAttribute(MovieAspect.ATTR_DURATION, l.Value);
          }
        }

        // The following code should be used in the slow batch mode (see Mantis #1977)
        //#region code testing the xbmc scraper
        //if (scraper.IsLoaded)
        //{
        //  scraper.CreateSearchUrl((string)movie["title"]);
        //  ServiceScope.Get<ILogger>().Info("MovieImporter: Getting online info from: {0} ", scraper.SearchUrl);
        //  scraper.GetSearchResults();
        //  ServiceScope.Get<ILogger>().Info("MovieImporter: Result found {0} ", scraper.SearchResults.Count);
        //  if (scraper.SearchResults.Count > 0)
        //  {

        //    QueueMessage msgc = new QueueMessage();
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

        //    ServiceScope.Get<ILogger>().Info("MovieImporter: Getting online info for: {0}", scraper.SearchResults[0].Title);
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
        //  ServiceScope.Get<ILogger>().Info("MovieImporter: No online scrapers are loaded ");
        //}

        //#endregion

        return true;
      }
      catch (Exception ex)
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here
        ServiceScope.Get<ILogger>().Info("MovieMetadataExtractor: Exception reading file '{0}' (media provider: '{1}')", path, provider.Metadata.Name);
        return false;
      }
    }

    #endregion
  }
}