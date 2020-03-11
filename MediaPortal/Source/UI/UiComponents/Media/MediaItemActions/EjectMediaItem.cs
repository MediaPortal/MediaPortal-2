#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using System;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public class EjectMediaItem : AbstractMediaItemAction
  {
    public EjectMediaItem()
    {
    }

    public override Task<bool> IsAvailableAsync(MediaItem mediaItem)
    {
      try
      {
        if (IsManagedByMediaLibrary(mediaItem))
          return Task.FromResult(false);

        string path = GetRemovableMediaItemPath(mediaItem);
        if (string.IsNullOrEmpty(path))
          return Task.FromResult(false);

        return Task.FromResult(true);
      }
      catch (Exception)
      {
        return Task.FromResult(false);
      }
    }

    public override Task<AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>> ProcessAsync(MediaItem mediaItem)
    {
      // If the MediaItem was not loaded from ML
      if (!IsManagedByMediaLibrary(mediaItem))
      {
        string path = GetRemovableMediaItemPath(mediaItem);
        if (!string.IsNullOrEmpty(path))
        {
          DriveUtils.EjectDrive(path);
        }
      }
      return Task.FromResult(new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(false, ContentDirectoryMessaging.MediaItemChangeType.None));
    }

    private string GetRemovableMediaItemPath(MediaItem mediaItem)
    {
      if (mediaItem == null)
        return null;

      foreach (var pra in mediaItem.PrimaryResources)
      {
        var resPath = ResourcePath.Deserialize(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
        var dosPath = LocalFsResourceProviderBase.ToDosPath(resPath);
        if (string.IsNullOrEmpty(dosPath))
          continue;
        if (DriveUtils.IsRemovable(dosPath) || DriveUtils.IsDVD(dosPath))
          return dosPath;
      }
      return null;
    }
  }
}
