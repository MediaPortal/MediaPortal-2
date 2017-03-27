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

// Define DEBUG_LOCKOWNER to make the WF-Manager track the last operation which is currently owner of the workflow manager's lock.
//#define DEBUG_LOCKOWNER

// Define DEBUG_LOCKREQUESTS to write lock requests and -releases to the system trace output writer.
//#define DEBUG_LOCKREQUESTS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Control.InputManager;
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
    public const string WORKFLOW_STATES_REGISTRATION_LOCATION = "/Workflow/States";

    public const int MODEL_CACHE_MAX_NUM_UNUSED = 50;

    protected static readonly NavigationContextConfig EMPTY_NAVIGATION_CONTEXT_CONFIG = new NavigationContextConfig();

    protected static readonly TimeSpan LOCK_TIMEOUT = TimeSpan.FromSeconds(10);

    #endregion

    #region Protected fields

#if DEBUG_LOCKOWNER
    protected int _lockDepth = 0;
    protected string _lastLockOperation = null;
#endif
    protected ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    protected Stack<NavigationContext> _navigationContextStack = new Stack<NavigationContext>();

    protected IDictionary<Guid, ModelEntry> _modelCache = new Dictionary<Guid, ModelEntry>();
    protected IPluginItemStateTracker _modelItemStateTracker;
    protected IPluginItemStateTracker _wfStateItemStateTracker;
    protected IItemRegistrationChangeListener _workflowPluginItemsChangeListener;
    protected IDictionary<Guid, WorkflowState> _states = new Dictionary<Guid, WorkflowState>();
    protected IDictionary<Guid, WorkflowAction> _menuActions =  new Dictionary<Guid, WorkflowAction>();
    protected IDictionary<Key, WorkflowState> _workflowStateShortcuts = new Dictionary<Key, WorkflowState>();
    protected IDictionary<Key, WorkflowAction> _workflowActionShortcuts = new Dictionary<Key, WorkflowAction>();
    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    #region Ctor

    public WorkflowManager()
    {
      _modelItemStateTracker = new DefaultItemStateTracker("WorkflowManager: Model usage")
        {
            // We could store the end-requested model in an array of "suspended models" in the WF manager,
            // method WFM.GetOrLoadModel would then fail to load any of the suspended models
            EndRequested = itemRegistration => !IsModelContainedInNavigationStack(new Guid(itemRegistration.Metadata.Id)),

            Stopped = itemRegistration => NavigatePopModel(new Guid(itemRegistration.Metadata.Id))

            // If we'd maintain a collection of "suspended models" (like said in comment in method RequestEnd),
            // we would need to cancel the suspension of the continued model in the Continued delegate.
        };
      _wfStateItemStateTracker = new DefaultItemStateTracker("WorkflowManager: Workflow state usage")
        {
            Stopped = itemRegistration => ReloadWorkflowStates()
        };
      _workflowPluginItemsChangeListener = new DefaultItemRegistrationChangeListener("WorkflowManager: Workflow state usage")
        {
            ItemsWereAdded = (location, items) => ReloadWorkflowStates(),

            ItemsWereRemoved = (location, items) => ReloadWorkflowStates()
        };
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
        switch (messageType)
        {
          case SkinResourcesMessaging.MessageType.SkinResourcesChanged:
          case SkinResourcesMessaging.MessageType.SkinOrThemeChanged:
            ReloadWorkflowActions();
            break;
        }
      }
    }

    protected void EnterWriteLock(string operation)
    {
      if (!_lock.TryEnterWriteLock(LOCK_TIMEOUT))
        throw new WorkflowManagerLockException("The workflow manager cannot be locked for write operation '{0}' (deadlock?)", operation);
#if DEBUG_LOCKOWNER
#if DEBUG_LOCKREQUESTS
      System.Diagnostics.Trace.WriteLine(string.Format("{0}Entering write operation '{1}' (Thread: {2})", StringUtils.Repeat("  ", _lockDepth), operation, Thread.CurrentThread.Name));
#endif
      _lockDepth++;
      if (_lockDepth == 1)
        _lastLockOperation = operation;
#endif
    }

    protected void EnterReadLock(string operation)
    {
      if (!_lock.TryEnterReadLock(LOCK_TIMEOUT))
        throw new WorkflowManagerLockException("The workflow manager cannot be locked for read operation '{0}' (deadlock?)", operation);
#if DEBUG_LOCKOWNER
#if DEBUG_LOCKREQUESTS
      System.Diagnostics.Trace.WriteLine(string.Format("{0}Entering read operation '{1}' (Thread: {2})", StringUtils.Repeat("  ", _lockDepth), operation, Thread.CurrentThread.Name));
#endif
      _lockDepth++;
      if (_lockDepth == 1)
        _lastLockOperation = operation;
#endif
    }

    protected void ExitWriteLock()
    {
#if DEBUG_LOCKOWNER
      _lockDepth--;
      if (_lockDepth == 0)
        _lastLockOperation = null;
#if DEBUG_LOCKREQUESTS
      System.Diagnostics.Trace.WriteLine(string.Format("{0}Exiting write operation (Thread: {1})", StringUtils.Repeat("  ", _lockDepth), Thread.CurrentThread.Name));
#endif
#endif
      _lock.ExitWriteLock();
    }

    protected void ExitReadLock()
    {
#if DEBUG_LOCKOWNER
      _lockDepth--;
      if (_lockDepth == 0)
        _lastLockOperation = null;
#if DEBUG_LOCKREQUESTS
      System.Diagnostics.Trace.WriteLine(string.Format("{0}Exiting read operation (Thread: {1})", StringUtils.Repeat("  ", _lockDepth), Thread.CurrentThread.Name));
#endif
#endif
      _lock.ExitReadLock();
    }

    protected void RegisterWorkflowPluginItemChangeListener()
    {
      ServiceRegistration.Get<IPluginManager>().AddItemRegistrationChangeListener(WORKFLOW_STATES_REGISTRATION_LOCATION, _workflowPluginItemsChangeListener);
    }

    /// <summary>
    /// (Re)loads all workflow resources from the skin resource manager. This also includes shortcut definitions.
    /// </summary>
    protected void ReloadWorkflowActions()
    {
      ServiceRegistration.Get<ILogger>().Debug(_states.Count == 0 ? "WorkflowManager: Loading workflow actions and shortcuts" :
          "WorkflowManager: Reloading workflow actions and shortcuts");

      WorkflowResourcesLoader loader = new WorkflowResourcesLoader();
      loader.Load();
      EnterWriteLock("ReloadWorkflowActions");

      // First remove any previously created shortcuts
      UnregisterActionShortcuts();

      _menuActions = loader.MenuActions;

      ServiceRegistration.Get<ILogger>().Debug("WorkflowManager: Loading workflow action shortcuts");
      ShortcutResourcesLoader shortcutLoader = new ShortcutResourcesLoader();
      shortcutLoader.LoadWorkflowActionShortcuts();
      _workflowActionShortcuts = shortcutLoader.WorkflowActionShortcuts;

      // Register shortcuts after (re-)loading
      RegisterActionShortcuts();

      ExitWriteLock();
    }

    protected void UnregisterActionShortcuts()
    {
      UnregisterKeyBindings(_workflowActionShortcuts.Keys);
    }

    protected void RegisterActionShortcuts()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      foreach (KeyValuePair<Key, WorkflowAction> workflowAction in _workflowActionShortcuts)
      {
        var action = workflowAction.Value;
        inputManager.AddKeyBinding(workflowAction.Key, () =>
        {
          IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
          if (action.IsEnabled(workflowManager.CurrentNavigationContext) && action.IsVisible(workflowManager.CurrentNavigationContext))
            action.Execute();
        });
      }
    }

    /// <summary>
    /// (Re)loads all workflow states from the plugin tree.
    /// </summary>
    protected void ReloadWorkflowStates()
    {
      ServiceRegistration.Get<ILogger>().Debug(_states.Count == 0 ? "WorkflowManager: Loading workflow states and shortcuts" :
          "WorkflowManager: Reloading workflow states and shortcuts");
      EnterWriteLock("ReloadWorkflowStates");
      try
      {
        // First remove any previously created shortcuts
        UnregisterStateShortcuts();

        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        _states.Clear();
        foreach (WorkflowState state in pluginManager.RequestAllPluginItems<WorkflowState>(WORKFLOW_STATES_REGISTRATION_LOCATION, _wfStateItemStateTracker))
          _states.Add(state.StateId, state);
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

        ServiceRegistration.Get<ILogger>().Debug("WorkflowManager: Loading workflow state shortcuts");
        ShortcutResourcesLoader shortcutLoader = new ShortcutResourcesLoader();
        shortcutLoader.LoadWorkflowStateShortcuts();
        _workflowStateShortcuts = shortcutLoader.WorkflowStateShortcuts;

        // Register shortcuts after (re-)loading
        RegisterStateShortcuts();
      }
      finally
      {
        ExitWriteLock();
      }
    }

    protected void UnregisterStateShortcuts()
    {
      UnregisterKeyBindings(_workflowStateShortcuts.Keys);
    }

    protected void UnregisterKeyBindings(IEnumerable<Key> keys)
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      foreach (Key key in keys)
        inputManager.RemoveKeyBinding(key);
    }

    protected void RegisterStateShortcuts()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      foreach (KeyValuePair<Key, WorkflowState> workflowAction in _workflowStateShortcuts)
      {
        var stateId = workflowAction.Value.StateId;
        inputManager.AddKeyBinding(workflowAction.Key, () =>
        {
          bool isInStack = NavigationContextStack.ToList().FirstOrDefault(n => n.WorkflowState.StateId == stateId) != null;
          if (isInStack)
            NavigatePopToState(stateId, false);
          else
            NavigatePush(stateId);
        });
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
        ServiceRegistration.Get<ILogger>().Debug("WorkflowManager: Loading GUI model '{0}'", modelId);
        object model = ServiceRegistration.Get<IPluginManager>().RequestPluginItem<object>(
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
      ServiceRegistration.Get<IPluginManager>().RevokePluginItem(MODELS_REGISTRATION_LOCATION, modelId.ToString(), _modelItemStateTracker);
    }

    protected static IEnumerable<WorkflowAction> FilterActionsBySourceState(Guid sourceState, ICollection<WorkflowAction> actions)
    {
      return actions.Where(action => action.SourceStateIds == null || action.SourceStateIds.Contains(sourceState));
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

    /// <summary>
    /// Synchronous method which pushes the given workflow <paramref name="state"/> onto the navigation stack.
    /// </summary>
    /// <remarks>
    /// This method rethrows all exceptions which are thrown by workflow models. In case an exception is thrown,
    /// the internal state is still valid but the caller must pop the current navigation context from the workflow stack.
    /// </remarks>
    /// <param name="state">Workflow state to push.</param>
    /// <param name="config">Additional navigation context configuration.</param>
    /// <returns><c>true</c> if the push action was successful, else <c>false</c>.</returns>
    protected bool DoPushNavigationContext(WorkflowState state, NavigationContextConfig config)
    {
      if (config == null)
        config = EMPTY_NAVIGATION_CONTEXT_CONFIG;
      EnterWriteLock("DoPushNavigationContext");
      try
      {
        ILogger logger = ServiceRegistration.Get<ILogger>();
        NavigationContext current = CurrentNavigationContext;

        if (current != null && current.WorkflowState.IsTemporary && state.WorkflowType == WorkflowType.Workflow)
        {
          logger.Info("Current workflow state '{0}' is temporary, popping it from navigation stack", current.WorkflowState.Name);
          // The next statement can throw an exception - don't catch it - our caller should pop the current navigation context
          bool workflowStatePopped;
          DoPopNavigationContext(1, out workflowStatePopped);
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
        bool canEnter = true;
        if (workflowModel != null)
          try
          {
            canEnter = workflowModel.CanEnterState(predecessor, newContext);
          }
          catch (Exception e)
          {
            logger.Error("WorkflowManager: Error checking if workflow model '{0}' can enter workflow state '{1}'", e,
              workflowModel.ModelId, newContext.WorkflowState.StateId);
            canEnter = false;
          }
        if (!canEnter)
        {
          logger.Debug("WorkflowManager: Workflow model with id '{0}' doesn't accept the state being pushed onto the workflow context stack. Reverting to old workflow state.", workflowModelId);
          return false;
        }

        // Store model exceptions
        IList<Exception> delayedExceptions = new List<Exception>();

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
            try
            {
              predecessorWorkflowModel.Deactivate(predecessor, newContext);
            }
            catch (Exception e)
            {
              logger.Error("WorkflowManager: Error deactivating workflow model '{0}'", e, predecessorWorkflowModel.ModelId);
              delayedExceptions.Add(e);
            }
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
            try
            {
              workflowModel.EnterModelContext(predecessor, newContext);
            }
            catch (Exception e)
            {
              logger.Error("WorkflowManager: Error entering model context of workflow model '{0}' for workflow state '{1}'", e,
                  workflowModel.ModelId, newContext.WorkflowState.StateId);
              delayedExceptions.Add(e);
            }
          }
          else
          {
            logger.Debug("WorkflowManager: Changing model context to workflow state '{0}' (old state was '{1}') in workflow model '{2}'",
                newContext.WorkflowState.StateId, predecessor == null ? null : predecessor.WorkflowState.StateId.ToString(), workflowModelId.Value);
            try
            {
              workflowModel.ChangeModelContext(predecessor, newContext, true);
            }
            catch (Exception e)
            {
              logger.Error("WorkflowManager: Error changing model context of workflow model '{0}' from workflow state '{1}' to workflow state '{2}'",
                e, workflowModel.ModelId, predecessor.WorkflowState.StateId, newContext.WorkflowState.StateId);
              delayedExceptions.Add(e);
            }
          }
        if (state.WorkflowType == WorkflowType.Workflow)
        {
          // Compile menu actions
          logger.Debug("WorkflowManager: Compiling menu actions for workflow state '{0}'", state.Name);
          IDictionary<Guid, WorkflowAction> menuActions = new Dictionary<Guid, WorkflowAction>();
          foreach (WorkflowAction action in FilterActionsBySourceState(state.StateId, _menuActions.Values))
            menuActions.Add(action.ActionId, action);
          if (workflowModel != null)
            try
            {
              workflowModel.UpdateMenuActions(newContext, menuActions);
            }
            catch (Exception e)
            {
              logger.Error("WorkflowManager: Error updating menu actions in workflow model '{0}' for workflow state '{1}'", e,
                  workflowModel.ModelId, newContext.WorkflowState.StateId);
              delayedExceptions.Add(e);
            }
          newContext.SetMenuActions(menuActions.Values);
        }
        if (delayedExceptions.Count > 0)
          throw delayedExceptions.First();
        WorkflowManagerMessaging.SendStatePushedMessage(newContext);
        return true;
      }
      finally
      {
        ExitWriteLock();
        IterateCache_NoLock();
      }
    }

    /// <summary>
    /// Pops <paramref name="count"/> workflow navigation contexts from the context stack.
    /// </summary>
    /// <remarks>
    /// This method rethrows all exceptions which are thrown by workflow models. In that case, the internal state is still
    /// valid but the caller must pop the current navigation context from the workflow stack.
    /// </remarks>
    /// <param name="count">Number of navigation contexts to pop.</param>
    /// <param name="workflowStatePopped">Will be set to <c>true</c>, if at least one state of type <see cref="WorkflowType.Workflow"/>
    /// was popped.</param>
    /// <returns><c>true</c> if the given <paramref name="count"/> of context stack frames could be removed from the
    /// navigation context stack. <c>false</c> if the context stack contains less than <c>count + 1</c> contexts.</returns>
    protected bool DoPopNavigationContext(int count, out bool workflowStatePopped)
    {
      EnterWriteLock("DoPopNavigationContext");
      try
      {
        workflowStatePopped = false;
        ILogger logger = ServiceRegistration.Get<ILogger>();

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
            IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
            // In fact this will trigger our close dialog delegate which we attached in the UpdateScreen_NeedsLock method,
            // but the anonymous close event delegate checks the current navigation context, which has already been
            // removed here (see method UpdateScreen_NeedsLock)
            Guid? dialogInstanceId = oldContext.DialogInstanceId;
            if (dialogInstanceId.HasValue)
              screenManager.CloseDialogs(dialogInstanceId.Value, true);
          }
          else
            workflowStatePopped = true;
          
          // Store model exceptions
          IList<Exception> delayedExceptions = new List<Exception>();

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
              try
              {
                predecessorWorkflowModel.ExitModelContext(oldContext, newContext);
              }
              catch (Exception e)
              {
                logger.Error("WorkflowManager: Error exiting model context of workflow model '{0}' at workflow state '{1}'", e,
                    predecessorWorkflowModel.ModelId, oldContext.WorkflowState.StateId);
                // No need to rethrow the exception - we have left the erroneous model and will enter a not-erroneous workflow state
              }
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
              try
              {
                workflowModel.Reactivate(oldContext, newContext);
              }
              catch (Exception e)
              {
                logger.Error("WorkflowManager: Error reactivating workflow model '{0}' for workflow state '{1}'", e,
                    workflowModel.ModelId, newContext.WorkflowState.StateId);
                // The current model produced an exception - rethrow it to make the caller pop the new workflow context from the stack
                delayedExceptions.Add(e);
              }
            }
            else
            {
              logger.Debug("WorkflowManager: Changing model context to workflow state '{0}' (old state was '{1}') in workflow model '{2}'",
                  newContext.WorkflowState.StateId, oldContext.WorkflowState.StateId, workflowModel.ModelId);
              try
              {
                workflowModel.ChangeModelContext(oldContext, newContext, false);
              }
              catch (Exception e)
              {
                logger.Error("WorkflowManager: Error changing model context of workflow model '{0}' from workflow state '{1}' to workflow state '{2}'",
                  e, workflowModel.ModelId, oldContext.WorkflowState.StateId, newContext.WorkflowState.StateId);
                // The current model produced an exception - rethrow it to make the caller pop the new workflow context from the stack
                delayedExceptions.Add(e);
              }
            }
          }
          oldContext.Dispose();
          if (delayedExceptions.Count > 0)
            throw delayedExceptions.First();
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
      ILogger logger = ServiceRegistration.Get<ILogger>();
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
    /// 
    /// This method just rethrows all exceptions thrown by models. It also throws an <see cref="EnvironmentException"/>
    /// if the screen to be loaded could not be loaded.
    /// </remarks>
    /// <param name="push">If the former screen operation was a workflow state push operation, this parameter is <c>true</c>,
    /// else <c>false</c>.</param>
    /// <param name="force">If set to <c>true</c>, the screen needs to be updated in any case. Else, the screen update may be prevented,
    /// if the required screen is already visible.</param>
    /// <exception cref="EnvironmentException">If the screen to be loaded could not be loaded.</exception>
    protected void UpdateScreen_NeedsLock(bool push, bool force)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      NavigationContext currentContext = CurrentNavigationContext;
      Guid? workflowModelId = currentContext.WorkflowModelId;
      IWorkflowModel workflowModel = workflowModelId.HasValue ?
          GetOrLoadModel(workflowModelId.Value) as IWorkflowModel : null;
      string screen = currentContext.WorkflowState.MainScreen;

      ScreenUpdateMode updateMode = ScreenUpdateMode.AutoWorkflowManager;
      if (workflowModel != null)
        try
        {
          updateMode = workflowModel.UpdateScreen(currentContext, ref screen);
        }
        catch (Exception e)
        {
          logger.Error("WorkflowManager: Error updating screen of workflow model '{0}' for workflow state '{1}'", e,
              workflowModel.ModelId, currentContext.WorkflowState.StateId);
          throw;
        }

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
        result = (force ? screenManager.ShowScreen(screen) : screenManager.CheckScreen(screen)).HasValue;
      }
      else if (workflowType == WorkflowType.Dialog)
      {
        if (push)
        {
          logger.Info("WorkflowManager: Trying to open dialog screen '{0}'...", screen);
          Guid? dialogInstanceId = screenManager.ShowDialog(screen, (dialogName, instanceId) => NavigatePopToStateAsync(currentContext.WorkflowState.StateId, true));
          if (dialogInstanceId.HasValue)
          {
            currentContext.DialogInstanceId = dialogInstanceId.Value;
            result = true;
          }
          else
            result = false;
        }
        else
        { // When states were popped, remove all screens on top of our workflow state dialog screen
          Guid? dialogInstanceId = currentContext.DialogInstanceId;
          if (dialogInstanceId.HasValue)
            screenManager.CloseDialogs(dialogInstanceId.Value, false);
          result = true;
        }
      }
      else
        throw new NotImplementedException(string.Format("WorkflowManager: WorkflowType '{0}' is not implemented", workflowType));

      if (result)
        logger.Info("WorkflowManager: Screen '{0}' successfully shown", screen);
      else
        throw new EnvironmentException("Error showing screen '{0}'", screen);
    }

    // Maybe called asynchronously.
    protected void NavigatePushInternal(Guid stateId, NavigationContextConfig config)
    {
      EnterWriteLock("NavigatePush");
      try
      {
        WorkflowState state;
        if (!_states.TryGetValue(stateId, out state))
          throw new ArgumentException(string.Format("WorkflowManager: Workflow state '{0}' is not available", stateId));
        try
        {
          if (DoPushNavigationContext(state, config))
            UpdateScreen_NeedsLock(true, true);
          WorkflowManagerMessaging.SendNavigationCompleteMessage();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("WorkflowManager.NavigatePushInternal: Error in workflow model or screen", e);
          NavigatePopInternal(1);
        }
      }
      finally
      {
        ExitWriteLock();
      }
    }

    // Maybe called asynchronously.
    protected void NavigatePushTransientInternal(WorkflowState state, NavigationContextConfig config)
    {
      EnterWriteLock("NavigatePushTransient");
      try
      {
        try
        {
          if (DoPushNavigationContext(state, config))
            UpdateScreen_NeedsLock(true, true);
          WorkflowManagerMessaging.SendNavigationCompleteMessage();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("WorkflowManager.NavigatePushTransientInternal: Error in workflow model or screen", e);
          NavigatePopInternal(1);
        }
      }
      finally
      {
        ExitWriteLock();
      }
    }

    // Maybe called asynchronously.
    protected void NavigatePopInternal(int count)
    {
      EnterWriteLock("NavigatePop");
      try
      {
        try
        {
          bool workflowStatePopped;
          if (DoPopNavigationContext(count, out workflowStatePopped))
            UpdateScreen_NeedsLock(false, workflowStatePopped);
          WorkflowManagerMessaging.SendNavigationCompleteMessage();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("WorkflowManager.NavigatePopInternal: Error in workflow model or screen", e);
          NavigatePopInternal(1);
        }
      }
      finally
      {
        ExitWriteLock();
      }
    }

    // Maybe called asynchronously.
    protected bool NavigatePopToStateInternal(Guid stateId, bool inclusive)
    {
      EnterWriteLock("NavigatePopToState");
      try
      {
        try
        {
          if (!IsStateContainedInNavigationStack(stateId))
            return false;
          bool removed = false;
          bool workflowStatePopped = false;
          while (CurrentNavigationContext.WorkflowState.StateId != stateId)
          {
            removed = true;
            if (!DoPopNavigationContext(1, out workflowStatePopped))
              break;
          }
          if (inclusive)
          {
            removed = true;
            DoPopNavigationContext(1, out workflowStatePopped);
          }
          if (removed)
          {
            UpdateScreen_NeedsLock(false, workflowStatePopped);
            WorkflowManagerMessaging.SendNavigationCompleteMessage();
            return true;
          }
          else
            return false;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("WorkflowManager.NavigatePopToStateInternal: Error in workflow model or screen", e);
          NavigatePopInternal(1);
          return false;
        }
      }
      finally
      {
        ExitWriteLock();
      }
    }

    protected bool NavigatePopStatesInternal(Guid[] workflowStateIds)
    {
      EnterWriteLock("NavigatePopToState");
      try
      {
        try
        {
          bool removed = false;
          bool workflowStatePopped = false;
          while (IsAnyStateContainedInNavigationStack(workflowStateIds))
          {
            removed = true;
            if (!DoPopNavigationContext(1, out workflowStatePopped))
              break;
          }
          if (removed)
          {
            UpdateScreen_NeedsLock(false, workflowStatePopped);
            WorkflowManagerMessaging.SendNavigationCompleteMessage();
            return true;
          }
          else
            return false;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("WorkflowManager.NavigatePopStatesInternal: Error in workflow model or screen", e);
          NavigatePopInternal(1);
          return false;
        }
      }
      finally
      {
        ExitWriteLock();
      }
    }

    // Maybe called asynchronously.
    protected void StartBatchUpdateInternal()
    {
      // We delegate the update lock for screens to the screen manager because it is easier to do it there.
      // If we wanted to implement a native batch update mechanism in this service, we would have to cope with
      // simple cases like static workflow state screens as well as with complex cases like screen updates which
      // are done by workflow models.
      // But with a batch update, the internal state of workflow models, which has been established by calls to
      // EnterModelContext/ChangeModelContext/ExitModelContext, will become out-of-sync with screen update requests,
      // which would need to take place after the actual workflow model state change.
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.StartBatchUpdate();
    }

    protected void EndBatchUpdateInternal()
    {
      // See comment in method StartBatchUpdate()
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.EndBatchUpdate();
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

    public bool TryExecuteAction(Guid actionId)
    {
      WorkflowAction action;
      if (!MenuStateActions.TryGetValue(actionId, out action))
        return false;

      if (action.IsEnabled(CurrentNavigationContext) && action.IsVisible(CurrentNavigationContext))
      {
        action.Execute();
        return true;
      }
      return false;
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
      ServiceRegistration.Get<ILogger>().Info("WorkflowManager: Startup");
      SubscribeToMessages();
      RegisterWorkflowPluginItemChangeListener();
      ReloadWorkflowStates();
      ReloadWorkflowActions();
    }

    public void Shutdown()
    {
      EnterWriteLock("Shutdown");
      try
      {
        ServiceRegistration.Get<ILogger>().Info("WorkflowManager: Shutdown");
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
      NavigatePushInternal(stateId, config);
    }

    public void NavigatePushAsync(Guid stateId, NavigationContextConfig config)
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(() => NavigatePushInternal(stateId, config));
    }

    public void NavigatePush(Guid stateId)
    {
      NavigatePush(stateId, null);
    }

    public void NavigatePushAsync(Guid stateId)
    {
      NavigatePushAsync(stateId, null);
    }

    public void NavigatePushTransient(WorkflowState state, NavigationContextConfig config)
    {
      NavigatePushTransientInternal(state, config);
    }

    public void NavigatePushTransientAsync(WorkflowState state, NavigationContextConfig config)
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(() => NavigatePushTransientInternal(state, config));
    }

    public void NavigatePop(int count)
    {
      NavigatePopInternal(count);
    }

    public void NavigatePopAsync(int count)
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(() => NavigatePopInternal(count));
    }

    public void NavigatePopToState(Guid stateId, bool inclusive)
    {
      NavigatePopToStateInternal(stateId, inclusive);
    }

    public void NavigatePopStates(Guid[] workflowStateIds)
    {
      NavigatePopStatesInternal(workflowStateIds);
    }

    public void NavigatePopToStateAsync(Guid stateId, bool inclusive)
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(() => NavigatePopToStateInternal(stateId, inclusive));
    }

    public bool NavigatePopModel(Guid modelId)
    {
      EnterWriteLock("NavigatePopModel");
      try
      {
        try
        {
          bool removed = false;
          bool workflowStatePopped = false;
          while (IsModelContainedInNavigationStack(modelId))
          {
            removed = true;
            if (!DoPopNavigationContext(1, out workflowStatePopped))
              break;
          }
          if (removed)
          {
            UpdateScreen_NeedsLock(false, workflowStatePopped);
            WorkflowManagerMessaging.SendNavigationCompleteMessage();
            return true;
          }
          else
            return false;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("WorkflowManager.NavigatePopModel: Error in workflow model or screen", e);
          NavigatePopInternal(1);
          return false;
        }
      }
      finally
      {
        ExitWriteLock();
      }
    }

    public void ShowDefaultScreen()
    {
      UpdateScreen_NeedsLock(false, false);
    }

    public void ShowDefaultScreenAsync()
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(ShowDefaultScreen);
    }

    public void StartBatchUpdate()
    {
      StartBatchUpdateInternal();
    }

    public void StartBatchUpdateAsync()
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(StartBatchUpdateInternal);
    }

    public void EndBatchUpdate()
    {
      EndBatchUpdateInternal();
    }

    public void EndBatchUpdateAsync()
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(EndBatchUpdateInternal);
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
            ServiceRegistration.Get<ILogger>().Debug(
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
        return _navigationContextStack.Any(context => context.WorkflowState.StateId == workflowStateId);
      }
      finally
      {
        ExitReadLock();
      }
    }

    public bool IsAnyStateContainedInNavigationStack(Guid[] workflowStateIds)
    {
      EnterReadLock("IsAnyStateContainedInNavigationStack");
      try
      {
        return _navigationContextStack.Any(context => workflowStateIds.Contains(context.WorkflowState.StateId));
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
        foreach (Guid modelId in new List<Guid>(_modelCache.Keys))
          if (!IsModelContainedInNavigationStack(modelId))
            FreeModel_NoLock(modelId);
      }
      finally
      {
        ExitWriteLock();
      }
    }

    #endregion
  }
}
