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
using MediaPortal.Core.Exceptions;
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
        if (!_states.ContainsKey(context.WorkflowState.StateId))
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
      ServiceScope.Get<IPluginManager>().RevokePluginItem(MODELS_REGISTRATION_LOCATION, modelId.ToString(), _modelItemStateTracker);
      _modelCache.Remove(modelId);
    }

    /// <summary>
    /// Returns the information if one of the active navigation contexts on the stack contains the model
    /// with the specified <paramref name="modelId"/>.
    /// </summary>
    /// <param name="modelId">Id of the model to search.</param>
    /// <returns><c>true</c>, if the specified model is currently used in any navigation context, else
    /// <c>false</c>.</returns>
    protected bool IsModelContainedInNavigationStack(Guid modelId)
    {
      foreach (NavigationContext context in _navigationContextStack)
        if (context.Models.ContainsKey(modelId))
          return true;
      return false;
    }

    protected void RemoveModelFromNavigationStack(Guid modelId)
    {
      while (IsModelContainedInNavigationStack(modelId))
      {
        // Pop others from stack
        while (!CurrentNavigationContext.Models.ContainsKey(modelId))
          DoPopNavigationContext(1);
        // Pop navigation context with requested model
        DoPopNavigationContext(1);
      }
      UpdateScreen();
    }

    protected static IEnumerable<WorkflowStateAction> FilterActionsBySourceState(Guid sourceState, ICollection<WorkflowStateAction> actions)
    {
      foreach (WorkflowStateAction action in actions)
        if (action.SourceState == sourceState)
          yield return action;
    }

    protected void DoPushNavigationContext(Guid stateId)
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      NavigationContext predecessor = CurrentNavigationContext;
      WorkflowState state;
      if (!_states.TryGetValue(stateId, out state))
        throw new ArgumentException(string.Format("WorkflowManager: Workflow state '{0}' is not available", stateId));

      logger.Info("WorkflowManager: Switching to workflow state '{0}' (id='{1}')...", state.Name, state.StateId);
      logger.Debug("WorkflowManager: Loading models for workflow state '{0}'", state.Name);
      IWorkflowModel workflowModel = null;
      if (state.WorkflowModelId.HasValue)
      {
        object model = GetOrLoadModel(state.WorkflowModelId.Value);
        if (model is IWorkflowModel)
        {
          logger.Debug("WorkflowManager: Using workflow model with id '{0}' for new workflow state '{1}'",
              state.WorkflowModelId.Value, state.StateId);
          workflowModel = (IWorkflowModel)model;
        }
        else
          logger.Error("WorkflowManager: Model with id '{0}', which is used as workflow model in state '{1}', doesn't implement the interface '{2}'",
              state.WorkflowModelId.Value, state.StateId, typeof(IWorkflowModel).Name);
      }
      IDictionary<Guid, object> models = new Dictionary<Guid, object>();
      if (workflowModel != null)
        models.Add(workflowModel.ModelId, workflowModel);
      foreach (Guid modelId in state.AdditionalModels)
        models.Add(modelId, GetOrLoadModel(modelId));
      NavigationContext newContext = new NavigationContext(state, predecessor, models);

      logger.Debug("WorkflowManager: Compiling menu actions and context menu actions for workflow state '{0}'", state.Name);
      ICollection<WorkflowStateAction> menuActions = new List<WorkflowStateAction>();
      ICollection<WorkflowStateAction> contextMenuActions = new List<WorkflowStateAction>();
      if (state.InheritMenu)
        CollectionUtils.AddAll(menuActions, predecessor.MenuActions.Values);
      if (state.InheritContextMenu)
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

      _navigationContextStack.Push(newContext);
      logger.Debug("WorkflowManager: Entering workflow state '{0}'", state.Name);
      if (workflowModel != null)
        workflowModel.StartModelContext(newContext);
    }

    protected void DoPopNavigationContext(int count)
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      logger.Info("WorkflowManager: Removing {0} workflow states from navigation stack...", count);

      if (_navigationContextStack.Count < count)
        throw new IllegalCallException("WorkflowManager: Cannot pop current navigation context - no more navigation levels left on stack");
      IDictionary<Guid, object> removedModels = new Dictionary<Guid, object>();
      for (int i=0; i<count; i++)
      {
        NavigationContext context = _navigationContextStack.Pop();
        NavigationContext newContext = _navigationContextStack.Count == 0 ? null : _navigationContextStack.Peek();
        foreach (KeyValuePair<Guid, object> model in context.Models)
        {
          if (model.Value is IWorkflowModel)
          {
            logger.Debug("WorkflowManager: Removing state '{0}' (id '{1}'): Exiting workflow state model context at workflow model '{2}'",
                context.WorkflowState.Name, context.WorkflowState.StateId, model.Key);
            ((IWorkflowModel) model.Value).ExitModelContext(context, newContext);
          }
          else
            logger.Debug("WorkflowManager: Removing state '{0}' (id '{1}'): No workflow model present to exit any model context",
                context.WorkflowState.Name, context.WorkflowState.StateId);
          removedModels[model.Key] = model.Value;
        }
      }
      if (removedModels.Count > 0)
      {
        logger.Debug("WorkflowManager: Tidying up...");
        foreach (Guid modelId in removedModels.Keys)
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

    public ICollection<WorkflowState> States
    {
      get { return _states.Values; }
    }

    public ICollection<WorkflowStateAction> MenuStateActions
    {
      get { return _menuActions.Values; }
    }

    public ICollection<WorkflowStateAction> ContextMenuStateActions
    {
      get { return _contextMenuActions.Values; }
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
      DoPushNavigationContext(stateId);
      if (!UpdateScreen())
        NavigatePop(1);
    }

    public void NavigatePop(int count)
    {
      DoPopNavigationContext(count);
      while (!UpdateScreen())
        DoPopNavigationContext(1);
    }

    #endregion
  }
}
