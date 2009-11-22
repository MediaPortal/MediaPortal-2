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
using MediaPortal.Core;
using MediaPortal.Core.Localization;

namespace MediaPortal.UI.Presentation.Workflow
{
  /// <summary>
  /// When invoked, this action pops a given count of workflow states from the workflow navigation stack.
  /// </summary>
  public class PopNavigationTransition : WorkflowAction
  {
    #region Protected fields

    protected int _numPop;

    #endregion

    public PopNavigationTransition(Guid actionId, string name, Guid? sourceStateId, int numPop,
        IResourceString displayTitle) :
        base(actionId, name, sourceStateId, displayTitle)
    {
      _numPop = numPop;
    }

    /// <summary>
    /// Returns the number of workflow navigation contexts to remove from the navigation
    /// context stack when this action is taken.
    /// </summary>
    public int NumPop
    {
      get { return _numPop; }
    }

    public override bool IsVisible
    {
      get { return true; }
    }

    public override bool IsEnabled
    {
      get { return true; }
    }

    /// <summary>
    /// Removes <see cref="NumPop"/> levels from the workflow navigation context stack.
    /// </summary>
    public override void Execute()
    {
      ServiceScope.Get<IWorkflowManager>().NavigatePop(NumPop);
    }
  }
}
