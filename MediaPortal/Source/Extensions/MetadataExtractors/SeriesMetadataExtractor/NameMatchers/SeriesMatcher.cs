#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Extensions.OnlineLibraries;
using System.Collections.Generic;
using System.Globalization;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.Settings;

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

      // First do replacements before match
      foreach (var replacement in settings.Replacements.Where(r => r.BeforeMatch))
      {
        replacement.Replace(ref folderOrFileName);
      }

      foreach (var pattern in settings.SeriesPatterns)
      {
        // Calling EnsureLocalFileSystemAccess not necessary; only string operation
        Regex matcher;
        if (pattern.GetRegex(out matcher))
        {
          Match ma = matcher.Match(folderOrFileName);
          ParseSeries(ma, episodeInfo);
          if (episodeInfo.IsBaseInfoPresent)
          {
            // Do replacements after successful match
            foreach (var replacement in settings.Replacements.Where(r => !r.BeforeMatch))
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
              Match yearMa = settings.SeriesYearPattern.Regex.Match(episodeInfo.SeriesName.Text);
              if (yearMa.Success)
              {
                //episodeInfo.SeriesName = new SimpleTitle(EpisodeInfo.CleanupWhiteSpaces(yearMa.Groups[GROUP_SERIES].Value), episodeInfo.SeriesName.DefaultLanguage);
                MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeriesFirstAired, new DateTime(Convert.ToInt32(yearMa.Groups[GROUP_YEAR].Value), 1, 1));
              }
              yearMa = settings.SeriesYearPattern.Regex.Match(folderOrFileName);
              if (yearMa.Success)
              {
                MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeriesFirstAired, new DateTime(Convert.ToInt32(yearMa.Groups[GROUP_YEAR].Value), 1, 1));
              }
            }
            if (!episodeInfo.SeriesName.IsEmpty)
            {
              episodeInfo.SeriesName.Text = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(episodeInfo.SeriesName.Text);
            }
            if (!episodeInfo.EpisodeName.IsEmpty)
            {
              episodeInfo.EpisodeName.Text = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(episodeInfo.EpisodeName.Text);
            }
            return true;
          }
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
        episodeInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(episodeInfo.EpisodeNumbers, episodeNums, true);
      }
      return true;
    }
  }
}
