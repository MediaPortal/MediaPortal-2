#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.Commands;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Shares;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// Provides a workflow model to attend the complex configuration process for server and client shares
  /// in the MP 2 configuration.
  /// </summary>
  public class SharesConfigModel : IWorkflowModel, IDisposable
  {
    #region Enums

    public enum ShareOrigin
    {
      Local,
      Server,
    }

    #endregion

    #region Consts

    public const string STR_MODEL_ID_SHARESCONFIG = "1768FC91-86B9-4f78-8A4C-E204F0D51502";
    public static readonly Guid MODEL_ID_SHARESCONFIG = new Guid(STR_MODEL_ID_SHARESCONFIG);

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected bool _updatingProperties = false;
    protected ItemsList _systemsList = null;
    protected ItemsList _serverSharesList = null;
    protected ItemsList _localSharesList = null;
    protected SharesProxy _shareProxy = null; // Encapsulates state and communication of shares configuration - either for server shares or for client shares
    protected AbstractProperty _isSharesSelectedProperty;
    protected AbstractProperty _isHomeServerConnectedProperty;
    protected AbstractProperty _isLocalHomeServerProperty;
    protected AbstractProperty _showLocalSharesProperty;
    protected AbstractProperty _isSystemSelectedProperty;
    protected AbstractProperty _anyShareAvailableProperty;
    protected bool _enableLocalShares = true;
    protected bool _enableServerShares = true;
    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    #region Ctor

    public SharesConfigModel()
    {
      _isSharesSelectedProperty = new WProperty(typeof(bool), false);
      _isHomeServerConnectedProperty = new WProperty(typeof(bool), false);
      _isLocalHomeServerProperty = new WProperty(typeof(bool), false);
      _showLocalSharesProperty = new WProperty(typeof(bool), false);
      _isSystemSelectedProperty = new WProperty(typeof(bool), false);
      _anyShareAvailableProperty = new WProperty(typeof(bool), false);
    }

    public void Dispose()
    {
      _shareProxy = null;
      _serverSharesList = null;
      _localSharesList = null;
    }

    #endregion

    void SubscribeToMessages()
    {
      AsynchronousMessageQueue messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           ServerConnectionMessaging.CHANNEL,
           SharesMessaging.CHANNEL,
        });
      messageQueue.MessageReceived += OnMessageReceived;
      messageQueue.Start();
      lock (_syncObj)
        _messageQueue = messageQueue;
    }

    void UnsubscribeFromMessages()
    {
      AsynchronousMessageQueue messageQueue;
      lock (_syncObj)
      {
        messageQueue = _messageQueue;
        _messageQueue = null;
      }
      if (messageQueue == null)
        return;
      messageQueue.Shutdown();
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType =
            (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
            UpdateProperties_NoLock();
            UpdateSharesLists_NoLock(false);
            break;
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            if (_shareProxy is ServerShares)
              // If in edit workflow for a server share, when the server gets disconneted, go back to the shares overview
              NavigateBackToOverview();
            else
            {
              UpdateProperties_NoLock();
              UpdateSharesLists_NoLock(false);
            }
            break;
        }
      }
      else if (message.ChannelName == SharesMessaging.CHANNEL)
      {
        SharesMessaging.MessageType messageType =
            (SharesMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case SharesMessaging.MessageType.ShareAdded:
          case SharesMessaging.MessageType.ShareRemoved:
            UpdateProperties_NoLock();
            UpdateSharesLists_NoLock(false);
            break;
        }
      }
    }

    void OnSystemSelectionChanged(AbstractProperty prop, object oldVal)
    {
      foreach (ListItem systemItem in _systemsList)
        if (systemItem.Selected)
        {
          lock (_syncObj)
            _shareProxy = (SharesProxy) systemItem.AdditionalProperties[Consts.KEY_SHARES_PROXY];
          IsSystemSelected = true;
          return;
        }
      lock (_syncObj)
        _shareProxy = null;
      IsSystemSelected = false;
    }

    #region Public properties (Also accessed from the GUI)

    public SharesProxy ShareProxy
    {
      get { return _shareProxy; }
    }

    public ItemsList SystemsList
    {
      get { return _systemsList; }
    }

    // Used by the skin to determine in state "choose resource provider" if the back button should be
    // enabled to go back to state "choose system", so the underlaying properties must only be set once at the beginning of
    // the shares add workflow
    public bool IsShowSystemsChoice
    {
      get
      {
        lock (_syncObj)
          return _enableLocalShares && _enableServerShares && (_shareProxy == null || _shareProxy.EditMode != SharesProxy.ShareEditMode.EditShare);
      }
    }

    public AbstractProperty IsSystemSelectedProperty
    {
      get { return _isSystemSelectedProperty; }
    }

    public bool IsSystemSelected
    {
      get { return (bool) _isSystemSelectedProperty.GetValue(); }
      set { _isSystemSelectedProperty.SetValue(value); }
    }

    public AbstractProperty IsHomeServerConnectedProperty
    {
      get { return _isHomeServerConnectedProperty; }
    }

    /// <summary>
    /// <c>true</c> if a home server is attached and it is currently connected.
    /// </summary>
    public bool IsHomeServerConnected
    {
      get { return (bool) _isHomeServerConnectedProperty.GetValue(); }
      set { _isHomeServerConnectedProperty.SetValue(value); }
    }

    public AbstractProperty IsLocalHomeServerProperty
    {
      get { return _isLocalHomeServerProperty; }
    }

    /// <summary>
    /// <c>true</c> if the home server is located at this machine.
    /// </summary>
    public bool IsLocalHomeServer
    {
      get { return (bool) _isLocalHomeServerProperty.GetValue(); }
      set { _isLocalHomeServerProperty.SetValue(value); }
    }

    public AbstractProperty ShowLocalSharesProperty
    {
      get { return _showLocalSharesProperty; }
    }

    /// <summary>
    /// <c>true</c> if the home server is located at another machine and thus local shares can be used. Also <c>true</c> if
    /// there are already local shares configured.
    /// </summary>
    public bool ShowLocalShares
    {
      get { return (bool) _showLocalSharesProperty.GetValue(); }
      set { _showLocalSharesProperty.SetValue(value); }
    }

    /// <summary>
    /// List of all local shares to be displayed in the shares config screens.
    /// </summary>
    public ItemsList LocalSharesList
    {
      get
      {
        lock (_syncObj)
          return _localSharesList;
      }
    }

    /// <summary>
    /// List of all server shares to be displayed in the shares config screens.
    /// </summary>
    public ItemsList ServerSharesList
    {
      get
      {
        lock (_syncObj)
          return _serverSharesList;
      }
    }

    public AbstractProperty IsSharesSelectedProperty
    {
      get { return _isSharesSelectedProperty; }
    }

    /// <summary>
    /// <c>true</c> if at least one share of the local shares list or of the server shares list is selected.
    /// </summary>
    public bool IsSharesSelected
    {
      get { return (bool) _isSharesSelectedProperty.GetValue(); }
      set { _isSharesSelectedProperty.SetValue(value); }
    }

    public AbstractProperty AnyShareAvailableProperty
    {
      get { return _anyShareAvailableProperty; }
    }

    /// <summary>
    /// <c>true</c> if at least one local share or server share is present to be shown/edited/removed.
    /// </summary>
    public bool AnyShareAvailable
    {
      get { return (bool) _anyShareAvailableProperty.GetValue(); }
      set { _anyShareAvailableProperty.SetValue(value); }
    }

    #endregion

    #region Public methods

    public void RemoveSelectedSharesAndFinish()
    {
      try
      {
        LocalShares.RemoveShares(GetSelectedLocalShares());
        ServerShares.RemoveShares(GetSelectedServerShares());
        NavigateBackToOverview();
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ErrorEditShare(e);
      }
    }

    public void SelectResourceProviderAndContinue()
    {
      try
      {
        ResourceProviderMetadata rpm = _shareProxy.GetSelectedBaseResourceProvider();
        if (rpm == null)
            // Error case: Should not happen
          return;
        ResourceProviderMetadata oldResourceProvider = _shareProxy.BaseResourceProvider;
        if (oldResourceProvider == null ||
            oldResourceProvider.ResourceProviderId != rpm.ResourceProviderId)
          _shareProxy.ClearAllConfiguredProperties();
        _shareProxy.BaseResourceProvider = rpm;
        // Check if the choosen MP implements a known path navigation interface and go to that screen,
        // if supported
        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
        if (_shareProxy.ResourceProviderSupportsResourceTreeNavigation)
          workflowManager.NavigatePush(Consts.WF_STATE_ID_SHARE_EDIT_CHOOSE_PATH);
        else // If needed, add other path navigation screens here
            // Fallback: Simple TextBox path editor screen
          workflowManager.NavigatePush(Consts.WF_STATE_ID_SHARE_EDIT_EDIT_PATH);
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ErrorEditShare(e);
      }
    }

    public void FinishShareConfiguration()
    {
      try
      {
        if (_shareProxy.EditMode == SharesProxy.ShareEditMode.AddShare)
        {
          _shareProxy.AddShare();
          NavigateBackToOverview();
        }
        else if (_shareProxy.EditMode == SharesProxy.ShareEditMode.EditShare)
        {
          if (_shareProxy.IsResourcePathChanged)
          {
            IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
            screenManager.ShowDialog(Consts.SCREEN_SHARES_CONFIG_RELOCATE_DIALOG);
          }
          else if (_shareProxy.IsCategoriesChanged)
            UpdateShareAndFinish(RelocationMode.ClearAndReImport);
          else
            UpdateShareAndFinish(RelocationMode.None);
        }
        else
          throw new NotImplementedException(string.Format("ShareEditMode '{0}' is not implemented", _shareProxy.EditMode));
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ErrorEditShare(e);
      }
    }

    public void FinishDoRelocate()
    {
      UpdateShareAndFinish(RelocationMode.Relocate);
    }

    public void FinishDoReImport()
    {
      UpdateShareAndFinish(RelocationMode.ClearAndReImport);
    }

    public void EditCurrentShare()
    {
      try
      {
        _shareProxy.EditMode = SharesProxy.ShareEditMode.EditShare;
        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
        workflowManager.NavigatePush(Consts.WF_STATE_ID_SHARE_EDIT_CHOOSE_RESOURCE_PROVIDER);
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ErrorEditShare(e);
      }
    }

    protected void ErrorEditShare(Exception exc)
    {
      ServiceRegistration.Get<ILogger>().Warn("SharesConfigModel: Problem adding/editing share", exc);
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowScreen(Consts.SCREEN_SHARES_CONFIG_PROBLEM);
    }

    // Currently not used; the shares edit workflow is started from the shares info screen calling EditCurrentShare.
    public void EditSelectedShare()
    {
      try
      {
        Share share = GetSelectedLocalShares().FirstOrDefault();
        if (share != null)
          lock (_syncObj)
            _shareProxy = new LocalShares(share);
        else
        {
          share = GetSelectedServerShares().FirstOrDefault();
          if (share == null)
              // Should never happen
            return;
          lock (_syncObj)
            _shareProxy = new ServerShares(share);
        }
        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
        workflowManager.NavigatePush(Consts.WF_STATE_ID_SHARE_EDIT_CHOOSE_RESOURCE_PROVIDER);
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ErrorEditShare(e);
      }
    }

    public void ReImportShare()
    {
      try
      {
        _shareProxy.ReImportShare();
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ErrorEditShare(e);
      }
    }

    public void NavigateBackToOverview()
    {
      lock (_syncObj)
        _shareProxy = null;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToState(Consts.WF_STATE_ID_SHARES_OVERVIEW, false);
    }

    #endregion

    #region Private and protected methods

    void OnShareItemSelectionChanged(AbstractProperty shareItem, object oldValue)
    {
      UpdateIsSharesSelected_NoLock();
    }

    protected ICollection<Share> GetSelectedShares(ItemsList sharesItemsList)
    {
      lock (_syncObj)
        // Fill the result inside this method to make it possible to lock other threads out while looking at the shares list
        return new List<Share>(sharesItemsList.Where(
            shareItem => shareItem.Selected).Select(
            shareItem => (Share) shareItem.AdditionalProperties[Consts.KEY_SHARE]));
    }

    protected ICollection<Share> GetSelectedLocalShares()
    {
      return GetSelectedShares(_localSharesList);
    }

    protected ICollection<Share> GetSelectedServerShares()
    {
      return GetSelectedShares(_serverSharesList);
    }

    protected void UpdateIsSharesSelected_NoLock()
    {
      bool result;
      lock (_syncObj)
        result = GetSelectedLocalShares().Count > 0 || GetSelectedServerShares().Count > 0;
      IsSharesSelected = result;
    }

    protected void UpdateSystemsList_NoLock()
    {
      lock (_syncObj)
      {
        _systemsList = new ItemsList();

        if (_enableLocalShares)
        {
          ListItem localSystemItem = new ListItem(Consts.KEY_NAME, Consts.RES_SHARES_CONFIG_LOCAL_SHARE);
          localSystemItem.AdditionalProperties[Consts.KEY_SHARES_PROXY] = new LocalShares();
          localSystemItem.SelectedProperty.Attach(OnSystemSelectionChanged);
          _systemsList.Add(localSystemItem);
        }

        if (_enableServerShares)
        {
          ListItem serverSystemItem = new ListItem(Consts.KEY_NAME, Consts.RES_SHARES_CONFIG_GLOBAL_SHARE);
          serverSystemItem.AdditionalProperties[Consts.KEY_SHARES_PROXY] = new ServerShares();
          serverSystemItem.SelectedProperty.Attach(OnSystemSelectionChanged);
          _systemsList.Add(serverSystemItem);
        }

        if (_systemsList.Count > 0)
          _systemsList[0].Selected = true;
      }
      _systemsList.FireChange();
    }

    protected internal void UpdateSharesLists_NoLock(bool create)
    {
      lock (_syncObj)
      {
        if (_updatingProperties)
          return;
        _updatingProperties = true;
        if (create)
          _localSharesList = new ItemsList();
        if (create)
          _serverSharesList = new ItemsList();
      }
      try
      {
        List<Share> localShareDescriptors = new List<Share>(LocalShares.GetShares());
        List<Share> serverShareDescriptors = IsHomeServerConnected ?
            new List<Share>(ServerShares.GetShares()) : new List<Share>(0);
        int numShares = localShareDescriptors.Count + serverShareDescriptors.Count;
        UpdateSharesList_NoLock(_localSharesList, localShareDescriptors, ShareOrigin.Local, numShares == 1);
        try
        {
          UpdateSharesList_NoLock(_serverSharesList, serverShareDescriptors, ShareOrigin.Server, numShares == 1);
        }
        catch (NotConnectedException)
        {
          _serverSharesList.Clear();
          _serverSharesList.FireChange();
          numShares = localShareDescriptors.Count;
        }
        ShowLocalShares = !IsLocalHomeServer || _localSharesList.Count > 0;
        IsSharesSelected = numShares == 1;
        bool anySharesAvailable;
        lock (_syncObj)
          anySharesAvailable = _serverSharesList.Count > 0 || _localSharesList.Count > 0;
        AnyShareAvailable = anySharesAvailable;
      }
      finally
      {
        lock (_syncObj)
          _updatingProperties = false;
      }
    }

    protected void UpdateSharesList_NoLock(ItemsList list, List<Share> shareDescriptors, ShareOrigin origin, bool selectFirstItem)
    {
      list.Clear();
      bool selectShare = selectFirstItem;
      shareDescriptors.Sort((a, b) => a.Name.CompareTo(b.Name));
      foreach (Share share in shareDescriptors)
      {
        ListItem shareItem = new ListItem(Consts.KEY_NAME, share.Name);
        shareItem.AdditionalProperties[Consts.KEY_SHARE] = share;
        try
        {
          string path = origin == ShareOrigin.Local ?
              LocalShares.GetLocalResourcePathDisplayName(share.BaseResourcePath) :
              ServerShares.GetServerResourcePathDisplayName(share.BaseResourcePath);
          if (string.IsNullOrEmpty(path))
            // Error case: The path is invalid
            path = LocalizationHelper.Translate(Consts.RES_INVALID_PATH, share.BaseResourcePath);
          shareItem.SetLabel(Consts.KEY_PATH, path);
          Guid? firstResourceProviderId = SharesProxy.GetBaseResourceProviderId(share.BaseResourcePath);
          if (firstResourceProviderId.HasValue)
          {
            ResourceProviderMetadata firstResourceProviderMetadata = origin == ShareOrigin.Local ?
                LocalShares.GetLocalResourceProviderMetadata(firstResourceProviderId.Value) :
                ServerShares.GetServerResourceProviderMetadata(firstResourceProviderId.Value);
            shareItem.AdditionalProperties[Consts.KEY_RESOURCE_PROVIDER_METADATA] = firstResourceProviderMetadata;
          }
          string categories = StringUtils.Join(", ", share.MediaCategories);
          shareItem.SetLabel(Consts.KEY_MEDIA_CATEGORIES, categories);
          Share shareCopy = share;
          shareItem.Command = new MethodDelegateCommand(() => ShowShareInfo(shareCopy, origin));
        }
        catch (NotConnectedException)
        {
          throw;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Problems building share item '{0}' (path '{1}')", e, share.Name, share.BaseResourcePath);
        }
        if (selectShare)
        {
          selectShare = false;
          shareItem.Selected = true;
        }
        shareItem.SelectedProperty.Attach(OnShareItemSelectionChanged);
        lock (_syncObj)
          list.Add(shareItem);
      }
      list.FireChange();
    }

    protected void ShowShareInfo(Share share, ShareOrigin origin)
    {
      if (share == null)
        return;
      if (origin == ShareOrigin.Local)
        lock (_syncObj)
          _shareProxy = new LocalShares(share);
      else if (origin == ShareOrigin.Server)
        lock (_syncObj)
          _shareProxy = new ServerShares(share);
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_SHARE_INFO, new NavigationContextConfig {NavigationContextDisplayLabel = share.Name});
    }

    protected void UpdateShareAndFinish(RelocationMode relocationMode)
    {
      try
      {
        _shareProxy.UpdateShare(relocationMode);
        NavigateBackToOverview();
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ErrorEditShare(e);
      }
    }

    protected void UpdateProperties_NoLock()
    {
      lock (_syncObj)
      {
        if (_updatingProperties)
          return;
        _updatingProperties = true;
      }
      try
      {
        IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
        IsHomeServerConnected = serverConnectionManager.IsHomeServerConnected;
        SystemName homeServerSystem = serverConnectionManager.LastHomeServerSystem;
        IsLocalHomeServer = homeServerSystem == null ? false : homeServerSystem.IsLocalSystem();
        lock (_syncObj)
        {
          _enableLocalShares = !IsLocalHomeServer;
          _enableServerShares = IsHomeServerConnected;
        }
      }
      finally
      {
        lock (_syncObj)
          _updatingProperties = false;
      }
    }

    /// <summary>
    /// Prepares the internal data of this model to match the specified new
    /// <paramref name="workflowState"/>. This method will be called in result of a
    /// forward state navigation as well as for a backward navigation.
    /// </summary>
    /// <param name="workflowState">The workflow state to prepare.</param>
    /// <param name="push">Set to <c>true</c>, if the given <paramref name="workflowState"/> has been pushed onto
    /// the workflow navigation stack. Else, set to <c>false</c>.</param>
    protected void PrepareState(Guid workflowState, bool push)
    {
      try
      {
        if (workflowState == Consts.WF_STATE_ID_SHARES_OVERVIEW)
        {
          UpdateSharesLists_NoLock(true);
        }
        else if (!push)
          return;
        if (workflowState == Consts.WF_STATE_ID_SHARES_REMOVE)
        {
          UpdateSharesLists_NoLock(push);
        }
        else if (workflowState == Consts.WF_STATE_ID_SHARE_INFO)
        {
          // Nothing to prepare
        }
        else if (workflowState == Consts.WF_STATE_ID_SHARE_ADD_CHOOSE_SYSTEM)
        {
          UpdateSystemsList_NoLock();
        }
        else if (workflowState == Consts.WF_STATE_ID_SHARE_EDIT_CHOOSE_RESOURCE_PROVIDER)
        {
          // This could be optimized - we don't need to update the MPs list every time we are popping a WF state
          _shareProxy.UpdateResourceProvidersList();
        }
        else if (workflowState == Consts.WF_STATE_ID_SHARE_EDIT_EDIT_PATH)
        {
          _shareProxy.UpdateIsChoosenPathValid();
        }
        else if (workflowState == Consts.WF_STATE_ID_SHARE_EDIT_CHOOSE_PATH)
        {
          _shareProxy.UpdateResourceProviderPathTree();
        }
        else if (workflowState == Consts.WF_STATE_ID_SHARE_EDIT_EDIT_NAME)
        {
          _shareProxy.PrepareShareName();
        }
        else if (workflowState == Consts.WF_STATE_ID_SHARE_EDIT_CHOOSE_CATEGORIES)
        {
          _shareProxy.UpdateMediaCategoriesTree();
        }
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ErrorEditShare(e);
      }
    }

    protected void ClearData()
    {
      lock (_syncObj)
      {
        _shareProxy = null;
        _localSharesList = null;
        _serverSharesList = null;
        _systemsList = null;
      }
    }

    protected void DisconnectedError()
    {
      // Called when a remote call crashes because the server was disconnected. We don't do anything here because
      // we automatically move to the overview state in the OnMessageReceived method when the server disconnects.
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID_SHARESCONFIG; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      SubscribeToMessages();
      ClearData();
      UpdateProperties_NoLock();
      PrepareState(newContext.WorkflowState.StateId, true);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      UnsubscribeFromMessages();
      ClearData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      PrepareState(newContext.WorkflowState.StateId, push);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareState(newContext.WorkflowState.StateId, false);
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Not used yet, currently we don't show any menu during the shares configuration process.
      // Perhaps we'll add menu actions later for different convenience procedures.
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}