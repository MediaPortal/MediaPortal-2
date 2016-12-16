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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Extensions;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models
{
  public class MediaItemsActionModel : IWorkflowModel
  {
    #region Consts

    public const string STR_MODEL_ID = "970649B2-CAE8-4830-A985-E5E78F3CB24F";
    public static readonly Guid MODEL_ID = new Guid(STR_MODEL_ID);

    // Constants for passing parameters into workflow states
    public const string KEY_MEDIA_ITEM = "MediaItemsActionModel: MediaItem";

    #endregion

    #region Protected fields

    // Action menu
    protected ItemsList _mediaItemActionItems = new ItemsList();
    private readonly List<MediaItemActionExtension> _actions = new List<MediaItemActionExtension>();
    private IPluginItemStateTracker _mediaActionPluginItemStateTracker; // Lazy initialized

    #endregion

    #region Public members

    /// <summary>
    /// Provides a list of items to be shown in the play menu.
    /// </summary>
    public ItemsList MediaItemActionItems
    {
      get { return _mediaItemActionItems; }
    }

    /// <summary>
    /// Tries to show actions for the given <paramref name="mediaItem"/>. This method can also be called from other models.
    /// </summary>
    /// <param name="mediaItem">MediaItem</param>
    public void ShowMediaItemActions(MediaItem mediaItem)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_CHECK_MEDIA_ITEM_ACTION, new NavigationContextConfig
      {
        AdditionalContextVariables = new Dictionary<string, object> { { KEY_MEDIA_ITEM, mediaItem } }
      });
    }

    public void Select(ListItem item)
    {
      if (item == null)
        return;
      object actionObj;
      object mediaItemObj;
      if (!item.AdditionalProperties.TryGetValue(Consts.KEY_MEDIA_ITEM_ACTION, out actionObj) || !item.AdditionalProperties.TryGetValue(Consts.KEY_MEDIA_ITEM, out mediaItemObj))
        return;

      IMediaItemAction action = actionObj as IMediaItemAction;
      MediaItem mediaItem = mediaItemObj as MediaItem;
      if (action == null || mediaItem == null)
        return;

      try
      {
        ContentDirectoryMessaging.MediaItemChangeType changeType;
        if (action.Process(mediaItem, out changeType) && changeType != ContentDirectoryMessaging.MediaItemChangeType.None)
        {
          ContentDirectoryMessaging.SendMediaItemChangedMessage(mediaItem, changeType);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error executing MediaItemAction '{0}':", ex, action.GetType());
      }
    }

    #endregion

    #region Protected members

    protected void BuildExtensions()
    {
      if (_mediaActionPluginItemStateTracker != null)
        return;

      _mediaActionPluginItemStateTracker = new FixedItemStateTracker("MediaItemsActionModel - Extension registration");

      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(MediaItemActionBuilder.MEDIA_EXTENSION_PATH))
      {
        try
        {
          MediaItemActionExtension mediaExtension = pluginManager.RequestPluginItem<MediaItemActionExtension>(
              MediaItemActionBuilder.MEDIA_EXTENSION_PATH, itemMetadata.Id, _mediaActionPluginItemStateTracker);
          if (mediaExtension == null)
            ServiceRegistration.Get<ILogger>().Warn("Could not instantiate Media extension with id '{0}'", itemMetadata.Id);
          else
          {
            Type extensionClass = mediaExtension.ExtensionClass;
            if (extensionClass == null)
              throw new PluginInvalidStateException("Could not find class type for extension {0}", mediaExtension.Caption);
            IMediaItemAction action = Activator.CreateInstance(extensionClass) as IMediaItemAction;
            if (action == null)
              throw new PluginInvalidStateException("Could not create IMediaItemAction instance of class {0}", extensionClass);

            mediaExtension.Action = action;
            _actions.Add(mediaExtension);
          }
        }
        catch (PluginInvalidStateException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Cannot add Media extension with id '{0}'", e, itemMetadata.Id);
        }
      }
    }

    protected bool FillItemsList(MediaItem mediaItem)
    {
      _mediaItemActionItems.Clear();
      foreach (MediaItemActionExtension action in _actions.OrderBy(a => a.Sort))
      {
        if (!action.Action.IsAvailable(mediaItem))
          continue;

        ListItem item = new ListItem(Consts.KEY_NAME, action.Caption);
        item.AdditionalProperties[Consts.KEY_MEDIA_ITEM] = mediaItem;
        item.AdditionalProperties[Consts.KEY_MEDIA_ITEM_ACTION] = action.Action;
        _mediaItemActionItems.Add(item);
      }
      _mediaItemActionItems.FireChange();
      return _mediaItemActionItems.Count > 0;
    }

    protected bool PrepareState(NavigationContext context)
    {
      MediaItem item = (MediaItem)context.GetContextVariable(KEY_MEDIA_ITEM, false);
      return item != null && FillItemsList(item);
    }

    protected void LeaveMediaItemActionState()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePop(1);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      BuildExtensions();
      return PrepareState(newContext);
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(Consts.DIALOG_MEDIAITEM_ACTION_MENU, (dialogName, dialogInstanceId) => LeaveMediaItemActionState());
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Nothing to do
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.ManualWorkflowModel; // Avoid automatic screen update - we only show dialogs if necessary
    }

    #endregion
  }
}