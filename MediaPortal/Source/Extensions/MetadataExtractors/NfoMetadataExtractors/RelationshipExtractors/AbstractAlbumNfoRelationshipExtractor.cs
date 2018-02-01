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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  public class AbstractAlbumNfoRelationshipExtractor : AbstractNfoRelationshipExtractor<NfoAudioMetadataExtractorSettings>
  {
    /// <summary>
    /// Asynchronously tries to extract album metadata for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAlbum">AlbumInfo object to update with metadata</param>
    /// <returns><c>true</c> if metadata was found and stored into the <paramref name="extractedAlbum"/>, else <c>false</c></returns>
    protected async Task<bool> TryExtractAlbumMetadataAsync(IResourceAccessor mediaItemAccessor, AlbumInfo extractedAlbum)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting album metadata for resource '{1}'", miNumber, mediaItemAccessor);

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return false;
        }

        // First we try to find an IFileSystemResourceAccessor pointing to the album nfo-file.
        IFileSystemResourceAccessor albumNfoFsra;
        if (TryGetAlbumNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out albumNfoFsra))
        {
          // If we found one, we (asynchronously) extract the metadata into a stub object and, if metadata was found,
          // we store it into the MediaItemAspects.
          var albumNfoReader = new NfoAlbumReader(_debugLogger, miNumber, false, false, _httpClient, _settings);
          using (albumNfoFsra)
          {
            if (await albumNfoReader.TryReadMetadataAsync(albumNfoFsra).ConfigureAwait(false))
            {
              Stubs.AlbumStub album = albumNfoReader.GetAlbumStubs().FirstOrDefault();
              if (album != null)
              {
                AlbumInfo albumInfo = new AlbumInfo();
                if (album.AudioDbId.HasValue && album.AudioDbId.Value > 0)
                  albumInfo.AudioDbId = album.AudioDbId.Value;

                albumInfo.MusicBrainzId = album.MusicBrainzAlbumId;
                albumInfo.MusicBrainzGroupId = album.MusicBrainzReleaseGroupId;
                albumInfo.Album = album.Title;
                albumInfo.AlbumSort = BaseInfo.GetSortTitle(album.Title);
                albumInfo.Description = album.Review;

                albumInfo.ReleaseDate = album.Year;
                if (album.Rating.HasValue)
                  albumInfo.Rating.RatingValue = Convert.ToDouble(album.Rating);
                if (album.Artists != null)
                  albumInfo.Artists = album.Artists.Select(a => new PersonInfo { Name = a, Occupation = PersonAspect.OCCUPATION_ARTIST }).ToList();
                if (album.Labels != null)
                  albumInfo.MusicLabels = album.Labels.Select(l => new CompanyInfo { Name = l, Type = CompanyAspect.COMPANY_MUSIC_LABEL }).ToList();
                if (album.Genres != null)
                  albumInfo.Genres = album.Genres.Select(g => new GenreInfo { Name = g }).ToList();

                extractedAlbum.MergeWith(albumInfo);
                _debugLogger.Info("[#{0}]: Successfully finished extracting metadata", miNumber);
              }
            }
            else
              _debugLogger.Warn("[#{0}]: No valid metadata found in album nfo-file", miNumber);
          }
        }
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoAudioMetadataExtractor: Exception while extracting album metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting metadata", e, miNumber);
        return false;
      }
    }

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
  }
}
