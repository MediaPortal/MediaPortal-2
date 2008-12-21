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

namespace MediaPortal.Presentation.Workflow
{
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
    protected Guid? _workflowModelId;
    protected ICollection<Guid> _additionalModels;
    protected bool _inheritMenu;
    protected bool _inheritContextMenu;

    #endregion

    public WorkflowState(Guid stateId, string name, string mainScreen, bool inheritMenu, bool inheritContextMenu,
        Guid? workflowModelId, ICollection<Guid> additionalModels)
    {
      _stateId = stateId;
      _name = name;
      _mainScreen = mainScreen;
      _inheritMenu = inheritMenu;
      _inheritContextMenu = inheritContextMenu;
      _workflowModelId = workflowModelId;
      _additionalModels = additionalModels;
    }

    /// <summary>
    /// Returns the id of this workflow state.
    /// </summary>
    public Guid StateId
    {
      get { return _stateId; }
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
    /// Returns the information if this state inherits the context menu of the previous workflow state.
    /// </summary>
    public bool InheritContextMenu
    {
      get { return _inheritContextMenu; }
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
    /// Returns a collection of ids of additional models providing information for screens of this
    /// workflow state.
    /// Screens may only access models which were registered at their workflow state(s), either as
    /// workflow model or as one of those additional models.
    /// </summary>
    public ICollection<Guid> AdditionalModels
    {
      get { return _additionalModels; }
    }
  }
}
