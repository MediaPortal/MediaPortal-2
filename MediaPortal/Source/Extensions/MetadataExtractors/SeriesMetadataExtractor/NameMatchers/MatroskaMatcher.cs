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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using MediaPortal.Utilities;
using MediaPortal.Extensions.OnlineLibraries;
using System.Globalization;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.NameMatchers
{
  /// <summary>
  /// <see cref="MatroskaMatcher"/> tries to read series, season and episode metadata from matroska files.
  /// </summary>
  public class MatroskaMatcher
  {
    /// <summary>
    /// Tries to match series by reading matroska tags from <paramref name="folderOrFileLfsra"/>.
    /// </summary>
    /// <param name="folderOrFileLfsra"><see cref="ILocalFsResourceAccessor"/> to file or folder</param>
    /// <param name="episodeInfo">Returns the parsed EpisodeInfo</param>
    /// <param name="extractedAspectData">Dictionary containing a mapping of media item aspect ids to
    /// already present media item aspects, this metadata extractor should edit. If a media item aspect is not present
    /// in this dictionary but found by this metadata extractor, it will add it to the dictionary.</param>
    /// <returns><c>true</c> if successful.</returns>
    public bool MatchSeries(ILocalFsResourceAccessor folderOrFileLfsra, EpisodeInfo episodeInfo)
    {
      // Calling EnsureLocalFileSystemAccess not necessary; only string operation
      string extensionLower = StringUtils.TrimToEmpty(Path.GetExtension(folderOrFileLfsra.LocalFileSystemPath)).ToLower();

      if (!MatroskaConsts.MATROSKA_VIDEO_EXTENSIONS.Contains(extensionLower))
      {
        return false;
      }

      MatroskaInfoReader mkvReader = new MatroskaInfoReader(folderOrFileLfsra);
      // Add keys to be extracted to tags dictionary, matching results will returned as value
      Dictionary<string, IList<string>> tagsToExtract = MatroskaConsts.DefaultTags;
      mkvReader.ReadTags(tagsToExtract);

      IList<string> tags = tagsToExtract[MatroskaConsts.TAG_EPISODE_SUMMARY];
      string plot = tags != null ? tags.FirstOrDefault() : string.Empty;
      if (!string.IsNullOrEmpty(plot))
        episodeInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, plot, true);

      // Series and episode handling. Prefer information from tags.
      if (tagsToExtract[MatroskaConsts.TAG_EPISODE_TITLE] != null)
      {
        string title = tagsToExtract[MatroskaConsts.TAG_EPISODE_TITLE].FirstOrDefault();
        if (!string.IsNullOrEmpty(title))
        {
          title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(title);
          episodeInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref episodeInfo.EpisodeName, title, true);
        }
      }

      if (tagsToExtract[MatroskaConsts.TAG_SERIES_TITLE] != null)
      {
        string title = tagsToExtract[MatroskaConsts.TAG_SERIES_TITLE].FirstOrDefault();
        if (!string.IsNullOrEmpty(title))
        {
          title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(title);
          episodeInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref episodeInfo.SeriesName, title, true);
        }
      }

      if (tagsToExtract[MatroskaConsts.TAG_SERIES_IMDB_ID] != null)
      {
        string imdbId;
        foreach (string candidate in tagsToExtract[MatroskaConsts.TAG_SERIES_IMDB_ID])
          if (ImdbIdMatcher.TryMatchImdbId(candidate, out imdbId))
          {
            episodeInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesImdbId, imdbId);
            break;
          }
      }

      if (tagsToExtract[MatroskaConsts.TAG_SERIES_ACTORS] != null)
      {
        episodeInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(episodeInfo.Actors,
          tagsToExtract[MatroskaConsts.TAG_SERIES_ACTORS].Select(t => new PersonInfo() { Name = t, Occupation = PersonAspect.OCCUPATION_ACTOR,
            MediaName = episodeInfo.EpisodeName.Text, ParentMediaName = episodeInfo.SeriesName.Text }).ToList(), false);
      }

      // On Series, the counting tag is "TVDB"
      if (tagsToExtract[MatroskaConsts.TAG_SERIES_TVDB_ID] != null)
      {
        int tmp;
        foreach (string candidate in tagsToExtract[MatroskaConsts.TAG_SERIES_TVDB_ID])
          if (int.TryParse(candidate, out tmp) == true)
          {
            episodeInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvdbId, tmp);
            break;
          }
      }

      int tmpInt;
      if (tagsToExtract[MatroskaConsts.TAG_SEASON_NUMBER] != null && int.TryParse(tagsToExtract[MatroskaConsts.TAG_SEASON_NUMBER].FirstOrDefault(), out tmpInt))
        episodeInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, tmpInt);

      if (tagsToExtract[MatroskaConsts.TAG_EPISODE_NUMBER] != null)
      {
        int episodeNum;

        foreach (string s in tagsToExtract[MatroskaConsts.TAG_EPISODE_NUMBER])
          if (int.TryParse(s, out episodeNum))
            if (!episodeInfo.EpisodeNumbers.Contains(episodeNum))
              episodeInfo.EpisodeNumbers.Add(episodeNum);
      }

      return true;
    }
  }
}
