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
    protected Object _originalValue = null;
    protected bool _isSet = false;
    protected object _setterValue;

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

    /// <summary>
    /// Gets or sets the information if this setter was already initialized, that
    /// means value or binding instance has been applied to the setter target property.
    /// </summary>
    public bool WasApplied
    {
      get { return _isSet; }
      set { _isSet = value; }
    }

    /// <summary>
    /// Gets or sets the value to be set which has already the right type.
    /// This value was converted from the <see cref="Value"/> property.
    /// </summary>
    public object SetterValue
    {
      get { return _setterValue; }
      set { _setterValue = value; }
    }

    #endregion

    protected IDataDescriptor GetPropertyDescriptor(UIElement element)
    {
      DependencyObject target = null;
      if (!string.IsNullOrEmpty(TargetName))
      {
        // Search the element in "normal" namescope and in the dynamic structure via the FindElement method
        // I think this is more than WPF does. It makes it possible to find elements instantiated
        // by a template, for example.
        target = element.FindElementInNamescope(TargetName) ??
            element.FindElement(new NameFinder(TargetName));
        if (target == null)
          return null;
      }
      if (target == null)
        target = element;
      int index = Property.IndexOf('.');
      if (index != -1)
      {
        string propertyProvider = Property.Substring(0, index);
        string propertyName = Property.Substring(index + 1);
        MpfAttachedPropertyDataDescriptor result;
        return MpfAttachedPropertyDataDescriptor.CreateAttachedPropertyDataDescriptor(
            element, propertyProvider, propertyName, out result) ? result : null;
      }
      else
      {
        string propertyName = Property;
        IDataDescriptor result;
        if (ReflectionHelper.FindMemberDescriptor(target, propertyName, out result))
          return result;
        throw new ArgumentException(
            string.Format("Property '{0}' cannot be set on element '{1}'", Property, target));
      }
    }

    public override void Set(UIElement element)
    {
      IDataDescriptor dd = GetPropertyDescriptor(element);
      if (dd == null)
        return;
      if (!WasApplied)
      { // We have to prepare our internal data the first time
        _originalValue = dd.Value;
        object obj;
        if (TypeConverter.Convert(Value, dd.DataType, out obj))
          SetterValue = obj;
        else
          // We cannot execute
          return;
        WasApplied = true;
      }
      // We have to copy the SetterValue because the Setter doesn't belong exclusively
      // to the UIElement. It may be part of a style for example, which is shared across
      // multiple controls.
      dd.Value = MpfCopyManager.DeepCopyCutLP(SetterValue);
    }

    /// <summary>
    /// Restore the target element's original value which was set to the setter's value before.
    /// </summary>
    /// <param name="element">The UI element which is used as starting point for this setter
    /// to earch the target element.</param>
    public void Restore(UIElement element)
    {
      IDataDescriptor dd = GetPropertyDescriptor(element);
      if (dd == null)
        return;
      if (WasApplied)
        dd.Value = _originalValue;
      _isSet = false;
    }
  }
}
