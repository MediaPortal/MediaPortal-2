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

using System.Collections.Generic;
using System.IO;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.UiComponents.Media.Views.RemovableMediaDrives
{
  public abstract class BaseDriveHandler : IRemovableDriveHandler
  {
    #region Protected fields

    protected DriveInfo _driveInfo;

    #endregion

    protected BaseDriveHandler(DriveInfo driveInfo)
    {
      _driveInfo = driveInfo;
    }

    public virtual string VolumeLabel
    {
      get
      {
        string volumeLabel = _driveInfo.VolumeLabel;
        return _driveInfo.RootDirectory.Name + (string.IsNullOrEmpty(volumeLabel) ? string.Empty : ("(" + volumeLabel + ")"));
      }
    }

    public abstract IList<MediaItem> MediaItems { get; }
    public abstract IList<ViewSpecification> SubViewSpecifications { get; }
    public abstract IEnumerable<MediaItem> GetAllMediaItems();
  }
}