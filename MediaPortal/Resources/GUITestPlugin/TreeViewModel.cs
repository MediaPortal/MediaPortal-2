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
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Test.GUITest
{
  /// <summary>
  /// Model which holds the GUI state for the TreeView state.
  /// </summary>
  public class TreeViewModel : IWorkflowModel
  {
    public const string MODEL_ID_STR = "A6C3F942-105C-48cd-AEFF-059DA79773A9";

    #region Protected fields

    protected ItemsList _tree = null;

    #endregion

    protected void CreateChildren(TreeItem item, int level, int maxLevel)
    {
      if (level > maxLevel)
        return;
      TreeItem childItem = new TreeItem("Name", "First item, level " + level);
      CreateChildren(childItem, level + 1, maxLevel);
      item.SubItems.Add(childItem);
      childItem = new TreeItem("Name", "Second item, level " + level);
      CreateChildren(childItem, level + 1, maxLevel);
      item.SubItems.Add(childItem);
      childItem = new TreeItem("Name", "Third item, level " + level);
      CreateChildren(childItem, level + 1, maxLevel);
      item.SubItems.Add(childItem);
    }

    protected void InitializeTree()
    {
      _tree = new ItemsList();
      TreeItem item = new TreeItem("Name", "First item");
      CreateChildren(item, 2, 3);
      _tree.Add(item);
      item = new TreeItem("Name", "Second item");
      CreateChildren(item, 2, 4);
      _tree.Add(item);
      item = new TreeItem("Name", "Third item");
      CreateChildren(item, 2, 5);
      _tree.Add(item);
    }

    protected void DisposeTree()
    {
      _tree = null;
    }

    #region Public properties

    public ItemsList Tree
    {
      get { return _tree; }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      InitializeTree();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      DisposeTree();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // We could initialize some data here when changing the media navigation state
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
