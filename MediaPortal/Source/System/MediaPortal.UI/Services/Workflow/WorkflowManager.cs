#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UI.Services.Workflow
{
  public class WorkflowManager : IWorkflowManager
  {
    #region Classes

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

    #endregion

    #region Consts

    public const string MODELS_REGISTRATION_LOCATION = "/Models";
    public const int MODEL_CACHE_MAX_NUM_UNUSED = 50;

    protected readonly static NavigationContextConfig EMPTY_NAVIGATION_CONTEXT_CONFIG = new NavigationContextConfig();

    protected static readonly TimeSpan LOCK_TIMEOUT = TimeSpan.FromSeconds(2);

    #endregion

    #region Protected fields

    protected ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    protected Stack<NavigationContext> _navigationContextStack = new Stack<NavigationContext>();

    protected IDictionary<Guid, ModelEntry> _modelCache = new Dictionary<Guid, ModelEntry>();
    protected ModelItemStateTracker _modelItemStateTracker;
    protected IDictionary<Guid, WorkflowState> _states = new Dictionary<Guid, WorkflowState>();
    protected IDictionary<Guid, WorkflowAction> _menuActions =  new Dictionary<Guid, WorkflowAction>();
    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    #region Ctor

    public WorkflowManager()
    {
      _modelItemStateTracker = new ModelItemStateTracker(this);
      ServiceScope.Get<IPluginManager>().RegisterSystemPluginItemBuilder("Model", new ModelBuilder());
    }

    #endregion

    #region Private & protected methods

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           SkinResourcesMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    protected void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SkinResourcesMessaging.CHANNEL)
      {
        SkinResourcesMessaging.MessageType messageType = (SkinResourcesMessaging.MessageType) message.MessageType;
        if (messageType == SkinResourcesMessaging.MessageType.SkinResourcesChanged)
          ReloadWorkflowResources();
      }
    }

    protected void EnterWriteLock(string operation)
    {
      if (!_lock.TryEnterWriteLock(LOCK_TIMEOUT))
        throw new WorkflowManagerLockException("The workflow manager cannot be locked for write operation '{0}' (deadlock?)", operation);
    }

    protected void EnterReadLock(string operation)
    {
      if (!_lock.TryEnterReadLock(LOCK_TIMEOUT))
        throw new WorkflowManagerLockException("The workflow manager cannot be locked for read operation '{0}' (deadlock?)", operation);
    }

    protected void ExitWriteLock()
    {
      _lock.ExitWriteLock();
    }

    protected void ExitReadLock()
    {
      _lock.ExitReadLock();
    }

    /// <summary>
    /// (Re)loads all workflow resources from the skin resource manager.
    /// </summary>
    protected void ReloadWorkflowResources()
    {
      if (_states.Count == 0)
        ServiceScope.Get<ILogger>().Debug("WorkflowManager: Loading workflow resources");
      else
        ServiceScope.Get<ILogger>().Debug("WorkflowManager: Reloading workflow resources");
      WorkflowResourcesLoader loader = new WorkflowResourcesLoader();
      loader.Load();
      EnterWriteLock("ReloadWorkflowResources");
      try
      {
        _states = loader.States;
        _menuActions = loader.MenuActions;
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
      finally
      {
        ExitWriteLock();
      }
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
      EnterWriteLock("GetOrLoadModel");
      try
      {
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
      finally
      {
        ExitWriteLock();
      }
    }

    /// <summary>
    /// Releases the usage of the specified model at the plugin manager and removes it from the model cache.
    /// </summary>
    /// <param name="modelId">Id of the model to free.</param>
    protected void FreeModel_NoLock(Guid modelId)
    {
      EnterWriteLock("FreeModel_NoLock");
      try
      {
        _modelCache.Remove(modelId);
      }
      finally
      {
        ExitWriteLock();
      }
      ServiceScope.Get<IPluginManager>().RevokePluginItem(MODELS_REGISTRATION_LOCATION, modelId.ToString(), _modelItemStateTracker);
    }

    protected void RemoveModelFromNavigationStack(Guid modelId)
    {
      EnterWriteLock("RemoveModelFromNavigationStack");
      try
      {
        // Pop all navigation contexts until requested model isn't used any more
        while (IsModelContainedInNavigationStack(modelId))
          if (!DoPopNavigationContext(1))
            break;
        UpdateScreen_NeedsLock();
      }
      finally
      {
        ExitWriteLock();
      }
    }

    protected static IEnumerable<WorkflowAction> FilterActionsBySourceState(Guid sourceState, ICollection<WorkflowAction> actions)
    {
      foreach (WorkflowAction action in actions)
        if (!action.SourceStateId.HasValue || action.SourceStateId.Value == sourceState)
          yield return action;
    }

    protected WorkflowState FindLastNonTransientState()
    {
      EnterReadLock("FindLastNonTransientState");
      try
      {
        NavigationContext current = CurrentNavigationContext;
        while (current != null && current.WorkflowState.IsTransient)
          current = current.Predecessor;
        // Now we have skipped all contexts with transient states on the stack
        return current == null ? null : current.WorkflowState;
      }
      finally
      {
        ExitReadLock();
      }
    }

    protected bool DoPushNavigationContext(WorkflowState state, NavigationContextConfig config)
    {
      if (config == null)
        config = EMPTY_NAVIGATION_CONTEXT_CONFIG;
      EnterWriteLock("DoPushNavigationContext");
      try
      {
        ILogger logger = ServiceScope.Get<ILogger>();
        NavigationContext current = CurrentNavigationContext;

        if (current != null && current.WorkflowState.IsTemporary && state.WorkflowType == WorkflowType.Workflow)
        {
          logger.Info("Current workflow state '{0}' is temporary, popping it from navigation stack", current.WorkflowState.Name);
          DoPopNavigationContext(1);
        }

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
        NavigationContext newContext = new NavigationContext(state, config.NavigationContextDisplayLabel, predecessor, workflowModel);
        if (config.AdditionalContextVariables != null)
          lock (newContext.SyncRoot)
            CollectionUtils.AddAll(newContext.ContextVariables, config.AdditionalContextVariables);

        // Check if state change is accepted
        if (workflowModel != null && !workflowModel.CanEnterState(predecessor, newContext))
        {
          logger.Debug("WorkflowManager: Workflow model with id '{0}' doesn't accept the state being pushed onto the workflow context stack. Reverting to old workflow state.", workflowModelId);
          return false;
        }

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
          IDictionary<Guid, WorkflowAction> menuActions = new Dictionary<Guid, WorkflowAction>();
          foreach (WorkflowAction action in FilterActionsBySourceState(state.StateId, _menuActions.Values))
            menuActions.Add(action.ActionId, action);
          if (workflowModel != null)
            workflowModel.UpdateMenuActions(newContext, menuActions);

          newContext.SetMenuActions(menuActions.Values);
        }

        WorkflowManagerMessaging.SendStatePushedMessage(newContext);
        return true;
      }
      finally
      {
        ExitWriteLock();
        IterateCache_NoLock();
      }
    }

    protected bool DoPopNavigationContext(int count)
    {
      EnterWriteLock("DoPopNavigationContext");
      try
      {
        ILogger logger = ServiceScope.Get<ILogger>();

        logger.Info("WorkflowManager: Trying to remove {0} workflow states from navigation stack...", count);
        IDictionary<Guid, NavigationContext> removedContexts = new Dictionary<Guid, NavigationContext>();
        for (int i=0; i<count; i++)
        {
          if (_navigationContextStack.Count <= 1)
          {
            logger.Info("WorkflowManager: Cannot remove the initial workflow state... We'll break the loop here");
            return false;
          }
          NavigationContext oldContext = _navigationContextStack.Pop();
          removedContexts[oldContext.WorkflowState.StateId] = oldContext;
          if (oldContext.WorkflowState.WorkflowType == WorkflowType.Dialog)
          {
            IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
            if (screenManager.IsDialogVisible && screenManager.ActiveScreenName == oldContext.WorkflowState.MainScreen)
              // In fact this will trigger our close dialog delegate which we attached in the UpdateScreen_NeedsLock method,
              // but the anonymous close event delegate checks the current navigation context, which has already been
              // removed here (see method UpdateScreen_NeedsLock)
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
            // Don't check "workflowModel.CanEnterState(oldContext, newContext)" here; the WF-Model is responsible
            // itself that it isn't in an invalid state
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
          oldContext.Dispose();
        }
        WorkflowManagerMessaging.SendStatesPoppedMessage(removedContexts);
        return true;
      }
      finally
      {
        ExitWriteLock();
        IterateCache_NoLock();
      }
    }

    protected void IterateCache_NoLock()
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      logger.Debug("WorkflowManager: Tidying up...");
      ICollection<Guid> modelsToFree = new List<Guid>();
      EnterWriteLock("IterateCache_NoLock");
      try
      {
        foreach (KeyValuePair<Guid, ModelEntry> modelEntry in _modelCache)
        {
          Guid modelId = modelEntry.Key;
          if (!IsModelContainedInNavigationStack(modelId))
            if (modelEntry.Value.NumStatesUnused > MODEL_CACHE_MAX_NUM_UNUSED)
            {
              logger.Debug("WorkflowManager: Freeing unused model with id '{0}'", modelId);
              modelsToFree.Add(modelId);
            }
            else
              modelEntry.Value.Iterate();
        }
      }
      finally
      {
        ExitWriteLock();
      }
      foreach (Guid modelId in modelsToFree)
        FreeModel_NoLock(modelId);
    }

    /// <summary>
    /// Show the screen for the current navigation context.
    /// </summary>
    /// <remarks>
    /// If the batch update mode is set, this method will just cache the screen update.
    /// </remarks>
    protected void UpdateScreen_NeedsLock()
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      NavigationContext currentContext = CurrentNavigationContext;
      Guid? workflowModelId = currentContext.WorkflowModelId;
      IWorkflowModel workflowModel = workflowModelId.HasValue ?
          GetOrLoadModel(workflowModelId.Value) as IWorkflowModel : null;
      string screen = currentContext.WorkflowState.MainScreen;
      ScreenUpdateMode updateMode = workflowModel == null ? ScreenUpdateMode.AutoWorkflowManager :
          workflowModel.UpdateScreen(currentContext, ref screen);

      if (updateMode == ScreenUpdateMode.ManualWorkflowModel)
      {
        logger.Info("WorkflowManager: Screen was updated by workflow model");
        return;
      }
      if (screen == null)
        throw new UnexpectedStateException("WorkflowManager: No main screen available for workflow state '{0}' (id '{1}')",
            currentContext.WorkflowState.Name, currentContext.WorkflowState.StateId);
      bool result;
      WorkflowType workflowType = currentContext.WorkflowState.WorkflowType;
      if (workflowType == WorkflowType.Workflow)
      {
        logger.Info("WorkflowManager: Trying to show screen '{0}'...", screen);
        result = screenManager.ShowScreen(screen);
      }
      else if (workflowType == WorkflowType.Dialog)
      {
        logger.Info("WorkflowManager: Trying to open dialog screen '{0}'...", screen);
        result = screenManager.ShowDialog(screen, dialogName => NavigatePopToState(currentContext.WorkflowState.StateId, true));
      }
      else
        throw new NotImplementedException(string.Format("WorkflowManager: WorkflowType '{0}' is not implemented", workflowType));

      if (result)
        logger.Info("WorkflowManager: Screen '{0}' successfully shown", screen);
      else
        logger.Info("WorkflowManager: Error showing screen '{0}'", screen);
    }

    #endregion

    #region IWorkflowManager implementation

    public ReaderWriterLockSlim Lock
    {
      get { return _lock; }
    }

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
      get
      {
        EnterReadLock("CurrentNavigationContext");
        try
        {
          return _navigationContextStack.Count == 0 ? null : _navigationContextStack.Peek();
        }
        finally
        {
          ExitReadLock();
        }
      }
    }

    public void Initialize()
    {
      ServiceScope.Get<ILogger>().Info("WorkflowManager: Startup");
      SubscribeToMessages();
      ReloadWorkflowResources();
    }

    public void Shutdown()
    {
      EnterWriteLock("Shutdown");
      try
      {
        ServiceScope.Get<ILogger>().Info("WorkflowManager: Shutdown");
        UnsubscribeFromMessages();
        foreach (Guid modelId in new List<Guid>(_modelCache.Keys))
          FreeModel_NoLock(modelId);
        foreach (NavigationContext context in _navigationContextStack)
          context.Dispose();
        _navigationContextStack.Clear();
      }
      finally
      {
        ExitWriteLock();
      }
    }

    public void NavigatePush(Guid stateId, NavigationContextConfig config)
    {
      EnterWriteLock("NavigatePush");
      try
      {
        WorkflowState state;
        if (!_states.TryGetValue(stateId, out state))
          throw new ArgumentException(string.Format("WorkflowManager: Workflow state '{0}' is not available", stateId));

        if (DoPushNavigationContext(state, config))
          UpdateScreen_NeedsLock();
        WorkflowManagerMessaging.SendNavigationCompleteMessage();
      }
      finally
      {
        ExitWriteLock();
      }
    }

    public void NavigatePushTransient(WorkflowState state, NavigationContextConfig config)
    {
      EnterWriteLock("NavigatePushTransient");
      try
      {
        if (DoPushNavigationContext(state, config))
          UpdateScreen_NeedsLock();
        WorkflowManagerMessaging.SendNavigationCompleteMessage();
      }
      finally
      {
        ExitWriteLock();
      }
    }

    public void NavigatePop(int count)
    {
      EnterWriteLock("NavigatePop");
      try
      {
        DoPopNavigationContext(count);
        UpdateScreen_NeedsLock();
        WorkflowManagerMessaging.SendNavigationCompleteMessage();
      }
      finally
      {
        ExitWriteLock();
      }
    }

    public bool NavigatePopToState(Guid stateId, bool inclusive)
    {
      EnterWriteLock("NavigatePopToState");
      try
      {
        if (!IsStateContainedInNavigationStack(stateId))
          return false;
        while (CurrentNavigationContext.WorkflowState.StateId != stateId)
          if (!DoPopNavigationContext(1))
            break;
        if (inclusive)
          DoPopNavigationContext(1);
        UpdateScreen_NeedsLock();
        WorkflowManagerMessaging.SendNavigationCompleteMessage();
        return true;
      }
      finally
      {
        ExitWriteLock();
      }
    }

    public void StartBatchUpdate()
    {
      // We delegate the update lock for screens to the screen manager because it is easier to do it there.
      // If we wanted to implement a native batch update mechanism in this service, we would have to cope with
      // simple cases like static workflow state screens as well as with complex cases like screen updates which
      // are done by workflow models.
      // But with a batch update, the internal state of workflow models, which has been established by calls to
      // EnterModelContext/ChangeModelContext/ExitModelContext, will become out-of-sync with screen update requests,
      // which would need to take place after the actual workflow model state change.
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      screenManager.StartBatchUpdate();
    }

    public void EndBatchUpdate()
    {
      // See comment in method StartBatchUpdate()
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      screenManager.EndBatchUpdate();
    }

    public object GetModel(Guid modelId)
    {
      EnterWriteLock("GetModel");
      try
      {
        object model = GetOrLoadModel(modelId);
        NavigationContext current = CurrentNavigationContext;
        lock (current.SyncRoot)
          if (!current.Models.ContainsKey(modelId))
          {
            ServiceScope.Get<ILogger>().Debug(
                "WorkflowManager: Attaching GUI model '{0}' to workflow state '{1}'",
                modelId, CurrentNavigationContext.WorkflowState.StateId);
            current.Models[modelId] = model;
          }
        return model;
      }
      finally
      {
        ExitWriteLock();
      }
    }

    public bool IsStateContainedInNavigationStack(Guid workflowStateId)
    {
      EnterReadLock("IsStateContainedInNavigationStack");
      try
      {
        foreach (NavigationContext context in _navigationContextStack)
          if (context.WorkflowState.StateId == workflowStateId)
            return true;
        return false;
      }
      finally
      {
        ExitReadLock();
      }
    }

    public bool IsModelContainedInNavigationStack(Guid modelId)
    {
      EnterReadLock("IsModelContainedInNavigationStack");
      try
      {
        foreach (NavigationContext context in _navigationContextStack)
          lock (context.SyncRoot)
            if (context.Models.ContainsKey(modelId))
              return true;
        return false;
      }
      finally
      {
        ExitReadLock();
      }
    }

    public void FlushModelCache()
    {
      EnterWriteLock("FlushModelCache");
      try
      {
        foreach (KeyValuePair<Guid, ModelEntry> modelEntry in _modelCache)
          if (!IsModelContainedInNavigationStack(modelEntry.Key))
            _modelCache.Remove(modelEntry);
      }
      finally
      {
        ExitWriteLock();
      }
    }

    #endregion
  }
}
