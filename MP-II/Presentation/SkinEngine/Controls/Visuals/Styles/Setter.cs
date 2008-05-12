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
using System.Reflection;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Visuals.Triggers;
using Presentation.SkinEngine.XamlParser;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Visuals.Styles
{
  public class Setter : TriggerAction
  {
    Property _propertyProperty;
    Property _valueProperty;
    Property _targetNameProperty;
    Object _originalValue = null;
    MethodInfo _methodInfo;
    bool _isSet = false;
    object _value;

    public Setter()
    {
      Init();
    }

    public Setter(Setter s)
      : base(s)
    {
      Init();
      Property = s.Property;
      TargetName = s.TargetName;
      if (s.Value is ICloneable)
        Value = ((ICloneable) s.Value).Clone();
      else
        Value = s.Value;
    }

    public override object Clone()
    {
      Setter result = new Setter(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }

    void Init()
    {
      _propertyProperty = new Property(typeof(string), "");
      _valueProperty = new Property(typeof(object), null);
      _targetNameProperty = new Property(typeof(string), "");
    }

    /// <summary>
    /// Gets or sets the property property.
    /// </summary>
    /// <value>The property property.</value>
    public Property PropertyProperty
    {
      get
      {
        return _propertyProperty;
      }
      set
      {
        _propertyProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the property.
    /// </summary>
    /// <value>The property.</value>
    public string Property
    {
      get
      {
        return (string)_propertyProperty.GetValue();
      }
      set
      {
        _propertyProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the target name property.
    /// </summary>
    /// <value>The target name property.</value>
    public Property TargetNameProperty
    {
      get
      {
        return _targetNameProperty;
      }
      set
      {
        _targetNameProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the name of the target.
    /// </summary>
    /// <value>The name of the target.</value>
    public string TargetName
    {
      get
      {
        return (string)_targetNameProperty.GetValue();
      }
      set
      {
        _targetNameProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the value property.
    /// </summary>
    /// <value>The value property.</value>
    public Property ValueProperty
    {
      get
      {
        return _valueProperty;
      }
      set
      {
        _valueProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    /// <value>The value.</value>
    public object Value
    {
      get
      {
        return _valueProperty.GetValue();
      }
      set
      {
        _valueProperty.SetValue(value);
      }
    }

    public void Restore(UIElement element, Trigger trigger)
    {
      object uiElement = VisualTreeHelper.Instance.FindElement(element, TargetName);
      if (uiElement == null)
        uiElement = VisualTreeHelper.Instance.FindElement(TargetName);
      if (uiElement == null) return;
      Type t = uiElement.GetType();
      PropertyInfo pinfo = t.GetProperty(this.Property + "Property");
      if (pinfo == null) return;
      PropertyInfo pinfo2 = t.GetProperty(Property);
      if (pinfo2 == null) return;
      MethodInfo minfo = pinfo.GetGetMethod();
      Property property = minfo.Invoke(uiElement, null) as Property;

      // FIXME Albert78: Shouldn't we directly assign _originalValue, without cloning it?
      if (_originalValue is ICloneable)
        property.SetValue(((ICloneable) _originalValue).Clone());
      else
        if (_originalValue != null)
          property.SetValue(_originalValue);
    }

    public override void Execute(UIElement element, Trigger trigger)
    {
      object uiElement = VisualTreeHelper.Instance.FindElement(element, TargetName);
      if (uiElement == null)
        uiElement = VisualTreeHelper.Instance.FindElement(TargetName);
      if (uiElement == null) return;
      Type t = uiElement.GetType();
      PropertyInfo pinfo = t.GetProperty(this.Property + "Property");
      if (pinfo == null) return;
      PropertyInfo pinfo2 = t.GetProperty(Property);
      if (pinfo2 == null) return;
      MethodInfo minfo = pinfo.GetGetMethod();
      Property property = minfo.Invoke(uiElement, null) as Property;

      if (_originalValue == null)
        _originalValue = property.GetValue();

      object obj;
      TypeConverter.Convert(Value, pinfo2.PropertyType, out obj);
      if (obj is ICloneable)
        property.SetValue(((ICloneable) obj).Clone());
      else
        property.SetValue(obj);
    }


    public MethodInfo MethodInfo
    {
      get
      {
        return _methodInfo;
      }
      set
      {
        _methodInfo = value;
      }
    }
    public bool IsSet
    {
      get
      {
        return _isSet;
      }
      set
      {
        _isSet = value;
      }
    }
    public object SetterValue
    {
      get
      {
        return _value;
      }
      set
      {
        _value = value;
      }
    }
  }
}
