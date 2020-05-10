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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;

namespace MediaPortal.Extensions.UserServices.FanArtService.Local
{
  public class LocalSeriesFanartProvider : IFanArtProvider
  {
    private readonly static Guid[] NECESSARY_MIAS = { ProviderResourceAspect.ASPECT_ID, EpisodeAspect.ASPECT_ID };

    public FanArtProviderSource Source { get { return FanArtProviderSource.File; } }

    /// <summary>
    /// Gets a list of <see cref="FanArtImage"/>s for a requested <paramref name="mediaType"/>, <paramref name="fanArtType"/> and <paramref name="name"/>.
    /// The name can be: Series name, Actor name, Artist name depending on the <paramref name="mediaType"/>.
    /// </summary>
    /// <param name="mediaType">Requested FanArtMediaType</param>
    /// <param name="fanArtType">Requested FanArtType</param>
    /// <param name="name">Requested name of Series, Actor, Artist...</param>
    /// <param name="maxWidth">Maximum width for image. <c>0</c> returns image in original size.</param>
    /// <param name="maxHeight">Maximum height for image. <c>0</c> returns image in original size.</param>
    /// <param name="singleRandom">If <c>true</c> only one random image URI will be returned</param>
    /// <param name="result">Result if return code is <c>true</c>.</param>
    /// <returns><c>true</c> if at least one match was found.</returns>
    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;
      Guid mediaItemId;

      if (mediaType != FanArtMediaTypes.Series && mediaType != FanArtMediaTypes.SeriesSeason && mediaType != FanArtMediaTypes.Episode)
        return false;

      if (!Guid.TryParse(name, out mediaItemId))
        return false;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return false;

