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
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Matchers
{
  /// <summary>
  /// <see cref="MP4Matcher"/> tries to read tags of MP4 files.
  /// </summary>
  public class MP4Matcher
  {
    public static bool ExtractFromTags(ILocalFsResourceAccessor folderOrFileLfsra, MovieInfo movieInfo)
    {
      // Calling EnsureLocalFileSystemAccess not necessary; only string operation
      string extensionUpper = StringUtils.TrimToEmpty(Path.GetExtension(folderOrFileLfsra.LocalFileSystemPath)).ToUpper();

      // Try to get extended information out of MP4 files)
      if (extensionUpper != ".MP4") return false;

      using (folderOrFileLfsra.EnsureLocalFileSystemAccess())
      {
        TagLib.File mp4File = TagLib.File.Create(folderOrFileLfsra.LocalFileSystemPath);
        if (ReferenceEquals(mp4File, null) || ReferenceEquals(mp4File.Tag, null))
          return false;

        TagLib.Tag tag = mp4File.Tag;

        if (!ReferenceEquals(tag.Genres, null) && tag.Genres.Length > 0)
          MetadataUpdater.SetOrUpdateList(movieInfo.Genres, new List<string>(tag.Genres), false);

        if (!ReferenceEquals(tag.Performers, null) && tag.Performers.Length > 0)
          MetadataUpdater.SetOrUpdateList(movieInfo.Actors,
            tag.Performers.Select(t => new PersonInfo() { Name = t, Occupation = PersonAspect.OCCUPATION_ACTOR }).ToList(), false);

        return true;
      }
    }
  }
}
