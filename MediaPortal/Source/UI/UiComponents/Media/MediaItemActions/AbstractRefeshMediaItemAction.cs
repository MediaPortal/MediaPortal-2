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

using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using System;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public class AbstractRefeshMediaItemAction : AbstractMediaItemAction
  {
    protected bool _clearMetadata = false;

    public override bool IsAvailable(MediaItem mediaItem)
    {
      try
      {
        if (mediaItem.PrimaryResources.Count > 0 || mediaItem.IsStub)
        {
          var rl = mediaItem.GetResourceLocator();
          return rl != null;
        }
        return false;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public override bool Process(MediaItem mediaItem, out ContentDirectoryMessaging.MediaItemChangeType changeType)
    {
      changeType = ContentDirectoryMessaging.MediaItemChangeType.None;

      // If the MediaItem was loaded from ML, remove it there as well.
      if (IsManagedByMediaLibrary(mediaItem))
      {
        IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
        if (cd == null)
          return true;

        var rl = mediaItem.GetResourceLocator();
        cd.RefreshMediaItemMetadata(rl.NativeSystemId, mediaItem.MediaItemId, _clearMetadata);

        changeType = ContentDirectoryMessaging.MediaItemChangeType.Updated;

        return true;
      }
      return false;
    }
  }
}