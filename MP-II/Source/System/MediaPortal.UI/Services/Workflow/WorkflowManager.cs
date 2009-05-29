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
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Screens;
using MediaPortal.Presentation.SkinResources;
using MediaPortal.Presentation.Workflow;
using MediaPortal.Utilities;

namespace MediaPortal.Services.Workflow
{
  public class WorkflowManager : IWorkflowManager
  {
    protected class ModelItemStateTracker : IPluginItemStateTracker
    {
      #region Protected fields

      protected WorkflowManager _parent;

      #endregion

      #region Ctor

      public ModelItemStateTracker(WorkflowManager parent)
      {
        _parent = parent;
      }

      #endregion

      #region IPluginItemStateTracker implementation

      public string UsageDescription
      {
        get { return "WorkflowManager: Model usage"; }
      }

      public bool RequestEnd(PluginItemRegistration itemRegistration)
      {
        // We could store the end-requested model in an array of "suspended models" in the WF manager,
        // method WFM.GetOrLoadModel would then fail to load any of the suspended models
        return !_parent.IsModelContainedInNavigationStack(new Guid(itemRegistration.Metadata.Id));
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        _parent.RemoveModelFromNavigationStack(new Guid(itemRegistration.Metadata.Id));
      }

      public void Continue(PluginItemRegistration itemRegistration)
      {
        // If we'd maintain a collection of "suspended models" (like said in comment in method
        // RequestEnd), we would need to cancel the suspension of the continued model here.
      }

      #endregion
    }

    protected class ModelEntry
    {
      protected object _instance;
      protected int _numStatesUnused = 0;

      public ModelEntry(object modelInstance)
      {
        _instance = modelInstance;
      }

      public object ModelInstance
      {
        get { return _instance; }
      }

      public int NumStatesUnused
      {
        get { return _numStatesUnused; }
        set { _numStatesUnused = value; }
      }

      public void Use()
      {
        _numStatesUnused = 0;
      }

      public void Iterate()
      {
        _numStatesUnused++;
      }
    }

    #region Consts

    public const string MODELS_REGISTRATION_LOCATION = "/Models";
    public const int MODEL_CACHE_MAX_NUM_UNUSED = 50;

    #endregion

    #region Protected fields

    protected Stack<NavigationContext> _navigationContextStack = new Stack<NavigationContext>();

    protected IDictionary<Guid, ModelEntry> _modelCache = new Dictionary<Guid, ModelEntry>();
    protected ModelItemStateTracker _modelItemStateTracker;
    protected IDictionary<Guid, WorkflowState> _states = new Dictionary<Guid, WorkflowState>();
    protected IDictionary<Guid, WorkflowAction> _menuActions =  new Dictionary<Guid, WorkflowAction>();

    #endregion

    #region Ctor

    public WorkflowManager()
    {
      _modelItemStateTracker = new ModelItemStateTracker(this);
      ServiceScope.Get<IPluginManager>().RegisterSystemPluginItemBuilder("Model", new ModelBuilder());
    }

    #endregion

    #region Protected methods

    protected void OnSkinResourcesChanged()
    {
      ReloadWorkflowResources();
    }

    /// <summary>
    /// (Re)loads all workflow resources from the skin resource manager.
    /// </summary>
    protected void ReloadWorkflowResources()
    {
      if (_menuActions != null)
        foreach (WorkflowAction action in _menuActions.Values)
          if (action is IDisposable)
            ((IDisposable) action).Dispose();
      if (_states.Count == 0)
        ServiceScope.Get<ILogger>().Debug("WorkflowManager: Loading workflow resources");
      else
        ServiceScope.Get<ILogger>().Debug("WorkflowManager: Reloading workflow resources");
      WorkflowResourcesLoader loader = new WorkflowResourcesLoader();
      loader.Load();
      _states = loader.States;
      _menuActions = loader.MenuActions;
      foreach (WorkflowAction action in _menuActions.Values)
        action.Initialize();
      int count = 0;
      int numPop = 0;
      foreach (NavigationContext context in _navigationContextStack)
      {
        count++;
        if (!context.WorkflowState.IsTransient && !_states.ContainsKey(context.WorkflowState.StateId))
          numPop = count;
      }
      if (numPop > 0)
        NavigatePop(numPop);
    }

