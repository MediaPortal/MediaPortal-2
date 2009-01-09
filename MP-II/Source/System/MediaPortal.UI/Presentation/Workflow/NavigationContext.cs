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
using MediaPortal.Presentation.Models;

namespace MediaPortal.Presentation.Workflow
{
  /// <summary>
  /// Holds the navigation context data for one workflow navigation step.
  /// </summary>
  /// <remarks>
  /// A navigation context is one entry in the navigation path in the hierarchical navigation
  /// structure presented by the menu. The current state of the application is composed by
  /// all navigation contexts in the navigation path taken so far.
  /// </remarks>
  public class NavigationContext
  {
    #region Protected fields

    protected IDictionary<string, object> _contextVariables = new Dictionary<string, object>();
    protected NavigationContext _predecessor;
    protected WorkflowState _workflowState;
    protected IDictionary<Guid, WorkflowStateAction> _menuActions = new Dictionary<Guid, WorkflowStateAction>();
    protected IDictionary<Guid, WorkflowStateAction> _contextMenuActions = new Dictionary<Guid, WorkflowStateAction>();
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
      get { return _contextVariables.ContainsKey(key) ? _contextVariables[key] : null; }
      set { _contextVariables[key] = value; }
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
      _contextVariables[key] = value;
    }

    /// <summary>
    /// Returns a collection of all menu actions available from this navigation context.
    /// </summary>
    public IDictionary<Guid, WorkflowStateAction> MenuActions
    {
      get { return _menuActions; }
    }

    /// <summary>
    /// Returns a collection of all context menu actions available from this navigation context.
    /// </summary>
    public IDictionary<Guid, WorkflowStateAction> ContextMenuActions
    {
      get { return _contextMenuActions; }
    }

    /// <summary>
    /// Returns the workflow model id used in this navigation context.
    /// </summary>
    public Guid? WorkflowModelId
    {
      get { return _workflowModelId; }
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
    /// Returns the predecessor navigation context, if it exists. For the root navigation context,
    /// <c>null</c> will be returned.
    /// </summary>
    public NavigationContext Predecessor
    {
      get { return _predecessor; }
    }

    #endregion
  }
}
