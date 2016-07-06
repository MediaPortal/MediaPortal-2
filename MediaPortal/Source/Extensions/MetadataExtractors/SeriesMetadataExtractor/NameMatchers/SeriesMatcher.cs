#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;

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

    /// <summary>
    /// Tries to match series by checking the <paramref name="folderOrFileLfsra"/> for known patterns. The match is only successful,
    /// if the <see cref="SeriesInfo.IsCompleteMatch"/> is <c>true</c>.
    /// </summary>
    /// <param name="folderOrFileLfsra"><see cref="ILocalFsResourceAccessor"/> to file</param>
    /// <param name="seriesInfo">Returns the parsed SeriesInfo</param>
    /// <returns><c>true</c> if successful.</returns>
    public bool MatchSeries(ILocalFsResourceAccessor folderOrFileLfsra, out SeriesInfo seriesInfo)
    {
      return MatchSeries(folderOrFileLfsra.LocalFileSystemPath, out seriesInfo);
    }

    /// <summary>
    /// Tries to match series by checking the <paramref name="folderOrFileName"/> for known patterns. The match is only successful,
    /// if the <see cref="SeriesInfo.IsCompleteMatch"/> is <c>true</c>.
    /// </summary>
    /// <param name="folderOrFileName">Full path to file</param>
    /// <param name="seriesInfo">Returns the parsed SeriesInfo</param>
    /// <returns><c>true</c> if successful.</returns>
    public bool MatchSeries(string folderOrFileName, out SeriesInfo seriesInfo)
    {
      var settings = ServiceRegistration.Get<ISettingsManager>().Load<SeriesMetadataExtractorSettings>();

      // First do replacements before match
      foreach (var replacement in settings.Replacements.Where(r => r.BeforeMatch))
      {
        replacement.Replace(ref folderOrFileName);
      }

      foreach (var pattern in settings.Patterns)
      {
        // Calling EnsureLocalFileSystemAccess not necessary; only string operation
        Regex matcher;
        if (pattern.GetRegex(out matcher))
        {
          Match ma = matcher.Match(folderOrFileName);
          seriesInfo = ParseSeries(ma);
          if (seriesInfo.IsCompleteMatch)
          {
            // Do replacements after successful match
            foreach (var replacement in settings.Replacements.Where(r => !r.BeforeMatch))
            {
              string tmp = seriesInfo.Series;
              replacement.Replace(ref tmp);
              seriesInfo.Series = tmp;

              tmp = seriesInfo.Episode;
              replacement.Replace(ref tmp);
              seriesInfo.Episode = tmp;
            }
            return true;
          }
        }
      }
      seriesInfo = null;
      return false;
    }

    static SeriesInfo ParseSeries(Match ma)
    {
      SeriesInfo info = new SeriesInfo();
      Group group = ma.Groups[GROUP_SERIES];
      if (group.Length > 0)
        info.Series = SeriesInfo.CleanupWhiteSpaces(group.Value);

      group = ma.Groups[GROUP_EPISODE];
      if (group.Length > 0)
        info.Episode = SeriesInfo.CleanupWhiteSpaces(group.Value);

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
