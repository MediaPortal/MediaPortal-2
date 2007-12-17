#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Localisation;
using MediaPortal.Core.Properties;
using MediaPortal.Core.WindowManager;
using SkinEngine.Commands;
using SkinEngine.Controls;

namespace SkinEngine.Properties
{
  public class ReflectionDependency : Dependency
  {
    private string _stringValue;
    private IControl _control;
    private IWindow _window;
    private List<Property> _properties = new List<Property>();
    private PropertyChangedHandler _propertyChangedHandler;
    private IScriptProperty _scriptProperty;
    private string _scriptParam;
    private Dependency _scriptParamDependency;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionDependency"/> class.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="prop">The property.</param>
    /// <param name="param">The paramater.</param>
    public ReflectionDependency(IWindow window, IControl control, IScriptProperty prop, string param)
    {
      _control = control;
      _window = window;
      _scriptProperty = prop;
      _scriptParam = param;
      if (_scriptParam != "")
      {
        _propertyChangedHandler = new PropertyChangedHandler(OnParamPropertyChanged);
        OnParamPropertyChanged(null);
      }
      else
      {
        base.DependencyObject = _scriptProperty.Get(control, param);
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionDependency"/> class.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="reflectionName">command .</param>
    public ReflectionDependency(IWindow window, IControl control, string reflectionName)
    {
      _window = window;
      _propertyChangedHandler = new PropertyChangedHandler(OnReflectedPropertyChanged);
      _control = control;
      _stringValue = reflectionName;
      if (StringId.IsString(_stringValue))
      {
        SetValue(new StringId(_stringValue));
        return;
      }

      if (!_stringValue.EndsWith(".jpg") && !_stringValue.EndsWith(".png"))
      {
        OnReflectedPropertyChanged(null);
        ((Window)_window).ControlCountProperty.Attach(OnReflectedPropertyChanged);
        return;
      }
      SetValue(_stringValue);
    }

    /// <summary>
    /// Called when base.DependencyObject changed...
    /// </summary>
    /// <param name="property">The property.</param>
    protected override void OnValueChanged(Property property)
    {
      SetValue(property.GetValue());
    }

    /// <summary>
    /// Called when reflected property/properties changed
    /// </summary>
    /// <param name="property">The property.</param>
    protected void OnReflectedPropertyChanged(Property property)
    {
      foreach (Property p in _properties)
      {
        p.Detach(_propertyChangedHandler);
      }
      _properties.Clear();
      string expression = _stringValue;
      string[] results=null;
      int pos = _stringValue.IndexOf("?");
      if (pos >= 0)
      {
        expression = _stringValue.Substring(0, pos);
        results = _stringValue.Substring(pos + 1).Split(new char[] { ',' });
      }
      object methodResult = GetObjectForReflection(expression);
      if (results==null)
      {
        SetValue(methodResult);
        return;
      }
      for (int i = 0; i < results.Length; ++i)
      {
        string[] parts = results[i].Split(new char[] { ':' });
        if (parts[0] == (methodResult as string))
        {
          SetValue(parts[1]);
        }
      }
    }

    object GetObjectForReflection(string reflection)
    {
      object finalResult = null;
      string[] propertyNames = reflection.Split(new char[] { '.' });
      if (propertyNames.Length > 1)
      {
        object obj = ObjectFactory.GetObject(_control, _window, propertyNames[0]);
        if (obj == null)
        {
          return finalResult;
        }
        int partNr = 1;
        while (partNr < propertyNames.Length)
        {
          object res = null;
          string propName = propertyNames[partNr] + "Property";
          MemberInfo[] props =
            obj.GetType().FindMembers(MemberTypes.Property,
                                      BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                      BindingFlags.InvokeMethod | BindingFlags.ExactBinding, Type.FilterName, propName);
          if (props.Length == 0)
          {
            MethodInfo info =
              obj.GetType().GetProperty(propertyNames[partNr],
                                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                        BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
            if (info == null)
            {
              ServiceScope.Get<ILogger>().Error("cannot get object for {0}", _stringValue);
              return finalResult;
            }
            res = info.Invoke(obj, null);
          }
          else
          {
            MethodInfo info =
              obj.GetType().GetProperty(propertyNames[partNr] + "Property",
                                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                        BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
            if (info == null)
            {
              ServiceScope.Get<ILogger>().Error("cannot get object for {0}", _stringValue);
              return finalResult;
            }
            res = info.Invoke(obj, null);
          }
          Property pr = res as Property;
          if (pr != null)
          {
            _properties.Add(pr);
            pr.Attach(_propertyChangedHandler);
            obj = pr.GetValue();
            finalResult = obj;
          }
          else
          {
            obj = res;
            finalResult = obj;
          }
          if (obj == null)
          {
            break;
          }
          partNr++;
        }
      }
      else
      {
        finalResult = reflection;
      }
      return finalResult;
    }

    void OnParamPropertyChanged(Property property)
    {
      if (_scriptParamDependency != null)
        _scriptParamDependency.Detach(_propertyChangedHandler);
      _scriptParamDependency = new ReflectionDependency(_window, _control, _scriptParam);
      _scriptParamDependency.Attach(OnParamPropertyChanged);
      base.DependencyObject = _scriptProperty.Get(_control, _scriptParamDependency.GetValue().ToString());
    }
  }
}