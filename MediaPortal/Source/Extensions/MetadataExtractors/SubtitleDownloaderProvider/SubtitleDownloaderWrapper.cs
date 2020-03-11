#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.MetadataExtractors.SubtitleDownloaderProvider.SubtitleDownloader;
using MediaPortal.Extensions.MetadataExtractors.SubtitleDownloaderProvider.SubtitleDownloader.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.SubtitleDownloaderProvider
{
  class SubtitleDownloaderWrapper : ApiSubtitleWrapper<string>
  {
    protected SubtitleDownloaderV1 _subtitleDownloaderHandler;
    protected string _providerName;

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string providerId, string name)
    {
      _subtitleDownloaderHandler = new SubtitleDownloaderV1(providerId);
      _providerName = name;

      return true;
    }

    #region Search

    protected override async Task<List<SubtitleInfo>> SearchMovieSubtitlesAsync(SubtitleInfo subtitleSearch, List<string> languages)
    {
      var langs = languages.Select(s => new CultureInfo(s).ThreeLetterISOLanguageName);

      List<SubtitleDownloaderResult> results = new List<SubtitleDownloaderResult>();

      if (!string.IsNullOrEmpty(subtitleSearch.ImdbId))
        results = await _subtitleDownloaderHandler.SearchMovieSubtitlesByImdbIdAsync(subtitleSearch.ImdbId, langs.ToArray());

      if (results.Count == 0)
        results = await _subtitleDownloaderHandler.SearchMovieSubtitlesByTitleAndYearAsync(subtitleSearch.MediaTitle, subtitleSearch.Year, langs.ToArray());

      if (results.Count == 0)
        results = await _subtitleDownloaderHandler.SearchMovieSubtitlesByTitleAndYearAsync(CleanMovieTitle(subtitleSearch.MediaTitle), subtitleSearch.Year, langs.ToArray());

      return results.Select(s => new SubtitleInfo
      {
        DisplayName = Path.GetFileNameWithoutExtension(s.FileName),
        Name = s.FileName,
        ImdbId = subtitleSearch.ImdbId,
        Language = GetCultureInfoName(s.LanguageCode),
        MediaFiles = subtitleSearch.MediaFiles,
        MediaTitle = subtitleSearch.MediaTitle,
        MovieDbId = subtitleSearch.MovieDbId,
        SubtitleId = s.Id,
        Year = subtitleSearch.Year,
        DataProviders = new List<string>() { _providerName }
      }).ToList();
    }

    protected override async Task<List<SubtitleInfo>> SearchSeriesEpisodeSubtitlesAsync(SubtitleInfo subtitleSearch, List<string> languages)
    {
      var langs = languages.Select(s => new CultureInfo(s).ThreeLetterISOLanguageName);

      List<SubtitleDownloaderResult> results = new List<SubtitleDownloaderResult>();

      results = await _subtitleDownloaderHandler.SearchSeriesSubtitlesAsync(subtitleSearch.MediaTitle, subtitleSearch.Season ?? 0, subtitleSearch.Episode ?? 0,
          subtitleSearch.TvdbId > 0 ? subtitleSearch.TvdbId : (int?)null, langs.ToArray());

      if (results.Count == 0)
        results = await _subtitleDownloaderHandler.SearchSeriesSubtitlesAsync(CleanSeriesTitle(subtitleSearch.MediaTitle, subtitleSearch.Year), subtitleSearch.Season ?? 0, 
          subtitleSearch.Episode ?? 0, subtitleSearch.TvdbId > 0 ? subtitleSearch.TvdbId : (int?)null, langs.ToArray());

      return results.Select(s => new SubtitleInfo
      {
        Name = s.FileName,
        TvdbId = subtitleSearch.TvdbId,
        Language = GetCultureInfoName(s.LanguageCode),
        MediaFiles = subtitleSearch.MediaFiles,
        MediaTitle = subtitleSearch.MediaTitle,
        Episode = subtitleSearch.Episode,
        Season = subtitleSearch.Season,
        SubtitleId = s.Id,
        Year = subtitleSearch.Year,
        DataProviders = new List<string>() { _providerName }
      }).ToList();
    }

    #endregion

    #region Download

    protected override async Task<IDictionary<BaseSubtitleMatch<string>, byte[]>> DownloadSubtitleAsync(SubtitleInfo subtitle)
    {
      var subs = new Dictionary<BaseSubtitleMatch<string>, byte[]>();
      if (!subtitle.DataProviders.Contains(_providerName))
        return subs;

      var files = await _subtitleDownloaderHandler.DownloadSubtileAsync(new SubtitleDownloaderResult(subtitle.SubtitleId, subtitle.MediaTitle, subtitle.Name, 
        new CultureInfo(subtitle.Language).ThreeLetterISOLanguageName));

      foreach (var file in files)
      {
        var match = new BaseSubtitleMatch<string>() { Id = subtitle.SubtitleId, ItemName = file.Name, Language = subtitle.Language };
        subs.Add(match, File.ReadAllBytes(file.FullName));
        //Delete temp file
        try { File.Delete(file.FullName); } catch { }
      }
      return subs;
    }

    #endregion
  }
}
