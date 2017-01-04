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

using System.Collections.Generic;
using System.IO;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.UiComponents.Media.Views.RemovableMediaDrives
{
  /// <summary>
  /// Base class for an <see cref="IRemovableDriveHandler"/>. Provides a default handling for the <see cref="VolumeLabel"/> property.
  /// </summary>
  public abstract class BaseDriveHandler : IRemovableDriveHandler
  {
    #region Protected fields

    protected DriveInfo _driveInfo;
    protected string _volumeLabel;

    #endregion

    protected BaseDriveHandler(DriveInfo driveInfo)
    {
      _driveInfo = driveInfo;
      _volumeLabel = null;
      if (_driveInfo.IsReady)
        try
        {
          _volumeLabel = _driveInfo.VolumeLabel;
        }
        catch (IOException)
        {
        }
      _volumeLabel = _driveInfo.RootDirectory.Name + (string.IsNullOrEmpty(_volumeLabel) ? string.Empty : ("(" + _volumeLabel + ")"));
    }

    /// <summary>
    /// Returns a string of the form <c>"D: (Videos)"</c> or <c>"D:"</c>.
    /// </summary>
    public virtual string VolumeLabel
    {
      get { return _volumeLabel; }
    }

    public abstract IList<MediaItem> MediaItems { get; }
    public abstract IList<ViewSpecification> SubViewSpecifications { get; }
    public abstract IEnumerable<MediaItem> GetAllMediaItems();
  }
}