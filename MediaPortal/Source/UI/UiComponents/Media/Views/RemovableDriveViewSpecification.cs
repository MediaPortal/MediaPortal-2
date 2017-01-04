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
using MediaPortal.Common.MediaManagement;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Views.RemovableMediaDrives;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// View implementation which presents the contents of a removable drive.
  /// </summary>
  public class RemovableDriveViewSpecification : ViewSpecification
  {
    #region Protected fields

    protected DriveInfo _driveInfo;
    protected IRemovableDriveHandler _removableDriveHandler;

    #endregion

    #region Ctor

    public RemovableDriveViewSpecification(string drive) :
        base(string.Empty, new Guid[] {}, new Guid[] {})
    {
      _driveInfo = new DriveInfo(drive);
      UpdateRemovableDriveHandler();
    }

    #endregion

    #region Public methods

    public static ICollection<RemovableDriveViewSpecification> CreateViewSpecificationsForRemovableDrives(IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds)
    {
      return DriveInfo.GetDrives().Where(
          driveInfo => driveInfo.DriveType == DriveType.CDRom || driveInfo.DriveType == DriveType.Removable).Select(
          driveInfo => new RemovableDriveViewSpecification(driveInfo.ToString())).ToList();
    }

    #endregion

    #region Protected methods

    protected void UpdateRemovableDriveHandler()
    {
      _removableDriveHandler = VideoDriveHandler.TryCreateVideoDriveHandler(_driveInfo, Consts.NECESSARY_VIDEO_MIAS) ??
          AudioCDDriveHandler.TryCreateAudioCDDriveHandler(_driveInfo) ??
          MultimediaDriveHandler.TryCreateMultimediaCDDriveHandler(_driveInfo, Consts.NECESSARY_VIDEO_MIAS, Consts.NECESSARY_IMAGE_MIAS, Consts.NECESSARY_AUDIO_MIAS) ??
          (IRemovableDriveHandler) new UnknownRemovableDriveHandler(_driveInfo);
    }

    #endregion

    #region Base overrides

    public override bool CanBeBuilt
    {
      get { return true; }
    }

    public DriveInfo Drive
    {
      get { return _driveInfo; }
    }

    public override string ViewDisplayName
    {
      get
      {
        string volumeLabel = _removableDriveHandler.VolumeLabel;
        return _driveInfo.RootDirectory.Name + (string.IsNullOrEmpty(volumeLabel) ? string.Empty : (" (" + volumeLabel + ")"));
      }
    }

    public bool IsDriveReady
    {
      get { return _driveInfo.IsReady; }
    }

    public override IViewChangeNotificator CreateChangeNotificator()
    {
      return new RemovableDriveChangeNotificator(_driveInfo.Name);
    }

    public override IEnumerable<MediaItem> GetAllMediaItems()
    {
      return _removableDriveHandler.GetAllMediaItems();
    }

    protected internal override void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications)
    {
      UpdateRemovableDriveHandler();
      mediaItems = _removableDriveHandler.MediaItems;
      subViewSpecifications = _removableDriveHandler.SubViewSpecifications;
    }

    public override string ToString()
    {
      return _driveInfo.ToString();
    }

    #endregion
  }
}
