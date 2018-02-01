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
  public class AbstractAlbumArtistNfoRelationshipExtractor : AbstractNfoRelationshipExtractor<NfoAudioMetadataExtractorSettings>
  {
    /// <summary>
    /// Asynchronously tries to extract album artist metadata for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedArtists">PersonInfo collection to update with metadata</param>
    /// <returns><c>true</c> if metadata was found and stored into <param name="extractedArtists"></param>, else <c>false</c></returns>
    protected async Task<bool> TryExtractAlbumArtistMetadataAsync(IResourceAccessor mediaItemAccessor, IList<PersonInfo> extractedArtists)
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
        // Try to find an IFileSystemResourceAccessor pointing to the artist nfo-file.
        IFileSystemResourceAccessor artistNfoFsra;
        if (TryGetArtistNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out artistNfoFsra))
        {
          // If we found one, we (asynchronously) extract the metadata into a stub object and, if metadata was found,
          // we store it into the MediaItemAspects.
          var artistNfoReader = new NfoArtistReader(_debugLogger, miNumber, false, _httpClient, _settings);
          using (artistNfoFsra)
          {
            if (await artistNfoReader.TryReadMetadataAsync(artistNfoFsra).ConfigureAwait(false))
            {
              foreach (Stubs.ArtistStub artist in artistNfoReader.GetArtistStubs())
              {
                PersonInfo extractedArtist = extractedArtists.FirstOrDefault(p => IsMatchingArtist(p, artist));
                if (extractedArtist == null)
                  continue;

                PersonInfo artistInfo = new PersonInfo();
                if (artist.AudioDbId.HasValue && artist.AudioDbId > 0)
                  artistInfo.AudioDbId = artist.AudioDbId.Value;
                if (string.IsNullOrEmpty(artistInfo.MusicBrainzId))
                  artistInfo.MusicBrainzId = artist.MusicBrainzArtistId;
                if (string.IsNullOrEmpty(artistInfo.Name))
                  artistInfo.Name = artist.Name;
                if (string.IsNullOrEmpty(artistInfo.Biography.Text))
                  artistInfo.Biography = artist.Biography;

                DateTime? birthDate;
                DateTime? deathDate;
                if (artist.Birthdate.HasValue || artist.Deathdate.HasValue)
                {
                  birthDate = artist.Birthdate;
                  deathDate = artist.Deathdate;
                }
                else
                {
                  extractedArtist.IsGroup = true;
                  birthDate = artist.Formeddate;
                  deathDate = artist.Disbandeddate;
                }
                if (!artistInfo.DateOfBirth.HasValue)
                  artistInfo.DateOfBirth = birthDate;
                if (!artistInfo.DateOfDeath.HasValue)
                  artistInfo.DateOfDeath = deathDate;
                artistInfo.Occupation = PersonAspect.OCCUPATION_ARTIST;
              }
            }
            else
              _debugLogger.Warn("[#{0}]: No valid metadata found in artist nfo-file", miNumber);
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

    protected bool IsMatchingArtist(PersonInfo artistInfo, Stubs.ArtistStub artistStub)
    {
      if (artistStub.AudioDbId.HasValue && artistStub.AudioDbId.Value > 0 && artistInfo.AudioDbId == artistStub.AudioDbId.Value)
        return true;
      if (!string.IsNullOrEmpty(artistStub.MusicBrainzArtistId) && artistInfo.MusicBrainzId == artistStub.MusicBrainzArtistId)
        return true;
      return !string.IsNullOrEmpty(artistStub.Name) && !string.IsNullOrEmpty(artistInfo.Name) &&
        string.Equals(artistInfo.Name, artistStub.Name, StringComparison.InvariantCultureIgnoreCase);
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
  }
}
