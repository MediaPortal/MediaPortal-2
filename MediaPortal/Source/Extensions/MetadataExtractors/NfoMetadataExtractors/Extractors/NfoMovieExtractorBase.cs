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
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors
{
  public class NfoMovieExtractorBase : NfoExtractorBase<NfoMovieMetadataExtractorSettings>
  {
    #region Reader helpers

    /// <summary>
    /// Asynchronously creates an <see cref="NfoMovieReader"/> for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to create an NfoMovieReader</param>
    /// <returns>An NfoMovieReader if an nfo file was found, else <c>null</c></returns>
    protected async Task<NfoMovieReader> TryGetNfoMovieReaderAsync(IResourceAccessor mediaItemAccessor)
    {      
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting metadata for resource '{1}'", miNumber, mediaItemAccessor);

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return null;
        }

        // Here we try to find an IFileSystemResourceAccessor pointing to the nfo-file.
        // If we don't find one, we cannot extract any metadata.
        IFileSystemResourceAccessor nfoFsra;
        if (!TryGetNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out nfoFsra))
          return null;

        // Now we (asynchronously) extract the metadata into a stub object.
        // If there is an error parsing the nfo-file with XmlNfoReader, we at least try to parse for a valid IMDB-ID.
        // If no metadata was found, nothing can be stored in the MediaItemAspects.
        NfoMovieReader nfoReader = new NfoMovieReader(_debugLogger, miNumber, false, false, false, _httpClient, _settings);
        using (nfoFsra)
        {
          if (!await nfoReader.TryReadMetadataAsync(nfoFsra).ConfigureAwait(false) &&
              !await nfoReader.TryParseForImdbId(nfoFsra).ConfigureAwait(false))
          {
            _debugLogger.Warn("[#{0}]: No valid metadata found", miNumber);
            return null;
          }
          return nfoReader;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoMovieMetadataExtractor: Exception while extracting metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting metadata", e, miNumber);
      }
      return null;
    }

    /// <summary>
    /// Verifies if the movie being reimported matches the movie in the nfo file
    /// </summary>
    /// <param name="reader">Reader used read the movie information from the nfo file</param>
    /// <param name="reimport">The movie being reimported</param>
    /// <returns>Result of the verification</returns>
    protected bool VerifyMovieReimport(NfoMovieReader reader, MovieInfo reimport)
    {
      if (reimport == null)
        return true;

      IDictionary<Guid, IList<MediaItemAspect>> aspectData = new Dictionary<Guid, IList<MediaItemAspect>>();
      if (reader.TryWriteMetadata(aspectData))
      {
        MovieInfo info = new MovieInfo();
        info.FromMetadata(aspectData);
        if (reimport.Equals(info))
          return true;
      }
      return false;
    }

    #endregion

    #region Resource helpers

    /// <summary>
    /// Tries to find a nfo-file for the given <param name="mediaFsra"></param>
    /// </summary>
    /// <param name="miNumber">Unique number for logging purposes</param>
    /// <param name="mediaFsra">FileSystemResourceAccessor for which we search a nfo-file</param>
    /// <param name="nfoFsra">FileSystemResourceAccessor of the nfo-file or <c>null</c> if no nfo-file was found</param>
    /// <returns><c>true</c> if a nfo-file was found, otherwise <c>false</c></returns>
    protected bool TryGetNfoSResourceAccessor(long miNumber, IFileSystemResourceAccessor mediaFsra, out IFileSystemResourceAccessor nfoFsra)
    {
      nfoFsra = null;

      // Determine the directory, in which we look for the nfo-file
      // We cannot use mediaFsra.GetResource, because for ChainedResourceProviders the parent directory
      // may be located in the ParentResourceProvider. For details see the comments for the ResourcePathHelper class.

      // First get the ResourcePath of the parent directory
      // The parent directory is
      // - for an IFilesystemResourceAcessor pointing to a file:
      //   the directory in which the file is located;
      // - for an IFilesystemResourceAcessor pointing to a root directory of a ChainedResourceProvider (e.g. in case of a DVD iso-file):
      //   the directory in which the file that was unfolded by the ChainedResourceProvider is located;
      // - for an IFilesystemResourceAcessor pointing to any other directory (e.g. DVD directories):
      //   the parent directory of such directory.
      var nfoDirectoryResourcePath = ResourcePathHelper.Combine(mediaFsra.CanonicalLocalResourcePath, "../");
      _debugLogger.Info("[#{0}]: nfo-directory: '{1}'", miNumber, nfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      IResourceAccessor nfoDirectoryRa;
      nfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out nfoDirectoryRa);
      var nfoDirectoryFsra = nfoDirectoryRa as IFileSystemResourceAccessor;
      if (nfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: Cannot extract metadata; nfo-directory not accessible'", miNumber, nfoDirectoryResourcePath);
        if (nfoDirectoryRa != null)
          nfoDirectoryRa.Dispose();
        return false;
      }

      // Finally try to find a nfo-file in that directory
      using (nfoDirectoryFsra)
      {
        var nfoFileNames = GetNfoFileNames(mediaFsra);
        foreach (var nfoFileName in nfoFileNames)
          if (nfoDirectoryFsra.ResourceExists(nfoFileName))
          {
            _debugLogger.Info("[#{0}]: nfo-file found: '{1}'", miNumber, nfoFileName);
            nfoFsra = nfoDirectoryFsra.GetResource(nfoFileName);
            return true;
          }
          else
            _debugLogger.Info("[#{0}]: nfo-file '{1}' not found; checking next possible file...", miNumber, nfoFileName);
      }

      _debugLogger.Info("[#{0}]: Cannot extract metadata; No nfo-file found", miNumber);
      return false;
    }

    /// <summary>
    /// Determines all possible file names for the nfo-file based on the respective NfoMovieMetadataExtractorSettings
    /// </summary>
    /// <param name="mediaFsra">IFilesystemResourceAccessor to the media file for which we search an nfo-file</param>
    /// <returns>IEnumerable of strings containing the possible nfo-file names</returns>
    protected IEnumerable<string> GetNfoFileNames(IFileSystemResourceAccessor mediaFsra)
    {
      var result = new List<string>();

      // Always consider the file or directory name of the media item
      string mediaFileOrDirectoryName;

      // If the MediaItem is a file, we simply take the filename without extension
      if (mediaFsra.IsFile)
        mediaFileOrDirectoryName = ResourcePathHelper.GetFileNameWithoutExtension(mediaFsra.CanonicalLocalResourcePath.Serialize());
      else
      {
        // if the media is a directory (such as a DVD or BluRay) we start with the ResourcePath
        mediaFileOrDirectoryName = mediaFsra.CanonicalLocalResourcePath.Serialize();

        // In case of the root path of a ChainedResourceProvider (such as for DVD- or BluRay-Iso-Files), we remove the last
        // ChainedResourceProvider, leaving us with the full path of the file, the ChainedResourceProvider has unfolded
        if (mediaFileOrDirectoryName.EndsWith(":///") && mediaFileOrDirectoryName.Contains(">"))
          mediaFileOrDirectoryName = mediaFileOrDirectoryName.Substring(0, mediaFileOrDirectoryName.LastIndexOf(">", StringComparison.Ordinal) - 1);

        // If it's a directory in a BaseResourceProvider, we just remove the last "/" so that the following
        // GetFileNameWithoutExtension considers the directory as a file.
        else
          mediaFileOrDirectoryName = StringUtils.RemoveSuffixIfPresent(mediaFileOrDirectoryName, "/");

        // Finally we get the file name without extension
        mediaFileOrDirectoryName = ResourcePathHelper.GetFileNameWithoutExtension(mediaFileOrDirectoryName);
      }

      // Combine the mediaFileOrDirectoryName and potentially further MovieNfoFileNames from the settings with
      // the NfoFileNameExtensions from the settings
      foreach (var extension in _settings.NfoFileNameExtensions)
      {
        result.Add(mediaFileOrDirectoryName + extension);
        result.AddRange(_settings.MovieNfoFileNames.Select(movieNfoFileName => movieNfoFileName + extension));
      }
      return result;
    }

    #endregion
  }
}
