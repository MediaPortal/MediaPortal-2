#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Extensions;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public class MediaInfoAction : AbstractMediaItemAction, IDeferredMediaItemAction
  {
    readonly Dictionary<Guid, Guid> _aspectScreens = new Dictionary<Guid, Guid>
    {
      { ImageAspect.ASPECT_ID, MediaInfoModel.WF_MEDIA_INFO_IMAGES },
      { VideoAspect.ASPECT_ID, MediaInfoModel.WF_MEDIA_INFO_VIDEO },
      { AudioAspect.ASPECT_ID, MediaInfoModel.WF_MEDIA_INFO_AUDIO },
    };

    public MediaInfoAction()
    {
    }

    public override Task<bool> IsAvailableAsync(MediaItem mediaItem)
    {
      try
      {
        if (!IsManagedByMediaLibrary(mediaItem))
          return Task.FromResult(false);

        var mediaInfoAvailable = mediaItem.Aspects.Any(a => _aspectScreens.ContainsKey(a.Key));
        return Task.FromResult(mediaInfoAvailable);
      }
      catch (Exception)
      {
        return Task.FromResult(false);
      }
    }

    public override async Task<AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>> ProcessAsync(MediaItem mediaItem)
    {
      // If the MediaItem was loaded from ML
      bool result = false;
      if (IsManagedByMediaLibrary(mediaItem))
      {
        foreach (KeyValuePair<Guid, Guid> aspectScreen in _aspectScreens)
        {
          if (mediaItem.Aspects.ContainsKey(aspectScreen.Key))
          {
            var wf = ServiceRegistration.Get<IWorkflowManager>();
            var contextConfig = new NavigationContextConfig { AdditionalContextVariables = new Dictionary<string, object> { { Consts.KEY_MEDIA_ITEM, mediaItem } } };
            // Push the new state asynchronously to queue it until after any current navigation state change has finished.
            wf.NavigatePushAsync(aspectScreen.Value, contextConfig);
            result = true;
            break;
          }
        }
      }
      return new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(result, ContentDirectoryMessaging.MediaItemChangeType.None);
    }

    public bool DoesChangeWorkflow { get; set; } = true;
  }
}
