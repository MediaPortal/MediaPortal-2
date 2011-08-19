#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.SpecialElements.Workflow
{
  /// <summary>
  /// Save and restore handler which can be bound to an <see cref="UIElement"/>.
  /// </summary>
  public class WorkflowSaveRestoreStateAction : IDeepCopyable
  {
    #region Protected fields

    protected string _contextVariable;
    protected UIElement _targetObject;

    #endregion

    public WorkflowSaveRestoreStateAction(string contextVariable)
    {
      _contextVariable = contextVariable;
    }

    public void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      WorkflowSaveRestoreStateAction w = (WorkflowSaveRestoreStateAction) source;
      _targetObject = copyManager.GetCopy(w._targetObject);
    }

    public void AttachToObject(UIElement targetObject)
    {
      _targetObject = targetObject;
      targetObject.EventOccured += OnUIEventOccured;
    }

    public void DetachFromObject()
    {
      if (_targetObject == null)
        return;
      _targetObject.EventOccured -= OnUIEventOccured;
      _targetObject = null;
    }

    private void OnUIEventOccured(string eventname)
    {
      if (eventname != Screen.SHOW_EVENT && eventname != Screen.CLOSE_EVENT)
        return;
      UIElement targetElement = _targetObject;
      if (targetElement == null)
        return;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      NavigationContext context = workflowManager.CurrentNavigationContext;
      if (eventname == Screen.SHOW_EVENT)
      {
        IDictionary<string, object> state = (IDictionary<string, object>) context.GetContextVariable(_contextVariable, false);
        if (state == null)
          return;
        targetElement.RestoreUIState(state, string.Empty);
      }
      else if (eventname == Screen.CLOSE_EVENT)
      {
        IDictionary<string, object> state = new Dictionary<string, object>(1000);
        targetElement.SaveUIState(state, string.Empty);
        context.SetContextVariable(_contextVariable, state);
      }
    }
  }
}