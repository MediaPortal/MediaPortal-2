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
    private readonly static Guid[] NECESSARY_MIAS = { ProviderResourceAspect.ASPECT_ID };

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

      if (mediaType != FanArtMediaTypes.Series && mediaType != FanArtMediaTypes.Episode)
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
        var seriesDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../../");
        var mediaItemFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(mediaItemPath.ToString()).ToLowerInvariant();
        var mediaItemExtension = ResourcePathHelper.GetExtension(mediaItemPath.ToString());

        //Series FanArt
        using (var directoryRa = new ResourceLocator(mediaIteamLocator.NativeSystemId, seriesDirectoryPath).CreateAccessor())
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

            files.AddRange(fanArtPaths.Select(path => new ResourceLocator(mediaIteamLocator.NativeSystemId, path)));

            //Season FanArt fallback
            if (!files.Any())
            {
              foreach (var seasonDirectoryFsra in directoryFsra.GetChildDirectories())
              {
                potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(seasonDirectoryFsra);

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

                files.AddRange(fanArtPaths.Select(path => new ResourceLocator(mediaIteamLocator.NativeSystemId, path)));

                if (files.Any())
                  break;
              }
            }
          }
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
  }
}
