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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;

namespace MediaPortal.UiComponents.Media.General
{
  public enum MultiMediaType
  {
    None,
    Video,
    Image,
    Audio,
    Diverse
  }

  /// <summary>
  /// Helper class to detect multimedia items in a local directory.
  /// </summary>
  public static class MultimediaDirectory
  {
    /// <summary>
    /// Detects if the given directory contains video, image or audio media files and returns <see cref="MediaItem"/> instances for those files.
    /// </summary>
    /// <param name="directory">The directory to be examined.</param>
    /// <param name="videoMIATypeIds">Ids of the media item aspects to be extracted from video items.</param>
    /// <param name="imageMIATypeIds">Ids of the media item aspects to be extracted from image items.</param>
    /// <param name="audioMIATypeIds">Ids of the media item aspects to be extracted from audio items.</param>
    /// <param name="mediaItems">Returns a collection of media items in the given <paramref name="directory"/> or <c>null</c>,
    /// if the return value is <see cref="MultiMediaType.None"/>.</param>
    /// <returns>Type of media items found.</returns>
    public static MultiMediaType DetectMultimedia(string directory,
        IEnumerable<Guid> videoMIATypeIds, IEnumerable<Guid> imageMIATypeIds, IEnumerable<Guid> audioMIATypeIds,
        out ICollection<MediaItem> mediaItems)
    {
      if (!Directory.Exists(directory))
      {
        mediaItems = null;
        return MultiMediaType.None;
      }

      return DetectMultimedia(LocalFsResourceProviderBase.ToResourcePath(directory),
          videoMIATypeIds, imageMIATypeIds, audioMIATypeIds, out mediaItems);
    }

    /// <summary>
    /// Detects if the directory of the given <paramref name="resourcePath"/> contains video, image or audio media files and
    /// returns <see cref="MediaItem"/> instances for those files.
    /// </summary>
    /// <param name="resourcePath">The resource path instance for the directory to be examined.</param>
    /// <param name="videoMIATypeIds">Ids of the media item aspects to be extracted from video items.</param>
    /// <param name="imageMIATypeIds">Ids of the media item aspects to be extracted from image items.</param>
    /// <param name="audioMIATypeIds">Ids of the media item aspects to be extracted from audio items.</param>
    /// <param name="mediaItems">Returns a collection of media items in the given <paramref name="resourcePath"/> or <c>null</c>,
    /// if the return value is <see cref="MultiMediaType.None"/>.</param>
    /// <returns>Type of media items found.</returns>
    public static MultiMediaType DetectMultimedia(ResourcePath resourcePath,
        IEnumerable<Guid> videoMIATypeIds, IEnumerable<Guid> imageMIATypeIds, IEnumerable<Guid> audioMIATypeIds,
        out ICollection<MediaItem> mediaItems)
    {
      try
      {
        mediaItems = new List<MediaItem>();
        IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
        IEnumerable<Guid> meIds = mediaAccessor.GetMetadataExtractorsForMIATypes(videoMIATypeIds.Union(imageMIATypeIds).Union(audioMIATypeIds));
        using (IResourceAccessor ra = new ResourceLocator(resourcePath).CreateAccessor())
        {
          IFileSystemResourceAccessor directoryRA = ra as IFileSystemResourceAccessor;
          if (ra != null)
            AddLocalMediaItemsRecursive(directoryRA, mediaItems, meIds, mediaAccessor);
        }
        MultiMediaType result = MultiMediaType.None;
        foreach (MediaItem item in mediaItems)
        { // Check the type of our extracted media items
          MultiMediaType itemType = GetTypeOfMediaItem(item);
          if (result == MultiMediaType.None) // Initialize the result type with the type of the first media item
            result = itemType;
          else if (result != itemType) // Check if we have different item types
          {
            result = MultiMediaType.Diverse;
            break;
          }
        }
        return result;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Error while detecting the media items in resource {0}", e, resourcePath);
        mediaItems = null;
        return MultiMediaType.None;
      }
    }

    /// <summary>
    /// Depending on the existence of an <see cref="VideoAspect"/>, <see cref="ImageAspect"/> or <see cref="AudioAspect"/>,
    /// this method returns <see cref="MultiMediaType.Video"/>, <see cref="MultiMediaType.Image"/> or <see cref="MultiMediaType.Audio"/>.
    /// </summary>
    /// <param name="mediaItem">The media item to be examined. For a value of <c>null</c>, <see cref="MultiMediaType.None"/> is returned.</param>
    /// <returns>Media type of the given <paramref name="mediaItem"/>.</returns>
    public static MultiMediaType GetTypeOfMediaItem(MediaItem mediaItem)
    {
      if (mediaItem == null)
        return MultiMediaType.None;
      if (mediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
        return MultiMediaType.Video;
      if (mediaItem.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
        return MultiMediaType.Image;
      if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
        return MultiMediaType.Audio;
      return MultiMediaType.None;
    }

    /// <summary>
    /// Adds all media items which are found in the directory to the given <paramref name="directoryRA"/> or in any sub directory to
    /// the <paramref name="mediaItems"/> collection.
    /// </summary>
    /// <param name="directoryRA">Directory resource to be recursively examined.</param>
    /// <param name="mediaItems">Collection where the found media items are added.</param>
    /// <param name="metadataExtractorIds">Ids of the metadata extractors to be applied to the resources.
    /// See <see cref="IMediaAccessor.LocalMetadataExtractors"/>.</param>
    /// <param name="mediaAccessor">The media accessor of the system.</param>
    public static void AddLocalMediaItemsRecursive(IFileSystemResourceAccessor directoryRA, ICollection<MediaItem> mediaItems,
        IEnumerable<Guid> metadataExtractorIds, IMediaAccessor mediaAccessor)
    {
      ICollection<IFileSystemResourceAccessor> directoryRAs = FileSystemResourceNavigator.GetChildDirectories(directoryRA, false);
      if (directoryRAs != null)
        foreach (IFileSystemResourceAccessor subDirectoryRA in directoryRAs)
          using (subDirectoryRA)
            AddLocalMediaItemsRecursive(subDirectoryRA, mediaItems, metadataExtractorIds, mediaAccessor);
      ICollection<IFileSystemResourceAccessor> fileRAs = FileSystemResourceNavigator.GetFiles(directoryRA, false);
      if (fileRAs != null)
        foreach (IFileSystemResourceAccessor fileRA in fileRAs)
          using (fileRA)
          {
            MediaItem item = mediaAccessor.CreateLocalMediaItem(fileRA, metadataExtractorIds);
            if (item != null)
              mediaItems.Add(item);
          }
    }
  }
}