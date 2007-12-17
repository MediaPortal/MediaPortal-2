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
using System.Reflection;
using MediaPortal.Core;
using MediaPortal.Core.Localisation;
using MediaPortal.Core.Properties;
using MediaPortal.Core.WindowManager;

namespace SkinEngine
{
  public class LabelProperty : ILabelProperty
  {
    private ILabelProperty _property;
    private string _stringValue;
    private string _name;
    private Window _window;
    private StringId _localizedString;

    public LabelProperty(string stringValue)
    {
      _stringValue = stringValue;
      if (StringId.IsString(_stringValue)) 
      {
        _localizedString = new StringId(_stringValue); 
        _stringValue = null;
      }
      _window = null;
    }

    public LabelProperty(Window window, ILabelProperty property, string name)
    {
      _property = property;
      _name = name;
      _window = window;
    }

    #region ILabelProperty Members

    public string Evaluate(IControl control, IControl container)
    {
      if (_property != null)
      {
        return _property.Evaluate(control, container, Decode(_name));
      }
      if (_localizedString != null)
      {
        return _localizedString.ToString();
      }
      return _stringValue;
    }

    public string Evaluate(IControl control, IControl container, string name)
    {
      return _property.Evaluate(control, container, Decode(_name));
    }

    private string Decode(string text)
    {
      if (!text.StartsWith("#"))
      {
        return text;
      }
      string command = text.Substring(1);
      int pos = command.IndexOf("?");
      if (pos < 0)
      {
        return text;
      }
      string expression = command.Substring(0, pos);
      string[] results = command.Substring(pos + 1).Split(new char[] {','});

      string[] parts = expression.Split(new char[] {'.'});
      object control = GetObject(parts[0]);
      if (control == null)
      {
        return "";
      }
      Type classType = control.GetType();

      string methodResult = "";
      string methodName = parts[1];
      object[] parameters = null;
      bool isMethod = false;
      pos = methodName.IndexOf("(");
      if (pos >= 0)
      {
        isMethod = true;
        string paramsText = methodName.Substring(pos + 1, methodName.Length - pos - 2);
        methodName = methodName.Substring(0, pos);
        parameters = GetParameters(paramsText);
      }
      if (isMethod)
      {
        MethodInfo info =
          control.GetType().GetMethod(methodName,
                                      BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                      BindingFlags.InvokeMethod | BindingFlags.ExactBinding);
        object res = info.Invoke(control, parameters);
        //object res = classType.InvokeMember(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.ExactBinding, System.Type.DefaultBinder, control, parameters);
        if (res != null)
        {
          methodResult = res.ToString();
        }
      }
      else
      {
        int partNr = 1;
        object res = null;
        while (partNr < parts.Length)
        {
          MethodInfo info =
            control.GetType().GetProperty(methodName,
                                          BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                          BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
          res = info.Invoke(control, null);
          //res = classType.InvokeMember(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.ExactBinding, System.Type.DefaultBinder, control, null);
          partNr++;
          if (partNr < parts.Length)
          {
            control = res;
          }
        }
        if (res != null)
        {
          methodResult = res.ToString();
        }
      }

      for (int i = 0; i < results.Length; ++i)
      {
        parts = results[i].Split(new char[] {':'});
        if (parts[0] == methodResult)
        {
          return parts[1];
        }
      }
      return "";
    }

    private object GetObject(string name)
    {
      if (name == "WindowManager")
      {
        WindowManager manager = (WindowManager) ServiceScope.Get<IWindowManager>();
        return manager;
      }
      object control = _window.GetControlByName(name);
      if (control != null)
      {
        return control;
      }
      Model model = _window.GetModelByName(name);
      if (model != null)
      {
        return model.Instance;
      }
      return null;
    }

    private object[] GetParameters(string paramsText)
    {
      string[] parts = paramsText.Split(new char[] {','});
      return parts;
    }

    #endregion
  }
}