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
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Extensions;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.Common.Async;

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
    protected abstract class ListItemAction
    {
      public string Caption;

      public string Sort;

      public IUserRestriction Restriction;

      abstract public string ConfirmationMessage(ListItem item);

      public bool Deferred;

      /// <summary>
      /// Checks if this action is available for the given <paramref name="mediaItem"/>.
      /// </summary>
      /// <param name="item">ListItem</param>
      /// <returns><c>true</c> if available</returns>
      abstract public Task<bool> IsAvailableAsync(ListItem item);

      /// <summary>
      /// Executes the action for the given MediaItem.
      /// </summary>
      /// <param name="item">ListItem</param>
      /// <returns>
      /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if successful.
      /// <see cref="AsyncResult{T}.Result"/> returns what kind of changes was done on MediaItem.
      /// </returns>
      abstract public Task<bool> ProcessAsync(ListItem item);
    }

    protected class MediaListItemAction : ListItemAction
    {
      IMediaItemAction _action;

      public MediaListItemAction(MediaItemActionExtension extension)
      {
        Caption = extension.Caption;
        Sort = extension.Sort;
        _action = extension.Action;
        Restriction = _action as IUserRestriction;
        if (Restriction != null)
          Restriction.RestrictionGroup = extension.RestrictionGroup;
        Deferred = _action is IDeferredMediaItemAction;
      }

      public override string ConfirmationMessage(ListItem item)
      {
        IMediaItemActionConfirmation confirmation = _action as IMediaItemActionConfirmation;
        return confirmation?.ConfirmationMessage;
      }

      public async override Task<bool> IsAvailableAsync(ListItem item)
      {
        IMediaItemListItem mediaItem = item as IMediaItemListItem;
        if (mediaItem != null)
          return await _action.IsAvailableAsync(mediaItem.MediaItem);
        return false;
      }

      public async override Task<bool> ProcessAsync(ListItem item)
      {
        IMediaItemListItem mediaItem = item as IMediaItemListItem;
        if (mediaItem != null)
        {
          var result = await _action.ProcessAsync(mediaItem.MediaItem);
          if (result.Success)
          {
            if(result.Result != ContentDirectoryMessaging.MediaItemChangeType.None)
              ContentDirectoryMessaging.SendMediaItemChangedMessage(mediaItem.MediaItem, result.Result);
            return true;
          }
        }
        return false;
      }

    }

    protected class MediaViewItemAction : ListItemAction
    {
      IMediaViewAction _action;

      public MediaViewItemAction(MediaViewActionExtension extension)
      {
        Caption = extension.Caption;
        Sort = extension.Sort;
        _action = extension.Action;
        Restriction = _action as IUserRestriction;
        if (Restriction != null)
          Restriction.RestrictionGroup = extension.RestrictionGroup;
        Deferred = _action is IDeferredMediaViewAction;
      }

      public override string ConfirmationMessage(ListItem item)
      {
        IViewListItem viewItem = item as IViewListItem;
        IMediaViewActionConfirmation confirmation = _action as IMediaViewActionConfirmation;
        return confirmation == null || viewItem == null ? null : confirmation.ConfirmationMessage(viewItem.View);
      }

      public async override Task<bool> IsAvailableAsync(ListItem item)
      {
        IViewListItem viewItem = item as IViewListItem;
        if (viewItem != null)
          return await _action.IsAvailableAsync(viewItem.View);
        return false;
      }

      public async override Task<bool> ProcessAsync(ListItem item)
      {
        IViewListItem viewItem = item as IViewListItem;
        if (viewItem != null)
          return await _action.ProcessAsync(viewItem.View);
        return false;
      }

    }

    protected class DummyListItem : ListItem, IMediaItemListItem
    {
      public DummyListItem(MediaItem mediaItem)
      {
        MediaItem = mediaItem;
      }

      public MediaItem MediaItem { get; private set; }
    }

    // Action menu
    protected ItemsList _mediaItemActionItems = new ItemsList();
    private readonly List<ListItemAction> _actions = new List<ListItemAction>();
    private IPluginItemStateTracker _mediaActionPluginItemStateTracker; // Lazy initialized
    private DialogCloseWatcher _dialogCloseWatcher;
    private ListItemAction _deferredAction;
    private ListItem _deferredItem;

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
    /// Tries to show actions for the given <paramref name="listItem"/>.
    /// </summary>
    /// <param name="item">ListItem</param>
    public void ShowMediaItemActionsEx(ListItem item)
    {
      if (item is IMediaItemListItem || item is IViewListItem)
      {
        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
        workflowManager.NavigatePush(Consts.WF_STATE_ID_CHECK_MEDIA_ITEM_ACTION, new NavigationContextConfig
        {
          AdditionalContextVariables = new Dictionary<string, object> { { KEY_MEDIA_ITEM, item } }
        });
      }
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

      ListItemAction action = actionObj as ListItemAction;
      item = mediaItemObj as ListItem;
      if (action == null || item == null)
        return;

      if (action.ConfirmationMessage(item) != null)
        ShowConfirmation(action, item);
      else
        _ = InvokeAction(action, item);
    }

    protected void ShowConfirmation(ListItemAction action, ListItem item)
    {
      IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
      string header = LocalizationHelper.Translate(Consts.RES_CONFIRM_HEADER);
      string text = LocalizationHelper.Translate(action.ConfirmationMessage(item));
      Guid handle = dialogManager.ShowDialog(header, text, DialogType.YesNoDialog, false, DialogButtonType.No);
      _dialogCloseWatcher = new DialogCloseWatcher(this, handle,
        async dialogResult =>
        {
          if (dialogResult == DialogResult.Yes)
          {
            await InvokeAction(action, item);
          }
          _dialogCloseWatcher?.Dispose();
        });
    }

    protected async Task InvokeAction(ListItemAction action, ListItem item)
    {
      if (action.Deferred)
      {
        // Will be called when context is left
        _deferredAction = action;
        _deferredItem = item;
        return;
      }
      await InvokeInternal(action, item);
    }

    private async Task<bool> InvokeDeferred()
    {
      if (_deferredAction != null && _deferredItem != null)
        return await InvokeInternal(_deferredAction, _deferredItem);
      return true;
    }

    private async Task<bool> InvokeInternal(ListItemAction action, ListItem item)
    {
      try
      {
        return await action.ProcessAsync(item);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error executing MediaItemAction '{0}':", ex, action.GetType());
        return false;
      }
    }

    #endregion

    #region Protected members

    public void BuildExtensions()
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
            _actions.Add(new MediaListItemAction(mediaExtension));
          }
        }
        catch (PluginInvalidStateException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Cannot add Media extension with id '{0}'", e, itemMetadata.Id);
        }
      }
      foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(MediaViewActionBuilder.MEDIA_EXTENSION_PATH))
      {
        try
        {
          MediaViewActionExtension mediaExtension = pluginManager.RequestPluginItem<MediaViewActionExtension>(
            MediaViewActionBuilder.MEDIA_EXTENSION_PATH, itemMetadata.Id, _mediaActionPluginItemStateTracker);
          if (mediaExtension == null)
            ServiceRegistration.Get<ILogger>().Warn("Could not instantiate MediaView extension with id '{0}'", itemMetadata.Id);
          else
          {
            Type extensionClass = mediaExtension.ExtensionClass;
            if (extensionClass == null)
              throw new PluginInvalidStateException("Could not find class type for extension {0}", mediaExtension.Caption);
            IMediaViewAction action = Activator.CreateInstance(extensionClass) as IMediaViewAction;
            if (action == null)
              throw new PluginInvalidStateException("Could not create IMediaViewAction instance of class {0}", extensionClass);

            mediaExtension.Action = action;
            _actions.Add(new MediaViewItemAction(mediaExtension));
          }
        } catch (PluginInvalidStateException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Cannot add MediaView extension with id '{0}'", e, itemMetadata.Id);
        }
      }
    }

    protected async Task<bool> FillItemsList(ListItem selectedItem)
    {
      _mediaItemActionItems.Clear();
      foreach (ListItemAction action in _actions.OrderBy(a => a.Sort))
      {
        if (!await action.IsAvailableAsync(selectedItem))
          continue;

        // Some actions can be restricted to users.
        if (action.Restriction != null)
        {
          if (!ServiceRegistration.Get<IUserManagement>().CheckUserAccess(action.Restriction))
            continue;
        }

        ListItem item = new ListItem(Consts.KEY_NAME, action.Caption);
        item.AdditionalProperties[Consts.KEY_MEDIA_ITEM] = selectedItem;
        item.AdditionalProperties[Consts.KEY_MEDIA_ITEM_ACTION] = action;
        _mediaItemActionItems.Add(item);
      }
      _mediaItemActionItems.FireChange();
      return _mediaItemActionItems.Count > 0;
    }

    protected async Task<bool> PrepareState(NavigationContext context)
    {
      object item = context.GetContextVariable(KEY_MEDIA_ITEM, false);
      if (item is MediaItem)
        item = new DummyListItem(item as MediaItem);
      return item != null && await FillItemsList(item as ListItem);
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
      return PrepareState(newContext).Result;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _deferredAction = null;
      _deferredItem = null;
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(Consts.DIALOG_MEDIAITEM_ACTION_MENU, (dialogName, dialogInstanceId) => LeaveMediaItemActionState());
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Check for pending actions that need to be invoked in former context
      InvokeDeferred().Wait();
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
