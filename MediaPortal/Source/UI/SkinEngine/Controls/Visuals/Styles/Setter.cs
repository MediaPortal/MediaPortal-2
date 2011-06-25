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

using System;
using MediaPortal.Core.General;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Styles
{
  public class Setter : SetterBase
  {
    #region Protected fields

    protected UIElement _element = null;
    protected AbstractProperty _valueProperty;

    #endregion

    #region Ctor

    public Setter()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _valueProperty = new SProperty(typeof(object), null);
    }

    void Attach()
    {
      _valueProperty.Attach(OnValueChanged);
    }

    void Detach()
    {
      _valueProperty.Detach(OnValueChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Setter s = (Setter) source;
      Value = copyManager.GetCopy(s.Value);
      Attach();
    }

    public override void Dispose()
    {
      Registration.CleanupAndDisposeResourceIfOwner(Value, this);
      DependencyObject targetObject;
      IDataDescriptor dd;
      if (_element != null && FindPropertyDescriptor(_element, out dd, out targetObject))
        Registration.TryCleanupAndDispose(GetOriginalValue(targetObject));
      base.Dispose();
    }

    #endregion

    void OnValueChanged(AbstractProperty prop, object oldVal)
    {
      Registration.SetOwner(oldVal, null);
      Registration.SetOwner(Value, this);
    }

    #region Properties

    public AbstractProperty ValueProperty
    {
      get { return _valueProperty; }
    }

    /// <summary>
    /// Gets or sets the value to be set on our target. This value will be
    /// later converted to the right target type and stored in the attached property "OriginalValue"
    /// (see <see cref="SetOriginalValue"/>).
    /// </summary>
    public object Value
    {
      get { return _valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    #endregion

    #region Protected methods

    protected bool FindPropertyDescriptor(UIElement element,
        out IDataDescriptor propertyDescriptor, out DependencyObject targetObject)
    {
      propertyDescriptor = null;
      if (string.IsNullOrEmpty(TargetName))
        targetObject = element;
      else
      {
        // Search the element in "normal" namescope and in the dynamic structure via the FindElement method
        // I think this is more than WPF does. It makes it possible to find elements instantiated
        // by a template, for example.
        targetObject = element.FindElementInNamescope(TargetName) ??
            element.FindElement(new NameMatcher(TargetName));
        if (targetObject == null)
          return false;
      }
      int index = Property.IndexOf('.');
      if (index != -1)
      {
        string propertyProvider = Property.Substring(0, index);
        string propertyName = Property.Substring(index + 1);
        MpfAttachedPropertyDataDescriptor result;
        if (!MpfAttachedPropertyDataDescriptor.CreateAttachedPropertyDataDescriptor(
            element, propertyProvider, propertyName, out result))
          throw new ArgumentException(
            string.Format("Attached property '{0}' cannot be set on element '{1}'", Property, targetObject));
        propertyDescriptor = result;
        return true;
      }
      else
      {
        string propertyName = Property;
        IDataDescriptor result;
        if (!ReflectionHelper.FindMemberDescriptor(targetObject, propertyName, out result))
          throw new ArgumentException(
              string.Format("Property '{0}' cannot be set on element '{1}'", Property, targetObject));
        propertyDescriptor = result;
        return true;
      }
    }

    protected string GetAttachedPropertyName_OriginalValue()
    {
      return "Setter." + Property + ".OriginalValue";
    }

    protected string GetAttachedPropertyName_CurrentSetter()
    {
      return "Setter." + Property + ".CurrentSetter";
    }

    protected object GetOriginalValue(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<object>(GetAttachedPropertyName_OriginalValue(), null);
    }

    protected void SetOriginalValue(DependencyObject targetObject, object value)
    {
      targetObject.SetAttachedPropertyValue<object>(GetAttachedPropertyName_OriginalValue(), value);
      targetObject.SetAttachedPropertyValue<object>(GetAttachedPropertyName_CurrentSetter(), this);
    }

    protected bool WasApplied(DependencyObject targetObject, bool checkSelf)
    {
      object currentSetter = targetObject.GetAttachedPropertyValue<object>(GetAttachedPropertyName_CurrentSetter(), null);
      return checkSelf ? ReferenceEquals(currentSetter, this) : (currentSetter != null);
    }

    protected void ClearSetterData(DependencyObject targetObject)
    {
      targetObject.RemoveAttachedProperty(GetAttachedPropertyName_OriginalValue());
      targetObject.RemoveAttachedProperty(GetAttachedPropertyName_CurrentSetter());
    }

    #endregion

    #region Public methods

    public override void Set(UIElement element)
    {
      _element = element;
      IDataDescriptor dd;
      DependencyObject targetObject;
      if (!FindPropertyDescriptor(element, out dd, out targetObject))
        return;
      if (WasApplied(targetObject, false))
        // If any setter is currently setting our property, we don't want to interfere
        return;
      object obj;

      // The next lines are necessary because the render thread is setting our values.
      // If there's still a value pending to be set by the render thread, we would get an old, obsolete value if
      // we just copied dd.Value to _originalValue.
      element.GetPendingOrCurrentValue(dd, out obj);
      SetOriginalValue(targetObject, obj);

      if (TypeConverter.Convert(Value, dd.DataType, out obj))
        if (ReferenceEquals(Value, obj))
          element.SetValueInRenderThread(dd, MpfCopyManager.DeepCopyCutLP(obj));
        else
          // Avoid creating a copy twice
          element.SetValueInRenderThread(dd, obj);
      else
        // TODO: Log output
        // Value is not compatible: We cannot execute
        return;
    }

    public override void Restore(UIElement element)
    {
      IDataDescriptor dd;
      DependencyObject targetObject;
      if (!FindPropertyDescriptor(element, out dd, out targetObject))
        return;
      if (WasApplied(targetObject, true))
      {
        element.SetValueInRenderThread(dd, GetOriginalValue(targetObject));
        ClearSetterData(targetObject);
        _element = null;
      }
    }

    #endregion
  }
}
