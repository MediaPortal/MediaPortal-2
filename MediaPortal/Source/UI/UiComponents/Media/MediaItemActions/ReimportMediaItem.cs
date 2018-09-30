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

using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public class ReimportMediaItem : AbstractMediaItemAction
  {
    public ReimportMediaItem()
    {
    }

    public override Task<bool> IsAvailableAsync(MediaItem mediaItem)
    {
      try
      {
        MediaItemMatchModel mimm = ServiceRegistration.Get<IWorkflowManager>().GetModel(MediaItemMatchModel.MODEL_ID_MIMATCH) as MediaItemMatchModel;
        return Task.FromResult(mimm?.IsValidMediaItem(mediaItem) ?? false);
      }
      catch (Exception)
      {
        return Task.FromResult(false);
      }
    }

    public override async Task<AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>> ProcessAsync(MediaItem mediaItem)
    {
      // If the MediaItem was loaded from ML, remove it there as well.
      if (IsManagedByMediaLibrary(mediaItem))
      {
        IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
        if (cd == null)
          return new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(false, ContentDirectoryMessaging.MediaItemChangeType.None);

        MediaItemMatchModel mimm = ServiceRegistration.Get<IWorkflowManager>().GetModel(MediaItemMatchModel.MODEL_ID_MIMATCH) as MediaItemMatchModel;
        IEnumerable<MediaItemAspect> aspects = await mimm.OpenSelectMatchDialogAsync(mediaItem).ConfigureAwait(false);
        if (aspects != null)
        {
          await cd.ReimportMediaItemMetadataAsync(mediaItem.MediaItemId, aspects);

          //After refresh is completed on server a change message will be fired
          return new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(true, ContentDirectoryMessaging.MediaItemChangeType.None);
        }
      }
      return new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(false, ContentDirectoryMessaging.MediaItemChangeType.None);
    }
  }
}
