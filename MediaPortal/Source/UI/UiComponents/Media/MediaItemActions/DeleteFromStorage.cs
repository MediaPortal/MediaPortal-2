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
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public class DeleteFromStorage : AbstractMediaItemAction
  {
    public override bool IsAvailable(MediaItem mediaItem)
    {
      try
      {
        var rl = mediaItem.GetResourceLocator();
        using (var ra = rl.CreateAccessor())
          return ra is IResourceDeletor;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public override bool Process(MediaItem mediaItem, out ContentDirectoryMessaging.MediaItemChangeType changeType)
    {
      changeType = ContentDirectoryMessaging.MediaItemChangeType.None;

      var rl = mediaItem.GetResourceLocator();
      using (var ra = rl.CreateAccessor())
      {
        var rad = ra as IResourceDeletor;
        if (rad == null)
          return false;

        // First try to delete the file from storage.
        if (rad.Delete())
        {
          changeType = ContentDirectoryMessaging.MediaItemChangeType.Deleted;

          // If the MediaItem was loaded from ML, remove it there as well.
          if (IsManagedByMediaLibrary(mediaItem))
          {
            IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
            if (cd == null)
              return true;

            cd.DeleteMediaItemOrPath(rl.NativeSystemId, rl.NativeResourcePath, true);
          }
          return true;
        }
      }
      return false;
    }
  }
}
