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

namespace MediaPortal.UI.Presentation.Workflow
{
  public enum WorkflowType
  {
    Workflow,
    Dialog,
  }

  /// <summary>
  /// Represents a state in the MediaPortal workflow network. The workflow state determines the
  /// attached workflow model, the GUI screen to be shown and the available navigation menu and
  /// context menu transitions.
  /// </summary>
  public class WorkflowState
  {
    #region Protected fields

    protected Guid _stateId;
    protected string _name;
    protected string _mainScreen;
    protected bool _isTransient;
    protected bool _inheritMenu;
    protected Guid? _workflowModelId;
    protected WorkflowType _type;

    #endregion

    /// <summary>
    /// Creates a new workflow state instance.
    /// </summary>
    /// <param name="stateId">The (unique) id of the new state.</param>
    /// <param name="name">A human-readable name for the new state.</param>
    /// <param name="mainScreen">For states of type <see cref="Workflow.WorkflowType.Workflow"/>, this is the name of the
    /// automatically loaded starting screen of this workflow state. For states of type <see cref="Workflow.WorkflowType.Dialog"/>,
    /// this is the name of the dialog to be shown.</param>
    /// <param name="inheritMenu">Whether the menu items should be inherited from the last workflow state.</param>
    /// <param name="isTransient">Whether the new state is a transient state. See the docs of the
    /// <see cref="IsTransient"/> property.</param>
    /// <param name="workflowModelId">The id of the workflow model which attends the workflow state.</param>
    /// <param name="type">The type of the new workflow state.</param>
    public WorkflowState(Guid stateId, string name, string mainScreen, bool inheritMenu, bool isTransient, Guid? workflowModelId, WorkflowType type)
    {
      _stateId = stateId;
      _name = name;
      _mainScreen = mainScreen;
      _inheritMenu = inheritMenu;
      _isTransient = isTransient;
      _workflowModelId = workflowModelId;
      _type = type;
    }

    /// <summary>
    /// Returns the id of this workflow state.
    /// </summary>
    public Guid StateId
    {
      get { return _stateId; }
    }

    /// <summary>
    /// Returns the type of the workflow this state belongs to.
    /// </summary>
    public WorkflowType WorkflowType
    {
      get { return _type; }
    }

    /// <summary>
    /// Returns a human-readable name for this workflow state. This property is only a
    /// hint for developers and designers to identify the state.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Returns the GUI screen which will be shown when this state is reached.
    /// </summary>
    public string MainScreen
    {
      get { return _mainScreen; }
    }

    /// <summary>
    /// Returns the information if this state inherits the menu of the previous workflow state.
    /// </summary>
    public bool InheritMenu
    {
      get { return _inheritMenu; }
    }

    /// <summary>
    /// Returns the id of the UI model which is attached to this state. The workflow model is responsible
    /// to trigger special actions and to deliver additional data which belongs to this state.
    /// </summary>
    public Guid? WorkflowModelId
    {
      get { return _workflowModelId; }
    }

    /// <summary>
    /// Returns the information if this workflow state is a transient state. Transient states are not
    /// defined persistently in a workflow resource file but are added on demand by some workflow model.
    /// </summary>
    /// <remarks>
    /// Transient states automatically inherit the workflow model from the last non-transient state on the workflow
    /// navigation stack.
    /// </remarks>
    public bool IsTransient
    {
      get { return _isTransient; }
    }

    /// <summary>
    /// Creates a new transient state. In transient states, some variables like the models are automatically
    /// inherited from the parent state.
    /// </summary>
    /// <param name="name">The human-readable name of the new state.</param>
    /// <param name="mainScreen">The main screen to be shown in the new state.</param>
    /// <param name="inheritMenu">If set to <c>true</c>, the menu items of the parent state will be
    /// inherited.</param>
    /// <param name="workflowType">The workflow type of the new transient state.</param>
    /// <returns>New transient workflow state.</returns>
    public static WorkflowState CreateTransientState(string name, string mainScreen, bool inheritMenu, WorkflowType workflowType)
    {
      return new WorkflowState(Guid.NewGuid(), name, mainScreen, inheritMenu, true, null, workflowType);
    }
  }
}
