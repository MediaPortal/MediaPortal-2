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

using InputDevices.Common.Mapping;
using MediaPortal.Common;
using MediaPortal.Common.Configuration;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Configuration;
using System;
using System.Collections.Generic;

namespace InputDevices.Mapping.ActionExecutors
{
  /// <summary>
  /// Executes a navigation to the config location defined by an <see cref="InputAction"/> with type <see cref="InputAction.CONFIG_LOCATION_TYPE"/>.
  /// </summary>
  public class ConfigSectionExecutor : IInputActionExecutor
  {
    protected static readonly Guid CONFIG_MODEL_ID = new Guid(ConfigurationModel.CONFIGURATION_MODEL_ID_STR);
    protected static readonly Guid CONFIG_MAIN_STATE = new Guid(ConfigurationModel.CONFIGURATION_MAIN_STATE_ID_STR);
    protected const string CONFIG_LOCATION_KEY = "ConfigurationModel: CONFIG_LOCATION";

    protected WorkflowAction _workflowAction;

    public ConfigSectionExecutor(InputAction inputAction)
    {
      if (inputAction.Type != InputAction.CONFIG_LOCATION_TYPE)
        throw new ArgumentException($"{nameof(ConfigSectionExecutor)}: {nameof(InputAction.Type)} must be {InputAction.CONFIG_LOCATION_TYPE}", nameof(inputAction));

      _workflowAction = GetWorkflowAction(inputAction);
    }

    protected static WorkflowAction GetWorkflowAction(InputAction inputAction)
    {
      InitializeConfigurationManager();
      // The input action should be a config location, try and get the node at that location
      IConfigurationManager configurationManager = ConfigurationManager;
      IConfigurationNode configNode = configurationManager.GetNode(inputAction.Action);
      ConfigSection configSection = (ConfigSection)configNode.ConfigObj;

      // Some state needs to be copied from the main state
      WorkflowState configMainState = GetConfigMainState();

      // Create a new state for the location based on the main state
      WorkflowState configLocationState = new WorkflowState(Guid.NewGuid(), string.Format("Config: '{0}'", configNode.Location), configSection.SectionMetadata.Text,
        false, configMainState.MainScreen, false, false, configMainState.WorkflowModelId, WorkflowType.Workflow, configMainState.HideGroups);

      // Create a workflow action that will navigate to the new state when executed
      WorkflowAction workflowAction = new PushTransientStateNavigationTransition(
              Guid.NewGuid(), null, null, null, configLocationState, null)
      {
        WorkflowNavigationContextVariables = new Dictionary<string, object>
        {
          {CONFIG_LOCATION_KEY, configNode.Location}
        }
      };

      return workflowAction;
    }

    protected static WorkflowState GetConfigMainState()
    {
      IWorkflowManager workflowManager = WorkflowManager;
      workflowManager.Lock.EnterReadLock();
      try
      {
        if (!workflowManager.States.TryGetValue(CONFIG_MAIN_STATE, out WorkflowState configMainState))
          throw new KeyNotFoundException($"{nameof(ConfigSectionExecutor)}: Unable to find main config model state");
        return configMainState;
      }
      finally
      {
        workflowManager.Lock.ExitReadLock();
      }
    }

    protected static void InitializeConfigurationManager()
    {
      // The configuration manager is already initialized if the model is on the navigation stack 
      if (!WorkflowManager.IsModelContainedInNavigationStack(CONFIG_MODEL_ID))
        ConfigurationManager.Initialize();
    }

    public void Execute()
    {
      InitializeConfigurationManager();
      _workflowAction.Execute();
    }

    protected static IWorkflowManager WorkflowManager
    {
      get { return ServiceRegistration.Get<IWorkflowManager>(); }
    }

    protected static IConfigurationManager ConfigurationManager
    {
      get { return ServiceRegistration.Get<IConfigurationManager>(); }
    }
  }
}
