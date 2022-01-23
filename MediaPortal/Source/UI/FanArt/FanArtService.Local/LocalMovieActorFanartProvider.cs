#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Extensions.UserServices.FanArtService.Local
{
  public class LocalMovieActorFanartProvider : IFanArtProvider
  {
    private readonly static Guid[] NECESSARY_MIAS = { ProviderResourceAspect.ASPECT_ID, VideoAspect.ASPECT_ID, RelationshipAspect.ASPECT_ID };

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

      if (mediaType != FanArtMediaTypes.Actor || (fanArtType != FanArtTypes.Undefined && fanArtType != FanArtTypes.Thumbnail))
        return false;

      // Don't try to load "fanart" for images
      if (!Guid.TryParse(name, out mediaItemId))
        return false;

      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>(false);
      if (mediaLibrary == null)
        return false;

      IFilter filter = new RelationshipFilter(MovieAspect.ROLE_MOVIE, PersonAspect.ROLE_ACTOR, mediaItemId);
      IList<MediaItem> items = null;
      List<Guid> necessaryMias = new List<Guid>(NECESSARY_MIAS);
      MediaItemQuery mediaQuery = new MediaItemQuery(necessaryMias, filter);
      mediaQuery.Limit = 1;
      items = mediaLibrary.Search(mediaQuery, false, null, false);
      if (items == null || items.Count == 0)
        return false;

      MediaItem mediaItem = items.First();
      // Virtual resources won't have any local fanart
      if (mediaItem.IsVirtual)
        return false;
      var mediaIteamLocator = mediaItem.GetResourceLocator();
      var fanArtPaths = new List<ResourcePath>();
      var files = new List<IResourceLocator>();

      string actorName = null;
      SingleMediaItemAspect videoAspect;
      List<string> actors = new List<string>();
      if (MediaItemAspect.TryGetAspect(mediaItem.Aspects, VideoAspect.Metadata, out videoAspect))
      {
        IEnumerable<object> actorObjects = videoAspect.GetCollectionAttribute<object>(VideoAspect.ATTR_ACTORS);
        if (actorObjects != null)
          actors.AddRange(actorObjects.Cast<string>());
      }

      IList<MultipleMediaItemAspect> relationAspects;
      if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, RelationshipAspect.Metadata, out relationAspects))
      {
        foreach (MultipleMediaItemAspect relation in relationAspects)
        {
          if ((Guid?)relation[RelationshipAspect.ATTR_LINKED_ROLE] == PersonAspect.ROLE_ACTOR && (Guid?)relation[RelationshipAspect.ATTR_LINKED_ID] == mediaItemId)
          {
            int? index = (int?)relation[RelationshipAspect.ATTR_RELATIONSHIP_INDEX];
            if (index.HasValue && actors.Count > index.Value && index.Value >= 0)
              actorName = actors[index.Value];
            break;
          }
        }
      }
      
      // File based access
      try
      {
        var mediaItemPath = mediaIteamLocator.NativeResourcePath;
        var mediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../");

        if (!string.IsNullOrEmpty(actorName))
        {
          using (var directoryRa = new ResourceLocator(mediaIteamLocator.NativeSystemId, mediaItemDirectoryPath).CreateAccessor())
          {
            var directoryFsra = directoryRa as IFileSystemResourceAccessor;
            if (directoryFsra != null)
            {
              //Get Artists thumbs
              IFileSystemResourceAccessor actorMediaItemDirectory = directoryFsra.GetResource(".actors");
              if (actorMediaItemDirectory != null)
              {
                  var potentialArtistFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(actorMediaItemDirectory);

                  foreach (ResourcePath thumbPath in
                      from potentialFanArtFile in potentialArtistFanArtFiles
                      let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                      where potentialFanArtFileNameWithoutExtension.StartsWith(actorName.Replace(" ", "_"), StringComparison.InvariantCultureIgnoreCase)
                      select potentialFanArtFile)
                  files.Add(new ResourceLocator(mediaIteamLocator.NativeSystemId, thumbPath));
              }
            }
          }

          //Find central actor information folder
          ResourcePath centralActorFolderPath = LocalFanartHelper.GetCentralPersonFolder(mediaItemDirectoryPath, CentralPersonFolderType.MovieActors);
          if (centralActorFolderPath != null)
          {
            // First get the ResourcePath of the central directory
            var actorFolderPath = ResourcePathHelper.Combine(centralActorFolderPath, $"{LocalFanartHelper.GetSafePersonFolderName(actorName)}/");

            // Then try to create an IFileSystemResourceAccessor for this directory
            actorFolderPath.TryCreateLocalResourceAccessor(out var artistNfoDirectoryRa);
            var directoryFsra = artistNfoDirectoryRa as IFileSystemResourceAccessor;
            if (directoryFsra != null)
            {
              var potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(directoryFsra);

              if (fanArtType == FanArtTypes.Poster || fanArtType == FanArtTypes.Thumbnail)
              {
                fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(potentialFanArtFiles, LocalFanartHelper.ARTIST_FILENAMES));
                fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(potentialFanArtFiles, LocalFanartHelper.POSTER_FILENAMES));
                fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(potentialFanArtFiles, LocalFanartHelper.THUMB_FILENAMES));
              }

              if (fanArtType == FanArtTypes.Banner)
                fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(potentialFanArtFiles, LocalFanartHelper.BANNER_FILENAMES));

              if (fanArtType == FanArtTypes.Logo)
                fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(potentialFanArtFiles, LocalFanartHelper.LOGO_FILENAMES));

              if (fanArtType == FanArtTypes.ClearArt)
                fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(potentialFanArtFiles, LocalFanartHelper.CLEARART_FILENAMES));

              if (fanArtType == FanArtTypes.FanArt)
              {
                fanArtPaths.AddRange(LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(potentialFanArtFiles, LocalFanartHelper.BACKDROP_FILENAMES));

                if (directoryFsra.ResourceExists("ExtraFanArt/"))
                  using (var extraFanArtDirectoryFsra = directoryFsra.GetResource("ExtraFanArt/"))
                    fanArtPaths.AddRange(LocalFanartHelper.GetPotentialFanArtFiles(extraFanArtDirectoryFsra));
              }

              files.AddRange(fanArtPaths.Select(path => new ResourceLocator(mediaIteamLocator.NativeSystemId, path)));
            }
          }
        }
      }
      catch (Exception ex)
      {
#if DEBUG
        ServiceRegistration.Get<ILogger>().Warn("LocalMovieActorFanArtProvider: Error while searching fanart of type '{0}' for '{1}'", ex, fanArtType, mediaIteamLocator);
#endif
      }
      result = files;
      return files.Count > 0;
    }
  }
}
