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

using System;
using System.Collections.Generic;
using MediaPortal.Common.Localization;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.UI.Presentation.Workflow
{
  public delegate void WorkflowActionStateChangeDelegate(WorkflowAction action);

  /// <summary>
  /// Stores the data for an action which can be triggered when a specified state is given.
  /// Typically, a workflow state action will provide the data for a menu item at the GUI.
  /// Sub classes will implement the abstract properties and methods of this class.
  /// </summary>
  public abstract class WorkflowAction: IUserRestriction
  {
    #region Protected fields

    protected string _displayCategory = null;
    protected string _sortOrder = null;
    protected Guid _actionId;
    protected string _name;
    protected string _group;
    protected ICollection<Guid> _sourceStateIds;
    protected IResourceString _displayTitle;
    protected IResourceString _helpText;

    #endregion

    protected WorkflowAction(Guid actionId, string name, IEnumerable<Guid> sourceStateIds, IResourceString displayTitle, IResourceString helpText)
      :this(actionId, name, sourceStateIds, displayTitle)
    {
      _helpText = helpText;
    }

    protected WorkflowAction(Guid actionId, string name, IEnumerable<Guid> sourceStateIds, IResourceString displayTitle)
    {
      _actionId = actionId;
      _name = name;
      _sourceStateIds = sourceStateIds == null ? null : new List<Guid>(sourceStateIds);
      _displayTitle = displayTitle;
    }

    /// <summary>
    /// Will be called from subclasses when the <see cref="IsEnabled"/> or <see cref="IsVisible"/> states have
    /// changed.
    /// </summary>
    protected void FireStateChanged()
    {
      if (StateChanged != null)
        StateChanged(this);
    }

    /// <summary>
    /// Returns the id of this action.
    /// </summary>
    public Guid ActionId
    {
      get { return _actionId; }
    }

    /// <summary>
    /// Returns a human-readable name for this action. This property is only a
    /// hint for developers and designers to identify the action.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Gets or sets the action's display category, which determines the place where the action will be displayed in the menu.
    /// The category can be an arbitrary string; it is proposed to use a string of the structure: <c>"a-CategoryName"</c>,
    /// where the starting letter will make the categories placed in the right order, while the category name describes
    /// the category for the implementor.
    /// </summary>
    public string DisplayCategory
    {
      get { return _displayCategory; }
      set { _displayCategory = value; }
    }

    /// <summary>
    /// Gets or sets the action's sort order inside its display category, which determines the place where the action will be
    /// displayed in the menu.
    /// The sort order can be an arbitrary string; it is proposed to simply use a letter: <c>"a"</c>,
    /// which simply describes the place to pose the action.
    /// </summary>
    public string SortOrder
    {
      get { return _sortOrder; }
      set { _sortOrder = value; }
    }

    /// <summary>
    /// Returns the ids of the all workflow states where this action is available. When the collection is empty,
    /// this action is not valid in any source state. If this property is <c>null</c>, this action is valid in all source states.
    /// </summary>
    public ICollection<Guid> SourceStateIds
    {
      get { return _sourceStateIds; }
    }

    /// <summary>
    /// Returns the localized string displayed at the GUI in the menu for this action.
    /// </summary>
    public virtual IResourceString DisplayTitle
    {
      get { return _displayTitle; }
    }

    /// <summary>
    /// Returns the localized help string for this action. It can be used to give an explanation for the current action.
    /// </summary>
    public virtual IResourceString HelpText
    {
      get { return _helpText; }
    }

    /// <summary>
    /// This event will be triggered when the <see cref="IsVisible"/> or <see cref="IsEnabled"/> states of this
    /// action have changed.
    /// </summary>
    public event WorkflowActionStateChangeDelegate StateChanged;

    /// <summary>
    /// Returns the information if this action should be displayed to the user in the given
    /// navigation <paramref name="context"/>.
    /// </summary>
    public abstract bool IsVisible(NavigationContext context);

    /// <summary>
    /// Returns the information if this action is currently able to be executed in the given
    /// navigation <paramref name="context"/>.
    /// </summary>
    public abstract bool IsEnabled(NavigationContext context);

    /// <summary>
    /// Executes this action. This method will be overridden in subclasses.
    /// </summary>
    public abstract void Execute();

    /// <summary>
    /// Can be overridden in sub classes to track a usage counter. If the number of <see cref="AddRef"/> is the same as
    /// the number of <see cref="RemoveRef"/> calls, the action can be unbound from the system.
    /// </summary>
    public virtual void AddRef() { }

    /// <summary>
    /// Can be overridden in sub classes to track a usage counter. See <see cref="AddRef"/>.
    /// </summary>
    public virtual void RemoveRef() { }

    /// <summary>
    /// Action group definition. This can be used to filter them out in specific workflow states.
    /// </summary>
    public string Group { get; set; }

    #region IUserRestriction members

    public string RestrictionGroup { get; set; }

    #endregion

    public override string ToString()
    {
      return _name + ": " + _actionId;
    }
  }
}
