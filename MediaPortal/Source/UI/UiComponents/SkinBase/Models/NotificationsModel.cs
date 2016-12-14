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
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.UiNotifications;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// Model which provides data about available system notifications.
  /// Used as workflow model and as pure UI model.
  /// </summary>
  public class NotificationsModel : IDisposable, IWorkflowModel
  {
    #region Consts

    public const string STR_NOTIFICATION_MODEL_ID = "843F373D-0B4B-47ba-8DD1-0D18F00FAAD3";
    public static readonly Guid NOTIFICATION_MODEL_ID = new Guid(STR_NOTIFICATION_MODEL_ID);
    
    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;
    protected AbstractProperty _isNotificationsHintVisibleProperty = new WProperty(typeof(bool), false);
    protected AbstractProperty _isNotificationsAvailableProperty = new WProperty(typeof(bool), false);
    protected AbstractProperty _isMoreThanOneNotificationAvailableProperty = new WProperty(typeof(bool), false);
    protected AbstractProperty _numNotificationsTotalProperty = new WProperty(typeof(int), 0);
    protected AbstractProperty _nMoreNotificationsTextProperty = new WProperty(typeof(string), string.Empty);
    protected AbstractProperty _currentNotificationProperty = new WProperty(typeof(INotification), null);
    protected AbstractProperty _notificationSymbolRelFilePath = new WProperty(typeof(string), null);
    protected AbstractProperty _hasSubWorkflowProperty = new WProperty(typeof(bool), false);

    #endregion

    #region Ctor & maintainance

    public NotificationsModel()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            NotificationServiceMessaging.CHANNEL,
            WorkflowManagerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
      Update();
    }

    public void Dispose()
    {
      _messageQueue.Shutdown();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == NotificationServiceMessaging.CHANNEL)
        Update();
      else if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
        Update();
    }

    protected void Update()
    {
      ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>();
      if (sss.CurrentState != SystemState.Running)
        return;
      INotificationService notificationService = ServiceRegistration.Get<INotificationService>();
      int numNotifications = notificationService.Notifications.Count;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (numNotifications == 0 && workflowManager.CurrentNavigationContext.WorkflowState.StateId == Consts.WF_STATE_ID_WATCH_NOTIFICATIONS)
        // Don't pop the watch-notifications state from the navigation stack if we are in a sub state
        workflowManager.NavigatePopToStateAsync(Consts.WF_STATE_ID_WATCH_NOTIFICATIONS, true);
      IsNotificationsHintVisible = !workflowManager.IsStateContainedInNavigationStack(Consts.WF_STATE_ID_WATCH_NOTIFICATIONS) && numNotifications > 0;
      NumNotificationsTotal = numNotifications;
      IsNotificationsAvailable = numNotifications > 0;
      IsMoreThanOneNotificationAvailable = numNotifications > 1;
      if (numNotifications <= 1)
        NMoreNotificationsText = string.Empty;
      else if (numNotifications == 2)
        NMoreNotificationsText = LocalizationHelper.Translate(Consts.RES_ONE_MORE_NOTIFICATION);
      else
        NMoreNotificationsText = LocalizationHelper.Translate(Consts.RES_N_MORE_NOTIFICATIONS, numNotifications - 1);
      INotification notification = notificationService.PeekNotification();
      CurrentNotification = notification;
      if (notification != null)
      {
        if (string.IsNullOrEmpty(notification.CustomIconPath))
          switch (notification.Type)
          {
            case NotificationType.UserInteractionRequired:
              NotificationSymbolRelFilePath = Consts.REL_PATH_USER_INTERACTION_REQUIRED_ICON;
              break;
            case NotificationType.Info:
              NotificationSymbolRelFilePath = Consts.REL_PATH_INFO_ICON;
              break;
            case NotificationType.Warning:
              NotificationSymbolRelFilePath = Consts.REL_PATH_WARNING_ICON;
              break;
            case NotificationType.Error:
              NotificationSymbolRelFilePath = Consts.REL_PATH_ERROR_ICON;
              break;
          }
        else
          NotificationSymbolRelFilePath = notification.CustomIconPath;
        HasSubWorkflow = notification.HandlerWorkflowState.HasValue;
      }
    }

    #endregion

    #region Public members to be accessed by the GUI

    public AbstractProperty IsNotificationsHintVisibleProperty
    {
      get { return _isNotificationsHintVisibleProperty; }
    }

    /// <summary>
    /// Gets the information if notifications are currently available.
    /// </summary>
    public bool IsNotificationsHintVisible
    {
      get { return (bool) _isNotificationsHintVisibleProperty.GetValue(); }
      set { _isNotificationsHintVisibleProperty.SetValue(value); }
    }

    public AbstractProperty IsNotificationsAvailableProperty
    {
      get { return _isNotificationsAvailableProperty; }
    }

    /// <summary>
    /// Gets the information if notifications are currently available.
    /// </summary>
    public bool IsNotificationsAvailable
    {
      get { return (bool) _isNotificationsAvailableProperty.GetValue(); }
      set { _isNotificationsAvailableProperty.SetValue(value); }
    }

    public AbstractProperty IsMoreThanOneNotificationAvailableProperty
    {
      get { return _isMoreThanOneNotificationAvailableProperty; }
    }

    /// <summary>
    /// Gets the information if more than one notification is currently available.
    /// </summary>
    public bool IsMoreThanOneNotificationAvailable
    {
      get { return (bool) _isMoreThanOneNotificationAvailableProperty.GetValue(); }
      set { _isMoreThanOneNotificationAvailableProperty.SetValue(value); }
    }

    public AbstractProperty NumNotificationsTotalProperty
    {
      get { return _numNotificationsTotalProperty; }
    }

    /// <summary>
    /// Gets the current number of available notification messages.
    /// </summary>
    public int NumNotificationsTotal
    {
      get { return (int) _numNotificationsTotalProperty.GetValue(); }
      set { _numNotificationsTotalProperty.SetValue(value); }
    }

    public AbstractProperty CurrentNotificationProperty
    {
      get { return _currentNotificationProperty; }
    }

    /// <summary>
    /// Gets the current notification.
    /// </summary>
    public INotification CurrentNotification
    {
      get { return (INotification) _currentNotificationProperty.GetValue(); }
      set { _currentNotificationProperty.SetValue(value); }
    }

    public AbstractProperty NotificationSymbolRelFilePathProperty
    {
      get { return _notificationSymbolRelFilePath; }
    }

    public string NotificationSymbolRelFilePath
    {
      get { return (string) _notificationSymbolRelFilePath.GetValue(); }
      set { _notificationSymbolRelFilePath.SetValue(value); }
    }

    public AbstractProperty NMoreNotificationsTextProperty
    {
      get { return _nMoreNotificationsTextProperty; }
    }

    public string NMoreNotificationsText
    {
      get { return (string) _nMoreNotificationsTextProperty.GetValue(); }
      set { _nMoreNotificationsTextProperty.SetValue(value); }
    }

    public AbstractProperty HasSubWorkflowProperty
    {
      get { return _hasSubWorkflowProperty; }
    }

    /// <summary>
    /// Gets the information whether the current notification has a workflow attached.
    /// </summary>
    public bool HasSubWorkflow
    {
      get { return (bool) _hasSubWorkflowProperty.GetValue(); }
      set { _hasSubWorkflowProperty.SetValue(value); }
    }

    /// <summary>
    /// Dequeues the current notification and makes the next notification available.
    /// </summary>
    public void DequeueNotification()
    {
      INotificationService notificationService = ServiceRegistration.Get<INotificationService>();
      notificationService.DequeueNotification();
    }

    public void GoToNotification()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_WATCH_NOTIFICATIONS);
    }

    public void HandleNotificationSubWorkflow()
    {
      INotification notification = CurrentNotification;
      if (notification == null || !notification.HandlerWorkflowState.HasValue)
        return;
      DequeueNotification();
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(notification.HandlerWorkflowState.Value);
    }

    #endregion

    #region Implementation of IWorkflowModel

    public Guid ModelId
    {
      get { return NOTIFICATION_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      INotificationService notificationService = ServiceRegistration.Get<INotificationService>();
      return notificationService.Notifications.Count > 0;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      INotificationService notificationService = ServiceRegistration.Get<INotificationService>();
      notificationService.CheckForTimeouts = false;
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      INotificationService notificationService = ServiceRegistration.Get<INotificationService>();
      notificationService.CheckForTimeouts = true;
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
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}