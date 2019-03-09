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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors
{
  public class NfoAudioExtractorBase : NfoExtractorBase<NfoAudioMetadataExtractorSettings>
  {
    #region Reader helpers

    /// <summary>
    /// Asynchronously tries to get an <see cref="NfoAlbumReader"/> for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s with the extracted metadata</param>
    /// <param name="forceQuickMode">If <c>true</c>, nothing is downloaded from the internet</param>
    /// <returns><c>true</c> if metadata was found and stored into <param name="extractedAspectData"></param>, else <c>false</c></returns>
    protected async Task<NfoAlbumReader> TryGetNfoAlbumReaderAsync(IResourceAccessor mediaItemAccessor)
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

        // First we try to find an IFileSystemResourceAccessor pointing to the album nfo-file.
        IFileSystemResourceAccessor albumNfoFsra;
        if (TryGetAlbumNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out albumNfoFsra))
        {
          // If we found one, we (asynchronously) extract the metadata into a stub object
          var albumNfoReader = new NfoAlbumReader(_debugLogger, miNumber, false, false, _httpClient, _settings);
          using (albumNfoFsra)
          {
            if (await albumNfoReader.TryReadMetadataAsync(albumNfoFsra).ConfigureAwait(false))
              return albumNfoReader;
            else
              _debugLogger.Warn("[#{0}]: No valid metadata found in album nfo-file", miNumber);
          }
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoAudioMetadataExtractor: Exception while extracting metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting metadata", e, miNumber);
      }
      return null;
    }

    /// <summary>
    /// Verifies if the album being reimported matches the album in the nfo file
    /// </summary>
    /// <param name="reader">Reader used read the album information from the nfo file</param>
    /// <param name="reimport">The album being reimported</param>
    /// <returns>Result of the verification</returns>
    protected bool VerifyAlbumReimport(NfoAlbumReader reader, AlbumInfo reimport)
    {
      if (reimport == null)
        return true;

      IDictionary<Guid, IList<MediaItemAspect>> aspectData = new Dictionary<Guid, IList<MediaItemAspect>>();
      if (reader.TryWriteMetadata(aspectData))
      {
        AlbumInfo info = new AlbumInfo();
        info.FromMetadata(aspectData);
        if (reimport.Equals(info))
          return true;
      }
      return false;
    }

    /// <summary>
    /// Asynchronously tries to get an <see cref="NfoArtistReader"/> for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s with the extracted metadata</param>
    /// <param name="forceQuickMode">If <c>true</c>, nothing is downloaded from the internet</param>
    /// <returns><c>true</c> if metadata was found and stored into <param name="extractedAspectData"></param>, else <c>false</c></returns>
    protected async Task<NfoArtistReader> TryGetNfoArtistReaderAsync(IResourceAccessor mediaItemAccessor)
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

        // First we try to find an IFileSystemResourceAccessor pointing to the artist nfo-file.
        IFileSystemResourceAccessor artistNfoFsra;
        if (TryGetArtistNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out artistNfoFsra))
        {
          // If we found one, we (asynchronously) extract the metadata into a stub object
          var artistNfoReader = new NfoArtistReader(_debugLogger, miNumber, false, _httpClient, _settings);
          using (artistNfoFsra)
          {
            if (await artistNfoReader.TryReadMetadataAsync(artistNfoFsra).ConfigureAwait(false))
              return artistNfoReader;
            else
              _debugLogger.Warn("[#{0}]: No valid metadata found in album nfo-file", miNumber);
          }
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoAudioMetadataExtractor: Exception while extracting metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting metadata", e, miNumber);
      }
      return null;
    }

    #endregion

    #region Resource helpers

    /// <summary>
    /// Tries to find an album nfo-file for the given <param name="mediaFsra"></param>
    /// </summary>
    /// <param name="miNumber">Unique number for logging purposes</param>
    /// <param name="mediaFsra">FileSystemResourceAccessor for which we search an album nfo-file</param>
    /// <param name="albumNfoFsra">FileSystemResourceAccessor of the album nfo-file or <c>null</c> if no album nfo-file was found</param>
    /// <returns><c>true</c> if an album nfo-file was found, otherwise <c>false</c></returns>
    protected bool TryGetAlbumNfoSResourceAccessor(long miNumber, IFileSystemResourceAccessor mediaFsra, out IFileSystemResourceAccessor albumNfoFsra)
    {
      albumNfoFsra = null;

      // Determine the directory, in which we look for the album nfo-file
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
      var albumNfoDirectoryResourcePath = ResourcePathHelper.Combine(mediaFsra.CanonicalLocalResourcePath, "../");
      _debugLogger.Info("[#{0}]: album nfo-directory: '{1}'", miNumber, albumNfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      IResourceAccessor albumNfoDirectoryRa;
      albumNfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out albumNfoDirectoryRa);
      var albumNfoDirectoryFsra = albumNfoDirectoryRa as IFileSystemResourceAccessor;
      if (albumNfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: Cannot extract metadata; album nfo-directory not accessible'", miNumber, albumNfoDirectoryResourcePath);
        if (albumNfoDirectoryRa != null)
          albumNfoDirectoryRa.Dispose();
        return false;
      }

      // Finally try to find an episode nfo-file in that directory
      using (albumNfoDirectoryFsra)
      {
        var albumNfoFileNames = GetAlbumNfoFileNames();
        foreach (var albumNfoFileName in albumNfoFileNames)
          if (albumNfoDirectoryFsra.ResourceExists(albumNfoFileName))
          {
            _debugLogger.Info("[#{0}]: album nfo-file found: '{1}'", miNumber, albumNfoFileName);
            albumNfoFsra = albumNfoDirectoryFsra.GetResource(albumNfoFileName);
            return true;
          }
          else
            _debugLogger.Info("[#{0}]: album nfo-file '{1}' not found; checking next possible file...", miNumber, albumNfoFileName);
      }

      _debugLogger.Info("[#{0}]: Cannot extract metadata; No album nfo-file found", miNumber);
      return false;
    }

    /// <summary>
    /// Tries to find a artist nfo-file for the given <param name="mediaFsra"></param>
    /// </summary>
    /// <param name="miNumber">Unique number for logging purposes</param>
    /// <param name="mediaFsra">FileSystemResourceAccessor for which we search a artist nfo-file</param>
    /// <param name="artistNfoFsra">FileSystemResourceAccessor of the artist nfo-file or <c>null</c> if no artist nfo-file was found</param>
    /// <returns><c>true</c> if a artist nfo-file was found, otherwise <c>false</c></returns>
    protected bool TryGetArtistNfoSResourceAccessor(long miNumber, IFileSystemResourceAccessor mediaFsra, out IFileSystemResourceAccessor artistNfoFsra)
    {
      artistNfoFsra = null;

      // Determine the first directory, in which we look for the artist nfo-file
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
      var firstArtistNfoDirectoryResourcePath = ResourcePathHelper.Combine(mediaFsra.CanonicalLocalResourcePath, "../");
      _debugLogger.Info("[#{0}]: first artist nfo-directory: '{1}'", miNumber, firstArtistNfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      IResourceAccessor artistNfoDirectoryRa;
      firstArtistNfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out artistNfoDirectoryRa);
      var artistNfoDirectoryFsra = artistNfoDirectoryRa as IFileSystemResourceAccessor;
      if (artistNfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: first artist nfo-directory not accessible'", miNumber, firstArtistNfoDirectoryResourcePath);
        if (artistNfoDirectoryRa != null)
          artistNfoDirectoryRa.Dispose();
      }
      else
      {
        // Try to find a artist nfo-file in the that directory
        using (artistNfoDirectoryFsra)
        {
          var artistNfoFileNames = GetArtistNfoFileNames();
          foreach (var artistNfoFileName in artistNfoFileNames)
            if (artistNfoDirectoryFsra.ResourceExists(artistNfoFileName))
            {
              _debugLogger.Info("[#{0}]: artist nfo-file found: '{1}'", miNumber, artistNfoFileName);
              artistNfoFsra = artistNfoDirectoryFsra.GetResource(artistNfoFileName);
              return true;
            }
            else
              _debugLogger.Info("[#{0}]: artist nfo-file '{1}' not found; checking next possible file...", miNumber, artistNfoFileName);
        }
      }

      // Determine the second directory, in which we look for the series nfo-file

      // First get the ResourcePath of the parent directory's parent directory
      var secondArtistNfoDirectoryResourcePath = ResourcePathHelper.Combine(firstArtistNfoDirectoryResourcePath, "../");
      _debugLogger.Info("[#{0}]: second artist nfo-directory: '{1}'", miNumber, secondArtistNfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      secondArtistNfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out artistNfoDirectoryRa);
      artistNfoDirectoryFsra = artistNfoDirectoryRa as IFileSystemResourceAccessor;
      if (artistNfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: second artist nfo-directory not accessible'", miNumber, secondArtistNfoDirectoryResourcePath);
        if (artistNfoDirectoryRa != null)
          artistNfoDirectoryRa.Dispose();
        return false;
      }

      // Finally try to find a artist nfo-file in the that second directory
      using (artistNfoDirectoryFsra)
      {
        var artistNfoFileNames = GetArtistNfoFileNames();
        foreach (var artistNfoFileName in artistNfoFileNames)
          if (artistNfoDirectoryFsra.ResourceExists(artistNfoFileName))
          {
            _debugLogger.Info("[#{0}]: artist nfo-file found: '{1}'", miNumber, artistNfoFileName);
            artistNfoFsra = artistNfoDirectoryFsra.GetResource(artistNfoFileName);
            return true;
          }
          else
            _debugLogger.Info("[#{0}]: artist nfo-file '{1}' not found; checking next possible file...", miNumber, artistNfoFileName);
      }

      _debugLogger.Info("[#{0}]: No artist nfo-file found", miNumber);
      return false;
    }

    /// <summary>
    /// Determines all possible file names for the album nfo-file based on the respective NfoSeriesMetadataExtractorSettings
    /// </summary>
    /// <returns>IEnumerable of strings containing the possible album nfo-file names</returns>
    protected IEnumerable<string> GetAlbumNfoFileNames()
    {
      var result = new List<string>();

      // Combine the SeriesNfoFileNames from the settings with the NfoFileNameExtensions from the settings
      foreach (var extension in _settings.NfoFileNameExtensions)
        result.AddRange(_settings.AlbumNfoFileNames.Select(albumNfoFileName => albumNfoFileName + extension));
      return result;
    }

    /// <summary>
    /// Determines all possible file names for the artist nfo-file based on the respective NfoSeriesMetadataExtractorSettings
    /// </summary>
    /// <returns>IEnumerable of strings containing the possible artist nfo-file names</returns>
    protected IEnumerable<string> GetArtistNfoFileNames()
    {
      var result = new List<string>();

      // Combine the SeriesNfoFileNames from the settings with the NfoFileNameExtensions from the settings
      foreach (var extension in _settings.NfoFileNameExtensions)
        result.AddRange(_settings.ArtistNfoFileNames.Select(seriesNfoFileName => seriesNfoFileName + extension));
      return result;
    }

    #endregion
  }
}
