#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using InputDevices.Common.Devices;
using InputDevices.Common.Inputs;
using InputDevices.Common.Mapping;
using InputDevices.Common.Messaging;
using InputDevices.Mapping;
using InputDevices.Models.MappableItemProviders;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;

namespace InputDevices.Models
{
  public class InputMappingModel : IWorkflowModel
  {
    #region Consts

    public static readonly Guid MODEL_ID = new Guid("5ADEEAF0-ED14-4590-8A71-BFEA13D7908A");

    /// <summary>
    /// Workflow state for detecting the device to map.
    /// </summary>
    public static readonly Guid SELECT_DEVICE_STATE = new Guid("67C43652-6880-4CFC-86D4-34230BFADCC7");

    /// <summary>
    /// Workflow state for mapping an input to an MP2 key.
    /// </summary>
    public static readonly Guid MAP_KEY_STATE = new Guid("299EB653-8C8A-4320-A0BD-A47A4F1576D0");

    /// <summary>
    /// Workflow state for mapping an input to a screen.
    /// </summary>
    public static readonly Guid MAP_SCREEN_STATE = new Guid("79FECE05-B72F-4CD8-AE76-690F931A96E8");

    /// <summary>
    /// Workflow state for mapping an input to a config section.
    /// </summary>
    public static readonly Guid MAP_CONFIG_STATE = new Guid("4E16A73A-7895-4CF6-879A-BD9668223FCA");

    /// <summary>
    /// Workflow state for mapping an input to a global action.
    /// </summary>
    public static readonly Guid MAP_GLOBAL_ACTION_STATE = new Guid("947121B7-A2C9-4D76-8424-B3747F09C3A6");

    /// <summary>
    /// Workflow state for detecting the inputs to map to an action.
    /// </summary>
    public static readonly Guid MAPPING_STATE = new Guid("D647288E-46C2-48C1-B330-2F0D05DFA80F");

    public static readonly Guid RESTORE_DEFAULT_MAPPING_ACTION_ID = new Guid("17AF9006-F3D9-4F67-AF05-B420D89BD460");

    public static readonly Guid CLEAR_MAPPING_ACTION_ID = new Guid("152B4472-88E4-44C5-9A8F-21F3EB5942CF");

    /// <summary>
    /// Key for the navigation context variable that contains the device to map.
    /// </summary>
    public const string DEVICE_TO_MAP_CONTEXT_VARIABLE = "InputMappingModel: Device";

    /// <summary>
    /// Key for the navigation context variable that contains the action to map.
    /// </summary>
    public const string ACTION_TO_MAP_CONTEXT_VARIABLE = "InputMappingModel: Action";

    #endregion

    protected readonly object _syncObj = new object();

    protected DeviceMappingWatcher _deviceMappingWatcher;
    protected SynchronousMessageQueue _messageQueue;

    protected Guid _currentState;

    protected DeviceMetadata _device;
    protected InputDeviceMapping _deviceMapping;

    protected AbstractProperty _actionsProxyProperty = new WProperty(typeof(IMappableActionsProxy), null);

    protected Dictionary<Guid, IMappableItemProvider> _itemProviders = new Dictionary<Guid, IMappableItemProvider>
    {
      { MAP_KEY_STATE, new KeyItemProvider() },
      { MAP_GLOBAL_ACTION_STATE, new GlobalActionItemProvider() },
      { MAP_SCREEN_STATE, new ScreenActionItemProvider() },
      { MAP_CONFIG_STATE, new ConfigItemProvider() }
    };

    #region GUI properties/methods

    public AbstractProperty ActionsProxyProperty
    {
      get { return _actionsProxyProperty; }
    }

    /// <summary>
    /// Gets the proxy that provides the mappable actions.
    /// </summary>
    public IMappableActionsProxy ActionsProxy
    {
      get { return (IMappableActionsProxy)_actionsProxyProperty.GetValue(); }
      protected set { _actionsProxyProperty.SetValue(value); }
    }

    public void BeginMapping(InputAction inputAction)
    {
      WorkflowManager.NavigatePushAsync(MAPPING_STATE, new NavigationContextConfig
      {
        AdditionalContextVariables = new Dictionary<string, object>
        {
          { ACTION_TO_MAP_CONTEXT_VARIABLE, inputAction }
        }
      });
    }

    public void DeleteMapping(InputAction inputAction)
    {
      if (inputAction == null)
        return;

      IMappableActionsProxy proxy = ActionsProxy;
      if (proxy == null)
        return;
      proxy.DeleteMapping(inputAction);
      _deviceMappingWatcher.AddOrUpdateMapping(proxy.DeviceMapping);
    }

    public void ClearMappings()
    {
      IMappableActionsProxy proxy = ActionsProxy;
      if (proxy == null)
        return;

      ShowYesNoDialog("[InputDevices.Mapping.ClearAllMappings]", "[InputDevices.Mapping.MappingsWillBeOverwrittenWarning]",
        () =>
        {
          proxy.ResetMappings(null);
          _deviceMappingWatcher.AddOrUpdateMapping(proxy.DeviceMapping);
        });
    }

