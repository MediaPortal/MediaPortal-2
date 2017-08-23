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

namespace MediaPortal.Common.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// <see cref="SelectionMarkerAspect"/> is a transient aspect which can be used to mark single <see cref="MediaItem"/>s as "selected".
  /// It is not intended to be persisted in MediaLibrary.
  /// </summary>
  public static class SelectionMarkerAspect
  {
    /// <summary>
    /// Media item aspect id of the selection marker aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("2D11A5EE-F996-416F-B700-3C371E78C390");
  }
}
