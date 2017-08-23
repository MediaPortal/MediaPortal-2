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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using MediaPortal.Utilities;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.OnlineLibraries;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Matchers
{
  /// <summary>
  /// <see cref="MatroskaMatcher"/> tries to read a valid IMDB id from tags of Matroska files.
  /// </summary>
  public class MatroskaMatcher
  {
    public static bool TryMatchImdbId(ILocalFsResourceAccessor folderOrFileLfsra, out string imdbId)
    {
      // Calling EnsureLocalFileSystemAccess not necessary; only string operation
      string extensionLower = StringUtils.TrimToEmpty(Path.GetExtension(folderOrFileLfsra.LocalFileSystemPath)).ToLower();
      if (!MatroskaConsts.MATROSKA_VIDEO_EXTENSIONS.Contains(extensionLower))
      {
        imdbId = null;
        return false;
      }

      MatroskaInfoReader mkvReader = new MatroskaInfoReader(folderOrFileLfsra);
      // Add keys to be extracted to tags dictionary, matching results will returned as value
      Dictionary<string, IList<string>> tagsToExtract = MatroskaConsts.DefaultTags;
      mkvReader.ReadTags(tagsToExtract);

      if (tagsToExtract[MatroskaConsts.TAG_MOVIE_IMDB_ID] != null)
      {
        foreach (string candidate in tagsToExtract[MatroskaConsts.TAG_MOVIE_IMDB_ID])
        {
          if (ImdbIdMatcher.TryMatchImdbId(candidate, out imdbId))
            return true;
        }
      }

      imdbId = null;
      return false;
    }

    public static bool TryMatchTmdbId(ILocalFsResourceAccessor folderOrFileLfsra, out string tmdbId)
    {
      // Calling EnsureLocalFileSystemAccess not necessary; only string operation
      string extensionLower = StringUtils.TrimToEmpty(Path.GetExtension(folderOrFileLfsra.LocalFileSystemPath)).ToLower();
      if (!MatroskaConsts.MATROSKA_VIDEO_EXTENSIONS.Contains(extensionLower))
      {
        tmdbId = "0";
        return false;
      }

      MatroskaInfoReader mkvReader = new MatroskaInfoReader(folderOrFileLfsra);
      // Add keys to be extracted to tags dictionary, matching results will returned as value
      Dictionary<string, IList<string>> tagsToExtract = MatroskaConsts.DefaultTags;
      mkvReader.ReadTags(tagsToExtract);

      if (tagsToExtract[MatroskaConsts.TAG_MOVIE_TMDB_ID] != null)
      {
        foreach (string candidate in tagsToExtract[MatroskaConsts.TAG_MOVIE_TMDB_ID])
        {
          int tmdbIdInt;
          if (int.TryParse(candidate, out tmdbIdInt))
          {
            tmdbId = tmdbIdInt.ToString();
            return true;
          }
        }
      }

      tmdbId = "0";
      return false;
    }

    public static bool ExtractFromTags(ILocalFsResourceAccessor folderOrFileLfsra, MovieInfo movieInfo)
    {
      // Calling EnsureLocalFileSystemAccess not necessary; only string operation
      string extensionLower = StringUtils.TrimToEmpty(Path.GetExtension(folderOrFileLfsra.LocalFileSystemPath)).ToLower();
      if (!MatroskaConsts.MATROSKA_VIDEO_EXTENSIONS.Contains(extensionLower))
        return false;

      // Try to get extended information out of matroska files)
      MatroskaInfoReader mkvReader = new MatroskaInfoReader(folderOrFileLfsra);
      // Add keys to be extracted to tags dictionary, matching results will returned as value
      Dictionary<string, IList<string>> tagsToExtract = MatroskaConsts.DefaultTags;
      mkvReader.ReadTags(tagsToExtract);

      // Read plot
      IList<string> tags = tagsToExtract[MatroskaConsts.TAG_EPISODE_SUMMARY];
      string plot = tags != null ? tags.FirstOrDefault() : string.Empty;
      if (!string.IsNullOrEmpty(plot))
        movieInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref movieInfo.Summary, plot, true);

      // Read genre
      tags = tagsToExtract[MatroskaConsts.TAG_SERIES_GENRE];
      if (tags != null)
      {
        List<GenreInfo> genreList = tags.Select(s => new GenreInfo { Name = s }).ToList();
        OnlineMatcherService.Instance.AssignMissingMovieGenreIds(genreList);
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
