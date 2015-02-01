#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using MediaPortal.Utilities;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.NameMatchers
{
  /// <summary>
  /// <see cref="MatroskaMatcher"/> tries to read series, season and episode metadata from matroska files.
  /// </summary>
  public class MatroskaMatcher
  {
    /// <summary>
    /// Tries to match series by reading matroska tags from <paramref name="folderOrFileName"/>.
    /// </summary>
    /// <param name="folderOrFileName">Full path to file</param>
    /// <param name="seriesInfo">Returns the parsed SeriesInfo</param>
    /// <param name="extractedAspectData">Dictionary containing a mapping of media item aspect ids to
    /// already present media item aspects, this metadata extractor should edit. If a media item aspect is not present
    /// in this dictionary but found by this metadata extractor, it will add it to the dictionary.</param>
    /// <returns><c>true</c> if successful.</returns>
    public bool MatchSeries(string folderOrFileName, out SeriesInfo seriesInfo, ref IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      string extensionLower = StringUtils.TrimToEmpty(Path.GetExtension(folderOrFileName)).ToLower();

      if (!MatroskaConsts.MATROSKA_VIDEO_EXTENSIONS.Contains(extensionLower))
      {
        seriesInfo = null;
        return false;
      }

      MatroskaInfoReader mkvReader = new MatroskaInfoReader(folderOrFileName);
      // Add keys to be extracted to tags dictionary, matching results will returned as value
      Dictionary<string, IList<string>> tagsToExtract = MatroskaConsts.DefaultTags;
      mkvReader.ReadTags(tagsToExtract);

      foreach(KeyValuePair<string, IList<string>> tag in tagsToExtract)
      {
        ServiceRegistration.Get<ILogger>().Debug("MatchSeries: MKV-Tag {0} Values are:", tag.Key);
        if (tag.Value != null)
        {
          foreach (string s in tag.Value)
            ServiceRegistration.Get<ILogger>().Debug("   {0}", s);
        }
        else
        {
          ServiceRegistration.Get<ILogger>().Debug("---NULL---");
        }
      }

      string title = string.Empty;
      IList<string> tags = tagsToExtract[MatroskaConsts.TAG_SIMPLE_TITLE];
      if (tags != null)
        title = tags.FirstOrDefault();

      if (!string.IsNullOrEmpty(title))
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, title);

      string yearCandidate = null;
      tags = tagsToExtract[MatroskaConsts.TAG_EPISODE_YEAR] ?? tagsToExtract[MatroskaConsts.TAG_SEASON_YEAR];
      if (tags != null)
        yearCandidate = (tags.FirstOrDefault() ?? string.Empty).Substring(0, 4);

      int year;
      if (int.TryParse(yearCandidate, out year))
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, new DateTime(year, 1, 1));

      tags = tagsToExtract[MatroskaConsts.TAG_EPISODE_SUMMARY];
      string plot = tags != null ? tags.FirstOrDefault() : string.Empty;
      if (!string.IsNullOrEmpty(plot))
        MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, plot);

      // Series and episode handling. Prefer information from tags.
      seriesInfo = GetSeriesFromTags(tagsToExtract);

      return true;
    }

    protected SeriesInfo GetSeriesFromTags(IDictionary<string, IList<string>> extractedTags)
    {
      SeriesInfo seriesInfo = new SeriesInfo();
      if (extractedTags[MatroskaConsts.TAG_EPISODE_TITLE] != null)
        seriesInfo.Episode = extractedTags[MatroskaConsts.TAG_EPISODE_TITLE].FirstOrDefault();

      if (extractedTags[MatroskaConsts.TAG_SERIES_TITLE] != null)
        seriesInfo.Series = extractedTags[MatroskaConsts.TAG_SERIES_TITLE].FirstOrDefault();

      if (extractedTags[MatroskaConsts.TAG_SERIES_IMDB_ID] != null)
      {
        string imdbId;
        foreach (string candidate in extractedTags[MatroskaConsts.TAG_SERIES_IMDB_ID])
          if (ImdbIdMatcher.TryMatchImdbId(candidate, out imdbId))
          { 
            seriesInfo.ImdbId = imdbId; 
            break;
          }
      }

      // On Series, the counting tag is "TVDB"
      if (extractedTags[MatroskaConsts.TAG_SERIES_TVDB_ID] != null)
      {
        int tmp;
        foreach (string candidate in extractedTags[MatroskaConsts.TAG_SERIES_TVDB_ID])
          if(int.TryParse(candidate, out tmp) == true)
          {
            seriesInfo.TvdbId = tmp;
            break;
          }
      }

      int tmpInt;
      if (extractedTags[MatroskaConsts.TAG_SEASON_NUMBER] != null && int.TryParse(extractedTags[MatroskaConsts.TAG_SEASON_NUMBER].FirstOrDefault(), out tmpInt))
        seriesInfo.SeasonNumber = tmpInt; 

      if (extractedTags[MatroskaConsts.TAG_EPISODE_NUMBER] != null)
      {
        int episodeNum;

        foreach (string s in extractedTags[MatroskaConsts.TAG_EPISODE_NUMBER])
          if (int.TryParse(s, out episodeNum))
            if (!seriesInfo.EpisodeNumbers.Contains(episodeNum))
              seriesInfo.EpisodeNumbers.Add(episodeNum);
      }
      return seriesInfo;
    }
  }
}
