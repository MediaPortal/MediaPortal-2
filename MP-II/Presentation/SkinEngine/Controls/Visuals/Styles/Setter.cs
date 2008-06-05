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
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Visuals.Triggers;
using Presentation.SkinEngine.XamlParser;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.MpfElements;

namespace Presentation.SkinEngine.Controls.Visuals.Styles
{
  public class Setter : TriggerAction
  {
    #region Private fields

    Property _propertyProperty;
    Property _valueProperty;
    Property _targetNameProperty;
    Object _originalValue = null;
    bool _isSet = false;
    object _value;

    #endregion

    #region Ctor

    public Setter()
    {
      Init();
    }

    void Init()
    {
      _targetNameProperty = new Property(typeof(string), "");
      _propertyProperty = new Property(typeof(string), "");
      _valueProperty = new Property(typeof(object), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Setter s = source as Setter;
      TargetName = copyManager.GetCopy(s.TargetName);
      Property = copyManager.GetCopy(s.Property);
      Value = copyManager.GetCopy(s.Value);
    }

    #endregion

    #region Properties

    public Property PropertyProperty
    {
      get { return _propertyProperty; }
      set { _propertyProperty = value; }
    }

    /// <summary>
    /// Gets or sets the name of the property to be set by this <see cref="Setter"/>.
    /// </summary>
    public string Property
    {
      get { return (string)_propertyProperty.GetValue(); }
      set { _propertyProperty.SetValue(value); }
    }

    public Property TargetNameProperty
    {
      get { return _targetNameProperty; }
    }

    /// <summary>
    /// Gets or sets the name of the target element where this setter will search
    /// the <see cref="Property"/> to be set.
    /// </summary>
    public string TargetName
    {
      get { return (string)_targetNameProperty.GetValue(); }
      set { _targetNameProperty.SetValue(value); }
    }

    public Property ValueProperty
    {
      get { return _valueProperty; }
    }

    /// <summary>
    /// Gets or sets the value to be set on our target. This value will be
    /// later converted to the right target type and stored in <see cref="SetterValue"/>.
    /// </summary>
    public object Value
    {
      get { return _valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the information if this setter was already initialized, that
    /// means its <see cref="SetterValue"/> and its <see cref="_originalValue"/>
    /// have been set and the value has been applied to the setter target.
    /// </summary>
    public bool IsSet
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
      get { return _value; }
      set { _value = value; }
    }

    #endregion

    public IDataDescriptor GetPropertyDescriptor(object element)
    {
      // Handle [Property] as well as [ClassName].[Property]
      // We'll simply ignore the [ClassName] here
      string propertyName;
      int index = Property.IndexOf('.');
      if (index != -1)
        propertyName = Property.Substring(index + 1);
      else
        propertyName = Property;
      IDataDescriptor result;
      if (ReflectionHelper.FindPropertyDescriptor(element, propertyName, out result))
        return result;
      else
        throw new ArgumentException(
          string.Format("Property '{0}' cannot be set on element '{1}'", Property, element));
    }

    public void Restore(UIElement element, Trigger trigger)
    {
      DependencyObject target = null;
      if (!string.IsNullOrEmpty(TargetName))
        target = VisualTreeHelper.FindElement(element, TargetName);
      if (target == null)
        target = element;
      IDataDescriptor dd = GetPropertyDescriptor(target);
      if (dd != null && IsSet)
        dd.Value = _originalValue;
    }

    public override void Execute(UIElement element, Trigger trigger)
    {
      DependencyObject target = null;
      if (!string.IsNullOrEmpty(TargetName))
        target = VisualTreeHelper.FindElement(element, TargetName);
      if (target == null)
        target = element;
      IDataDescriptor dd = GetPropertyDescriptor(target);
      if (dd == null)
        return;

      if (!IsSet)
      {
        IsSet = true;
        _originalValue = dd.Value;
        object obj;
        if (TypeConverter.Convert(Value, dd.DataType, out obj))
          SetterValue = obj;
      }
      // We have to copy the SetterValue because the Setter doesn't belong exclusively
      // to the UIElement. It may be part of a style for example, which is shared across
      // multiple controls.
      dd.Value = MpfCopyManager.DeepCopy(SetterValue);
    }
  }
}
