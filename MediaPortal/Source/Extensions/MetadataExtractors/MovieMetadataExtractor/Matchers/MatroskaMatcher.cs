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

using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using MediaPortal.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Matchers
{
  /// <summary>
  /// <see cref="MatroskaMatcher"/> tries to read a valid IMDB id from tags of Matroska files.
  /// </summary>
  public class MatroskaMatcher
  {
    public static async Task<string> TryMatchImdbIdAsync(ILocalFsResourceAccessor folderOrFileLfsra)
    {
      // Calling EnsureLocalFileSystemAccess not necessary; only string operation
      string extensionLower = StringUtils.TrimToEmpty(Path.GetExtension(folderOrFileLfsra.LocalFileSystemPath)).ToLower();
      if (!MatroskaConsts.MATROSKA_VIDEO_EXTENSIONS.Contains(extensionLower))
        return null;

      MatroskaBinaryReader mkvReader = new MatroskaBinaryReader(folderOrFileLfsra);
      // Add keys to be extracted to tags dictionary, matching results will returned as value
      Dictionary<string, IList<string>> tagsToExtract = MatroskaConsts.DefaultVideoTags;
      await mkvReader.ReadTagsAsync(tagsToExtract).ConfigureAwait(false);

      if (tagsToExtract[MatroskaConsts.TAG_MOVIE_IMDB_ID] != null)
      {
        foreach (string candidate in tagsToExtract[MatroskaConsts.TAG_MOVIE_IMDB_ID])
        {
          if (ImdbIdMatcher.TryMatchImdbId(candidate, out string imdbId))
            return imdbId;
        }
      }

      return null;
    }

    public static async Task<string> TryMatchTmdbIdAsync(ILocalFsResourceAccessor folderOrFileLfsra)
    {
      // Calling EnsureLocalFileSystemAccess not necessary; only string operation
      string extensionLower = StringUtils.TrimToEmpty(Path.GetExtension(folderOrFileLfsra.LocalFileSystemPath)).ToLower();
      if (!MatroskaConsts.MATROSKA_VIDEO_EXTENSIONS.Contains(extensionLower))
        return null;

      MatroskaBinaryReader mkvReader = new MatroskaBinaryReader(folderOrFileLfsra);
      // Add keys to be extracted to tags dictionary, matching results will returned as value
      Dictionary<string, IList<string>> tagsToExtract = MatroskaConsts.DefaultVideoTags;
      await mkvReader.ReadTagsAsync(tagsToExtract).ConfigureAwait(false);

      if (tagsToExtract[MatroskaConsts.TAG_MOVIE_TMDB_ID] != null)
      {
        foreach (string candidate in tagsToExtract[MatroskaConsts.TAG_MOVIE_TMDB_ID])
        {
          if (int.TryParse(candidate, out int tmdbIdInt))
            return tmdbIdInt.ToString();
        }
      }
      
      return null;
    }

    public static async Task<bool> ExtractFromTagsAsync(ILocalFsResourceAccessor folderOrFileLfsra, MovieInfo movieInfo)
    {
      // Calling EnsureLocalFileSystemAccess not necessary; only string operation
      string extensionLower = StringUtils.TrimToEmpty(Path.GetExtension(folderOrFileLfsra.LocalFileSystemPath)).ToLower();
      if (!MatroskaConsts.MATROSKA_VIDEO_EXTENSIONS.Contains(extensionLower))
        return false;

      // Try to get extended information out of matroska files)
      MatroskaBinaryReader mkvReader = new MatroskaBinaryReader(folderOrFileLfsra);
      // Add keys to be extracted to tags dictionary, matching results will returned as value
      Dictionary<string, IList<string>> tagsToExtract = MatroskaConsts.DefaultVideoTags;
      await mkvReader.ReadTagsAsync(tagsToExtract).ConfigureAwait(false);

      // Read plot
      IList<string> tags = tagsToExtract[MatroskaConsts.TAG_EPISODE_SUMMARY];
      string plot = tags != null ? tags.FirstOrDefault() : string.Empty;
      if (!string.IsNullOrEmpty(plot))
        movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref movieInfo.Summary, plot, true);

      // Read genre
      tags = tagsToExtract[MatroskaConsts.TAG_SERIES_GENRE];
      if (tags != null)
      {
        List<GenreInfo> genreList = tags.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList();
        movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(movieInfo.Genres, genreList, movieInfo.Genres.Count == 0);
      }

      // Read actors
      tags = tagsToExtract[MatroskaConsts.TAG_ACTORS];
      if (tags != null)
        movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(movieInfo.Actors,
          tags.Select(t => new PersonInfo() { Name = t, Occupation = PersonAspect.OCCUPATION_ACTOR, MediaName = movieInfo.MovieName.Text }).ToList(), false);

      tags = tagsToExtract[MatroskaConsts.TAG_DIRECTORS];
      if (tags != null)
        movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(movieInfo.Directors,
          tags.Select(t => new PersonInfo() { Name = t, Occupation = PersonAspect.OCCUPATION_DIRECTOR, MediaName = movieInfo.MovieName.Text }).ToList(), false);

      tags = tagsToExtract[MatroskaConsts.TAG_WRITTEN_BY];
      if (tags != null)
        movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(movieInfo.Writers,
          tags.Select(t => new PersonInfo() { Name = t, Occupation = PersonAspect.OCCUPATION_WRITER, MediaName = movieInfo.MovieName.Text }).ToList(), false);

      if (tagsToExtract[MatroskaConsts.TAG_MOVIE_IMDB_ID] != null)
      {
        string imdbId;
        foreach (string candidate in tagsToExtract[MatroskaConsts.TAG_MOVIE_IMDB_ID])
          if (ImdbIdMatcher.TryMatchImdbId(candidate, out imdbId))
          {
            movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref movieInfo.ImdbId, imdbId);
            break;
          }
      }
      if (tagsToExtract[MatroskaConsts.TAG_MOVIE_TMDB_ID] != null)
      {
        int tmp;
        foreach (string candidate in tagsToExtract[MatroskaConsts.TAG_MOVIE_TMDB_ID])
          if (int.TryParse(candidate, out tmp) == true)
          {
            movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateId(ref movieInfo.MovieDbId, tmp);
            break;
          }
      }

      return true;
    }
  }
}
