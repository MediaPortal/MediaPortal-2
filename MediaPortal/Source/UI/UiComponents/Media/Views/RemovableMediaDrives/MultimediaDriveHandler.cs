#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.Services.MediaManagement;
using MediaPortal.Core.SystemResolver;

namespace MediaPortal.UiComponents.Media.Views.RemovableMediaDrives
{
  /// <summary>
  /// Drive handler which covers media drives with simple multimedia files like MP3, AVI or PNG.
  /// </summary>
  public class MultimediaDriveHandler : BaseDriveHandler
  {
    #region Enums

    public enum MultiMediaType
    {
      None,
      Video,
      Image,
      Audio,
      Diverse
    }

    #endregion

    #region Consts

    protected static IList<MediaItem> EMPTY_MEDIA_ITEM_LIST = new List<MediaItem>(0);

    #endregion

    #region Protected fields

    protected MultiMediaType _mediaType;
    protected StaticViewSpecification _mediaItemsSubViewSpecification;

    #endregion

    protected MultimediaDriveHandler(DriveInfo driveInfo, IEnumerable<MediaItem> mediaItems, MultiMediaType mediaType) : base(driveInfo)
    {
      _mediaType = mediaType;
      _mediaItemsSubViewSpecification = new StaticViewSpecification(driveInfo.VolumeLabel, new Guid[] {}, new Guid[] {});
      foreach (MediaItem item in mediaItems)
        _mediaItemsSubViewSpecification.AddMediaItem(item);
    }

    /// <summary>
    /// Creates a <see cref="MultimediaDriveHandler"/> if the drive of the given <paramref name="driveInfo"/> contains one or more
    /// media item files.
    /// </summary>
    /// <param name="driveInfo">Drive info object for the drive to examine.</param>
    /// <param name="videoMIATypeIds">Media item aspect types to be extracted from the video items. The given MIAs will be present
    /// in all created instance's video items.</param>
    /// <param name="imageMIATypeIds">Media item aspect types to be extracted from the image items. The given MIAs will be present
    /// in all created instance's image items.</param>
    /// <param name="audioMIATypeIds">Media item aspect types to be extracted from the audio items. The given MIAs will be present
    /// in all created instance's audio items.</param>
    /// <returns><see cref="MultimediaDriveHandler"/> instance for the multimedia CD/DVD/BD or <c>null</c>, if the given drive doesn't contain
    /// media items.</returns>
    public static MultimediaDriveHandler TryCreateMultimediaCDDriveHandler(DriveInfo driveInfo,
        IEnumerable<Guid> videoMIATypeIds, IEnumerable<Guid> imageMIATypeIds, IEnumerable<Guid> audioMIATypeIds)
    {
      string drive = driveInfo.Name;
      if (string.IsNullOrEmpty(drive) || drive.Length < 2)
        return null;
      drive = drive.Substring(0, 2); // Clip potential '\\' at the end
      string directory = drive + "\\";

      ICollection<MediaItem> mediaItems;
      MultiMediaType mediaType;
      return (mediaType = DetectMultimedia(directory, videoMIATypeIds, imageMIATypeIds, audioMIATypeIds, out mediaItems)) == MultiMediaType.None ? null :
          new MultimediaDriveHandler(driveInfo, mediaItems, mediaType);
    }

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

      try
      {
        mediaItems = new List<MediaItem>();

        ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
        IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
        IEnumerable<Guid> meIds = mediaAccessor.GetMetadataExtractorsForMIATypes(videoMIATypeIds.Union(imageMIATypeIds).Union(audioMIATypeIds));
        ResourcePath resourcePath = LocalFsMediaProviderBase.ToResourcePath(directory);
        using (IFileSystemResourceAccessor directoryRA = new ResourceLocator(resourcePath).CreateLocalFsAccessor())
          AddMediaItems(directoryRA, mediaItems, meIds, mediaAccessor, systemResolver);
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
      catch (IOException)
      {
        ServiceRegistration.Get<ILogger>().Warn("Error while detecting the media items in directory {0}", directory);
        mediaItems = null;
        return MultiMediaType.None;
      }
    }

    protected static MultiMediaType GetTypeOfMediaItem(MediaItem mediaItem)
    {
      if (mediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
        return MultiMediaType.Video;
      if (mediaItem.Aspects.ContainsKey(PictureAspect.ASPECT_ID))
        return MultiMediaType.Image;
      if (mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
        return MultiMediaType.Audio;
      return MultiMediaType.None;
    }

    protected static void AddMediaItems(IFileSystemResourceAccessor directoryRA, ICollection<MediaItem> mediaItems,
        IEnumerable<Guid> metadataExtractorIds, IMediaAccessor mediaAccessor, ISystemResolver systemResolver)
    {
      ICollection<IFileSystemResourceAccessor> directoryRAs = FileSystemResourceNavigator.GetChildDirectories(directoryRA);
      foreach (IFileSystemResourceAccessor subDirectoryRA in directoryRAs)
        AddMediaItems(subDirectoryRA, mediaItems, metadataExtractorIds, mediaAccessor, systemResolver);
      ICollection<IFileSystemResourceAccessor> fileRAs = FileSystemResourceNavigator.GetFiles(directoryRA);
      foreach (IFileSystemResourceAccessor fileRA in fileRAs)
      {
        MediaItem item = mediaAccessor.CreateMediaItem(systemResolver, fileRA, metadataExtractorIds);
        if (item != null)
          mediaItems.Add(item);
      }
    }

    public MultiMediaType MediaType
    {
      get { return _mediaType; }
    }

    #region IRemovableDriveHandler implementation

    public override IList<MediaItem> MediaItems
    {
      get { return EMPTY_MEDIA_ITEM_LIST; }
    }

    public override IList<ViewSpecification> SubViewSpecifications
    {
      get { return new List<ViewSpecification> {_mediaItemsSubViewSpecification}; }
    }

    public override IEnumerable<MediaItem> GetAllMediaItems()
    {
      return _mediaItemsSubViewSpecification.GetAllMediaItems();
    }

    #endregion
  }
}