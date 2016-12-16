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
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Backend.MediaLibrary;

namespace MediaPortal.Backend.Services.MediaManagement
{
  /// <summary>
  /// Media item aspect type registration class for the MediaPortal server. Stores all registered media item aspect types
  /// and automatically registers them at the media library.
  /// </summary>
  public class MediaItemAspectTypeRegistration : IMediaItemAspectTypeRegistration
  {
    public IDictionary<Guid, MediaItemAspectMetadata> LocallyKnownMediaItemAspectTypes
    {
      get
      {
        IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
        return mediaLibrary.GetManagedMediaItemAspectMetadata();
      }
    }

    public void RegisterLocallyKnownMediaItemAspectType(MediaItemAspectMetadata miam)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      mediaLibrary.AddMediaItemAspectStorage(miam);
    }
  }
}
