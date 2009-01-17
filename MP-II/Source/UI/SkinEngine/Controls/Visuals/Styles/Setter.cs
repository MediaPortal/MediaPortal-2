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
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.MpfElements;
using MediaPortal.SkinEngine.Xaml;

namespace MediaPortal.SkinEngine.Controls.Visuals.Styles
{
  public class Setter : SetterBase
  {
    #region Protected fields

    protected object _value;

    #endregion

    #region Ctor

    public Setter() { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Setter s = (Setter) source;
      Value = copyManager.GetCopy(s.Value);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the value to be set on our target. This value will be
    /// later converted to the right target type and stored in <see cref="SetterValue"/>.
    /// </summary>
    public object Value
    {
      get { return _value; }
      set { _value = value; }
    }

    #endregion

    #region Protected methods

    protected bool FindPropertyDescriptor(UIElement element,
        out IDataDescriptor propertyDescriptor, out DependencyObject targetObject)
    {
      targetObject = null;
      propertyDescriptor = null;
      if (!string.IsNullOrEmpty(TargetName))
      {
        // Search the element in "normal" namescope and in the dynamic structure via the FindElement method
        // I think this is more than WPF does. It makes it possible to find elements instantiated
        // by a template, for example.
        targetObject = element.FindElementInNamescope(TargetName) ??
            element.FindElement(new NameFinder(TargetName));
        if (targetObject == null)
          return false;
      }
      else
        targetObject = element;
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

    protected bool WasApplied(DependencyObject targetObject)
    {
      return ReferenceEquals(targetObject.GetAttachedPropertyValue<object>(
          GetAttachedPropertyName_CurrentSetter(), null), this);
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
      IDataDescriptor dd;
      DependencyObject targetObject;
      if (!FindPropertyDescriptor(element, out dd, out targetObject))
        return;
      object obj;
      if (!WasApplied(targetObject))
      { // We have to the original property value the first time for this target object

        // The next lines are necessary because the render thread is setting our values.
        // If the render thread wasn't able to set the value yet, we would get the old, unchanged and
        // thus wrong value dd.Value for _originalValue.
        if (!element.TryGetPendingValue(dd, out obj))
          obj = dd.Value;
        SetOriginalValue(targetObject, obj);
      }
      if (TypeConverter.Convert(Value, dd.DataType, out obj))
        element.SetValueInRenderThread(dd, MpfCopyManager.DeepCopyCutLP(obj));
      else
        // Value is not compatible: We cannot execute
        return;
    }

    /// <summary>
    /// Restore the target element's original value which was set to the setter's value before.
    /// </summary>
    /// <param name="element">The UI element which is used as starting point for this setter
    /// to earch the target element.</param>
    public void Restore(UIElement element)
    {
      IDataDescriptor dd;
      DependencyObject targetObject;
      if (!FindPropertyDescriptor(element, out dd, out targetObject))
        return;
      if (WasApplied(targetObject))
      {
        element.SetValueInRenderThread(dd, GetOriginalValue(targetObject));
        ClearSetterData(targetObject);
      }
    }

    #endregion
  }
}
