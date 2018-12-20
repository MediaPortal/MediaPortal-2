#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements;

namespace MediaPortal.UI.SkinEngine.SpecialElements.Behaviors
{
  /// <summary>
  /// Attached behavior for an <see cref="ItemsControl"/> that can handle the currently selected
  /// <see cref="Presentation.DataObjects.ListItem"/>. 
  /// </summary>
  public class SelectedItemBehavior
  {
    #region Consts

    public const string FOCUS_SELECTED_ITEM_ATTACHED_PROPERTY = "SelectedItemBehavior.FocusSelectedItem";
    public const string FOCUS_SELECTED_ITEM_ACTION_ATTACHED_PROPERTY = "SelectedItemBehavior.FocusSelectedItemAction";

    #endregion

    private static void OnFocusCurrentItemChanged(DependencyObject targetObject, bool focusCurrentItem)
    {
      if (focusCurrentItem)
      {
        if (GetFocusSelectedItemAction(targetObject) != null)
          return;
        FocusSelectedItemAction action = new FocusSelectedItemAction();
        SetFocusSelectedItemAction(targetObject, action);
        action.AttachToObject(targetObject as ItemsControl);
      }
      else
      {
        FocusSelectedItemAction action = GetFocusSelectedItemAction(targetObject);
        if (action != null)
        {
          action.DetachFromObject();
          RemoveFocusSelectedItemAction(targetObject);
        }
      }
    }

    /// <summary>
    /// Returns the attached property instance for the <c>FocusSelectedItemBehavior.FocusSelectedItem</c> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the <c>FocusSelectedItemBehavior.FocusSelectedItem</c> property is set to <c>true</c> for an <see cref="ItemsControl"/>, that control
    /// will set the initial focus on the element bound to the currently selected <see cref="Presentation.DataObjects.ListItem"/>.
    /// </para>
    /// <para>
    /// This behavior is needed because setting the focus using the selected item's template won't work if the templated
    /// element hasn't been created yet, which might be the case when using virtualization.
    /// Instead, this behavior finds the index of the selected item in the <see cref="ItemsControl.ItemsSource"/>
    /// and ensures that the element at that index is visible before setting the focus.
    /// </para>
    /// <para>
    /// The usage is like this:
    /// <example>
    /// <code>
    /// &lt;ListView
    ///     xmlns:mp_special_behavior="clr-namespace:MediaPortal.UI.SkinEngine.SpecialElements.Behaviors;assembly=SkinEngine"
    ///     ...
    ///     mp_special_behavior:SelectedItemBehavior.FocusSelectedItem="True"&gt;
    ///   ...
    /// &lt;/ListView&gt;
    /// </code>
    /// </example>
    /// </para>
    /// </remarks>
    /// <param name="targetObject">The object whose attached property should be returned.</param>
    /// <returns>Attached <c>FocusSelectedItem</c> property.</returns>
    public static AbstractProperty GetFocusSelectedItemAttachedProperty(DependencyObject targetObject)
    {
      AbstractProperty result = targetObject.GetAttachedProperty(FOCUS_SELECTED_ITEM_ATTACHED_PROPERTY);
      if (result != null)
        return result;
      result = targetObject.GetOrCreateAttachedProperty(FOCUS_SELECTED_ITEM_ATTACHED_PROPERTY, false);
      result.Attach((prop, oldVal) => OnFocusCurrentItemChanged(targetObject, (bool)prop.GetValue()));
      return result;
    }

    protected static FocusSelectedItemAction GetFocusSelectedItemAction(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<FocusSelectedItemAction>(FOCUS_SELECTED_ITEM_ACTION_ATTACHED_PROPERTY, null);
    }

    protected static void SetFocusSelectedItemAction(DependencyObject targetObject, FocusSelectedItemAction value)
    {
      targetObject.SetAttachedPropertyValue(FOCUS_SELECTED_ITEM_ACTION_ATTACHED_PROPERTY, value);
    }

    protected static void RemoveFocusSelectedItemAction(DependencyObject targetObject)
    {
      targetObject.RemoveAttachedProperty(FOCUS_SELECTED_ITEM_ACTION_ATTACHED_PROPERTY);
    }
  }
}
