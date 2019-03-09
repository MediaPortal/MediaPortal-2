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

using System;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.UiComponents.Media.Extensions;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public class AddAllToPlaylist : AbstractMediaItemAction, IDeferredMediaItemAction
  {
    public static Guid ACTION_ID_ADD_ALL_TO_PLAYLIST = new Guid("09243059-EE44-460d-8412-2E994CCB5A98");

    public override Task<bool> IsAvailableAsync(MediaItem mediaItem)
    {
      var result = ServiceRegistration.Get<IWorkflowManager>().MenuStateActions.ContainsKey(ACTION_ID_ADD_ALL_TO_PLAYLIST);
      return Task.FromResult(result);
    }

    public override Task<AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>> ProcessAsync(MediaItem mediaItem)
    {
      var result = new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(false, ContentDirectoryMessaging.MediaItemChangeType.None);
      result.Success = ServiceRegistration.Get<IWorkflowManager>().TryExecuteAction(ACTION_ID_ADD_ALL_TO_PLAYLIST);
      return Task.FromResult(result);
    }
  }
}
