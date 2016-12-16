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
using MediaPortal.Common.MediaManagement;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.UiComponents.Media.Views.RemovableMediaDrives
{
  /// <summary>
  /// Drive handler which covers media drives with simple multimedia files like MP3, AVI or PNG.
  /// </summary>
  public class MultimediaDriveHandler : BaseDriveHandler
  {
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
      _mediaItemsSubViewSpecification = new StaticViewSpecification(
          driveInfo.VolumeLabel + " (" + DriveUtils.GetDriveNameWithoutRootDirectory(driveInfo) + ")", new Guid[] {}, new Guid[] {});
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
      return (mediaType = MultimediaDirectory.DetectMultimedia(
          directory, videoMIATypeIds, imageMIATypeIds, audioMIATypeIds, out mediaItems)) == MultiMediaType.None ? null :
          new MultimediaDriveHandler(driveInfo, mediaItems, mediaType);
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