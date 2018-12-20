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
  public class NfoSeriesExtractorBase : NfoExtractorBase<NfoSeriesMetadataExtractorSettings>
  {
    #region Reader helpers

    /// <summary>
    /// Asynchronously creates an <see cref="NfoSeriesEpisodeReader"/> for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to create an NfoSeriesEpisodeReader</param>
    /// <param name="season">Season number of the episode to create an NfoSeriesEpisodeReader for</param>
    /// <param name="episode">Episode number of the episode to create an NfoSeriesEpisodeReader for</param>
    /// <returns>An NfoSeriesEpisodeReader if an nfo file was found, else <c>null</c></returns>
    protected async Task<NfoSeriesEpisodeReader> TryGetNfoSeriesEpisodeReaderAsync(IResourceAccessor mediaItemAccessor, int? season, int? episode)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting metadata for resource '{1}'", miNumber, mediaItemAccessor);

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory or parent directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return null;
        }

        // Here we try to find an IFileSystemResourceAccessor pointing to the episode nfo-file.
        // If we don't find one, we cannot extract any metadata.
        IFileSystemResourceAccessor episodeNfoFsra;
        NfoSeriesEpisodeReader episodeNfoReader = null;
        bool episodeDetailsFound = false;
        if (TryGetEpisodeNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out episodeNfoFsra))
        {
          episodeDetailsFound = true;
          // Now we (asynchronously) extract the metadata into a stub object.
          // If no metadata was found, nothing can be stored in the MediaItemAspects.
          episodeNfoReader = new NfoSeriesEpisodeReader(_debugLogger, miNumber, false, false, _httpClient, _settings);
          using (episodeNfoFsra)
          {
            if (!await episodeNfoReader.TryReadMetadataAsync(episodeNfoFsra).ConfigureAwait(false))
            {
              _debugLogger.Warn("[#{0}]: No valid metadata found in episode nfo-file", miNumber);
              return null;
            }
          }
        }

        // Then we try to find an IFileSystemResourceAccessor pointing to the series nfo-file.
        IFileSystemResourceAccessor seriesNfoFsra;
        if (TryGetSeriesNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out seriesNfoFsra))
        {
          // If we found one, we (asynchronously) extract the metadata into a stub object and, if metadata was found,
          // we store it into the episodeNfoReader so that the latter can store metadata from series and episode level into the MediaItemAspects.
          var seriesNfoReader = new NfoSeriesReader(_debugLogger, miNumber, false, !episodeDetailsFound, false, _httpClient, _settings);
          using (seriesNfoFsra)
          {
            if (await seriesNfoReader.TryReadMetadataAsync(seriesNfoFsra).ConfigureAwait(false))
            {
              Stubs.SeriesStub series = seriesNfoReader.GetSeriesStubs().FirstOrDefault();
              if (series != null)
              {
                if (!episodeDetailsFound && series.Episodes != null && season.HasValue && episode.HasValue)
                {
                  Stubs.SeriesEpisodeStub episodeStub = series.Episodes.FirstOrDefault(e => e.Season == season && e.Episodes != null && e.Episodes.Contains(episode.Value));
                  if (episode != null)
                  {
                    episodeNfoReader = new NfoSeriesEpisodeReader(_debugLogger, miNumber, false, false, _httpClient, _settings);
                    episodeNfoReader.SetEpisodeStubs(new List<Stubs.SeriesEpisodeStub>(new[] { episodeStub }));
                  }
                }

                if (episodeNfoReader != null)
                  episodeNfoReader.SetSeriesStubs(new List<Stubs.SeriesStub> { series });
              }
              else
                _debugLogger.Warn("[#{0}]: No valid metadata found in series nfo-file", miNumber);
            }
          }
        }

        if (episodeNfoReader != null)
          return episodeNfoReader;

        _debugLogger.Warn("[#{0}]: No valid nfo-file found", miNumber);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoSeriesMetadataExtractor: Exception while extracting metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting metadata", e, miNumber);
      }
      return null;
    }

    /// <summary>
    /// Verifies if the episode being reimported matches the episode in the nfo file
    /// </summary>
    /// <param name="reader">Reader used read the episode information from the nfo file</param>
    /// <param name="reimport">The episode being reimported</param>
    /// <returns>Result of the verification</returns>
    protected bool VerifyEpisodeReimport(NfoSeriesEpisodeReader reader, EpisodeInfo reimport)
    {
      if (reimport == null)
        return true;

      IDictionary<Guid, IList<MediaItemAspect>> aspectData = new Dictionary<Guid, IList<MediaItemAspect>>();
      if (reader.TryWriteMetadata(aspectData))
      {
        EpisodeInfo info = new EpisodeInfo();
        info.FromMetadata(aspectData);
        if (reimport.Equals(info))
          return true;
      }
      return false;
    }

    /// <summary>
    /// Asynchronously creates an <see cref="NfoSeriesReader"/> for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to create an NfoSeriesReader</param>
    /// <returns>An NfoSeriesReader if an nfo file was found, else <c>null</c></returns>
    protected async Task<NfoSeriesReader> TryGetNfoSeriesReaderAsync(IResourceAccessor mediaItemAccessor)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting metadata for resource '{1}' (importOnly: {2}, forceQuickMode: {3})", miNumber, mediaItemAccessor, false, false);

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory or parent directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return null;
        }

        // Then we try to find an IFileSystemResourceAccessor pointing to the series nfo-file.
        IFileSystemResourceAccessor seriesNfoFsra;
        if (TryGetSeriesNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out seriesNfoFsra))
        {
          // If we found one, we (asynchronously) extract the metadata into a stub object and, if metadata was found,
          // we store it into the episodeNfoReader so that the latter can store metadata from series and episode level into the MediaItemAspects.
          var seriesNfoReader = new NfoSeriesReader(_debugLogger, miNumber, false, false, false, _httpClient, _settings);
          using (seriesNfoFsra)
          {
            if (await seriesNfoReader.TryReadMetadataAsync(seriesNfoFsra).ConfigureAwait(false))
              return seriesNfoReader;
            else
              _debugLogger.Warn("[#{0}]: No valid metadata found in series nfo-file", miNumber);
          }
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoSeriesMetadataExtractor: Exception while extracting series metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting metadata", e, miNumber);
      }
      return null;
    }

    /// <summary>
    /// Verifies if the series being reimported matches the series in the nfo file
    /// </summary>
    /// <param name="reader">Reader used read the series information from the nfo file</param>
    /// <param name="reimport">The series being reimported</param>
    /// <returns>Result of the verification</returns>
    protected bool VerifySeriesReimport(NfoSeriesReader reader, SeriesInfo reimport)
    {
      if (reimport == null)
        return true;

      IDictionary<Guid, IList<MediaItemAspect>> aspectData = new Dictionary<Guid, IList<MediaItemAspect>>();
      if (reader.TryWriteMetadata(aspectData))
      {
        SeriesInfo info = new SeriesInfo();
        info.FromMetadata(aspectData);
        if (reimport.Equals(info))
          return true;
      }
      return false;
    }

    #endregion

    #region Resource helpers

    /// <summary>
    /// Tries to find an episode nfo-file for the given <param name="mediaFsra"></param>
    /// </summary>
    /// <param name="miNumber">Unique number for logging purposes</param>
    /// <param name="mediaFsra">FileSystemResourceAccessor for which we search an episode nfo-file</param>
    /// <param name="episodeNfoFsra">FileSystemResourceAccessor of the episode nfo-file or <c>null</c> if no epsiode nfo-file was found</param>
    /// <returns><c>true</c> if an episode nfo-file was found, otherwise <c>false</c></returns>
    protected bool TryGetEpisodeNfoSResourceAccessor(long miNumber, IFileSystemResourceAccessor mediaFsra, out IFileSystemResourceAccessor episodeNfoFsra)
    {
      episodeNfoFsra = null;

      // Determine the directory, in which we look for the episode nfo-file
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
      var episodeNfoDirectoryResourcePath = ResourcePathHelper.Combine(mediaFsra.CanonicalLocalResourcePath, "../");
      _debugLogger.Info("[#{0}]: episode nfo-directory: '{1}'", miNumber, episodeNfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      IResourceAccessor episodeNfoDirectoryRa;
      episodeNfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out episodeNfoDirectoryRa);
      var episodeNfoDirectoryFsra = episodeNfoDirectoryRa as IFileSystemResourceAccessor;
      if (episodeNfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: Cannot extract metadata; episode nfo-directory not accessible'", miNumber, episodeNfoDirectoryResourcePath);
        if (episodeNfoDirectoryRa != null)
          episodeNfoDirectoryRa.Dispose();
        return false;
      }

      // Finally try to find an episode nfo-file in that directory
      using (episodeNfoDirectoryFsra)
      {
        var episodeNfoFileNames = GetEpisodeNfoFileNames(mediaFsra);
        foreach (var episodeNfoFileName in episodeNfoFileNames)
          if (episodeNfoDirectoryFsra.ResourceExists(episodeNfoFileName))
          {
            _debugLogger.Info("[#{0}]: episode nfo-file found: '{1}'", miNumber, episodeNfoFileName);
            episodeNfoFsra = episodeNfoDirectoryFsra.GetResource(episodeNfoFileName);
            return true;
          }
          else
            _debugLogger.Info("[#{0}]: episode nfo-file '{1}' not found; checking next possible file...", miNumber, episodeNfoFileName);
      }

      _debugLogger.Info("[#{0}]: Cannot extract metadata; No episode nfo-file found", miNumber);
      return false;
    }

    /// <summary>
    /// Tries to find a series nfo-file for the given <param name="mediaFsra"></param>
    /// </summary>
    /// <param name="miNumber">Unique number for logging purposes</param>
    /// <param name="mediaFsra">FileSystemResourceAccessor for which we search a series nfo-file</param>
    /// <param name="seriesNfoFsra">FileSystemResourceAccessor of the series nfo-file or <c>null</c> if no series nfo-file was found</param>
    /// <returns><c>true</c> if a series nfo-file was found, otherwise <c>false</c></returns>
    protected bool TryGetSeriesNfoSResourceAccessor(long miNumber, IFileSystemResourceAccessor mediaFsra, out IFileSystemResourceAccessor seriesNfoFsra)
    {
      seriesNfoFsra = null;

      // Determine the first directory, in which we look for the series nfo-file
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
      var firstSeriesNfoDirectoryResourcePath = ResourcePathHelper.Combine(mediaFsra.CanonicalLocalResourcePath, "../");
      _debugLogger.Info("[#{0}]: first series nfo-directory: '{1}'", miNumber, firstSeriesNfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      IResourceAccessor seriesNfoDirectoryRa;
      firstSeriesNfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out seriesNfoDirectoryRa);
      var seriesNfoDirectoryFsra = seriesNfoDirectoryRa as IFileSystemResourceAccessor;
      if (seriesNfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: first series nfo-directory not accessible'", miNumber, firstSeriesNfoDirectoryResourcePath);
        if (seriesNfoDirectoryRa != null)
          seriesNfoDirectoryRa.Dispose();
      }
      else
      {
        // Try to find a series nfo-file in the that directory
        using (seriesNfoDirectoryFsra)
        {
          var seriesNfoFileNames = GetSeriesNfoFileNames();
          foreach (var seriesNfoFileName in seriesNfoFileNames)
            if (seriesNfoDirectoryFsra.ResourceExists(seriesNfoFileName))
            {
              _debugLogger.Info("[#{0}]: series nfo-file found: '{1}'", miNumber, seriesNfoFileName);
              seriesNfoFsra = seriesNfoDirectoryFsra.GetResource(seriesNfoFileName);
              return true;
            }
            else
              _debugLogger.Info("[#{0}]: series nfo-file '{1}' not found; checking next possible file...", miNumber, seriesNfoFileName);
        }
      }

      // Determine the second directory, in which we look for the series nfo-file

      // First get the ResourcePath of the parent directory's parent directory
      var secondSeriesNfoDirectoryResourcePath = ResourcePathHelper.Combine(firstSeriesNfoDirectoryResourcePath, "../");
      _debugLogger.Info("[#{0}]: second series nfo-directory: '{1}'", miNumber, secondSeriesNfoDirectoryResourcePath);

      // Then try to create an IFileSystemResourceAccessor for this directory
      secondSeriesNfoDirectoryResourcePath.TryCreateLocalResourceAccessor(out seriesNfoDirectoryRa);
      seriesNfoDirectoryFsra = seriesNfoDirectoryRa as IFileSystemResourceAccessor;
      if (seriesNfoDirectoryFsra == null)
      {
        _debugLogger.Info("[#{0}]: second series nfo-directory not accessible'", miNumber, secondSeriesNfoDirectoryResourcePath);
        if (seriesNfoDirectoryRa != null)
          seriesNfoDirectoryRa.Dispose();
        return false;
      }

      // Finally try to find a series nfo-file in the that second directory
      using (seriesNfoDirectoryFsra)
      {
        var seriesNfoFileNames = GetSeriesNfoFileNames();
        foreach (var seriesNfoFileName in seriesNfoFileNames)
          if (seriesNfoDirectoryFsra.ResourceExists(seriesNfoFileName))
          {
            _debugLogger.Info("[#{0}]: series nfo-file found: '{1}'", miNumber, seriesNfoFileName);
            seriesNfoFsra = seriesNfoDirectoryFsra.GetResource(seriesNfoFileName);
            return true;
          }
          else
            _debugLogger.Info("[#{0}]: series nfo-file '{1}' not found; checking next possible file...", miNumber, seriesNfoFileName);
      }

      _debugLogger.Info("[#{0}]: No series nfo-file found", miNumber);
      return false;
    }

    /// <summary>
    /// Determines all possible file names for the episode nfo-file based on the respective NfoSeriesMetadataExtractorSettings
    /// </summary>
    /// <param name="mediaFsra">IFilesystemResourceAccessor to the media file for which we search an episode nfo-file</param>
    /// <returns>IEnumerable of strings containing the possible episode nfo-file names</returns>
    protected IEnumerable<string> GetEpisodeNfoFileNames(IFileSystemResourceAccessor mediaFsra)
    {
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

      // Combine the mediaFileOrDirectoryName with the NfoFileNameExtensions from the settings
      return _settings.NfoFileNameExtensions.Select(extension => mediaFileOrDirectoryName + extension).ToList();
    }

    /// <summary>
    /// Determines all possible file names for the series nfo-file based on the respective NfoSeriesMetadataExtractorSettings
    /// </summary>
    /// <returns>IEnumerable of strings containing the possible series nfo-file names</returns>
    protected IEnumerable<string> GetSeriesNfoFileNames()
    {
      var result = new List<string>();

      // Combine the SeriesNfoFileNames from the settings with the NfoFileNameExtensions from the settings
      foreach (var extension in _settings.NfoFileNameExtensions)
        result.AddRange(_settings.SeriesNfoFileNames.Select(seriesNfoFileName => seriesNfoFileName + extension));
      return result;
    }

    #endregion
  }
}