    /// <summary>
    /// Returns the model with the specified <paramref name="modelId"/> either from the model cache or
    /// requests and loads it from the plugin manager.
    /// </summary>
    /// <param name="modelId">Id of the model to retrieve.</param>
    /// <returns>Model with the specified <paramref name="modelId"/>.</returns>
    /// <exception cref="ArgumentException">If the specified <paramref name="modelId"/> is not registered.</exception>
    protected object GetOrLoadModel(Guid modelId)
    {
      ModelEntry entry;
      if (_modelCache.TryGetValue(modelId, out entry))
      {
        entry.Use();
        return entry.ModelInstance;
      }
      ServiceScope.Get<ILogger>().Debug("WorkflowManager: Loading GUI model '{0}'", modelId);
      object model = ServiceScope.Get<IPluginManager>().RequestPluginItem<object>(
          MODELS_REGISTRATION_LOCATION, modelId.ToString(), _modelItemStateTracker);
      if (model == null)
        throw new ArgumentException(string.Format("WorkflowManager: Model with id '{0}' is not available", modelId));
      _modelCache[modelId] = new ModelEntry(model);
      return model;
    }

    /// <summary>
    /// Releases the usage of the specified model at the plugin manager and removes it from the model cache.
    /// </summary>
    /// <param name="modelId">Id of the model to free.</param>
    protected void FreeModel(Guid modelId)
    {
      ServiceScope.Get<IPluginManager>().RevokePluginItem(MODELS_REGISTRATION_LOCATION, modelId.ToString(), _modelItemStateTracker);
      _modelCache.Remove(modelId);
    }

    protected void RemoveModelFromNavigationStack(Guid modelId)
    {
      // Pop all navigation contexts until requested model isn't used any more
      while (IsModelContainedInNavigationStack(modelId))
        if (!DoPopNavigationContext(1))
          break;
      UpdateScreen();
    }

    protected static IEnumerable<WorkflowAction> FilterActionsBySourceState(Guid sourceState, ICollection<WorkflowAction> actions)
    {
      foreach (WorkflowAction action in actions)
        if (!action.SourceStateId.HasValue || action.SourceStateId.Value == sourceState)
          yield return action;
    }

    protected WorkflowState FindLastNonTransientState()
    {
      NavigationContext current = CurrentNavigationContext;
      while (current != null && current.WorkflowState.IsTransient)
        current = current.Predecessor;
      // Now we have skipped all contexts with transient states on the stack
      return current == null ? null : current.WorkflowState;
    }

    protected bool DoPushNavigationContext(WorkflowState state, IDictionary<string, object> additionalContextVariables)
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      NavigationContext predecessor = CurrentNavigationContext;

      logger.Info("WorkflowManager: Pushing workflow state '{0}' (id='{1}') onto the navigation stack...", state.Name, state.StateId);

      // Find non-transient state. If new state is transient, search for last non-transient state on stack.
      WorkflowState nonTransientState = state.IsTransient ? FindLastNonTransientState() : state;
      if (nonTransientState == null)
      {
        logger.Error("WorkflowManager: No non-transient state found on workflow context stack to be used as reference state. Workflow state to be pushed is '{0}' (id: '{1}')",
            state.Name, state.StateId);
        return false;
      }

      // Initialize workflow model
      Guid? workflowModelId = nonTransientState.WorkflowModelId;
      IWorkflowModel workflowModel = null;
      if (workflowModelId.HasValue)
      {
        object model = GetOrLoadModel(workflowModelId.Value);
        if (model is IWorkflowModel)
        {
          logger.Debug("WorkflowManager: Using workflow model with id '{0}' for new workflow state '{1}'",
              workflowModelId.Value, state.StateId);
          workflowModel = (IWorkflowModel) model;
        }
        else
          logger.Error("WorkflowManager: Model with id '{0}', which is used as workflow model in state '{1}', doesn't implement the interface '{2}'",
              workflowModelId.Value, state.StateId, typeof(IWorkflowModel).Name);
      }

