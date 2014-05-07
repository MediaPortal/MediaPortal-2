#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public class RemoveFromML : AbstractMediaItemAction
  {
    private const string ACTION_NAME = "[Media.RemoveFromML]";

    public RemoveFromML()
    {
      Caption = ACTION_NAME;
    }

    public override bool IsAvailable(MediaItem mediaItem)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      return cd != null;
    }

    public override bool Process(MediaItem mediaItem, out ContentDirectoryMessaging.MediaItemChangeType changeType)
    {
      changeType = ContentDirectoryMessaging.MediaItemChangeType.None;
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return false;

      var rl = mediaItem.GetResourceLocator();
      cd.DeleteMediaItemOrPath(rl.NativeSystemId, rl.NativeResourcePath, true);

      changeType = ContentDirectoryMessaging.MediaItemChangeType.Deleted;
      return true;
    }
  }
}
