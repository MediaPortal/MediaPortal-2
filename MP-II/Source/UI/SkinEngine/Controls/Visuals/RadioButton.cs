#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class RadioButton : Button
  {
    #region Protected fields

    protected const string GROUPCONTEXT_ATTACHED_PROPERTY = "RadioButton.GroupContext";

    protected ICollection<RadioButton> _radioButtonGroup = null;
    protected AbstractProperty _isCheckedProperty;
    protected AbstractProperty _groupNameProperty;

    #endregion

    #region Ctor

    public RadioButton()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _groupNameProperty = new SProperty(typeof(string), null);
      _isCheckedProperty = new SProperty(typeof(bool), false);
    }

    void Attach()
    {
      _isCheckedProperty.Attach(OnCheckChanged);
      _groupNameProperty.Attach(OnGroupNameChanged);
      _isPressedProperty.Attach(OnButtonPressedChanged);
    }

    void Detach()
    {
      _isCheckedProperty.Detach(OnCheckChanged);
      _groupNameProperty.Detach(OnGroupNameChanged);
      _isPressedProperty.Detach(OnButtonPressedChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      RadioButton rb = (RadioButton) source;
      IsChecked = rb.IsChecked;
      GroupName = rb.GroupName;
      Attach();
    }

    #endregion

    #region Private/protected methods

    void OnButtonPressedChanged(AbstractProperty property, object oldValue)
    {
      // Copy the "IsPressed" status to "IsChecked". This will automatically clear the
      // "IsChecked" status at other radio buttons.
      // This event will be triggered before the button executes its command.
      if (IsPressed)
        IsChecked = IsPressed;
    }

    void OnGroupNameChanged(AbstractProperty property, object oldValue)
    {
      InitializeGroup();
    }

    void OnCheckChanged(AbstractProperty property, object oldValue)
    {
      if (IsChecked)
      {
        if (_radioButtonGroup == null)
          InitializeGroup();
        if (_radioButtonGroup != null)
          foreach (RadioButton radioButton in _radioButtonGroup)
            if (!ReferenceEquals(this, radioButton))
              radioButton.IsChecked = false;
      }
    }

    /// <summary>
    /// Searches the namescope where our radio button group should be registered.
    /// This is the first parent which has the attached property "RadioButton.GroupContext" set,
    /// or the top-level visual parent, if the property is not set at any parent.
    /// </summary>
    protected INameScope FindGroupNamescope()
    {
      Visual current = this;
      while (current.VisualParent != null)
      {
        ICollection<string> groups = new List<string>(GetGroupContext(current).Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries));
        if (groups.Contains(GroupName))
          break;
        current = current.VisualParent;
      }
      return current.FindNameScope();
    }

    /// <summary>
    /// Searches the radio button group and adds this radio button to the specified
    /// group.
    /// </summary>
    protected void InitializeGroup()
    {
      if (_radioButtonGroup != null)
        _radioButtonGroup.Remove(this);
      _radioButtonGroup = null;
      if (!string.IsNullOrEmpty(GroupName))
      {
        INameScope ns = FindGroupNamescope();
        if (ns == null)
          return;
        _radioButtonGroup = ns.FindName(GroupName) as ICollection<RadioButton>;
        if (_radioButtonGroup == null)
        {
          _radioButtonGroup = new List<RadioButton>();
          ns.RegisterName(GroupName, _radioButtonGroup);
        }
        _radioButtonGroup.Add(this);
      }
    }

    #endregion

    #region Public properties

    public AbstractProperty GroupNameProperty
    {
      get { return _groupNameProperty; }
    }

    public string GroupName
    {
      get { return (string) _groupNameProperty.GetValue(); }
      set { _groupNameProperty.SetValue(value); }
    }

    public AbstractProperty IsCheckedProperty
    {
      get { return _isCheckedProperty; }
    }

    public bool IsChecked
    {
      get { return (bool) _isCheckedProperty.GetValue(); }
      set { _isCheckedProperty.SetValue(value); }
    }

    #endregion

    #region Base overrides

    public override void FireEvent(string eventName)
    {
      if (eventName == LOADED_EVENT)
        InitializeGroup();
      base.FireEvent(eventName);
    }

    #endregion

    #region Attached properties

    /// <summary>
    /// Getter method for the attached property <c>GroupContext</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>GroupContext</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static string GetGroupContext(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue(GROUPCONTEXT_ATTACHED_PROPERTY, string.Empty);
    }

    /// <summary>
    /// Setter method for the attached property <c>GroupContext</c>.
    /// </summary>
    /// <remarks>
    /// The <c>GroupContext</c> should be set at the nearest parent of this radio button,
    /// which contains all other radio buttons of the same radio button group.
    /// This limits the scope of the search for the radio button group to this element's
    /// name scope.
    /// If this property isn't set at any of the visual parents up to the root,
    /// the root visual will be used as group context.
    /// To specify the group context on a visual <c>V</c>, use this syntax:
    /// <c>&lt;V RadioButtoon.GroupContext="GroupName1,GroupName2,...,GroupNameN"&gt;</c>
    /// All the specifed groups will then be created in <c>V</c>'s name scope and all
    /// radio buttons under <c>V</c>, which use one of the specified group names, will then
    /// be contained in those groups. Radio buttons not located in <c>V</c>'s namescope
    /// (or child namescopes) will use other group instances, even if they are using the
    /// same <see cref="GroupName"/>.
    /// </remarks>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>GroupContext</c> property on the
    /// <paramref name="targetObject"/> to be set.</returns>
    public static void SetGroupContext(DependencyObject targetObject, string value)
    {
      targetObject.SetAttachedPropertyValue<string>(GROUPCONTEXT_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>GroupContext</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>GroupContext</c> property.</returns>
    public static AbstractProperty GetGroupContextAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty<string>(GROUPCONTEXT_ATTACHED_PROPERTY, string.Empty);
    }

    #endregion
  }
}
