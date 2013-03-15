﻿#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

using System.Collections.Generic;
using System.Text.RegularExpressions;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.NameMatchers
{
  /// <summary>
  /// <see cref="SeriesMatcher"/> tries to match series episodes from file and folder names. It uses regular expressions to extract series name, 
  /// season number, episode number and optional episode title.
  /// </summary>
  public class SeriesMatcher
  {
    private const string GROUP_SERIES = "series";
    private const string GROUP_SEASONNUM = "seasonnum";
    private const string GROUP_EPISODENUM = "episodenum";
    private const string GROUP_EPISODE = "episode";

    protected static List<Regex> _matchers = new List<Regex>
        {
          // Filename only pattern
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
          // "Series.Name.101.Episode.Or.Release.Info", can lead to false matches for every filename with nnn included
          //new Regex(@"(?<series>[^\\]+).(?<seasonnum>\d{1})(?<episodenum>\d{2})(?<episode>.*)\.", RegexOptions.IgnoreCase),

          // Folder + filename pattern
          // "Series\1\11 - Episode" "Series\Staffel 2\11 - Episode" "Series\Season 3\12 Episode" "Series\3. Season\13-Episode"
          new Regex(@"(?<series>[^\\]*)\\[^\\|\d]*(?<seasonnum>\d+)\D*\\(?<episodenum>\d+)\s*-\s*(?<episode>[^\\]+)\.", RegexOptions.IgnoreCase),
        };

    /// <summary>
    /// Tries to match series by checking the <paramref name="folderOrFileName"/> for known patterns. The match is only successful,
    /// if the <see cref="SeriesInfo.IsCompleteMatch"/> is <c>true</c>.
    /// </summary>
    /// <param name="folderOrFileName">Full path to file</param>
    /// <param name="seriesInfo">Returns the parsed SeriesInfo</param>
    /// <returns><c>true</c> if successful.</returns>
    public bool MatchSeries(string folderOrFileName, out SeriesInfo seriesInfo)
    {
      foreach (Regex matcher in _matchers)
      {
        Match ma = matcher.Match(folderOrFileName);
        seriesInfo = ParseSeries(ma);
        if (seriesInfo.IsCompleteMatch)
          return true;
      }
      seriesInfo = null;
      return false;
    }

    static SeriesInfo ParseSeries(Match ma)
    {
      SeriesInfo info = new SeriesInfo();
      Group group = ma.Groups[GROUP_SERIES];
      if (group.Length > 0)
        info.Series = group.Value;

      group = ma.Groups[GROUP_EPISODE];
      if (group.Length > 0)
        info.Episode = group.Value;

      group = ma.Groups[GROUP_SEASONNUM];
      int tmpInt;
      if (group.Length > 0 && int.TryParse(group.Value, out tmpInt))
        info.SeasonNumber = tmpInt;

      // There can be multipe episode numbers in one file
      group = ma.Groups[GROUP_EPISODENUM];
      if (group.Length > 0)
      {
        foreach (Capture capture in group.Captures)
        {
          int episodeNum;
          if (int.TryParse(capture.Value, out episodeNum))
            info.EpisodeNumbers.Add(episodeNum);
        }
      }
      return info;
    }
  }
}
