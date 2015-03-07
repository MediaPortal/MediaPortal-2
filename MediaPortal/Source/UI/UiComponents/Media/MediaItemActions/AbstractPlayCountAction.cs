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

using System;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public abstract class AbstractPlayCountAction : AbstractMediaItemAction
  {
    protected abstract bool AppliesForPlayCount(int playCount);
    protected abstract int GetNewPlayCount();

    public override bool IsAvailable(MediaItem mediaItem)
    {
      int playCount;
      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MediaAspect.ATTR_PLAYCOUNT, 0, out playCount))
        return false;
      if (!IsManagedByMediaLibrary(mediaItem) || !AppliesForPlayCount(playCount))
        return false;
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

      Guid parentDirectoryId;
      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, out parentDirectoryId))
        return false;

      MediaItemAspect.SetAttribute(mediaItem.Aspects, MediaAspect.ATTR_PLAYCOUNT, GetNewPlayCount());

      cd.AddOrUpdateMediaItem(parentDirectoryId, rl.NativeSystemId, rl.NativeResourcePath, mediaItem.Aspects.Values);

      changeType = ContentDirectoryMessaging.MediaItemChangeType.Updated;
      return true;
    }
  }
}
