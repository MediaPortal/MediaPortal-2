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
using MediaPortal.Common.Commands;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.Helpers;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.Navigation;
using System;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Media.MediaLists
{
  public abstract class BaseFavoriteMediaListProvider : IMediaListProvider
  {
    public delegate PlayableMediaItem MediaItemToListItemAction(MediaItem mediaItem);

    protected Guid[] _necessaryMias;
    protected MediaItemToListItemAction _converterAction;

    public BaseFavoriteMediaListProvider()
    {
      AllItems = new ItemsList();
    }

    public ItemsList AllItems { get; private set; }

    public bool UpdateItems(int maxItems)
    {
      var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory == null)
        return false;

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
      }
      bool showVirtual = VirtualMediaHelper.ShowVirtualMedia(_necessaryMias);

      MediaItemQuery query = new MediaItemQuery(_necessaryMias, null)
      {
        Filter = userProfile.HasValue ? new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_COUNT)) : null,
        Limit = (uint)maxItems, // Last 5 imported items
        SortInformation = new List<ISortInformation> { new DataSortInformation(UserDataKeysKnown.KEY_PLAY_COUNT, SortDirection.Descending) }
      };

      var items = contentDirectory.Search(query, false, userProfile, showVirtual);
      AllItems.Clear();
      foreach (MediaItem mediaItem in items)
      {
        PlayableMediaItem listItem = _converterAction(mediaItem);
        listItem.Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(listItem.MediaItem));
        AllItems.Add(listItem);
      }
      AllItems.FireChange();

      return true;
    }
  }
}
