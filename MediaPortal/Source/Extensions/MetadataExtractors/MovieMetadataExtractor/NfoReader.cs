#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.IO;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  /// <summary>
  /// <see cref="NfoReader"/> tries to read a valid IMDB id from additional .nfo or .txt files.
  /// </summary>
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
      if (fsra == null || !fsra.IsFile)
        return false;

      // First try to find a nfo file that has the same name as our main movie.
      foreach (string extension in NFO_EXTENSIONS)
      {
        string metaFilePath = ProviderPathHelper.ChangeExtension(fsra.CanonicalLocalResourcePath.ToString(), extension);
        if (TryRead(metaFilePath, out imdbId)) 
          return true;
      }

      // Then test for special named files, like "movie.nfo"
      foreach (string fileName in NFO_FILENAMES)
      {
        string metaFilePath = ProviderPathHelper.GetDirectoryName(fsra.CanonicalLocalResourcePath.ToString());
        metaFilePath = ProviderPathHelper.Combine(metaFilePath, fileName);
        if (TryRead(metaFilePath, out imdbId))
          return true;
      }

      return false;
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
        ILocalFsResourceAccessor disposeLfsra = null;
        ILocalFsResourceAccessor lfsra = metaFileAccessor as ILocalFsResourceAccessor;
        try
        {
          if (lfsra == null)
            disposeLfsra = lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(metaFileAccessor);

          string content = File.ReadAllText(lfsra.LocalFileSystemPath);
          return ImdbIdMatcher.TryMatchImdbId(content, out imdbId);
        }
        finally
        {
          if (disposeLfsra != null)
            disposeLfsra.Dispose();
        }
      }
    }
  }
}
