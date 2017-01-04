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
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Matchers
{
  /// <summary>
  /// <see cref="NfoReader"/> tries to read a valid IMDB id from additional .nfo or .txt files.
  /// </summary>
  /// <remarks>ToDo: Remove ths class once the NfoMetadataExtractors plugin has proven to be stable</remarks>
  [Obsolete("Functionaility now contained in NfoMetadataExtractors plugin")]
  public class NfoReader
  {
    protected static string[] NFO_EXTENSIONS = new[] { ".nfo", ".txt" };
    protected static string[] NFO_FILENAMES = new[] { "movie.nfo" };

    /// <summary>
    /// Tries to read a valid IMDB id from additional .nfo or .txt files.
    /// </summary>
    /// <param name="fsra">FileSystemResourceAccessor</param>
    /// <param name="imdbId">Returns a valid IMDB or <c>null</c></param>
    /// <returns>true if matched</returns>
    public static bool TryMatchImdbId(IFileSystemResourceAccessor fsra, out string imdbId)
    {
      imdbId = null;
      if (fsra == null)
        return false;

      // First try to find a nfo file that has the same name as our main movie.
      if (fsra.IsFile)
        foreach (string extension in NFO_EXTENSIONS)
        {
          string metaFilePath = ResourcePathHelper.ChangeExtension(fsra.CanonicalLocalResourcePath.ToString(), extension);
          if (TryRead(metaFilePath, out imdbId))
            return true;
        }

      // Prepare a list of paths to check: for chained resource path we will also check relative parent paths (like for DVD-ISO files)
      List<string> pathsToCheck = new List<string> { fsra.CanonicalLocalResourcePath.ToString() };
      if (fsra.CanonicalLocalResourcePath.PathSegments.Count > 1)
      {
        string canocialPath = fsra.CanonicalLocalResourcePath.ToString();
        pathsToCheck.Add(canocialPath.Substring(0, canocialPath.LastIndexOf('>')));
      }

      // Then test for special named files, like "movie.nfo"
      foreach (string path in pathsToCheck)
        foreach (string fileName in NFO_FILENAMES)
        {
          string metaFilePath = ResourcePathHelper.GetDirectoryName(path);
          metaFilePath = ResourcePathHelper.Combine(metaFilePath, fileName);
          if (TryRead(metaFilePath, out imdbId))
            return true;
        }

      // Now check siblings of movie for any IMDB id containing filename.
      // Morpheus_xx, 2014-01-04: disabled code because it leads to false detections if there are multiple video files in same folder. In this case the first
      // video with TT-number is wrongly used.
      // TODO: this part could be reworked based on different ideas:
      // - Exclude known video extensions from file name matching (this would require a reference to VideoMDE's settings for extension list)
      // - Only use folder lookup for chained resources, i.e. for a DVD-ISO, where any "TT000000.bla" is located next to it

      //IFileSystemResourceAccessor directoryFsra = null;
      //if (!fsra.IsFile)
      //  directoryFsra = fsra.Clone() as IFileSystemResourceAccessor;
      //if (fsra.IsFile)
      //  directoryFsra = GetContainingDirectory(fsra);

      //if (directoryFsra == null)
      //  return false;

      //using (directoryFsra)
      //  foreach (IFileSystemResourceAccessor file in directoryFsra.GetFiles())
      //    using (file)
      //      if (ImdbIdMatcher.TryMatchImdbId(file.ResourceName, out imdbId))
      //        return true;

      return false;
    }

    /// <summary>
    /// Tries to create a <see cref="IFileSystemResourceAccessor"/> for the directory of a given file.
    /// </summary>
    /// <param name="fsra">IFileSystemResourceAccessor of file.</param>
    /// <returns>IFileSystemResourceAccessor or <c>null</c>, if no fsra could be created.</returns>
    private static IFileSystemResourceAccessor GetContainingDirectory(IFileSystemResourceAccessor fsra)
    {
      string filePath = fsra.CanonicalLocalResourcePath.ToString();
      string directoryPath = ResourcePathHelper.GetDirectoryName(filePath);
      IResourceAccessor metaFileAccessor;
      if (!ResourcePath.Deserialize(directoryPath).TryCreateLocalResourceAccessor(out metaFileAccessor))
        return null;
      if (!(metaFileAccessor is IFileSystemResourceAccessor))
      {
        metaFileAccessor.Dispose();
        return null;
      }
      return (IFileSystemResourceAccessor) metaFileAccessor;
    }

    /// <summary>
    /// Tries to create a LocalResourceAccessor for the given <paramref name="metaFilePath"/> and to read the contents to match the IMDB id.
    /// </summary>
    /// <param name="metaFilePath">Path to file</param>
    /// <param name="imdbId">Returns a valid IMDB or <c>null</c></param>
    /// <returns>true if matched</returns>
    private static bool TryRead(string metaFilePath, out string imdbId)
    {
      imdbId = null;
      IResourceAccessor metaFileAccessor;
      if (!ResourcePath.Deserialize(metaFilePath).TryCreateLocalResourceAccessor(out metaFileAccessor))
        return false;

      using (metaFileAccessor)
      {
        ILocalFsResourceAccessor lfsra = metaFileAccessor as ILocalFsResourceAccessor;
        if (lfsra == null || !lfsra.Exists)
          return false;
        string content;
        using (lfsra.EnsureLocalFileSystemAccess())
          content = File.ReadAllText(lfsra.LocalFileSystemPath);
        return ImdbIdMatcher.TryMatchImdbId(content, out imdbId);
      }
    }
  }
}