      // Create new workflow context
      NavigationContext newContext = new NavigationContext(state, predecessor, workflowModel);
      if (additionalContextVariables != null)
        CollectionUtils.AddAll(newContext.ContextVariables, additionalContextVariables);

      // Check if state change is accepted
      if (workflowModel != null && !workflowModel.CanEnterState(predecessor, newContext))
        return false;

      // Push new context
      logger.Debug("WorkflowManager: Entering workflow state '{0}'", state.Name);
      _navigationContextStack.Push(newContext);

      Guid? predecessorModelId = predecessor == null ? null : predecessor.WorkflowModelId;

      // Communicate context change to models
      bool modelChange = workflowModelId != predecessorModelId;

      // - Handle predecessor workflow model
      IWorkflowModel predecessorWorkflowModel = predecessorModelId.HasValue ?
          GetOrLoadModel(predecessorModelId.Value) as IWorkflowModel : null;
      if (predecessorWorkflowModel != null)
        if (modelChange)
        {
          logger.Debug("WorkflowManager: Deactivating predecessor workflow model '{0}'", predecessorModelId.Value);
          predecessorWorkflowModel.Deactivate(predecessor, newContext);
        }
      // else: same model is currently active - model context change will be handled in the next block

      // - Handle new workflow model
      if (workflowModel != null)
        if (modelChange)
        {
          if (predecessor == null)
            logger.Debug("WorkflowManager: Starting first model context with workflow state '{0}' in workflow model '{1}'",
                newContext.WorkflowState.StateId, workflowModelId.Value);
          else
            logger.Debug("WorkflowManager: Entering model context with workflow state '{0}' (old state was '{1}') in new workflow model '{2}'",
                newContext.WorkflowState.StateId, predecessor.WorkflowState.StateId, workflowModelId.Value);
          workflowModel.EnterModelContext(predecessor, newContext);
        }
        else
        {
          logger.Debug("WorkflowManager: Changing model context to workflow state '{0}' (old state was '{1}') in workflow model '{2}'",
              newContext.WorkflowState.StateId, predecessor == null ? null : predecessor.WorkflowState.StateId.ToString(), workflowModelId.Value);
          workflowModel.ChangeModelContext(predecessor, newContext, true);
        }
      if (state.WorkflowType == WorkflowType.Workflow)
      {
        // Compile menu actions
        logger.Debug("WorkflowManager: Compiling menu actions for workflow state '{0}'", state.Name);
        ICollection<WorkflowAction> menuActions = new List<WorkflowAction>();
        if (state.InheritMenu && predecessor != null)
          CollectionUtils.AddAll(menuActions, predecessor.MenuActions.Values);
        CollectionUtils.AddAll(menuActions, FilterActionsBySourceState(state.StateId, _menuActions.Values));
        if (workflowModel != null)
          workflowModel.UpdateMenuActions(newContext, menuActions);

        foreach (WorkflowAction menuAction in menuActions)
          newContext.MenuActions[menuAction.ActionId] = menuAction;
      }

      IterateCache();
      WorkflowManagerMessaging.SendStatePushedMessage(state.StateId);
      return true;
    }

