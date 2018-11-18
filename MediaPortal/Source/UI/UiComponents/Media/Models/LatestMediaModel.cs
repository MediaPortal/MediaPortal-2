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
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.UserManagement;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.UiComponents.Media.Settings;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.Helpers;

namespace MediaPortal.UiComponents.Media.Models
{
  public class LatestMediaModel : IWorkflowModel
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
    public const string LATEST_MEDIA_MODEL_ID_STR = "11193401-D85D-4D50-9825-E9EB34D87062";

    // ID variables
    public static readonly Guid LATEST_MEDIA_MODEL_ID = new Guid(LATEST_MEDIA_MODEL_ID_STR);

    #endregion

    public const int QUERY_LIMIT = 5;

    public delegate PlayableMediaItem MediaItemToListItemAction(MediaItem mediaItem);

    public ItemsList AllItems { get; private set; }

    public LatestMediaModel()
    {
      AllItems = new ItemsList();
    }

    protected void ClearAll()
    {
      AllItems.Clear();
      AllItems.FireChange();
    }

    protected void Update()
    {
      ClearAll();
      var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory == null)
      {
        return;
      }

      ItemsList l1 = new ItemsList();
      ItemsList l2 = new ItemsList();
      ItemsList l3 = new ItemsList();
      ItemsList l4 = new ItemsList();
      var t1 = FillListAsync(contentDirectory, Consts.NECESSARY_MOVIES_MIAS, l1, item => new MovieItem(item));
      var t2 = FillListAsync(contentDirectory, Consts.NECESSARY_EPISODE_MIAS, l2, item => new EpisodeItem(item));
      var t3 = FillListAsync(contentDirectory, Consts.NECESSARY_IMAGE_MIAS, l3, item => new ImageItem(item));
      var t4 = FillListAsync(contentDirectory, Consts.NECESSARY_AUDIO_MIAS, l4, item => new AudioItem(item));
      Task.WaitAll(t1, t2, t3, t4);
      AllItems.Add(new TitledItem("[Media.MoviesMenuItem]", l1));
      AllItems.Add(new TitledItem("[Media.SeriesMenuItem]", l2));
      AllItems.Add(new TitledItem("[Media.ImagesMenuItem]", l3));
      AllItems.Add(new TitledItem("[Media.AudioMenuItem]", l4));

      AllItems.FireChange();
    }

    protected static async Task FillListAsync(IContentDirectory contentDirectory, Guid[] necessaryMIATypeIds, ItemsList list, MediaItemToListItemAction converterAction)
    {
      MediaItemQuery query = new MediaItemQuery(necessaryMIATypeIds, null)
      {
        Limit = QUERY_LIMIT, // Last 5 imported items
        SortInformation = new List<ISortInformation> { new AttributeSortInformation(ImporterAspect.ATTR_DATEADDED, SortDirection.Descending) }
      };

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
      {
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;
      }
      bool showVirtual = VirtualMediaHelper.ShowVirtualMedia(necessaryMIATypeIds);

      var items = await contentDirectory.SearchAsync(query, false, userProfile, showVirtual);
      list.Clear();
      foreach (MediaItem mediaItem in items)
      {
        PlayableMediaItem listItem = converterAction(mediaItem);
        listItem.Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(listItem.MediaItem));
        list.Add(listItem);
      }
      list.FireChange();
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

    public Guid ModelId
    {
      get { return LATEST_MEDIA_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      Update();
      SetLayout();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Don't disable the current navigation data when we leave our model - the last navigation data must be
      // available in sub workflows, for example to make the GetMediaItemsFromCurrentView method work
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // The last navigation data was not disabled so we don't need to enable it here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
