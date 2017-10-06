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
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.Helpers;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.Navigation;
using System;
using System.Linq;
using MediaPortal.Common.UserProfileDataManagement;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Media.MediaLists
{
  public abstract class BaseMediaListProvider : IMediaListProvider
  {
    public delegate PlayableMediaItem PlayableMediaItemToListItemAction(MediaItem mediaItem);
    public delegate PlayableContainerMediaItem PlayableContainerMediaItemToListItemAction(MediaItem mediaItem);

    protected Guid[] _necessaryMias;
    protected PlayableMediaItemToListItemAction _playableConverterAction;
    protected PlayableContainerMediaItemToListItemAction _playableContainerConverterAction;
    protected MediaItemQuery _mediaQuery;
    protected object _syncLock = new object();

    public BaseMediaListProvider()
    {
      AllItems = new ItemsList();
    }

    public ItemsList AllItems { get; private set; }

    public UserProfile CurrentUserProfile
    {
      get
      {
        IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
        if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        {
          return userProfileDataManagement.CurrentUser;
        }
        return null;
      }
    }

    public IFilter AppendUserFilter(IFilter filter, IEnumerable<Guid> filterMias)
    {
      IFilter userFilter = CertificationHelper.GetUserCertificateFilter(filterMias);
      if (userFilter != null)
      {
        return filter != null ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, userFilter) : userFilter;
      }
      return filter;
    }

    public virtual bool UpdateItems(int maxItems, UpdateReason updateReason)
    {
      var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory == null)
        return false;

      if ((updateReason & UpdateReason.Forced) == UpdateReason.Forced)
      {
        Guid? userProfile = CurrentUserProfile?.ProfileId;
        bool showVirtual = VirtualMediaHelper.ShowVirtualMedia(_necessaryMias);

        var items = contentDirectory.Search(_mediaQuery, false, userProfile, showVirtual);
        if (_playableConverterAction != null)
        {
          lock (_syncLock)
          {
            if (!AllItems.Select(pmi => ((PlayableMediaItem)pmi).MediaItem.MediaItemId).SequenceEqual(items.Select(mi => mi.MediaItemId)))
            {
              AllItems.Clear();
              foreach (MediaItem mediaItem in items)
              {
                PlayableMediaItem listItem = _playableConverterAction(mediaItem);
                listItem.Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(listItem.MediaItem));
                AllItems.Add(listItem);
              }
              AllItems.FireChange();
            }
          }
        }
        else if (_playableContainerConverterAction != null)
        {
          lock (_syncLock)
          {
            if (!AllItems.Select(pmi => ((PlayableContainerMediaItem)pmi).MediaItem.MediaItemId).SequenceEqual(items.Select(mi => mi.MediaItemId)))
            {
              AllItems.Clear();
              foreach (MediaItem mediaItem in items)
              {
                PlayableContainerMediaItem listItem = _playableContainerConverterAction(mediaItem);
                AllItems.Add(listItem);
              }
              AllItems.FireChange();
            }
          }
        }
      }
      return true;
    }
  }
}
