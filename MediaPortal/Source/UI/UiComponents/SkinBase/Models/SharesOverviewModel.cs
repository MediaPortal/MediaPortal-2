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
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// Provides a workflow model to attend the shares overview workflow.
  /// </summary>
  public class SharesOverviewModel : IWorkflowModel, IDisposable
  {
    #region Consts

    public const string STR_MODEL_ID_IMPORTS = "0F1B04C1-0914-4AEB-BBE6-44708BADB25D";
    public static readonly Guid MODEL_ID_IMPORTS = new Guid(STR_MODEL_ID_IMPORTS);

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected ItemsList _sharesList = null;
    protected AbstractProperty _isHomeServerConnectedProperty;
    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    #region Ctor

    public SharesOverviewModel()
    {
      _isHomeServerConnectedProperty = new WProperty(typeof(bool), false);
    }

    public void Dispose()
    {
    }

    #endregion

    void SubscribeToMessages()
    {
      AsynchronousMessageQueue messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           ServerConnectionMessaging.CHANNEL,
           ContentDirectoryMessaging.CHANNEL,
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
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
          case ServerConnectionMessaging.MessageType.ClientsOnlineStateChanged:
            UpdateProperties_NoLock();
            UpdateSharesList_NoLock(false);
            break;
        }
      }
      else if (message.ChannelName == ContentDirectoryMessaging.CHANNEL)
      {
        ContentDirectoryMessaging.MessageType messageType = (ContentDirectoryMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ContentDirectoryMessaging.MessageType.RegisteredSharesChanged:
            UpdateSharesList_NoLock(false);
            break;
          case ContentDirectoryMessaging.MessageType.ShareImportStarted:
          case ContentDirectoryMessaging.MessageType.ShareImportCompleted:
            {
              Guid shareId = (Guid)message.MessageData[ContentDirectoryMessaging.SHARE_ID];
              IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
              IContentDirectory cd = scm.ContentDirectory;
              if (cd == null)
                break;
              UpdateShareImportState_NoLock(shareId, messageType == ContentDirectoryMessaging.MessageType.ShareImportStarted, null);
            }
            break;
          case ContentDirectoryMessaging.MessageType.ShareImportProgress:
            {
              Guid shareId = (Guid)message.MessageData[ContentDirectoryMessaging.SHARE_ID];
              int? progress = (int)message.MessageData[ContentDirectoryMessaging.PROGRESS];
              if (progress < 0)
                progress = null;
              IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
              IContentDirectory cd = scm.ContentDirectory;
              if (cd == null)
                break;
              UpdateShareImportState_NoLock(shareId, true, progress);
            }
            break;
        }
      }
    }

    #region Public properties (Also accessed from the GUI)

    public ItemsList SharesList
    {
      get { return _sharesList; }
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

    #endregion

    #region Public methods

    public void ReImportAllShares()
    {
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory cd = scm.ContentDirectory;
      IServerController sc = scm.ServerController;
      if (cd == null || sc == null)
        return;
      sc.ScheduleImports(cd.GetShares(null, SharesFilter.All).Select(share => share.ShareId), ImportJobType.Refresh);
    }

    public void ReImportShare(Share share)
    {
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      IServerController sc = scm.ServerController;
      if (sc == null)
        return;
      sc.ScheduleImports(new Guid[] { share.ShareId }, ImportJobType.Refresh);
    }

    #endregion

    #region Protected methods

    protected void UpdateSharesList_NoLock(bool create)
    {
      lock (_syncObj)
        if (create)
          _sharesList = new ItemsList();
        else
          _sharesList.Clear();
      try
      {
        IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
        IContentDirectory cd = scm.ContentDirectory;
        IServerController sc = scm.ServerController;
        if (cd == null || sc == null)
          return;
        IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
        ICollection<Share> allShares = cd.GetShares(null, SharesFilter.All);
        IDictionary<string, ICollection<Share>> systems2Shares = new Dictionary<string, ICollection<Share>>();
        foreach (Share share in allShares)
        {
          ICollection<Share> systemShares;
          if (systems2Shares.TryGetValue(share.SystemId, out systemShares))
            systemShares.Add(share);
          else
            systems2Shares[share.SystemId] = new List<Share> { share };
        }
        ICollection<Guid> importingShares = cd.GetCurrentlyImportingShares() ?? new List<Guid>();
        ICollection<string> onlineSystems = sc.GetConnectedClients();
        onlineSystems = onlineSystems == null ? new List<string> { scm.HomeServerSystemId } : new List<string>(onlineSystems) { scm.HomeServerSystemId };
        foreach (KeyValuePair<string, ICollection<Share>> system2Shares in systems2Shares)
        {
          string systemId = system2Shares.Key;
          ICollection<Share> systemShares = system2Shares.Value;
          string systemName;
          string hostName;
          if (systemId == scm.HomeServerSystemId)
          {
            systemName = scm.LastHomeServerName;
            SystemName system = scm.LastHomeServerSystem;
            hostName = system != null ? system.HostName : null;
          }
          else
          {
            MPClientMetadata clientMetadata = ServerCommunicationHelper.GetClientMetadata(systemId);
            if (clientMetadata == null)
            {
              systemName = null;
              hostName = null;
            }
            else
            {
              systemName = clientMetadata.LastClientName;
              SystemName system = clientMetadata.LastSystem;
              hostName = system != null ? system.HostName : null;
            }
          }
          ListItem systemSharesItem = new ListItem(Consts.KEY_NAME, systemName);
          systemSharesItem.AdditionalProperties[Consts.KEY_SYSTEM] = systemId;
          systemSharesItem.AdditionalProperties[Consts.KEY_HOSTNAME] = hostName;
          bool isConnected = onlineSystems.Contains(systemId);
          systemSharesItem.AdditionalProperties[Consts.KEY_IS_CONNECTED] = isConnected;
          ItemsList sharesItemsList = new ItemsList();
          foreach (Share share in systemShares)
          {
            ListItem shareItem = new ListItem(Consts.KEY_NAME, share.Name);
            shareItem.AdditionalProperties[Consts.KEY_SHARE] = share;
            string resourcePathName;
            try
            {
              bool isFileSystemResource;
              bool isFile;
              string resourceName;
              DateTime lastChanged;
              long size;
              if (!rris.GetResourceInformation(share.SystemId, share.BaseResourcePath, out isFileSystemResource, out isFile, out resourcePathName, out resourceName, out lastChanged, out size))
                // Error case: The path is invalid
                resourcePathName = LocalizationHelper.Translate(Consts.RES_INVALID_PATH, share.BaseResourcePath);
            }
            catch (Exception) // NotConnectedException when remote system is not connected at all, UPnPDisconnectedException when remote system gets disconnected during the call
            {
              resourcePathName = share.BaseResourcePath.ToString();
            }
            shareItem.SetLabel(Consts.KEY_PATH, resourcePathName);
            string categories = StringUtils.Join(", ", share.MediaCategories);
            shareItem.SetLabel(Consts.KEY_MEDIA_CATEGORIES, categories);
            UpdateShareImportState_NoLock(shareItem, importingShares.Contains(share.ShareId), null);
            Share shareCopy = share;
            shareItem.Command = new MethodDelegateCommand(() => ReImportShare(shareCopy));
            shareItem.AdditionalProperties[Consts.KEY_REIMPORT_ENABLED] = isConnected;
            sharesItemsList.Add(shareItem);
          }
          systemSharesItem.AdditionalProperties[Consts.KEY_SYSTEM_SHARES] = sharesItemsList;
          lock (_syncObj)
            _sharesList.Add(systemSharesItem);
        }
      }
      finally
      {
        _sharesList.FireChange();
      }
    }

    protected void UpdateShareImportState_NoLock(Guid shareId, bool isImporting, int? progress)
    {
      ListItem itemToUpdate = null;
      lock (_syncObj)
      {
        if (_sharesList == null)
          return;
        foreach (ListItem systemSharesItem in _sharesList)
        {
          ItemsList sharesItemsList = (ItemsList) systemSharesItem.AdditionalProperties[Consts.KEY_SYSTEM_SHARES];
          foreach (ListItem shareItem in sharesItemsList)
          {
            if (((Share) shareItem.AdditionalProperties[Consts.KEY_SHARE]).ShareId == shareId)
              itemToUpdate = shareItem;
          }
        }
      }
      if (itemToUpdate == null)
        return;
      UpdateShareImportState_NoLock(itemToUpdate, isImporting, progress);
    }

    protected void UpdateShareImportState_NoLock(ListItem shareItem, bool isImporting, int? progress)
    {
      shareItem.AdditionalProperties[Consts.KEY_IS_IMPORTING] = isImporting;
      shareItem.AdditionalProperties[Consts.KEY_IMPORTING_PROGRESS] = progress;
      shareItem.FireChange();
    }

    protected void UpdateProperties_NoLock()
    {
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      IsHomeServerConnected = scm.IsHomeServerConnected;
    }

    /// <summary>
    /// Prepares the internal data of this model to match the specified new
    /// <paramref name="workflowState"/>. This method will be called in result of a
    /// forward state navigation as well as for a backward navigation.
    /// </summary>
    /// <param name="workflowState">The workflow state to prepare.</param>
    protected void PrepareState(Guid workflowState)
    {
      if (workflowState == Consts.WF_STATE_ID_IMPORT_OVERVIEW)
      {
        UpdateProperties_NoLock();
        UpdateSharesList_NoLock(true);
      }
    }

    protected void ClearData()
    {
      lock (_syncObj)
      {
        _sharesList = null;
      }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID_IMPORTS; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      SubscribeToMessages();
      ClearData();
      PrepareState(newContext.WorkflowState.StateId);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      UnsubscribeFromMessages();
      ClearData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      PrepareState(newContext.WorkflowState.StateId);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareState(newContext.WorkflowState.StateId);
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      actions.Add(Consts.ACTION_ID_REIMPORT_ALL_SHARES,
          new MethodCallAction(Consts.ACTION_ID_REIMPORT_ALL_SHARES, "ReImportAllShares", null,
              LocalizationHelper.CreateResourceString(Consts.RES_REIMPORT_ALL_SHARES), MODEL_ID_IMPORTS, "ReImportAllShares"));
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
