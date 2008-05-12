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
using System.ComponentModel;
using System.Reflection;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.XamlParser;
using TypeConverter = Presentation.SkinEngine.XamlParser.TypeConverter;

namespace Presentation.SkinEngine.Controls.Visuals.Styles      
{
  public class Style: NameScope, IAddChild, IImplicitKey
  {
    SetterCollection _setters;
    Property _targetTypeProperty;

    public Style()
    {
      Init();
    }

    public Style(Style s)
    {
      Init();
      _setters = s._setters;
      /*
      foreach (Setter set in s._setters)
      {
        _setters.Add((Setter)set.Clone());
      }*/
    }

    void Init()
    {
      _setters = new SetterCollection();
      _targetTypeProperty = new Property(typeof(Type), null);
    }

    /// <summary>
    /// Gets or sets the based on property (we dont use it in our xaml engine, but real xaml requires it)
    /// FIXME: New XAML engine perhaps uses it!
    /// </summary>
    public Style BasedOn
    {
      get
      {
        return null;
      }
      set
      {
      }
    }

    /// <summary>
    /// Gets or sets the target type property.
    /// </summary>
    public Property TargetTypeProperty
    {
      get { return _targetTypeProperty; }
      set { _targetTypeProperty = value; }
    }

    /// <summary>
    /// Gets or sets the type of the target (we dont use it in our xaml engine, but real xaml requires it)
    /// FIXME: New XAML engine uses it!
    /// </summary>
    /// <value>The type of the target.</value>
    public Type TargetType
    {
      get { return _targetTypeProperty.GetValue() as Type; }
      set { _targetTypeProperty.SetValue(value); }
    }

    public FrameworkElement Get(Window window)
    {
      foreach (Setter setter in _setters)
      {
        if (setter.Property == "Template")
        {
          FrameworkElement source;
          FrameworkElement element;
          if (setter.Value is FrameworkTemplate)
          {
            source = (FrameworkElement)((FrameworkTemplate)setter.Value).LoadContent(window);
            element = source;
          }
          else
          {
            source = (FrameworkElement)setter.Value;
            element = (FrameworkElement)source.Clone();
          }
          foreach (Setter setter2 in _setters)
          {
            if (setter2.Property != "Template")
              Set(element, setter2);
          }
          element.SetWindow(window);
          return element;
        }
      }
      return null;
    }

    public void Set(UIElement element)
    {
      Window w = element.Window;
      foreach (Setter setter in _setters)
      {
        setter.Setup(element);
        Set(element, setter);
      }
      element.SetWindow(w);
    }

    void Set(UIElement element, Setter setter)
    {
      if (setter.IsSet == false)
      {
        setter.IsSet = true;
        Type t = element.GetType();
        PropertyInfo pinfo = t.GetProperty(setter.Property + "Property");
        if (pinfo == null) return;
        setter.MethodInfo = pinfo.GetGetMethod();

        object obj = setter.Value;
        if (obj as String != null)
        {
          PropertyInfo pinfo2 = t.GetProperty(setter.Property);
          if (pinfo2 != null)
          {
            if (TypeDescriptor.GetConverter(pinfo2.PropertyType).CanConvertFrom(typeof(string)))
            {
              obj = TypeDescriptor.GetConverter(pinfo2.PropertyType).ConvertFromString((string)obj);
            }
            else
            {
              obj = TypeConverter.Convert(obj, pinfo2.PropertyType, out obj);
            }
          }
        }
        setter.SetterValue = obj;
      }
      MethodInfo info = setter.MethodInfo;
      if (info == null) return;
      Property property = info.Invoke(element, null) as Property;

      if (setter.SetterValue is ICloneable)
        property.SetValue(((ICloneable) setter.SetterValue).Clone());
      else
        property.SetValue(setter.SetterValue);
    }

    #region IAddChild implementation

    public void AddChild(object o)
    {
      _setters.Add((Setter)o);
    }

    #endregion

    #region IImplicitKey implementation

    public object GetImplicitKey()
    {
      return TargetType;
    }

    #endregion
  }
}
