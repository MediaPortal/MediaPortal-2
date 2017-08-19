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
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.UiComponents.Media.Settings;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.SlimTv.Client.Models.Navigation;
using MediaPortal.Plugins.SlimTv.Client.TvHandler;
using MediaPortal.UiComponents.Media.Models;
using System.Linq;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  public class SlimTvFavoriteMediaModel : SlimTvModelBase
  {
    public class TitledItem : ListItem
    {
      public ItemsList Items { get; private set; }

      public TitledItem()
        : this(string.Empty)
      {
      }

      public TitledItem(string title, ItemsList nestedList = null)
      {
        Items = nestedList ?? new ItemsList();
        SetLabel(Consts.KEY_NAME, title);
      }
    }

    #region Consts

    // Global ID definitions and references
    public const string FAVORITE_MEDIA_MODEL_ID_STR = "CD7E4464-3245-460E-860A-696D6A863951";

    // ID variables
    public static readonly Guid FAVORITE_MEDIA_MODEL_ID = new Guid(FAVORITE_MEDIA_MODEL_ID_STR);

    #endregion

    public const int QUERY_LIMIT = 5;

    public delegate PlayableMediaItem MediaItemToListItemAction(MediaItem mediaItem);

    public ItemsList AllItems { get; private set; }

    public SlimTvFavoriteMediaModel()
    {
      AllItems = new ItemsList();
    }

    protected void ClearAll()
    {
      AllItems.Clear();
      AllItems.FireChange();
    }

    protected override void Update()
    {
      ClearAll();
      var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory == null)
      {
        return;
      }

      ItemsList list = new ItemsList();
      FillChannelList(list);
      AllItems.Add(new TitledItem("[SlimTvClient.ChannelsMenuItem]", list));

      list = new ItemsList();
      FillRecordingList(contentDirectory, SlimTvConsts.NECESSARY_RECORDING_MIAS, list, item => new RecordingItem(item));
      AllItems.Add(new TitledItem("[SlimTvClient.RecordingsMenuItem]", list));

      AllItems.FireChange();
    }

    protected static void FillRecordingList(IContentDirectory contentDirectory, Guid[] necessaryMIAs, ItemsList list, MediaItemToListItemAction converterAction)
    {
      Guid? userProfile = null;
      bool applyUserRestrictions = false;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
        applyUserRestrictions = userProfileDataManagement.ApplyUserRestriction;
      }
      bool showVirtual = ShowVirtualSetting.ShowVirtualMedia(necessaryMIAs);

      MediaItemQuery query = new MediaItemQuery(necessaryMIAs, null)
      {
        Filter = userProfile.HasValue ? new NotFilter(new EmptyUserDataFilter(userProfile.Value, UserDataKeysKnown.KEY_PLAY_COUNT)) : null,
        Limit = QUERY_LIMIT, // Most watched 5 items
        SortInformation = new List<ISortInformation> { new DataSortInformation(UserDataKeysKnown.KEY_PLAY_COUNT, SortDirection.Descending) }
      };

      var items = contentDirectory.Search(query, false, userProfile, showVirtual, applyUserRestrictions);
      list.Clear();
      foreach (MediaItem mediaItem in items)
      {
        PlayableMediaItem listItem = converterAction(mediaItem);
        listItem.Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(listItem.MediaItem));
        list.Add(listItem);
      }
      list.FireChange();
    }

    protected static void FillChannelList(ItemsList list)
    {
      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;

        IEnumerable<Tuple<int, string>> channelList;
        if (userProfileDataManagement.UserProfileDataManagement.GetUserAdditionalDataList(userProfile.Value, UserDataKeysKnown.KEY_PLAY_COUNT, out channelList))
        {
          int count = 0;
          IOrderedEnumerable<Tuple<int, string>> sortedList = channelList.OrderByDescending(c => c.Item2);
          foreach (var channelData in sortedList)
          {
            IChannel channel = ChannelContext.Instance.Channels.FirstOrDefault(c => c.ChannelId == channelData.Item1);
            if (channel != null)
            {
              count++;
              ChannelProgramListItem item = new ChannelProgramListItem(channel, null)
              {
                Command = new MethodDelegateCommand(() => TuneChannel(channel)),
                Selected = false
              };
              item.AdditionalProperties["CHANNEL"] = channel;
              list.Add(item);
            }
            if (count >= QUERY_LIMIT)
              break;
          }
          list.FireChange();
        }
      }
    }

    protected void SetLayout()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      ViewModeModel vwm = workflowManager.GetModel(ViewModeModel.VM_MODEL_ID) as ViewModeModel;
      if (vwm != null)
      {
        vwm.LayoutType = LayoutType.GridLayout;
        vwm.LayoutSize = LayoutSize.Medium;
      }
    }

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return FAVORITE_MEDIA_MODEL_ID; }
    }

    public override void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      base.EnterModelContext(oldContext, newContext);
      Update();
      SetLayout();
    }

    public override void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Don't disable the current navigation data when we leave our model - the last navigation data must be
      // available in sub workflows, for example to make the GetMediaItemsFromCurrentView method work
    }

    public override void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // The last navigation data was not disabled so we don't need to enable it here
    }

    #endregion
  }
}
