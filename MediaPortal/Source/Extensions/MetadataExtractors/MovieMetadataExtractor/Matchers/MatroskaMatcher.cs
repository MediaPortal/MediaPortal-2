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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using MediaPortal.Utilities;

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

    public static bool TryMatchTmdbId(ILocalFsResourceAccessor folderOrFileLfsra, out int tmdbId)
    {
      // Calling EnsureLocalFileSystemAccess not necessary; only string operation
      string extensionLower = StringUtils.TrimToEmpty(Path.GetExtension(folderOrFileLfsra.LocalFileSystemPath)).ToLower();
      if (!MatroskaConsts.MATROSKA_VIDEO_EXTENSIONS.Contains(extensionLower))
      {
        tmdbId = 0;
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
          if (int.TryParse(candidate, out tmdbId))
            return true;
        }
      }

      tmdbId = 0;
      return false;
    }
  }
}
