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
  public class LocalMovieCollectionFanartProvider : IFanArtProvider
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

      if (mediaType != FanArtMediaTypes.MovieCollection)
        return false;

      // Don't try to load "fanart" for images
      if (!Guid.TryParse(name, out mediaItemId) || mediaType == FanArtMediaTypes.Image)
        return false;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return false;

      IFilter filter = new RelationshipFilter(MovieAspect.ROLE_MOVIE, MovieCollectionAspect.ROLE_MOVIE_COLLECTION, mediaItemId);
      MediaItemQuery movieQuery = new MediaItemQuery(NECESSARY_MIAS, filter);
      IList<MediaItem> items = mediaLibrary.Search(movieQuery, false, null, false);
      if (items == null || items.Count == 0)
        return false;

      var files = new List<IResourceLocator>();
      foreach (MediaItem mediaItem in items)
      {
        // Virtual resources won't have any local fanart
        if (mediaItem.IsVirtual)
          continue;
        var mediaIteamLocator = mediaItem.GetResourceLocator();
        var fanArtPaths = new List<ResourcePath>();
        
        // File based access
        try
        {
          var mediaItemPath = mediaIteamLocator.NativeResourcePath;
          var mediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../");
          var mediaItemCollectionDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../../");
          var mediaItemFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(mediaItemPath.ToString()).ToLowerInvariant();

          using (var directoryRa = new ResourceLocator(mediaIteamLocator.NativeSystemId, mediaItemDirectoryPath).CreateAccessor())
          {
            var directoryFsra = directoryRa as IFileSystemResourceAccessor;
            if (directoryFsra != null)
            {
              var potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(directoryFsra);

              if (fanArtType == FanArtTypes.Poster || fanArtType == FanArtTypes.Thumbnail)
                fanArtPaths.AddRange(
                  from potentialFanArtFile in potentialFanArtFiles
                  let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                  where potentialFanArtFileNameWithoutExtension == "movieset-poster"
                  select potentialFanArtFile);

              if (fanArtType == FanArtTypes.Banner)
                fanArtPaths.AddRange(
                  from potentialFanArtFile in potentialFanArtFiles
                  let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                  where potentialFanArtFileNameWithoutExtension == "movieset-banner"
                  select potentialFanArtFile);

              if (fanArtType == FanArtTypes.FanArt)
              {
                fanArtPaths.AddRange(
                  from potentialFanArtFile in potentialFanArtFiles
                  let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                  where potentialFanArtFileNameWithoutExtension == "movieset-fanart"
                  select potentialFanArtFile);
              }

              files.AddRange(fanArtPaths.Select(path => new ResourceLocator(mediaIteamLocator.NativeSystemId, path)));
            }
          }

          using (var directoryRa = new ResourceLocator(mediaIteamLocator.NativeSystemId, mediaItemCollectionDirectoryPath).CreateAccessor())
          {
            var directoryFsra = directoryRa as IFileSystemResourceAccessor;
            if (directoryFsra != null)
            {
              var potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(directoryFsra);

              if (fanArtType == FanArtTypes.Poster || fanArtType == FanArtTypes.Thumbnail)
                fanArtPaths.AddRange(
                  from potentialFanArtFile in potentialFanArtFiles
                  let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                  where potentialFanArtFileNameWithoutExtension == "poster" || potentialFanArtFileNameWithoutExtension == "folder" || potentialFanArtFileNameWithoutExtension == "movieset-poster" ||
                    potentialFanArtFileNameWithoutExtension.EndsWith("-poster")
                  select potentialFanArtFile);

              if (fanArtType == FanArtTypes.Banner)
                fanArtPaths.AddRange(
                  from potentialFanArtFile in potentialFanArtFiles
                  let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                  where potentialFanArtFileNameWithoutExtension == "banner" || potentialFanArtFileNameWithoutExtension == "movieset-banner" || potentialFanArtFileNameWithoutExtension.EndsWith("-banner")
                  select potentialFanArtFile);

              if (fanArtType == FanArtTypes.FanArt)
              {
                fanArtPaths.AddRange(
                  from potentialFanArtFile in potentialFanArtFiles
                  let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                  where potentialFanArtFileNameWithoutExtension == "backdrop" || potentialFanArtFileNameWithoutExtension == "fanart" || potentialFanArtFileNameWithoutExtension == "movieset-fanart" ||
                    potentialFanArtFileNameWithoutExtension.EndsWith("-fanart")
                  select potentialFanArtFile);

                if (directoryFsra.ResourceExists("ExtraFanArt/"))
                  using (var extraFanArtDirectoryFsra = directoryFsra.GetResource("ExtraFanArt/"))
                    fanArtPaths.AddRange(LocalFanartHelper.GetPotentialFanArtFiles(extraFanArtDirectoryFsra));
              }

              files.AddRange(fanArtPaths.Select(path => new ResourceLocator(mediaIteamLocator.NativeSystemId, path)));
            }
          }
        }
        catch (Exception ex)
        {
#if DEBUG
          ServiceRegistration.Get<ILogger>().Warn("LocalMovieCollectionFanArtProvider: Error while searching fanart of type '{0}' for '{1}'", ex, fanArtType, mediaIteamLocator);
#endif
        }

        if (files.Count > 0)
          break;
      }
      if (files.Count == 0)
      {
        foreach (MediaItem mediaItem in items)
        {
          // Virtual resources won't have any local fanart
          if (mediaItem.IsVirtual)
            continue;
          var mediaIteamLocator = mediaItem.GetResourceLocator();
          var fanArtPaths = new List<ResourcePath>();

          // File based access
          try
          {
            var mediaItemPath = mediaIteamLocator.NativeResourcePath;
            var mediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../");
            var mediaItemFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(mediaItemPath.ToString()).ToLowerInvariant();

            using (var directoryRa = new ResourceLocator(mediaIteamLocator.NativeSystemId, mediaItemDirectoryPath).CreateAccessor())
            {
              var directoryFsra = directoryRa as IFileSystemResourceAccessor;
              if (directoryFsra != null)
              {
                var potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(directoryFsra);

                if (fanArtType == FanArtTypes.Poster || fanArtType == FanArtTypes.Thumbnail)
                  fanArtPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "poster" || potentialFanArtFileNameWithoutExtension == "folder" || potentialFanArtFileNameWithoutExtension.StartsWith(mediaItemFileNameWithoutExtension + "-poster")
                    select potentialFanArtFile);

                if (fanArtType == FanArtTypes.Banner)
                  fanArtPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "banner" || potentialFanArtFileNameWithoutExtension.StartsWith(mediaItemFileNameWithoutExtension + "-banner")
                    select potentialFanArtFile);

                if (fanArtType == FanArtTypes.FanArt)
                {
                  fanArtPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "backdrop" || potentialFanArtFileNameWithoutExtension == "fanart" || potentialFanArtFileNameWithoutExtension.StartsWith(mediaItemFileNameWithoutExtension + "-fanart")
                    select potentialFanArtFile);

                  if (directoryFsra.ResourceExists("ExtraFanArt/"))
                    using (var extraFanArtDirectoryFsra = directoryFsra.GetResource("ExtraFanArt/"))
                      fanArtPaths.AddRange(LocalFanartHelper.GetPotentialFanArtFiles(extraFanArtDirectoryFsra));
                }

                files.AddRange(fanArtPaths.Select(path => new ResourceLocator(mediaIteamLocator.NativeSystemId, path)));
              }
            }
          }
          catch (Exception ex)
          {
#if DEBUG
            ServiceRegistration.Get<ILogger>().Warn("LocalMovieCollectionFanArtProvider: Error while searching fanart of type '{0}' for '{1}'", ex, fanArtType, mediaIteamLocator);
#endif
          }

          if (files.Count > 0)
            break;
        }
      }
      result = files;
      return files.Count > 0;
    }
  }
}
