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
  /// Removable drive handler which doesn't produce any media items or sub view specifications. It can be used as
  /// "no op" drive handler for unknown drive contents.
  /// </summary>
  public class UnknownRemovableDriveHandler : BaseDriveHandler
  {
    public UnknownRemovableDriveHandler(DriveInfo driveInfo) : base(driveInfo) { }

    public override IList<MediaItem> MediaItems
    {
      get { return new List<MediaItem>(0); }
    }

    public override IList<ViewSpecification> SubViewSpecifications
    {
      get { return new List<ViewSpecification>(0); }
    }

    public override IEnumerable<MediaItem> GetAllMediaItems()
    {
      yield break;
    }
  }
}