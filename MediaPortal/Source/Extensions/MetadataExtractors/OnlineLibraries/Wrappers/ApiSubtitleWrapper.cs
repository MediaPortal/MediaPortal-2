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
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  public abstract class ApiSubtitleWrapper<TId>
  {
    private char[] TAG_SPLITTERS = new[] { '.', ' ', '-', '[', ']' };
    private const int BASE_MATCH_PCT = 30;
    private const int BASE_YEAR_MATCH_PCT = 50;
    private const int MULTIFILE_MATCH_PCT = 20;
    private const double TAG_MATCH_WEIGHT_PCT = 50;
    private const double LETTER_PAIR_MATCH_WEIGHT_PCT = 50;

    protected Regex _regexDoubleEpisode = new Regex(@"(?<series>.+)[\s|\.]S*(?<seasonnum>\d+)[EX](?<episodenum>\d+)[\-_]E*(?<endepisodenum>\d+)+.*\.", RegexOptions.IgnoreCase);
    protected Regex _regexEpisode = new Regex(@"(?<series>.+)[\s|\.]S*(?<seasonnum>\d+)[EX](?<episodenum>\d+).*\.", RegexOptions.IgnoreCase);
    protected Regex _regexTitleYear = new Regex(@"(?<title>.+)[.|\s]*[\(\[\.]?(?<year>(19|20)\d{2})[\]\)\.]?[\.|\\|\/]*", RegexOptions.IgnoreCase);
    protected Regex _regexMultiPartVideo = new Regex(@"(.*)(?<media>Disc|Disk|CD|DVD|File|#)(\s*)(?<disc>\d{1,2})(.*)", RegexOptions.IgnoreCase);

    #region Movies

    /// <summary>
    /// Search for Movie subtitles.
    /// </summary>
    /// <param name="subtitleSearch">Movie subtitle search parameters</param>
    /// <returns>A list of matches..</returns>
    protected virtual Task<List<SubtitleInfo>> SearchMovieSubtitlesAsync(SubtitleInfo subtitleSearch, List<string> languages)
    {
      return Task.FromResult<List<SubtitleInfo>>(null);
    }

    /// <summary>
    /// Search for matches of Movie. This method tries to find the any matching Movies in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// - If movies name contains " - ", it splits on this and tries to runs again using the first part (combined titles)
    /// </summary>
    /// <param name="movieSearch">Movie search parameters</param>
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns>A list of all matching movies found.</returns>
    public async Task<List<SubtitleInfo>> SearchMovieSubtitleMatchesAsync(SubtitleInfo subtitleSearch, List<string> languages)
    {
      List<SubtitleInfo> subtitles = await SearchMovieSubtitlesAsync(subtitleSearch, languages).ConfigureAwait(false);
      if (subtitles?.Count > 0)
      {
        subtitles.ForEach(s => RankMovieSubtitleMatch(s, languages));
        return MergeMultiPartSubtitles(subtitles);
      }

      return null;
    }

    /// <summary>
    /// Ranks movie subtitle matches. 
    /// </summary>
    /// <param name="subtitleSearch">Subtitle search result</param>
    protected virtual void RankMovieSubtitleMatch(SubtitleInfo subtitleSearch, List<string> languages)
    {
      int matchPct = 0;

      var match = _regexTitleYear.Match(CleanMovieTitle(subtitleSearch.Name));
      if (match.Success && (match.Groups["title"].Value.Equals(subtitleSearch.MediaTitle, StringComparison.InvariantCultureIgnoreCase) || 
        match.Groups["title"].Value.StartsWith(subtitleSearch.MediaTitle, StringComparison.InvariantCultureIgnoreCase)))
      {
        if (subtitleSearch.Year.HasValue && int.TryParse(match.Groups["year"].Value, out int subYear) && subYear == subtitleSearch.Year.Value)
          matchPct = BASE_YEAR_MATCH_PCT;
        else if (!subtitleSearch.Year.HasValue || string.IsNullOrEmpty(match.Groups["year"].Value))
          matchPct = BASE_MATCH_PCT;
      }

      match = _regexMultiPartVideo.Match(subtitleSearch.Name);
      if (match.Success)
      {
        if (!string.IsNullOrEmpty(match.Groups["disc"].Value) && subtitleSearch.MediaFiles.Count > 1)
        {
          matchPct += MULTIFILE_MATCH_PCT;
        }
        else if (!string.IsNullOrEmpty(match.Groups["disc"].Value) && subtitleSearch.MediaFiles.Count == 1)
        {
          return;
        }
      }
      else if (subtitleSearch.MediaFiles.Count == 1)
      {
        matchPct += MULTIFILE_MATCH_PCT;
      }
      else if (subtitleSearch.MediaFiles.Count > 1)
      {
        return;
      }

      matchPct = CompareMediaFiles(subtitleSearch, matchPct);
      subtitleSearch.MatchPercentage = Math.Min(matchPct, 100);

      int languageRank = languages?.IndexOf(subtitleSearch.Language) ?? -1;
      if (languageRank >= 0)
        subtitleSearch.LanguageMatchRank = languageRank;
    }

    protected string CleanMovieTitle(string title)
    {
      return title.Replace(".", " ").Replace("'", "");
    }

    #endregion

    #region Series

    /// <summary>
    /// Search for Series.
    /// </summary>
    /// <param name="subtitleSearch">Episode search parameters.</param>    /// 
    /// <param name="language">Language, if <c>null</c> it takes the <see cref="PreferredLanguage"/></param>
    /// <returns>A list of matches.</returns>
    protected virtual Task<List<SubtitleInfo>> SearchSeriesEpisodeSubtitlesAsync(SubtitleInfo subtitleSearch, List<string> languages)
    {
      return Task.FromResult<List<SubtitleInfo>>(null);
    }

    /// <summary>
    /// Search for any matches of Series episode names. This method tries to find the best matching Series episode in following order:
    /// - Exact match using PreferredLanguage
    /// - Exact match using DefaultLanguage
    /// - If series name contains " - ", it splits on this and tries to runs again using the first part (combined titles)
    /// </summary>
    /// <param name="episodeSearch">Episode search parameters.</param>
    /// <param name = "language" > Language, if <c>null</c> it takes the<see cref="PreferredLanguage"/></param>
    /// <returns>List of matching episodes found.</returns>
    public async Task<List<SubtitleInfo>> SearchSeriesEpisodeSubtitleMatchesAsync(SubtitleInfo subtitleSearch, List<string> languages)
    {
      List<SubtitleInfo> subtitles = await SearchSeriesEpisodeSubtitlesAsync(subtitleSearch, languages).ConfigureAwait(false);
      if (subtitles?.Count > 0)
      {
        subtitles.ForEach(s => RankSeriesEpisodeSubtitleMatch(s, languages));
        return subtitles;
      }

      return null;
    }

    /// <summary>
    /// Ranks movie subtitle matches. 
    /// </summary>
    /// <param name="subtitleSearch">Subtitle search result</param>
    protected virtual void RankSeriesEpisodeSubtitleMatch(SubtitleInfo subtitleSearch, List<string> languages)
    {
      int matchPct = 0;
      string seriesName = null;
      var match = _regexDoubleEpisode.Match(subtitleSearch.Name);
      if (match.Success)
      {
        if (int.TryParse(match.Groups["seasonnum"].Value, out var season) &&
          int.TryParse(match.Groups["episodenum"].Value, out var episode) && int.TryParse(match.Groups["endepisodenum"].Value, out var endEpisode))
        {
          if (season == subtitleSearch.Season && episode <= subtitleSearch.Episode && endEpisode >= subtitleSearch.Episode)
            seriesName = match.Groups["series"].Value;
          else
            return;
        }
      }
      else
      {
        match = _regexEpisode.Match(subtitleSearch.Name);
        if (match.Success)
        {
          if (int.TryParse(match.Groups["seasonnum"].Value, out var season) && int.TryParse(match.Groups["episodenum"].Value, out var episode))
          {
            if (season == subtitleSearch.Season && episode == subtitleSearch.Episode)
              seriesName = match.Groups["series"].Value;
            else
              return;
          }
        }
      }

      if (!string.IsNullOrEmpty(seriesName))
      {
        var cleanName = CleanSeriesTitle(seriesName);
        match = _regexTitleYear.Match(cleanName);
        if (match.Success && match.Groups["title"].Value.Equals(subtitleSearch.MediaTitle, StringComparison.InvariantCultureIgnoreCase) ||
          match.Groups["title"].Value.StartsWith(subtitleSearch.MediaTitle, StringComparison.InvariantCultureIgnoreCase))
        {
          if (subtitleSearch.Year.HasValue && int.TryParse(match.Groups["year"].Value, out int subYear) && subYear == subtitleSearch.Year.Value)
            matchPct = BASE_YEAR_MATCH_PCT;
          else if (!subtitleSearch.Year.HasValue || string.IsNullOrEmpty(match.Groups["year"].Value))
            matchPct = BASE_MATCH_PCT;
        }
        else if (!match.Success && cleanName.Equals(subtitleSearch.MediaTitle, StringComparison.InvariantCultureIgnoreCase) ||
          cleanName.StartsWith(subtitleSearch.MediaTitle, StringComparison.InvariantCultureIgnoreCase))
        {
          matchPct = BASE_MATCH_PCT;
        }
      }

      matchPct = CompareMediaFiles(subtitleSearch, matchPct);
      subtitleSearch.MatchPercentage = Math.Min(matchPct, 100);

      int languageRank = languages?.IndexOf(subtitleSearch.Language) ?? -1;
      if (languageRank >= 0)
        subtitleSearch.LanguageMatchRank = languageRank;
    }

    protected string CleanSeriesTitle(string title, int? year = null)
    {
      var match = _regexTitleYear.Match(title);
      if (match.Success)
        title = match.Groups["title"].Value;
      else if (year.HasValue)
        title = $"{title} ({year.Value})";
      else if (int.TryParse(match.Groups["year"].Value, out int regexYear))
        title = $"{title} ({year.Value})";

      return title.Replace(".", " ").Replace("'", "");
    }

    #endregion

    #region Helpers

    protected List<SubtitleInfo> MergeMultiPartSubtitles(List<SubtitleInfo> subtitles)
    {
      Dictionary<string, SubtitleInfo> subtitlePairs = new Dictionary<string, SubtitleInfo>();
      foreach (var subtitle in subtitles)
      {
        var subPartMatch = _regexMultiPartVideo.Match(subtitle.Name);
        if (subPartMatch.Success && int.TryParse(subPartMatch.Groups["disc"].Value, out var partNum))
        {
          var subName = _regexMultiPartVideo.Replace(subtitle.DisplayName, "${1}${2}${4}${3}");
          if (!subtitlePairs.ContainsKey(subName))
          {
            var sub = subtitle.Clone();
            sub.DisplayName = subName;
            subtitlePairs.Add(subName, sub);
          }
          else if (!subtitlePairs[subName].SubtitleId.Contains(subtitle.SubtitleId))
          {
            subtitlePairs[subName].MergeWith(subtitle);
          }
        }
        else if (!subtitlePairs.ContainsKey(subtitle.SubtitleId))
        {
          subtitlePairs.Add(subtitle.SubtitleId, subtitle);
        }
      }
      return subtitlePairs.Values.ToList();
    }

    protected int CompareMediaFiles(SubtitleInfo subtitle, int matchPct)
    {
      int maxPct = 0;
      var subtitleFileName = ResourcePath.GetFileNameWithoutExtension(subtitle.Name);
      foreach (var mediaFile in subtitle.MediaFiles)
      {
        int pct = 0;
        var mediaFileName = ResourcePath.GetFileNameWithoutExtension(mediaFile.NativeResourcePath.FileName);

        //Compare file names
        if (subtitleFileName.Equals(mediaFileName, StringComparison.InvariantCultureIgnoreCase))
          return 100;

        //Compare tags
        pct = Math.Max(CompareTags(subtitleFileName, mediaFileName), pct);

        //Compare letter pairs
        pct = Math.Max(CompareLetterPairs(subtitleFileName, mediaFileName), pct);

        maxPct = Math.Max(maxPct, pct);
      }
      return maxPct + matchPct;
    }

    protected int CompareTags(string subtitleFileName, string mediaFileName)
    {
      var subtitleTags = subtitleFileName.Split(TAG_SPLITTERS, StringSplitOptions.RemoveEmptyEntries);
      var mediaTags = mediaFileName.Split(TAG_SPLITTERS, StringSplitOptions.RemoveEmptyEntries);
      int matchingTags = 0;
      int totalTags = Math.Max(mediaTags.Length, subtitleTags.Length);
      if (mediaTags.Length > 2 && subtitleTags.Length > 2)
      {
        for (int tag = 0; tag < totalTags; tag++)
        {
          if ((subtitleTags.Length > tag && mediaTags.Contains(subtitleTags[tag])) || (mediaTags.Length > tag && subtitleTags.Contains(mediaTags[tag])))
            matchingTags++;
        }
      }
      if (totalTags == 0)
        return 0;

      return Convert.ToInt32(((double)matchingTags / (double)totalTags) * TAG_MATCH_WEIGHT_PCT);
    }

    protected int CompareLetterPairs(string subtitleFileName, string mediaFileName)
    {
      var subtitleList = GetWordLetterPairs(subtitleFileName.ToUpper());
      var mediaList = GetWordLetterPairs(mediaFileName.ToUpper());
      int matchingPairs = 0;
      int totalPairs = subtitleList.Count + mediaList.Count;
      for (int index1 = 0; index1 < subtitleList.Count; index1++)
      {
        for (int index2 = 0; index2 < mediaList.Count; index2++)
        {
          if (subtitleList[index1].Equals(mediaList[index2], StringComparison.InvariantCultureIgnoreCase))
          {
            matchingPairs++;
            mediaList.RemoveAt(index2);
            break;
          }
        }
      }
      if (totalPairs == 0)
        return 0;

      return Convert.ToInt32(((double)matchingPairs / (double)totalPairs) * LETTER_PAIR_MATCH_WEIGHT_PCT);
    }

    private List<string> GetWordLetterPairs(string str)
    {
      List<string> list = new List<string>();
      string[] strArray = str.Split(TAG_SPLITTERS, StringSplitOptions.RemoveEmptyEntries);
      for (int index = 0; index < strArray.Length; ++index)
      {
        if (!string.IsNullOrEmpty(strArray[index]))
        {
          foreach (string str1 in GetLetterPairs(strArray[index]))
            list.Add(str1);
        }
      }
      return list;
    }

    private string[] GetLetterPairs(string str)
    {
      int length = str.Length - 1;
      string[] strArray = new string[length];
      for (int startIndex = 0; startIndex < length; startIndex++)
        strArray[startIndex] = str.Substring(startIndex, 2);
      return strArray;
    }

    #endregion

    #region Download

    /// <summary>
    /// Downloads the specified subtitle.
    /// </summary>
    /// <param name="subtitle">Subtitle to download.</param>
    public async Task<bool> DownloadSubtitleMatchesAsync(SubtitleInfo subtitle, bool overwriteExisting)
    {
      string[] subIds = new string[] { subtitle.SubtitleId };
      if (subtitle.SubtitleId.Contains(";"))
        subIds = subtitle.SubtitleId.Split(';');

      string[] subFileNames = new string[] { subtitle.Name };
      if (subtitle.Name.Contains(";"))
        subFileNames = subtitle.Name.Split(';');

      IDictionary<BaseSubtitleMatch<TId>, byte[]> subtitles = new Dictionary<BaseSubtitleMatch<TId>, byte[]>();
      for(int i = 0; i < subIds.Length; i++)
      {
        var clone = subtitle.Clone();
        clone.SubtitleId = subIds[i];
        if (subFileNames.Length > i)
          clone.Name = subFileNames[i];
        else
          clone.Name = subFileNames[0];
        var subs = await DownloadSubtitleAsync(clone);
        if (!(subs?.Count > 0))
          return false;

        foreach (var sub in subs)
          subtitles.Add(sub.Key, sub.Value);
      }

      if (await SaveSubtitleAsync(subtitle, subtitles, overwriteExisting))
        return true;

      return false;
    }

    protected virtual Task<IDictionary<BaseSubtitleMatch<TId>, byte[]>> DownloadSubtitleAsync(SubtitleInfo subtitle)
    {
      return Task.FromResult<IDictionary<BaseSubtitleMatch<TId>, byte[]>>(null);
    }

    protected virtual async Task<bool> SaveSubtitleAsync(SubtitleInfo subtitle, IDictionary<BaseSubtitleMatch<TId>, byte[]> downloads, bool overwriteExisting)
    {
      var mediaFile = subtitle.MediaFiles.First();
      var namingTemplate = ResourcePath.GetFileNameWithoutExtension(mediaFile.NativeResourcePath.FileName);
      var templatePartMatch = _regexMultiPartVideo.Match(namingTemplate);
      foreach (var subtitleMatch in downloads.Keys)
      {
        var subPartMatch = _regexMultiPartVideo.Match(subtitleMatch.ItemName);
        string subName = namingTemplate;
        if (subPartMatch.Success && templatePartMatch.Success)
        {
          if (!int.TryParse(templatePartMatch.Groups["disc"].Value, out var _) || !int.TryParse(subPartMatch.Groups["disc"].Value, out var partNum))
            continue;

          subName = _regexMultiPartVideo.Replace(namingTemplate, "${1}${2}${4}" + partNum + "${3}");
        }

        string lang = new CultureInfo(subtitleMatch.Language).EnglishName;
        var dir = ResourcePathHelper.GetDirectoryName(mediaFile.NativeResourcePath.Serialize());
        var sub = $"{subName}.{lang}{Path.GetExtension(subtitleMatch.ItemName)}";
        ResourcePath subtitlePath = ResourcePath.Deserialize(dir);

        //File based access
        var resLoc = new ResourceLocator(mediaFile.NativeSystemId, subtitlePath);
        using (IResourceAccessor mediaItemAccessor = resLoc.CreateAccessor())
        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
        using (rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess())
        {
          using (var stream = rah.LocalFsResourceAccessor.CreateOpenWrite(sub, overwriteExisting))
          {
            var bytes = downloads[subtitleMatch];
            if (stream != null)
              await stream.WriteAsync(bytes, 0, bytes.Length);
          }
        }
      }
      return true;
    }

    #endregion
  }
}