    public void RestoreDefaultMappings()
    {
      InputDeviceMapping defaultMapping = _device?.DefaultMapping;
      if (defaultMapping == null)
        return;
      IMappableActionsProxy proxy = ActionsProxy;
      if (proxy == null)
        return;

      ShowYesNoDialog("[InputDevices.Mapping.RestoreDefaultMapping]", "[InputDevices.Mapping.MappingsWillBeOverwrittenWarning]",
        () =>
        {
          proxy.ResetMappings(defaultMapping.MappedActions);
          _deviceMappingWatcher.AddOrUpdateMapping(proxy.DeviceMapping);
        });
    }

    #endregion

    /// <summary>
    /// Called when the model is entered, initializes listeners.
    /// </summary>
    protected void InitModel()
    {
      _deviceMappingWatcher = new DeviceMappingWatcher();
      StartMessageQueue();
    }

    /// <summary>
    /// Called when the model is exited, deinitializes listeners but
    /// otherwise retains state so that navigating back to the
    /// model works correctly.
    /// </summary>
    protected void DeinitModel()
    {
      StopMessageQueue();
      _deviceMappingWatcher?.Dispose();
    }

    protected void StartMessageQueue()
    {
      if (_messageQueue == null)
      {
        _messageQueue = new SynchronousMessageQueue(this, new string[0]);
        _messageQueue.MessagesAvailable += MessageAvailable;
      }
      _messageQueue.SubscribeToMessageChannel(InputDeviceMessaging.PREVIEW_CHANNEL);
    }

    protected void StopMessageQueue()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.UnsubscribeFromMessageChannel(InputDeviceMessaging.PREVIEW_CHANNEL);
    }

    private void MessageAvailable(SynchronousMessageQueue queue)
    {
      SystemMessage message;
      while ((message = queue.Dequeue()) != null)
      {
        if (message.MessageType as InputDeviceMessaging.MessageType? == InputDeviceMessaging.MessageType.InputPressed)
        {
          bool handled = HandleDeviceInput(message);
          if (handled)
            message.MessageData[InputDeviceMessaging.HANDLED] = true;
        }
      }
    }

    protected void ChangeModelContext(NavigationContext newContext, bool push)
    {
      // Cancel any ongoing mapping
      ActionsProxy?.CancelMapping();

      lock (_syncObj)
      {
        _currentState = newContext.WorkflowState.StateId;

        // If changing to select device state then do nothing, the device that provides the next input message
        // will be used and the message handler will handle changing to the appropriate state.
        if (_currentState == SELECT_DEVICE_STATE)
          return;

        // All other states require a device, so see if one was provided in the navigation context
        string deviceId = newContext.GetContextVariable(DEVICE_TO_MAP_CONTEXT_VARIABLE, false) as string;
        if (deviceId != null && _deviceMapping?.DeviceId != deviceId)
          SetDeviceMapping(new DeviceMetadata(deviceId, null));
      }

      // No device currently available, switch to the select device state
      if (_deviceMapping == null)
      {
        WorkflowManager.NavigatePushAsync(SELECT_DEVICE_STATE);
        return;
      }

      // Changing to the map action state, get the action to map and begin handling of device input
      if (newContext.WorkflowState.StateId == MAPPING_STATE)
      {
        InputAction inputAction = newContext.GetContextVariable(ACTION_TO_MAP_CONTEXT_VARIABLE, false) as InputAction;
        if (inputAction != null)
          ActionsProxy?.BeginMapping(inputAction);
      }
      // If not in the mapping state, see if the list of mappable actions should be updated
      else
      {
        UpdateMappingProxyForWorkflowState(_currentState, push);
        return;
      }
    }

    /// <summary>
    /// Tries to handle a device input message. The message will be handled if the current state is <see cref="SELECT_DEVICE_STATE"/>,
    /// in which case the device that provided the input will be selected, or the current state is <see cref="MAPPING_STATE"/>, in which case
    /// the input will be mapped to the action currently being mapped.
    /// </summary>
    /// <param name="message">The device input message.</param>
    /// <returns><c>true</c> if the message was handled; else <c>false</c>.</returns>
    protected bool HandleDeviceInput(SystemMessage message)
    {
      IMappableActionsProxy actionsProxy = null;

      DeviceMetadata device = message.MessageData[InputDeviceMessaging.DEVICE_METADATA] as DeviceMetadata;
      lock (_syncObj)
      {
        if (_currentState == SELECT_DEVICE_STATE)
        {
          // Select the device that provided the input for mapping
          SetDeviceMapping(device);
          // and pop the select device state from the navigation stack
          WorkflowManager.NavigatePopToStateAsync(SELECT_DEVICE_STATE, true);
          return true;
        }
        
        // Not selecting device so get a reference to the current action mapping proxy inside the lock
        if (_currentState == MAPPING_STATE)
          actionsProxy = ActionsProxy;
      }

      // See if the current action mapping proxy can handle the input, i.e. is it currently mapping input to an action
      if (actionsProxy == null || !actionsProxy.HandleDeviceInput(device?.Id, message.MessageData[InputDeviceMessaging.PRESSED_INPUTS] as IList<Input>, out bool isMappingComplete))
        return false;

      // If the mapping is considered complete, save the mapping and pop the mapping state from the navigation stack
      if (isMappingComplete)
      {
        _deviceMappingWatcher.AddOrUpdateMapping(actionsProxy.DeviceMapping);
        WorkflowManager.NavigatePopToStateAsync(MAPPING_STATE, true);
      }
      return true;
    }

