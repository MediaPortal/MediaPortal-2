#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.UI.Presentation.Models;

namespace MediaPortal.UI.Presentation.Workflow
{
  /// <summary>
  /// Holds the navigation context data for one workflow navigation step.
  /// </summary>
  /// <remarks>
  /// A navigation context is one entry in the navigation path in the hierarchical navigation
  /// structure presented by the menu. The current state of the application is composed by
  /// all navigation contexts in the navigation path taken so far.
  /// </remarks>
  public class NavigationContext : IDisposable
  {
    #region Protected fields

    protected object _syncObj = new object();
    protected IDictionary<string, object> _contextVariables = new Dictionary<string, object>();
    protected NavigationContext _predecessor;
    protected WorkflowState _workflowState;
    protected IDictionary<Guid, WorkflowAction> _menuActions = new Dictionary<Guid, WorkflowAction>();
    protected Guid? _workflowModelId = null;
    protected IDictionary<Guid, object> _models = new Dictionary<Guid, object>();

    #endregion

    #region Ctor

    /// <summary>
    /// This constructor has to be called from the component managing the navigation context stack.
    /// </summary>
    public NavigationContext(WorkflowState workflowState, NavigationContext predecessor,
        IWorkflowModel workflowModel)
    {
      _workflowState = workflowState;
      _predecessor = predecessor;
      if (workflowModel != null)
      {
        _workflowModelId = workflowModel.ModelId;
        _models.Add(workflowModel.ModelId, workflowModel);
      }
    }

    public void Dispose()
    {
      UninitializeMenuActions();
    }

    #endregion

    #region Protected methods

    protected void InitializeMenuActions()
    {
      foreach (WorkflowAction action in _menuActions.Values)
        action.AddRef();
    }

    protected void UninitializeMenuActions()
    {
      foreach (WorkflowAction action in _menuActions.Values)
        action.RemoveRef();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Returns the workflow state of this navigation context.
    /// </summary>
    public WorkflowState WorkflowState
    {
      get { return _workflowState; }
    }

    /// <summary>
    /// Returns a (key; value) mapping of all context variables. Changing the returned dictionary will
    /// change the context variables.
    /// </summary>
    public IDictionary<string, object> ContextVariables
    {
      get { return _contextVariables; }
    }

    /// <summary>
    /// Returns the workflow model id used in this navigation context.
    /// </summary>
    public Guid? WorkflowModelId
    {
      get { return _workflowModelId; }
    }

    /// <summary>
    /// Returns the predecessor navigation context, if it exists. For the root navigation context,
    /// <c>null</c> will be returned.
    /// </summary>
    public NavigationContext Predecessor
    {
      get { return _predecessor; }
    }

    /// <summary>
    /// Returns a collection of all menu actions available from this navigation context and inherited from
    /// the predecessor navigation context.
    /// </summary>
    public IDictionary<Guid, WorkflowAction> MenuActions
    {
      get
      {
        if (_predecessor == null || !_workflowState.InheritMenu)
          return _menuActions;
        // Try to inherit menu actions from predecessor
        IDictionary<Guid, WorkflowAction> result =
            new Dictionary<Guid, WorkflowAction>(_predecessor.MenuActions);
        if (result.Count == 0)
          // Nothing to inherit
          return _menuActions;
        foreach (KeyValuePair<Guid, WorkflowAction> pair in _menuActions)
          // Don't use method CollectionUtils.AddAll for the copying process, because we can get duplicate keys here
          // (for example if an action is visible in both our predecessor and in this context). We simply use the action
          // which is registered in this instance.
          result[pair.Key] = pair.Value;
        return result;
      }
    }

    /// <summary>
    /// Sets the menu actions which belong to this navigation context. The collection of actions
    /// which will be bound to the system in this method and unbound when this navigation context is disposed.
    /// </summary>
    public void SetMenuActions(IEnumerable<WorkflowAction> actions)
    {
      UninitializeMenuActions();
      lock (_syncObj)
      {
        _menuActions.Clear();
        foreach (WorkflowAction action in actions)
          _menuActions[action.ActionId] = action;
      }
      InitializeMenuActions();
    }

    /// <summary>
    /// Returns the dictionary of all models which are used in this navigation context.
    /// This contains the workflow model as well as additional models for this state.
    /// </summary>
    public IDictionary<Guid, object> Models
    {
      get { return _models; }
    }

    /// <summary>
    /// Synchronization object for multithreaded access to this <see cref="NavigationContext"/>.
    /// </summary>
    public object SyncRoot
    {
      get { return _syncObj; }
    }

    /// <summary>
    /// Gets or sets context variables in the current navigation context.
    /// </summary>
    /// <remarks>
    /// The getter will retrieve the specified entry at the current context, the setter will set it at the
    /// current context.
    /// A search of context variables with additionally searching the predecessor contexts can be done
    /// by a call of <see cref="GetContextVariable"/> with appropriate parameters.
    /// </remarks>
    /// <param name="key">Key of the value to get or set.</param>
    public object this[string key]
    {
      get
      {
        lock (_syncObj)
          return _contextVariables.ContainsKey(key) ? _contextVariables[key] : null;
      }
      set
      {
        lock (_syncObj)
          _contextVariables[key] = value;
      }
    }

    /// <summary>
    /// Gets the context variable specified by <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to retrieve the context variable.</param>
    /// <param name="inheritFromPredecessor">If set to <c>true</c> and the current context doesn't contain
    /// the value specified by <paramref name="key"/>, the search will be continued at the predecessors
    /// until the value is found.</param>
    /// <returns>Specified context variable or <c>null</c>, if the context variable could not be found.</returns>
    public object GetContextVariable(string key, bool inheritFromPredecessor)
    {
      lock (_syncObj)
        return _contextVariables.ContainsKey(key) ? _contextVariables[key] :
          (inheritFromPredecessor && _predecessor != null ? _predecessor.GetContextVariable(key, true) : null);
    }

    /// <summary>
    /// Sets the context variable specified by <paramref name="key"/> to the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="key">The key of the context variable to set.</param>
    /// <param name="value">The value to set.</param>
    public void SetContextVariable(string key, object value)
    {
      lock (_syncObj)
        _contextVariables[key] = value;
    }

    /// <summary>
    /// Removes the context variable specified by <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the context variable to remove.</param>
    public void ResetContextVariable(string key)
    {
      lock (_syncObj)
        _contextVariables.Remove(key);
    }

    #endregion
  }
}