    protected bool DoPopNavigationContext(int count)
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      logger.Info("WorkflowManager: Trying to remove {0} workflow states from navigation stack...", count);
      bool stateChangeAccepted = true;
      ICollection<Guid> statesRemoved = new List<Guid>();
      for (int i=0; i<count || !stateChangeAccepted; i++)
      {
        if (_navigationContextStack.Count <= 1)
        {
          logger.Info("WorkflowManager: Cannot remove the initial workflow state... We'll break the loop here");
          return false;
        }
        NavigationContext oldContext = _navigationContextStack.Pop();
        statesRemoved.Add(oldContext.WorkflowState.StateId);
        if (oldContext.WorkflowState.WorkflowType == WorkflowType.Dialog)
        {
          IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
          if (screenManager.IsDialogVisible && screenManager.ActiveScreenName == oldContext.WorkflowState.MainScreen)
            // This will trigger our close dialog delegate which we attached in the UpdateScreen method,
            // but the anonymous method checks the current navigation context, which has already been removed here
            screenManager.CloseDialog();
        }
        NavigationContext newContext = _navigationContextStack.Count == 0 ? null : _navigationContextStack.Peek();
        Guid? workflowModelId = newContext == null ? null : newContext.WorkflowModelId;
        IWorkflowModel workflowModel = workflowModelId.HasValue ?
            GetOrLoadModel(workflowModelId.Value) as IWorkflowModel : null;

        // Communicate context change to models
        bool modelChange = oldContext.WorkflowModelId != workflowModelId;

        // - Handle predecessor workflow model
        IWorkflowModel predecessorWorkflowModel = oldContext.WorkflowModelId.HasValue ?
            GetOrLoadModel(oldContext.WorkflowModelId.Value) as IWorkflowModel : null;
        if (predecessorWorkflowModel != null)
          if (modelChange)
          {
            logger.Debug("WorkflowManager: Exiting predecessor workflow model '{0}'", oldContext.WorkflowModelId.Value);
            predecessorWorkflowModel.ExitModelContext(oldContext, newContext);
          }
        // else: same model is currently active - model context change will be handled in the next block

        // - Handle new workflow model
        if (workflowModel != null)
        {
          stateChangeAccepted = workflowModel.CanEnterState(oldContext, newContext);
          if (stateChangeAccepted)
            if (modelChange)
            {
              logger.Debug("WorkflowManager: Reactivating model context with workflow state '{0}' (old state was '{1}') in temporary deactivated workflow model '{2}'",
                  newContext.WorkflowState.StateId, oldContext.WorkflowState.StateId, workflowModelId.Value);
              workflowModel.ReActivate(oldContext, newContext);
            }
            else
            {
              logger.Debug("WorkflowManager: Changing model context to workflow state '{0}' (old state was '{1}') in workflow model '{2}'",
                  newContext.WorkflowState.StateId, oldContext.WorkflowState.StateId, workflowModelId.Value);
              workflowModel.ChangeModelContext(oldContext, newContext, false);
            }
        }
        else
          stateChangeAccepted = true;
      }
      IterateCache();
      Guid[] statesRemovedArray = new Guid[statesRemoved.Count];
      statesRemoved.CopyTo(statesRemovedArray, 0);
      WorkflowManagerMessaging.SendStatesPoppedMessage(statesRemovedArray);
      return true;
    }

    protected void IterateCache()
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      logger.Debug("WorkflowManager: Tidying up...");
      foreach (KeyValuePair<Guid, ModelEntry> modelEntry in _modelCache)
      {
        Guid modelId = modelEntry.Key;
        if (!IsModelContainedInNavigationStack(modelId))
          if (modelEntry.Value.NumStatesUnused > MODEL_CACHE_MAX_NUM_UNUSED)
          {
            logger.Debug("WorkflowManager: Freeing unused model with id '{0}'", modelId);
            FreeModel(modelId);
          }
          else
            modelEntry.Value.Iterate();
      }
    }

    /// <summary>
    /// Tries to show the screen for the current navigation context, if it is defined.
    /// </summary>
    /// <returns><c>true</c>, if the screen was successfully shown or if the current navigation context
    /// doesn't define a screen, else <c>false</c>.</returns>
    protected bool UpdateScreen()
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      NavigationContext currentContext = CurrentNavigationContext;
      string screen = currentContext.WorkflowState.MainScreen;
      if (screen == null)
        return true;
      bool result;
      if (currentContext.WorkflowState.WorkflowType == WorkflowType.Workflow)
      {
        logger.Info("WorkflowManager: Trying to show screen '{0}'...", screen);
        result = screenManager.ShowScreen(screen);
      }
      else if (currentContext.WorkflowState.WorkflowType == WorkflowType.Dialog)
      {
        logger.Info("WorkflowManager: Trying to open dialog screen '{0}'...", screen);
        result = screenManager.ShowDialog(screen, dialogName =>
          {
            if (ReferenceEquals(CurrentNavigationContext, currentContext))
              NavigatePop(1);
          });
      }
      else
        result = true;

      if (result)
        logger.Info("WorkflowManager: Screen '{0}' successfully shown", screen);
      else
        logger.Info("WorkflowManager: Error showing screen '{0}'", screen);
      return result;
    }

    #endregion

    #region IWorkflowManager implementation

    public IDictionary<Guid, WorkflowState> States
    {
      get { return _states; }
    }

    public IDictionary<Guid, WorkflowAction> MenuStateActions
    {
      get { return _menuActions; }
    }

    public Stack<NavigationContext> NavigationContextStack
    {
      get { return _navigationContextStack; }
    }

    public NavigationContext CurrentNavigationContext
    {
      get { return _navigationContextStack.Count == 0 ? null : _navigationContextStack.Peek(); }
    }

    public void Initialize()
    {
      ServiceScope.Get<ILogger>().Info("WorkflowManager: Startup");
      ISkinResourceManager skinResourceManager = ServiceScope.Get<ISkinResourceManager>();
      skinResourceManager.SkinResourcesChanged += OnSkinResourcesChanged;
      ReloadWorkflowResources();
    }

    public void Shutdown()
    {
      ServiceScope.Get<ILogger>().Info("WorkflowManager: Shutdown");
      ISkinResourceManager skinResourceManager = ServiceScope.Get<ISkinResourceManager>(false);
      if (skinResourceManager != null)
        skinResourceManager.SkinResourcesChanged -= OnSkinResourcesChanged;
      foreach (Guid modelId in new List<Guid>(_modelCache.Keys))
        FreeModel(modelId);
    }

    public void NavigatePush(Guid stateId, IDictionary<string, object> additionalContextVariables)
    {
      WorkflowState state;
      if (!_states.TryGetValue(stateId, out state))
        throw new ArgumentException(string.Format("WorkflowManager: Workflow state '{0}' is not available", stateId));

      if (DoPushNavigationContext(state, additionalContextVariables))
        if (!UpdateScreen())
          NavigatePop(1);
      WorkflowManagerMessaging.SendNavigationCompleteMessage();
    }

    public void NavigatePush(Guid stateId)
    {
      NavigatePush(stateId, null);
    }

    public void NavigatePushTransient(WorkflowState state, IDictionary<string, object> additionalContextVariables)
    {
      if (DoPushNavigationContext(state, additionalContextVariables))
        if (!UpdateScreen())
          NavigatePop(1);
      WorkflowManagerMessaging.SendNavigationCompleteMessage();
    }

    public void NavigatePushTransient(WorkflowState state)
    {
      NavigatePushTransient(state, null);
    }

    public void NavigatePop(int count)
    {
      DoPopNavigationContext(count);
      while (!UpdateScreen())
        if (!DoPopNavigationContext(1))
          break;
      WorkflowManagerMessaging.SendNavigationCompleteMessage();
    }

    public void NavigatePopToState(Guid stateId, bool inclusive)
    {
      while (CurrentNavigationContext.WorkflowState.StateId != stateId)
        if (!DoPopNavigationContext(1))
          break;
      if (inclusive)
        DoPopNavigationContext(1);
      UpdateScreen();
      WorkflowManagerMessaging.SendNavigationCompleteMessage();
    }

    public object GetModel(Guid modelId)
    {
      object model = GetOrLoadModel(modelId);
      if (!CurrentNavigationContext.Models.ContainsKey(modelId))
      {
        ServiceScope.Get<ILogger>().Debug(
            "WorkflowManager: Attaching GUI model '{0}' to workflow state '{1}'",
            modelId, CurrentNavigationContext.WorkflowState.StateId);
        CurrentNavigationContext.Models[modelId] = model;
      }
      return model;
    }

    public bool IsModelContainedInNavigationStack(Guid modelId)
    {
      foreach (NavigationContext context in _navigationContextStack)
        if (context.Models.ContainsKey(modelId))
          return true;
      return false;
    }

    public void FlushModelCache()
    {
      foreach (KeyValuePair<Guid, ModelEntry> modelEntry in _modelCache)
        if (!IsModelContainedInNavigationStack(modelEntry.Key))
          _modelCache.Remove(modelEntry);
    }

    #endregion
  }
}
