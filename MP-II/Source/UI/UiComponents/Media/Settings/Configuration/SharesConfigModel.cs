#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MediaProviders;
using MediaPortal.Media.ClientMediaManager;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Workflow;
using MediaPortal.Utilities;

namespace UiComponents.Media.Settings.Configuration
{
  /// <summary>
  /// Provides a model to attend the complex shares configuration process in the MP-II configuration.
  /// This model is the workflow model for this process.
  /// </summary>
  public class SharesConfigModel : IWorkflowModel
  {
    #region Consts

    public const string SHARESCONFIG_MODEL_ID_STR = "1768FC91-86B9-4f78-8A4C-E204F0D51502";

    public const string SHARES_OVERVIEW_STATE_ID_STR = "36B3F24A-29B4-4cb4-BC7D-434C51491CD2";
    public const string SHARE_ADD_CHOOSE_MEDIA_PROVIDER_STATE_ID_STR = "F3163500-3015-4a6f-91F6-A3DA5DC3593C";
    public const string REMOVE_SHARES_STATE_ID_STR = "900BA520-F989-48c0-B076-5DAD61945845";

    public const string SHARE_NAME_KEY = "Name";
    public const string SHARE_PATH_KEY = "Path";
    public const string SHARE_MEDIAPROVIDER_KEY = "MediaProvider";
    public const string SHARE_CATEGORY_KEY = "Category";

    #endregion

    #region Protected fields

    protected ItemsList _sharesList;

    #endregion

    #region Ctor

    public SharesConfigModel()
    {
      _sharesList = new ItemsList();
      UpdateSharesList();
    }

    #endregion

    #region Properties

    public ItemsList Shares
    {
      get { return _sharesList; }
    }

    #endregion

    #region Protected methods

    protected void UpdateSharesList()
    {
      // TODO: Re-validate this when we have implemented the communication with the MP-II server
      _sharesList.Clear();
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      foreach (ShareDescriptor share in mediaManager.GetSharesBySystem(SystemName.GetLocalSystemName()).Values)
      {
        ListItem shareItem = new ListItem(SHARE_NAME_KEY, share.Name);
        shareItem.SetLabel(SHARE_PATH_KEY, share.Path);
        IMediaProvider mediaProvider;
        if (!mediaManager.LocalMediaProviders.TryGetValue(share.MediaProviderId, out mediaProvider))
          mediaProvider = null;
        shareItem.SetLabel(SHARE_MEDIAPROVIDER_KEY, mediaProvider == null ? null : mediaProvider.Metadata.Name);
        string categories = StringUtils.Join(", ", share.MediaCategories);
        shareItem.SetLabel(SHARE_CATEGORY_KEY, categories);
        _sharesList.Add(shareItem);
      }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(SHARESCONFIG_MODEL_ID_STR); }
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // TODO
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void UpdateMenuActions(NavigationContext context, ICollection<WorkflowStateAction> actions)
    {
      // Not used yet, currently we don't show any menu during the shares configuration process.
      // Perhaps we'll add menu actions for different convenience procedures like initializing the
      // shares to their default setting, ...
    }

    public void UpdateContextMenuActions(NavigationContext context, ICollection<WorkflowStateAction> actions)
    {
      // Not used yet
    }

    #endregion
  }
}