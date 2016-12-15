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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements;

namespace MediaPortal.UI.SkinEngine.SpecialElements.Workflow
{
  /// <summary>
  /// Provider class for attached properties which control cooperation with the MediaPortal 2 workflow manager.
  /// </summary>
  public class WorkflowContext
  {
    #region Consts

    public const string STATE_SLOT_ATTACHED_PROPERTY = "WorkflowContext.StateSlot";
    public const string SAVE_RESTORE_ACTION_ATTACHED_PROPERTY = "WorkflowContext.SaveRestoreAction";

    #endregion

    private static void OnStateSlotChanged(DependencyObject targetObject, NavigationContext context, string contextVariable)
    {
      if (string.IsNullOrEmpty(contextVariable))
      {
        WorkflowSaveRestoreStateAction action = GetSaveRestoreAction(targetObject);
        if (action != null)
        {
          action.DetachFromObject();
          RemoveSaveRestoreAction(targetObject);
        }
      }
      else
      {
        if (GetSaveRestoreAction(targetObject) != null)
          // Action already attached to object
          return;
        UIElement uiElement = targetObject as UIElement;
        WorkflowSaveRestoreStateAction action = new WorkflowSaveRestoreStateAction(context, contextVariable);
        SetSaveRestoreAction(targetObject, action);
        action.AttachToObject(uiElement);
      }
    }

    #region Attached properties

    /// <summary>
    /// Returns the attached property instance for the <c>WorkflowContext.StateSlot</c> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the <c>WorkflowContext.StateSlot</c> property is set to a context name for a UI element, that element and
    /// all its children will save their state in the current MediaPortal 2 workflow navigation context in the context variable of
    /// the given name when the current screen is quit and restore the state when the screen is reloaded for the same
    /// workflow navigation context.
    /// </para>
    /// <para>
    /// The usage is like this:
    /// <example>
    /// <code>
    /// &lt;ListView
    ///     xmlns:mp_special_workflow="clr-namespace:MediaPortal.UI.SkinEngine.SpecialElements.Workflow;assembly=SkinEngine"
    ///     ...
    ///     mp_special_workflow:WorkflowContext.StateSlot="MainMenu"&gt;
    ///   ...
    /// &lt;/ListView&gt;
    /// </code>
    /// </example>
    /// </para>
    /// </remarks>
    /// <param name="targetObject">The object whose attached property should be returned.</param>
    /// <returns>Attached <c>SaveState</c> property.</returns>
    public static AbstractProperty GetStateSlotAttachedProperty(DependencyObject targetObject)
    {
      AbstractProperty result = targetObject.GetAttachedProperty(STATE_SLOT_ATTACHED_PROPERTY);
      if (result != null)
        return result;
      result = targetObject.GetOrCreateAttachedProperty(STATE_SLOT_ATTACHED_PROPERTY, string.Empty);
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      // We save the workflow navigation context at the time when this attached property is requested because that is the time when
      // the navigation context is the right one for the current screen. At the time when the Screen.HIDE_EVENT is raised,
      // the navigation context has already moved to the next state.
      NavigationContext context = workflowManager.CurrentNavigationContext;
      result.Attach((prop, oldVal) => OnStateSlotChanged(targetObject, context, (string) prop.GetValue()));
      return result;
    }

    protected static WorkflowSaveRestoreStateAction GetSaveRestoreAction(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<WorkflowSaveRestoreStateAction>(SAVE_RESTORE_ACTION_ATTACHED_PROPERTY, null);
    }

    protected static void SetSaveRestoreAction(DependencyObject targetObject, WorkflowSaveRestoreStateAction value)
    {
      targetObject.SetAttachedPropertyValue(SAVE_RESTORE_ACTION_ATTACHED_PROPERTY, value);
    }

    protected static void RemoveSaveRestoreAction(DependencyObject targetObject)
    {
      targetObject.RemoveAttachedProperty(SAVE_RESTORE_ACTION_ATTACHED_PROPERTY);
    }

    #endregion
  }
}