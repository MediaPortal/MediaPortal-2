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

using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public class AddSingleToPlaylist : AbstractMediaItemAction
  {
    public override Task<bool> IsAvailableAsync(MediaItem mediaItem)
    {
      // We only add all items to playlist for Image and Audio. Other media types are using single item only.
      var result = mediaItem.Aspects.ContainsKey(ImageAspect.ASPECT_ID) || mediaItem.Aspects.ContainsKey(AudioAspect.ASPECT_ID);
      return Task.FromResult(result);
    }

    public override async Task<AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>> ProcessAsync(MediaItem mediaItem)
    {
      await PlayItemsModel.PlayOrEnqueueItem(mediaItem, false, PlayerContextConcurrencyMode.None);
      AsyncResult<ContentDirectoryMessaging.MediaItemChangeType> result = new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(true, ContentDirectoryMessaging.MediaItemChangeType.None);
      return result;
    }
  }
}
