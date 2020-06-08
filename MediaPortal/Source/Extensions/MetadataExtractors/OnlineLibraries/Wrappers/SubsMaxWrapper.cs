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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.OnlineLibraries.Libraries.SubsMaxV1;
using MediaPortal.Extensions.OnlineLibraries.Libraries.SubsMaxV1.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  class SubsMaxWrapper : ApiSubtitleWrapper<string>
  {
    protected SubsMaxV1 _subsMaxHandler;
    protected readonly string _name;

    public SubsMaxWrapper(string name)
    {
      _name = name;
    }

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init()
    {
      _subsMaxHandler = new SubsMaxV1();

      return true;
    }

    #region Search

    protected override async Task<List<SubtitleInfo>> SearchMovieSubtitlesAsync(SubtitleInfo subtitleSearch, List<string> languages)
    {
      var lang = languages.Select(s => new CultureInfo(s).TwoLetterISOLanguageName).FirstOrDefault();

      List<SubsMaxSearchResult> results = new List<SubsMaxSearchResult>();
      results = await _subsMaxHandler.SearchMovieSubtitlesByTitleAndYearAsync(subtitleSearch.MediaTitle, subtitleSearch.Year, lang ?? "en");

      return results.Where(s => s.ArchiveFiles.Count > 0).Select(s => new SubtitleInfo
      {
        DisplayName = Path.GetFileNameWithoutExtension(GetNameWithoutPartNo(s.ArchiveFiles.First().Name)),
        Name = s.ArchiveFiles.First().Name,
        ImdbId = subtitleSearch.ImdbId,
        Language = GetCultureInfoName(s.ArchiveFiles.First().Language),
        MediaFiles = subtitleSearch.MediaFiles,
        MediaTitle = subtitleSearch.MediaTitle,
        MovieDbId = subtitleSearch.MovieDbId,
        SubtitleId = s.DownloadUrl,
        Year = subtitleSearch.Year,
        DataProviders = new List<string>() { _name }
      }).ToList();
    }

    protected override async Task<List<SubtitleInfo>> SearchSeriesEpisodeSubtitlesAsync(SubtitleInfo subtitleSearch, List<string> languages)
    {
      var lang = languages.Select(s => new CultureInfo(s).TwoLetterISOLanguageName).FirstOrDefault();

      List<SubsMaxSearchResult> results = new List<SubsMaxSearchResult>();
      results = await _subsMaxHandler.SearchSeriesSubtitlesAsync(subtitleSearch.MediaTitle, subtitleSearch.Season ?? 0, subtitleSearch.Episode ?? 0, lang ?? "en");

      return results.Where(s => s.ArchiveFiles.Count > 0).Select(s => new SubtitleInfo
      {
        Name = Path.GetFileNameWithoutExtension(GetNameWithoutPartNo(s.ArchiveFiles.First().Name)),
        TvdbId = subtitleSearch.TvdbId,
        Language = GetCultureInfoName(s.ArchiveFiles.First().Language),
        MediaFiles = subtitleSearch.MediaFiles,
        MediaTitle = subtitleSearch.MediaTitle,
        Episode = subtitleSearch.Episode,
        Season = subtitleSearch.Season,
        SubtitleId = s.DownloadUrl,
        Year = subtitleSearch.Year,
        DataProviders = new List<string>() { _name }
      }).ToList();
    }

    #endregion

    #region Download

    protected override async Task<IDictionary<BaseSubtitleMatch<string>, byte[]>> DownloadSubtitleAsync(SubtitleInfo subtitle)
    {
      var subs = new Dictionary<BaseSubtitleMatch<string>, byte[]>();
      if (!subtitle.DataProviders.Contains(_name))
        return subs;

      var files = await _subsMaxHandler.DownloadSubtileAsync(subtitle.SubtitleId);
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