    protected void SetDeviceMapping(DeviceMetadata device)
    {
      Logger.Debug($"{GetType().Name}: Mapping device '{device?.Id}' ({device?.FriendlyName})");
      _device = device;
      if (!_deviceMappingWatcher.TryGetMapping(device?.Id, out _deviceMapping))
        _deviceMapping = new InputDeviceMapping(device?.Id, device?.DefaultMapping?.MappedActions);
    }

    protected void UpdateMappingProxyForWorkflowState(Guid currentState, bool push)
    {
      if (_deviceMapping == null)
        throw new InvalidOperationException("Cannot set mapping proxy, no device mapping has been set");

      IMappableActionsProxy proxy = ActionsProxy;

      bool update = push || proxy == null || proxy.DeviceMapping.DeviceId != _deviceMapping.DeviceId || proxy.MainWorkflowStateId != currentState;
      if (!update)
        return;

      if (!_itemProviders.TryGetValue(currentState, out IMappableItemProvider itemProvider))
        return;

      ActionsProxy = new MappableActionsProxy(currentState, _device, _deviceMapping, itemProvider);
    }

    protected void UpdateMenuItemVisibility(NavigationContext context)
    {
      if (context.MenuActions.TryGetValue(RESTORE_DEFAULT_MAPPING_ACTION_ID, out WorkflowAction workflowAction) && workflowAction is MethodDelegateAction restoreDefaultMappingAction)
        restoreDefaultMappingAction.SetVisible(_device?.DefaultMapping != null);
    }

    protected void ShowYesNoDialog(string headerText, string text, Action yesResultHandler)
    {
      Guid hanlde = ServiceRegistration.Get<IDialogManager>().ShowDialog(headerText, text,
        DialogType.YesNoDialog, false, DialogButtonType.Yes);
      DialogCloseWatcher dialogCloseWatcher = null;
      dialogCloseWatcher = new DialogCloseWatcher(this, hanlde, dialogResult =>
      {
        dialogCloseWatcher?.Dispose();
        if (dialogResult == DialogResult.Yes)
          yesResultHandler?.Invoke();
      });
    }

    #region IWorkflow Model

    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      if (newContext.WorkflowState.StateId == MAPPING_STATE)
      {
        bool hasAction = newContext.GetContextVariable(ACTION_TO_MAP_CONTEXT_VARIABLE, false) as InputAction != null;
        if (!hasAction)
        {
          Logger.Error($"{nameof(InputMappingModel)}: Cannot enter mapping state, NavigationContext does not include the action to map.");
          return false;
        }
      }
      return true;
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      ChangeModelContext(newContext, push);
      if (!push)
        UpdateMenuItemVisibility(newContext);
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      InitModel();
      ChangeModelContext(newContext, true);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      DeinitModel();
      ActionsProxy = null;
      _deviceMapping = null;
      _device = null;
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      InitModel();
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      DeinitModel();
      ActionsProxy?.CancelMapping();
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      if (context.WorkflowState.StateId == SELECT_DEVICE_STATE || context.WorkflowState.StateId == MAPPING_STATE)
        return;

      MethodDelegateAction clearMappingAction = new MethodDelegateAction(CLEAR_MAPPING_ACTION_ID, "InputMappingModel->ClearMapping",
        null, LocalizationHelper.CreateResourceString("[InputDevices.Mapping.ClearAllMappings]"), ClearMappings);
      actions[CLEAR_MAPPING_ACTION_ID] = clearMappingAction;

      MethodDelegateAction restoreDefaultMapping = new MethodDelegateAction(RESTORE_DEFAULT_MAPPING_ACTION_ID, "InputMappingModel->RestoreDefaultMapping",
        null, LocalizationHelper.CreateResourceString("[InputDevices.Mapping.RestoreDefaultMapping]"), RestoreDefaultMappings);
      restoreDefaultMapping.SetVisible(_device?.DefaultMapping != null);
      actions[RESTORE_DEFAULT_MAPPING_ACTION_ID] = restoreDefaultMapping;
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion

    protected IWorkflowManager WorkflowManager
    {
      get { return ServiceRegistration.Get<IWorkflowManager>(); }
    }

    protected ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
