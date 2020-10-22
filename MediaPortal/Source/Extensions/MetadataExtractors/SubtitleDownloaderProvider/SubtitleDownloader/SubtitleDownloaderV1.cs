#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.MetadataExtractors.SubtitleDownloaderProvider.SubtitleDownloader.Data;
using SubtitleDownloader.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.SubtitleDownloaderProvider.SubtitleDownloader
{
  internal static class SubtitleDownloaderSetup
  {
    public static List<string> GetSupportedProviderNames()
    {
      return SubtitleDownloaderFactory.GetSubtitleDownloaderNames();
    }
  }

  internal class SubtitleDownloaderV1
  {
    #region Fields

    private readonly ISubtitleDownloader _downloader;
    private bool _movieSearchSupport = true;
    private bool _movieImdbSearchSupport = true;
    private bool _episodeSearchSupport = true;

    #endregion

    #region Constructor

    public SubtitleDownloaderV1(string downloader)
    {
      _downloader = SubtitleDownloaderFactory.GetSubtitleDownloader(downloader);
    }

    #endregion

    #region Public members

    /// <summary>
    /// Search for subtitles by title and year.
    /// </summary>
    /// <returns>List of available subtitles</returns>
    public Task<List<SubtitleDownloaderResult>> SearchMovieSubtitlesByTitleAndYearAsync(string title, int? year, params string[] languages)
    {
      try
      {
        if (_movieSearchSupport)
        {
          SearchQuery query = new SearchQuery(title);
          query.Year = year;
          query.LanguageCodes = languages;
          List<Subtitle> results = _downloader.SearchSubtitles(query);
          return Task.FromResult(results.Select(s => new SubtitleDownloaderResult(s)).ToList());
        }
      }
      catch (NotSupportedException)
      {
        _movieSearchSupport = false;
      }
      return Task.FromResult(new List<SubtitleDownloaderResult>());
    }

    /// <summary>
    /// Search for subtitles by IMDB ID.
    /// </summary>
    /// <returns>List of available subtitles</returns>
    public Task<List<SubtitleDownloaderResult>> SearchMovieSubtitlesByImdbIdAsync(string imdbid, params string[] languages)
    {
      try
      {
        if (_movieImdbSearchSupport)
        {
          ImdbSearchQuery query = new ImdbSearchQuery(imdbid);
          query.LanguageCodes = languages;
          List<Subtitle> results = _downloader.SearchSubtitles(query);
          return Task.FromResult(results.Select(s => new SubtitleDownloaderResult(s)).ToList());
        }
      }
      catch (NotSupportedException)
      {
        _movieImdbSearchSupport = false;
      }
      return Task.FromResult(new List<SubtitleDownloaderResult>());
    }

    /// <summary>
    /// Search for subtitles by episode.
    /// </summary>
    /// <returns>List of available subtitles</returns>
    public Task<List<SubtitleDownloaderResult>> SearchSeriesSubtitlesAsync(string series, int season, int episode, int? tvdbid, params string[] languages)
    {
      try
      {
        if (_episodeSearchSupport)
        {
          EpisodeSearchQuery query = new EpisodeSearchQuery(series, season, episode, tvdbid);
          query.LanguageCodes = languages;
          List<Subtitle> results = _downloader.SearchSubtitles(query);
          return Task.FromResult(results.Select(s => new SubtitleDownloaderResult(s)).ToList());
        }
      }
      catch (NotSupportedException)
      {
        _episodeSearchSupport = false;
      }
      return Task.FromResult(new List<SubtitleDownloaderResult>());
    }

    /// <summary>
    /// Downloads subtitle to the specified path.
    /// </summary>
    /// <param name="subtitle">Theubtitle to download</param>
    /// <returns>File info if successful</returns>
    public Task<List<FileInfo>> DownloadSubtileAsync(SubtitleDownloaderResult subtitle)
    {
      return Task.FromResult(_downloader.SaveSubtitle(subtitle));
    }

    #endregion

    #region Protected members

    protected static ILogger Logger
    {
      get
      {
        return ServiceRegistration.Get<ILogger>();
      }
    }

    #endregion
  }
}
