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
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Screen;
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

      public bool RequestEnd(PluginItemRegistration itemRegistration)
      {
        return !_parent.IsModelContainedInNavigationStack(new Guid(itemRegistration.Metadata.Id));
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        _parent.RemoveModelFromNavigationStack(new Guid(itemRegistration.Metadata.Id));
      }

      public void Continue(PluginItemRegistration itemRegistration) { }

      #endregion
    }

    #region Consts

    public const string MODELS_REGISTRATION_LOCATION = "/Models";

    #endregion

    #region Protected fields

    protected Stack<NavigationContext> _navigationContextStack = new Stack<NavigationContext>();
    protected IDictionary<Guid, object> _modelCache = new Dictionary<Guid, object>();
    protected ModelItemStateTracker _modelItemStateTracker;
    protected IDictionary<Guid, WorkflowState> _states = new Dictionary<Guid, WorkflowState>();
    protected IDictionary<Guid, WorkflowStateAction> _menuActions =  new Dictionary<Guid, WorkflowStateAction>();
    protected IDictionary<Guid, WorkflowStateAction> _contextMenuActions = new Dictionary<Guid, WorkflowStateAction>();

    #endregion

    #region Ctor

    public WorkflowManager()
    {
      _modelItemStateTracker = new ModelItemStateTracker(this);
      ServiceScope.Get<IPluginManager>().RegisterSystemPluginItemBuilder("Model", new ModelBuilder());
    }

    #endregion

    #region Public methods

    public void Startup()
    {
      ServiceScope.Get<ILogger>().Info("WorkflowManager: Startup");
      ISkinResourceManager skinResourceManager = ServiceScope.Get<ISkinResourceManager>();
      skinResourceManager.SkinResourcesChanged += OnSkinResourcesChanged;
      ReloadWorkflowResources();
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
      ServiceScope.Get<ILogger>().Debug("WorkflowManager: (Re)loading workflow resources");
      WorkflowResourcesLoader loader = new WorkflowResourcesLoader();
      loader.Load();
      _states = loader.States;
      _menuActions = loader.MenuActions;
      _contextMenuActions = loader.ContextMenuActions;
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
      if (_modelCache.ContainsKey(modelId))
        return _modelCache[modelId];
      object model = ServiceScope.Get<IPluginManager>().RequestPluginItem<object>(
          MODELS_REGISTRATION_LOCATION, modelId.ToString(), _modelItemStateTracker);
      if (model == null)
        throw new ArgumentException(string.Format("WorkflowManager: Model with id '{0}' is not available", modelId));
      _modelCache[modelId] = model;
      return model;
    }

    /// <summary>
    /// Releases the usage of the specified model at the plugin manager and removes it from the model cache.
    /// </summary>
    /// <param name="modelId">Id of the model to free.</param>
    protected void FreeModel(Guid modelId)
    {
      object model;
      if (_modelCache.TryGetValue(modelId, out model))
      {
        if (model is IDisposable)
          ((IDisposable) model).Dispose();
      }
      ServiceScope.Get<IPluginManager>().RevokePluginItem(MODELS_REGISTRATION_LOCATION, modelId.ToString(), _modelItemStateTracker);
      _modelCache.Remove(modelId);
    }

    protected void RemoveModelFromNavigationStack(Guid modelId)
    {
      while (IsModelContainedInNavigationStack(modelId))
        // Pop all navigation context until requested model isn't used any more
        DoPopNavigationContext(1);
      UpdateScreen();
    }

    protected static IEnumerable<WorkflowStateAction> FilterActionsBySourceState(Guid sourceState, ICollection<WorkflowStateAction> actions)
    {
      foreach (WorkflowStateAction action in actions)
        if (action.SourceStateId == sourceState)
          yield return action;
    }

    protected WorkflowState FindNonTransientState()
    {
      NavigationContext current = CurrentNavigationContext;
      while (current != null && current.WorkflowState.IsTransient)
        current = current.Predecessor;
      // Now we have found the last context with a non-transient state on the stack
      return current == null ? null : current.WorkflowState;
    }

    protected void DoPushNavigationContext(WorkflowState state)
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      NavigationContext predecessor = CurrentNavigationContext;

      logger.Info("WorkflowManager: Pushing workflow state '{0}' (id='{1}') on the navigation stack...", state.Name, state.StateId);
      logger.Debug("WorkflowManager: Loading models for workflow state '{0}'", state.Name);

      // Find non-transient state. If new state is transient, search for last non-transient state on stack.
      WorkflowState nonTransientState = state.IsTransient ? FindNonTransientState() : state;
      if (nonTransientState == null)
      {
        logger.Error("WorkflowManager: No non-transient state found on workflow context stack to be used as reference state. Workflow state to be pushed is '{0}' (id: '{1}')",
            state.Name, state.StateId);
        return;
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

      // Initialize models dictionary
      IDictionary<Guid, object> models = new Dictionary<Guid, object>();
      if (workflowModel != null)
        models.Add(workflowModel.ModelId, workflowModel);
      foreach (Guid modelId in nonTransientState.AdditionalModels)
        models.Add(modelId, GetOrLoadModel(modelId));

      // Create new workflow context
      NavigationContext newContext = new NavigationContext(state, predecessor, workflowModelId, models);

      // Push new context
      _navigationContextStack.Push(newContext);
      logger.Debug("WorkflowManager: Entering workflow state '{0}'", state.Name);

      Guid? predecessorModelId = predecessor == null ? null : predecessor.WorkflowModelId;

      // Communicate context change to models
      bool modelChange = predecessorModelId.HasValue != workflowModelId.HasValue ||
          (predecessorModelId.HasValue && workflowModelId.Value != predecessorModelId.Value);

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
              newContext.WorkflowState.StateId, predecessor.WorkflowState.StateId, workflowModelId.Value);
          workflowModel.ChangeModelContext(predecessor, newContext, true);
        }

      // Compile menu actions and context menu actions
      logger.Debug("WorkflowManager: Compiling menu actions and context menu actions for workflow state '{0}'", state.Name);
      ICollection<WorkflowStateAction> menuActions = new List<WorkflowStateAction>();
      ICollection<WorkflowStateAction> contextMenuActions = new List<WorkflowStateAction>();
      if (state.InheritMenu && predecessor != null)
        CollectionUtils.AddAll(menuActions, predecessor.MenuActions.Values);
      if (state.InheritContextMenu && predecessor != null)
        CollectionUtils.AddAll(contextMenuActions, predecessor.ContextMenuActions.Values);
      CollectionUtils.AddAll(menuActions, FilterActionsBySourceState(state.StateId, _menuActions.Values));
      CollectionUtils.AddAll(contextMenuActions, FilterActionsBySourceState(state.StateId, _contextMenuActions.Values));
      if (workflowModel != null)
      {
        workflowModel.UpdateMenuActions(newContext, menuActions);
        workflowModel.UpdateContextMenuActions(newContext, contextMenuActions);
      }

      foreach (WorkflowStateAction menuAction in menuActions)
        newContext.MenuActions.Add(menuAction.ActionId, menuAction);
      foreach (WorkflowStateAction contextMenuAction in contextMenuActions)
        newContext.MenuActions.Add(contextMenuAction.ActionId, contextMenuAction);
    }

    protected void DoPopNavigationContext(int count)
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      if (_navigationContextStack.Count <= count)
      {
        int newCount = _navigationContextStack.Count - 1;
        logger.Info("WorkflowManager: Should remove {0} workflow navigation contexts from navigation stack, but only {1} contexts available... limiting to {2} contexts",
            count, _navigationContextStack.Count, newCount);
        count = newCount;
      }
      if (count == 0)
        return;
      logger.Info("WorkflowManager: Removing {0} workflow states from navigation stack...", count);
      IDictionary<Guid, object> oldModels = new Dictionary<Guid, object>();
      for (int i=0; i<count; i++)
      {
        NavigationContext oldContext = _navigationContextStack.Pop();
        NavigationContext newContext = _navigationContextStack.Count == 0 ? null : _navigationContextStack.Peek();
        Guid? workflowModelId = newContext == null ? null : newContext.WorkflowModelId;
        IWorkflowModel workflowModel = workflowModelId.HasValue ?
            GetOrLoadModel(workflowModelId.Value) as IWorkflowModel : null;
        CollectionUtils.AddAll(oldModels, oldContext.Models);

        // Communicate context change to models
        bool modelChange = oldContext.WorkflowModelId.HasValue != workflowModelId.HasValue ||
            (oldContext.WorkflowModelId.HasValue && workflowModelId.Value != oldContext.WorkflowModelId.Value);

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
      if (oldModels.Count > 0)
      {
        logger.Debug("WorkflowManager: Tidying up...");
        foreach (Guid modelId in oldModels.Keys)
          if (!IsModelContainedInNavigationStack(modelId))
          {
            logger.Debug("WorkflowManager: Freeing model with id '{0}'", modelId);
            FreeModel(modelId);
          }
      }
      else
        logger.Debug("WorkflowManager: Nothing to tidy up");
    }

    protected bool UpdateScreen()
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      NavigationContext context = CurrentNavigationContext;
      string screen = context.WorkflowState.MainScreen;
      logger.Info("WorkflowManager: Trying to show screen '{0}'...", screen);
      bool result = ServiceScope.Get<IScreenManager>().ShowScreen(screen);
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

    public IDictionary<Guid, WorkflowStateAction> MenuStateActions
    {
      get { return _menuActions; }
    }

    public IDictionary<Guid, WorkflowStateAction> ContextMenuStateActions
    {
      get { return _contextMenuActions; }
    }

    public Stack<NavigationContext> NavigationContextStack
    {
      get { return _navigationContextStack; }
    }

    public NavigationContext CurrentNavigationContext
    {
      get { return _navigationContextStack.Count == 0 ? null : _navigationContextStack.Peek(); }
    }

    public void NavigatePush(Guid stateId)
    {
      WorkflowState state;
      if (!_states.TryGetValue(stateId, out state))
        throw new ArgumentException(string.Format("WorkflowManager: Workflow state '{0}' is not available", stateId));

      DoPushNavigationContext(state);
      if (!UpdateScreen())
        NavigatePop(1);
    }

    public void NavigatePushTransient(WorkflowState state)
    {
      DoPushNavigationContext(state);
      if (!UpdateScreen())
        NavigatePop(1);
    }

    public void NavigatePop(int count)
    {
      DoPopNavigationContext(count);
      while (!UpdateScreen())
        DoPopNavigationContext(1);
    }

    public bool IsModelContainedInNavigationStack(Guid modelId)
    {
      foreach (NavigationContext context in _navigationContextStack)
        if (context.Models.ContainsKey(modelId))
          return true;
      return false;
    }

    #endregion
  }
}
