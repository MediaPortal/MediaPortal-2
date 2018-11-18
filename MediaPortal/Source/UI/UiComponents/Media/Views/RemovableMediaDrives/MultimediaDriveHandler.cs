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
using System.IO;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Utilities.FileSystem;
using System.Linq;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UiComponents.Media.Helpers;

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
    protected ViewSpecification _mediaItemsSubViewSpecification;

    #endregion

    protected MultimediaDriveHandler(DriveInfo driveInfo, IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds, MultiMediaType mediaType) : base(driveInfo)
    {
      _mediaType = mediaType;
      string drive = driveInfo.Name;
      drive = drive.Substring(0, 2); // Clip potential '\\' at the end
      string directory = "/" + drive + "/";
      _mediaItemsSubViewSpecification = new LocalDirectoryViewSpecification(driveInfo.VolumeLabel + " (" + DriveUtils.GetDriveNameWithoutRootDirectory(driveInfo) + ")",
        ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, directory),
        necessaryMIATypeIds, optionalMIATypeIds);
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

      MultiMediaType mediaType = MultimediaDirectory.DetectMultimedia(directory, videoMIATypeIds, imageMIATypeIds, audioMIATypeIds);
      if (mediaType == MultiMediaType.None)
        return null;

      IEnumerable<Guid> necessaryMIATypeIds = Consts.NECESSARY_BROWSING_MIAS;
      IEnumerable<Guid> optionalMIATypeIds = videoMIATypeIds.Union(imageMIATypeIds).Union(audioMIATypeIds).Except(necessaryMIATypeIds);
      return new MultimediaDriveHandler(driveInfo, necessaryMIATypeIds, optionalMIATypeIds, mediaType);
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
      get { return new[] { _mediaItemsSubViewSpecification }; }
    }

    public override IEnumerable<MediaItem> GetAllMediaItems()
    {
      return _mediaItemsSubViewSpecification.GetAllMediaItems().Result;
    }

    #endregion
  }
}
