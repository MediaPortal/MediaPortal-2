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

using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.OnlineLibraries.Libraries.SubDbV1;
using MediaPortal.Extensions.OnlineLibraries.Libraries.SubDbV1.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  class SubDbWrapper : ApiSubtitleWrapper<string>
  {
    protected SubDbV1 _subDbHandler;
    protected readonly string _name;

    public SubDbWrapper(string name)
    {
      _name = name;
    }

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init()
    {
      _subDbHandler = new SubDbV1();

      return true;
    }

    #region Search

    protected override async Task<List<SubtitleInfo>> SearchMovieSubtitlesAsync(SubtitleInfo subtitleSearch, List<string> languages)
    {
      List<SubDbSearchResult> results = new List<SubDbSearchResult>();
      var mediaFile = subtitleSearch.MediaFiles.First();
      using (IResourceAccessor mediaItemAccessor = mediaFile.CreateAccessor())
      using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
      using (rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess())
      {
        using (var stream = await rah.LocalFsResourceAccessor.OpenReadAsync())
        {
          if (stream != null)
            results = await _subDbHandler.SearchSubtitlesAsync(stream);
        }
      }

      if (!results.Any())
        return new List<SubtitleInfo>();

      var langs = languages.Select(s => new CultureInfo(s).TwoLetterISOLanguageName);
      if (!results.Any(r => langs.Any(l => l.Equals(r.LanguageCode, System.StringComparison.InvariantCultureIgnoreCase))))
        return new List<SubtitleInfo>();

      return results.Where(r => langs.Any(l => l.Equals(r.LanguageCode, System.StringComparison.InvariantCultureIgnoreCase))).
        Select(s => new SubtitleInfo
        {
          DisplayName = Path.GetFileNameWithoutExtension(mediaFile.NativeResourcePath.FileName),
          Name = Path.GetFileNameWithoutExtension(mediaFile.NativeResourcePath.FileName) + ".srt",
          ImdbId = subtitleSearch.ImdbId,
          Language = new CultureInfo(s.LanguageCode).Name,
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
      List<SubDbSearchResult> results = new List<SubDbSearchResult>();
      var mediaFile = subtitleSearch.MediaFiles.First();
      using (IResourceAccessor mediaItemAccessor = mediaFile.CreateAccessor())
      using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
      using (rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess())
      {
        using (var stream = await rah.LocalFsResourceAccessor.OpenReadAsync())
        {
          if (stream != null)
            results = await _subDbHandler.SearchSubtitlesAsync(stream);
        }
      }

      if (!results.Any())
        return new List<SubtitleInfo>();

      var langs = languages.Select(s => new CultureInfo(s).TwoLetterISOLanguageName);
      if (!results.Any(r => langs.Any(l => l.Equals(r.LanguageCode, System.StringComparison.InvariantCultureIgnoreCase))))
        return new List<SubtitleInfo>();

      return results.Where(r => langs.Any(l => l.Equals(r.LanguageCode, System.StringComparison.InvariantCultureIgnoreCase))).
        Select(s => new SubtitleInfo
        {
          DisplayName = Path.GetFileNameWithoutExtension(mediaFile.NativeResourcePath.FileName),
          Name = Path.GetFileNameWithoutExtension(mediaFile.NativeResourcePath.FileName) + ".srt",
          TvdbId = subtitleSearch.TvdbId,
          Language = new CultureInfo(s.LanguageCode).Name,
          MediaFiles = subtitleSearch.MediaFiles,
          MediaTitle = subtitleSearch.MediaTitle,
          Episode = subtitleSearch.Episode,
          Season = subtitleSearch.Season,
          SubtitleId = s.DownloadUrl,
          Year = subtitleSearch.Year,
          DataProviders = new List<string>() { _name }
        }).ToList();
    }

    protected override void RankMovieSubtitleMatch(SubtitleInfo subtitleSearch, List<string> languages)
    {
      subtitleSearch.MatchPercentage = 100;
      int languageRank = languages?.IndexOf(subtitleSearch.Language) ?? -1;
      if (languageRank >= 0)
        subtitleSearch.LanguageMatchRank = languageRank;
    }

    protected override void RankSeriesEpisodeSubtitleMatch(SubtitleInfo subtitleSearch, List<string> languages)
    {
      subtitleSearch.MatchPercentage = 100;
      int languageRank = languages?.IndexOf(subtitleSearch.Language) ?? -1;
      if (languageRank >= 0)
        subtitleSearch.LanguageMatchRank = languageRank;
    }

    #endregion

    #region Download

    protected override async Task<IDictionary<BaseSubtitleMatch<string>, byte[]>> DownloadSubtitleAsync(SubtitleInfo subtitle)
    {
      var subs = new Dictionary<BaseSubtitleMatch<string>, byte[]>();
      if (!subtitle.DataProviders.Contains(_name))
        return subs;

      var file = await _subDbHandler.DownloadSubtileAsync(subtitle.SubtitleId);
      var match = new BaseSubtitleMatch<string>() { Id = subtitle.SubtitleId, ItemName = file.Name, Language = subtitle.Language };
      subs.Add(match, File.ReadAllBytes(file.FullName));
      //Delete temp file
      try { File.Delete(file.FullName); } catch { }
      return subs;
    }

    #endregion
  }
}