      IFilter filter = null;
      if (mediaType == FanArtMediaTypes.Series)
      {
        filter = new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, mediaItemId);
      }
      else if (mediaType == FanArtMediaTypes.SeriesSeason)
      {
        filter = new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeasonAspect.ROLE_SEASON, mediaItemId);
      }
      else if (mediaType == FanArtMediaTypes.Episode)
      {
        filter = new MediaItemIdFilter(mediaItemId);
      }
      MediaItemQuery episodeQuery = new MediaItemQuery(NECESSARY_MIAS, filter);
      episodeQuery.Limit = 1;
      IList<MediaItem> items = mediaLibrary.Search(episodeQuery, false, null, false);
      if (items == null || items.Count == 0)
        return false;

      MediaItem mediaItem = items.First();
      // Virtual resources won't have any local fanart
      if (mediaItem.IsVirtual)
        return false;
      var mediaIteamLocator = mediaItem.GetResourceLocator();
      var fanArtPaths = new List<ResourcePath>();
      var files = new List<IResourceLocator>();
      // File based access
      try
      {
        var mediaItemPath = mediaIteamLocator.NativeResourcePath;
        int seasonNo = -1;
        MediaItemAspect.TryGetAttribute(mediaItem.Aspects, EpisodeAspect.ATTR_SEASON, out seasonNo);
        var seasonFolderPath = ResourcePathHelper.Combine(mediaItemPath, "../");
        var seriesFolderPath = GetSeriesFolderFromEpisodePath(mediaIteamLocator.NativeSystemId, mediaItemPath, seasonNo);
        bool hasSeasonFolders = seasonFolderPath != seriesFolderPath;

        //Episode FanArt
        if (mediaType == FanArtMediaTypes.Episode)
        {
          if (fanArtType == FanArtTypes.Undefined || fanArtType == FanArtTypes.Thumbnail)
            AddEpisodeFanArt(fanArtPaths, fanArtType, mediaIteamLocator.NativeSystemId, mediaItemPath);
          else
            AddSeriesFanArt(fanArtPaths, fanArtType, mediaIteamLocator.NativeSystemId, seriesFolderPath);
        }

        //Season FanArt
        if (mediaType == FanArtMediaTypes.SeriesSeason)
        {
          if (hasSeasonFolders)
          {
            AddSeriesFanArt(fanArtPaths, fanArtType, mediaIteamLocator.NativeSystemId, seasonFolderPath);
            AddSpecialSeasonFolderFanArt(fanArtPaths, fanArtType, mediaIteamLocator.NativeSystemId, seasonFolderPath, seasonNo);
          }
          else
          {
            AddSpecialSeasonFolderFanArt(fanArtPaths, fanArtType, mediaIteamLocator.NativeSystemId, seasonFolderPath, seasonNo);
          }

          if (hasSeasonFolders && fanArtPaths.Count == 0)
          {
            //Series fallback
            AddSeriesFanArt(fanArtPaths, fanArtType, mediaIteamLocator.NativeSystemId, seriesFolderPath);
          }
        }

        //Series FanArt
        if (mediaType == FanArtMediaTypes.Series)
        {
          AddSeriesFanArt(fanArtPaths, fanArtType, mediaIteamLocator.NativeSystemId, seriesFolderPath);
        }
      }
      catch (Exception ex)
      {
#if DEBUG
        ServiceRegistration.Get<ILogger>().Warn("LocalSeriesFanArtProvider: Error while searching fanart of type '{0}' for '{1}'", ex, fanArtType, mediaIteamLocator);
#endif
      }
      result = files;
      return files.Count > 0;
    }

    private void AddEpisodeFanArt(List<ResourcePath> fanArtPaths, string fanArtType, string systemId, ResourcePath mediaItemPath)
    {
      var directory = ResourcePathHelper.Combine(mediaItemPath, "../");
      using (IResourceAccessor directoryRa = new ResourceLocator(systemId, directory).CreateAccessor())
      {
        var directoryFsra = directoryRa as IFileSystemResourceAccessor;
        if (directoryFsra != null)
        {
          var mediaItemFileName = ResourcePathHelper.GetFileNameWithoutExtension(mediaItemPath.ToString()).ToLowerInvariant();
          var potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(directoryFsra);

          if (fanArtType == FanArtTypes.Undefined || fanArtType == FanArtTypes.Thumbnail)
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles, null, LocalFanartHelper.THUMB_FILENAMES.Select(f => mediaItemFileName + "-" + f)));
        }
      }
    }

    private void AddSeriesFanArt(List<ResourcePath> fanArtPaths, string fanArtType, string systemId, ResourcePath directory)
    {
      using (IResourceAccessor directoryRa = new ResourceLocator(systemId, directory).CreateAccessor())
      {
        var directoryFsra = directoryRa as IFileSystemResourceAccessor;
        if (directoryFsra != null)
        {
          var potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(directoryFsra);

          if (fanArtType == FanArtTypes.Undefined || fanArtType == FanArtTypes.Thumbnail)
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles, LocalFanartHelper.THUMB_FILENAMES));

          if (fanArtType == FanArtTypes.Poster)
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles, LocalFanartHelper.POSTER_FILENAMES));

          if (fanArtType == FanArtTypes.Banner)
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles, LocalFanartHelper.BANNER_FILENAMES));

          if (fanArtType == FanArtTypes.Logo)
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles, LocalFanartHelper.LOGO_FILENAMES));

          if (fanArtType == FanArtTypes.ClearArt)
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles, LocalFanartHelper.CLEARART_FILENAMES));

          if (fanArtType == FanArtTypes.FanArt)
          {
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(potentialFanArtFiles, LocalFanartHelper.BACKDROP_FILENAMES));

            if (directoryFsra.ResourceExists("ExtraFanArt/"))
              using (var extraFanArtDirectoryFsra = directoryFsra.GetResource("ExtraFanArt/"))
                fanArtPaths.AddRange(LocalFanartHelper.GetPotentialFanArtFiles(extraFanArtDirectoryFsra));
          }
        }
      }
    }

    private void AddSpecialSeasonFolderFanArt(List<ResourcePath> fanArtPaths, string fanArtType, string systemId, ResourcePath directory, int? seasonNumber)
    {
      if (!seasonNumber.HasValue)
        return;

      using (IResourceAccessor directoryRa = new ResourceLocator(systemId, directory).CreateAccessor())
      {
        var directoryFsra = directoryRa as IFileSystemResourceAccessor;
        if (directoryFsra != null)
        {
          var potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(directoryFsra);

          string[] prefixes = new[]
          {
            string.Format("season{0:00}", seasonNumber),
            seasonNumber == 0 ? "season-specials" : "season-all"
          };

          if (fanArtType == FanArtTypes.Undefined || fanArtType == FanArtTypes.Thumbnail)
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles,
              LocalFanartHelper.THUMB_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));

          if (fanArtType == FanArtTypes.Poster)
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles,
              LocalFanartHelper.POSTER_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));

          if (fanArtType == FanArtTypes.Logo)
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles,
              LocalFanartHelper.LOGO_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));

          if (fanArtType == FanArtTypes.ClearArt)
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles,
              LocalFanartHelper.CLEARART_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));

          if (fanArtType == FanArtTypes.Banner)
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles,
              LocalFanartHelper.BANNER_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));

          if (fanArtType == FanArtTypes.FanArt)
            fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(potentialFanArtFiles,
              LocalFanartHelper.BACKDROP_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));
        }
      }
    }

    private ResourcePath GetSeriesFolderFromEpisodePath(string systemId, ResourcePath episodePath, int knownSeasonNo = -1)
    {
      //Check series folder with season folders
      var seriesDirectoryPath = ResourcePathHelper.Combine(episodePath, "../../");
      using (var seriesRa = new ResourceLocator(systemId, seriesDirectoryPath).CreateAccessor())
      {
        if (IsSeriesFolder(seriesRa as IFileSystemResourceAccessor, knownSeasonNo))
          return seriesDirectoryPath;
      }

      //Presume there are no season folders
      return ResourcePathHelper.Combine(episodePath, "../");
    }

    private bool IsSeriesFolder(IFileSystemResourceAccessor seriesFolder, int knownSeasonNo = -1)
    {
      if (seriesFolder == null)
        return false;

      int maxInvalidFolders = 3;
      var seasonFolders = seriesFolder.GetChildDirectories();
      var seasonNos = seasonFolders.Select(GetSeasonFromFolder).ToList();
      var invalidSeasonCount = seasonNos.Count(s => s < 0);
      var validSeasonCount = seasonNos.Count(s => s >= 0);
      if (invalidSeasonCount <= maxInvalidFolders && validSeasonCount > 0)
        return true;
      if (invalidSeasonCount > maxInvalidFolders)
        return false;
      if (validSeasonCount > 0 && knownSeasonNo >= 0 && !seasonNos.Contains(knownSeasonNo))
        return false;

      return true;
    }

    private int GetSeasonFromFolder(IFileSystemResourceAccessor seasonFolder)
    {
      int beforeSeasonNoIndex = seasonFolder.ResourceName.LastIndexOf(" ");
      if (beforeSeasonNoIndex >= 0 && int.TryParse(seasonFolder.ResourceName.Substring(beforeSeasonNoIndex + 1), out int seasonNo))
        return seasonNo;

      return -1;
    }
  }
}
