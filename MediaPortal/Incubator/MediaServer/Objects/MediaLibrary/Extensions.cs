﻿#region Copyright (C) 2007-2017 Team MediaPortal

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
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public static class Extensions
  {
    public static MediaItem GetMediaItem(this IMediaLibrary mediaLibrary, Guid mediaItemId, IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes)
    {
      var items = mediaLibrary.LoadCustomPlaylist(new List<Guid>() { mediaItemId }, necessaryMIATypes, optionalMIATypes);
      return items.Count > 0 ? items[0] : null;
    }
  }
}
