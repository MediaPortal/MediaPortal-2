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
using MediaPortal.Core.MediaManagement;
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

    public RemovableDriveViewSpecification(string drive, IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds) :
        base(string.Empty, necessaryMIATypeIds, optionalMIATypeIds)
    {
      _driveInfo = new DriveInfo(drive);
      UpdateRemovableDriveHandler();
    }

    #endregion

    #region Public methods

    public static IEnumerable<RemovableDriveViewSpecification> CreateViewSpecificationsForRemovableDrives(IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds)
    {
      return DriveInfo.GetDrives().Where(
          driveInfo => driveInfo.DriveType == DriveType.CDRom || driveInfo.DriveType == DriveType.Removable).Select(
          driveInfo => new RemovableDriveViewSpecification(driveInfo.ToString(), necessaryMIATypeIds, optionalMIATypeIds));
    }

    #endregion

    #region Protected methods

    protected void UpdateRemovableDriveHandler()
    {
      _removableDriveHandler = VideoDriveHandler.TryCreateVideoDriveHandler(_driveInfo) ??
          AudioCDDriveHandler.TryCreateAudioDriveHandler(_driveInfo, _necessaryMIATypeIds, _optionalMIATypeIds) ??
          (IRemovableDriveHandler) new UnknownRemovableDriveHandler(_driveInfo);
    }

    #endregion

    #region Base overrides

    public override bool CanBeBuilt
    {
      get { return true; }
    }

    public override string ViewDisplayName
    {
      get
      {
        string volumeLabel = _removableDriveHandler.VolumeLabel;
        return _driveInfo.RootDirectory.Name + (string.IsNullOrEmpty(volumeLabel) ? string.Empty : ("(" + volumeLabel + ")"));
      }
    }

    public bool IsDriveReady
    {
      get { return _driveInfo.IsReady; }
    }

    public override IViewChangeNotificator GetChangeNotificator()
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

    #endregion
  }
}
