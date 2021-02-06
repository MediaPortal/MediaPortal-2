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

using System;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using System.Collections.Generic;
using System.Globalization;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.Settings;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.NameMatchers
{
  /// <summary>
  /// <see cref="SeriesMatcher"/> tries to match series episodes from file and folder names. It uses regular expressions to extract series name, 
  /// season number, episode number and optional episode title.
  /// </summary>
  public class SeriesMatcher
  {
    public static readonly IList<Replacement> REGEXP_REPLACEMENTS = new List<Replacement>
    {
      new Replacement { Enabled = true, BeforeMatch = true, Pattern = "720p", ReplaceBy = "", IsRegex = false },
      new Replacement { Enabled = true, BeforeMatch = true, Pattern = "1080i", ReplaceBy = "", IsRegex = false },
      new Replacement { Enabled = true, BeforeMatch = true, Pattern = "1080p", ReplaceBy = "", IsRegex = false },
      new Replacement { Enabled = true, BeforeMatch = true, Pattern = "x264", ReplaceBy = "", IsRegex = false },
      new Replacement { Enabled = true, BeforeMatch = true, Pattern = @"(?<!(?:S\d+.?E\\d+\-E\d+.*|S\d+.?E\d+.*|\s\d+x\d+.*))P[ar]*t[\s|\.|\-|_]?(\d+)(\s?of\s\d{1,2})?", ReplaceBy = "S01E${1}", IsRegex = true },
    };

    public static readonly IList<Regex> REGEXP_SERIES = new List<Regex>
    {
      // Multi-episodes pattern
      // "Series S1E01-E02 - Episodes"
      new Regex(@"(?<series>[^\\]+)\WS(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)[\-_]?)+E(?<endepisodenum>\d+)+ - (?<episode>.*)\.", RegexOptions.IgnoreCase),
      // "Series.Name.S01E01-E02.Episode.Or.Release.Info"
      new Regex(@"(?<series>[^\\]+).S(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)[\-|_]?)+E(?<endepisodenum>\d+)+(?<episode>.*)\.", RegexOptions.IgnoreCase),
      // Series\Season...\S01E01-E02* or Series\Season...\1x01-02*
      new Regex(@"(?<series>[^\\]*)\\[^\\]*(?<seasonnum>\d+)[^\\]*\\S*(?<seasonnum>\d+)[EX]((?<episodenum>\d+)[\-|_]?)+[EX](?<endepisodenum>\d+)*(?<episode>.*)\.", RegexOptions.IgnoreCase),

      // Series\Season...\S01E01* or Series\Season...\1x01*
      new Regex(@"(?<series>[^\\]*)\\[^\\]*(?<seasonnum>\d+)[^\\]*\\S*(?<seasonnum>\d+)[EX](?<episodenum>\d+)*(?<episode>.*)\.", RegexOptions.IgnoreCase),
      // MP1 EpisodeScanner recommendations for recordings: Series - (Episode) S1E1, also "S1 E1", "S1-E1", "S1.E1", "S1_E1"
      new Regex(@"(?<series>[^\\]+) - \((?<episode>.*)\) S(?<seasonnum>[0-9]+?)[\s|\.|\-|_]{0,1}E(?<episodenum>[0-9]+?)", RegexOptions.IgnoreCase),
      // "Series 1x1 - Episode" and multi-episodes "Series 1x1_2 - Episodes"
      new Regex(@"(?<series>[^\\]+)\W(?<seasonnum>\d+)x((?<episodenum>\d+)_?)+ - (?<episode>.*)\.", RegexOptions.IgnoreCase),
      // "Series S1E01 - Episode" and multi-episodes "Series S1E01_02 - Episodes", also "S1 E1", "S1-E1", "S1.E1", "S1_E1"
      new Regex(@"(?<series>[^\\]+)\WS(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)_?)+ - (?<episode>.*)\.", RegexOptions.IgnoreCase),
      // "Series.Name.1x01.Episode.Or.Release.Info"
      new Regex(@"(?<series>[^\\]+).(?<seasonnum>\d+)x((?<episodenum>\d+)_?)+(?<episode>.*)\.", RegexOptions.IgnoreCase),
      // "Series.Name.S01E01.Episode.Or.Release.Info", also "S1 E1", "S1-E1", "S1.E1", "S1_E1"
      new Regex(@"(?<series>[^\\]+).S(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)_?)+(?<episode>.*)\.", RegexOptions.IgnoreCase),

      // Folder + filename pattern
      // "Series\1\11 - Episode" "Series\Staffel 2\11 - Episode" "Series\Season 3\12 Episode" "Series\3. Season\13-Episode"
      new Regex(@"(?<series>[^\\]*)\\[^\\|\d]*(?<seasonnum>\d+)\D*\\(?<episodenum>\d+)\s*-\s*(?<episode>[^\\]+)\.", RegexOptions.IgnoreCase),
      // "Series.Name.101.Episode.Or.Release.Info", attention: this expression can lead to false matches for every filename with nnn included
      new Regex(@"(?<series>[^\\]+)\D(?<seasonnum>\d{1})(?<episodenum>\d{2})\D(?<episode>.*)\.", RegexOptions.IgnoreCase),
    };

    public static readonly IList<Regex> REGEXP_SERIES_YEAR = new List<Regex>
    {
      //new Regex(@"(?<series>.*)[( .-_]+(?<year>\d{4})", RegexOptions.IgnoreCase),
      new Regex(@"(?<series>[^\\|\/]+?\s*[(\.|\s)(19|20)\d{2}]*)[\.|\s][\[\(]?(?<year>(19|20)\d{2})[\]\)]?[\.|\\|\/]*", RegexOptions.IgnoreCase)
    };

    private const string GROUP_SERIES = "series";
    private const string GROUP_SEASONNUM = "seasonnum";
    private const string GROUP_EPISODENUM = "episodenum";
    private const string GROUP_ENDEPISODENUM = "endepisodenum";
    private const string GROUP_EPISODE = "episode";
    private const string GROUP_YEAR = "year";

    /// <summary>
    /// Tries to match series by checking the <paramref name="folderOrFileLfsra"/> for known patterns. The match is only successful,
    /// if the <see cref="EpisodeInfo.IsBaseInfoPresent"/> is <c>true</c>.
    /// </summary>
    /// <param name="folderOrFileLfsra"><see cref="ILocalFsResourceAccessor"/> to file</param>
    /// <param name="episodeInfo">Returns the parsed EpisodeInfo</param>
    /// <returns><c>true</c> if successful.</returns>
    public bool MatchSeries(ILocalFsResourceAccessor folderOrFileLfsra, EpisodeInfo episodeInfo)
    {
      return MatchSeries(folderOrFileLfsra.LocalFileSystemPath, episodeInfo);
    }

    /// <summary>
    /// Tries to match series by checking the <paramref name="folderOrFileName"/> for known patterns. The match is only successful,
    /// if the <see cref="EpisodeInfo.IsBaseInfoPresent"/> is <c>true</c>.
    /// </summary>
    /// <param name="folderOrFileName">Full path to file</param>
    /// <param name="episodeInfo">Returns the parsed EpisodeInfo</param>
    /// <returns><c>true</c> if successful.</returns>
    public bool MatchSeries(string folderOrFileName, EpisodeInfo episodeInfo)
    {
      var settings = ServiceRegistration.Get<ISettingsManager>().Load<SeriesMetadataExtractorSettings>();

      // General cleanup for remote mounted resources (will be a local path)
      string remoteResourcePattern = @".*\\RemoteResources\\[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\\";
      Regex re = new Regex(remoteResourcePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
      folderOrFileName = re.Replace(folderOrFileName, "");

      List<Replacement> replacements = new List<Replacement>();
      if (settings.ReplacementPatternUsage == PatternUsageMode.UseInternal || settings.ReplacementPatternUsage == PatternUsageMode.UseInternalAndSettings)
        replacements.AddRange(REGEXP_REPLACEMENTS);
      if (settings.ReplacementPatternUsage == PatternUsageMode.UseSettings || settings.ReplacementPatternUsage == PatternUsageMode.UseInternalAndSettings)
        replacements.AddRange(settings.Replacements);

      List<Regex> titleYearRegexes = new List<Regex>();
      if (settings.SeriesYearPatternUsage == PatternUsageMode.UseInternal || settings.SeriesYearPatternUsage == PatternUsageMode.UseInternalAndSettings)
        titleYearRegexes.AddRange(REGEXP_SERIES_YEAR);
      if (settings.SeriesYearPatternUsage == PatternUsageMode.UseSettings || settings.SeriesYearPatternUsage == PatternUsageMode.UseInternalAndSettings)
        titleYearRegexes.AddRange(settings.SeriesYearPatterns.Select(p => p.GetRegex(out var regex) ? regex : null).Where(r => r != null));

      List<Regex> episodeRegexes = new List<Regex>();
      if (settings.SeriesPatternUsage == PatternUsageMode.UseInternal || settings.SeriesPatternUsage == PatternUsageMode.UseInternalAndSettings)
        episodeRegexes.AddRange(REGEXP_SERIES);
      if (settings.SeriesPatternUsage == PatternUsageMode.UseSettings || settings.SeriesPatternUsage == PatternUsageMode.UseInternalAndSettings)
        episodeRegexes.AddRange(settings.SeriesPatterns.Select(p => p.GetRegex(out var regex) ? regex : null).Where(r => r != null));

      // First do replacements before match
      foreach (var replacement in replacements.Where(r => r.BeforeMatch && r.Enabled))
      {
        replacement.Replace(ref folderOrFileName);
      }

      foreach (var pattern in episodeRegexes)
      {
        // Calling EnsureLocalFileSystemAccess not necessary; only string operation
        Match ma = pattern.Match(folderOrFileName);
        ParseSeries(ma, episodeInfo);
        if (episodeInfo.IsBaseInfoPresent)
        {
          // Do replacements after successful match
          foreach (var replacement in replacements.Where(r => !r.BeforeMatch && r.Enabled))
          {
            string tmp;
            if (!episodeInfo.SeriesName.IsEmpty)
            {
              tmp = episodeInfo.SeriesName.Text;
              replacement.Replace(ref tmp);
              episodeInfo.SeriesName.Text = tmp;
            }

            if (!episodeInfo.EpisodeName.IsEmpty)
            {
              tmp = episodeInfo.EpisodeName.Text;
              replacement.Replace(ref tmp);
              episodeInfo.EpisodeName.Text = tmp;
            }
          }

          if (!episodeInfo.SeriesName.IsEmpty)
          {
            foreach (var regex in titleYearRegexes)
            {
              Match yearMa = regex.Match(episodeInfo.SeriesName.Text);
              if (yearMa.Success)
              {
                //episodeInfo.SeriesName = new SimpleTitle(EpisodeInfo.CleanupWhiteSpaces(yearMa.Groups[GROUP_SERIES].Value), episodeInfo.SeriesName.DefaultLanguage);
                MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeriesFirstAired, new DateTime(Convert.ToInt32(yearMa.Groups[GROUP_YEAR].Value), 1, 1));
                break;
              }

              yearMa = regex.Match(folderOrFileName);
              if (yearMa.Success)
              {
                int year = Convert.ToInt32(yearMa.Groups[GROUP_YEAR].Value);
                if (year >= 1940 && year <= (DateTime.Now.Year + 1) && !folderOrFileName.EndsWith(year.ToString())) //It is a valid year and not the episode title
                {
                  MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeriesFirstAired, new DateTime(year, 1, 1));
                  break;
                }
              }
            }
          }

          return true;
        }
      }

      return false;
    }

    static bool ParseSeries(Match ma, EpisodeInfo episodeInfo)
    {
      if (!ma.Success)
        return false;

      Group group = ma.Groups[GROUP_SERIES];
      if (group.Length > 0)
        episodeInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref episodeInfo.SeriesName, EpisodeInfo.CleanupWhiteSpaces(group.Value), true);

      group = ma.Groups[GROUP_EPISODE];
      if (group.Length > 0)
        episodeInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref episodeInfo.EpisodeName, EpisodeInfo.CleanupWhiteSpaces(group.Value), true);

      group = ma.Groups[GROUP_SEASONNUM];
      int tmpInt;
      if (group.Length > 0 && int.TryParse(group.Value, out tmpInt))
        episodeInfo.SeasonNumber = tmpInt;

      // There can be multiple episode numbers in one file
      group = ma.Groups[GROUP_EPISODENUM];
      if (group.Length > 0)
      {
        List<int> episodeNums = new List<int>();
        if (group.Captures.Count > 1)
        {
          foreach (Capture capture in group.Captures)
          {
            int episodeNum;
            if (int.TryParse(capture.Value, out episodeNum))
              episodeNums.Add(episodeNum);
          }
        }
        else if(ma.Groups[GROUP_ENDEPISODENUM].Length > 0)
        {
          int start;
          if (int.TryParse(group.Value, out start))
          {
            int end;
            group = ma.Groups[GROUP_ENDEPISODENUM];
            if (group.Length > 0 && int.TryParse(group.Value, out end))
            {
              for(int episode = start; episode <= end; episode++)
              {
                episodeNums.Add(episode);
              }
            }
          }
        }
        else
        {
          foreach (Capture capture in group.Captures)
          {
            int episodeNum;
            if (int.TryParse(capture.Value, out episodeNum))
              episodeNums.Add(episodeNum);
          }
        }
        if (episodeNums.Count > 0 && !episodeInfo.EpisodeNumbers.SequenceEqual(episodeNums))
        {
          episodeInfo.HasChanged = true;
          episodeInfo.EpisodeNumbers = new List<int>(episodeNums);
        }
      }
      return true;
    }
  }
}
