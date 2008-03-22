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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using Presentation.SkinEngine.Controls.Visuals;
using MediaPortal.Core;
using MediaPortal.Presentation.Properties;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Collections;
using MediaPortal.Presentation.WindowManager;

namespace Presentation.SkinEngine.Controls.Bindings
{
  public class Binding : ICloneable
  {
    public string _expression;
    PropertyInfo _propertyInfo;
    UIElement _context;
    protected BindingDependency _dependency;
    BindingMode _mode = BindingMode.OneWay;

    public Binding()
    {
    }

    public Binding(Binding bind)
    {
      Expression = bind.Expression;
      this.PropertyInfo = bind.PropertyInfo;
    }

    public virtual object Clone()
    {
      return new Binding(this);
    }

    /// <summary>
    /// Gets or sets the expression.
    /// </summary>
    /// <value>The expression.</value>
    public virtual string Expression
    {
      get
      {
        return _expression;
      }
      set
      {
        if (value.IndexOf("=") < 0)
          _expression = "Path=" + value;
        else
          _expression = value;
      }
    }

    /// <summary>
    /// Gets or sets the info.
    /// </summary>
    /// <value>The info.</value>
    public PropertyInfo PropertyInfo
    {
      get
      {
        return _propertyInfo;
      }
      set
      {
        _propertyInfo = value;
      }
    }

    public virtual void Initialize(object bindingDestinationObject)
    {
      UIElement element = bindingDestinationObject as UIElement;
      if (element != null)
      {
        Initialize(bindingDestinationObject, element);
      }
    }

    /// <summary>
    /// Initializes the binding to the object specified
    /// </summary>
    /// <param name="obj">The object.</param>
    public virtual void Initialize(object bindingDestinationObject, UIElement context)
    {
      _context = context;
      //{Binding bindingPropertyName1=value,bindingPropertyName2=value,bindingPropertyNameN=value}
      // binding properties
      //   ElementName=
      //   Source=
      //   RelativeSource=
      //   Path=
      //   Mode=

      //examples:
      // Width="{TemplateBinding ListBox.Width}"
      // ViewMode="{Binding Path=ViewModeType}" 
      // Value="{Binding Path=TopProgressBarRed,Mode=OneWay}"

      string elementName = "";
      Regex regex = new Regex(@"[:!a-zA-Z0-9.\[\]\(\)]+=[:!a-zA-Z0-9.\[\]\(\)]+");
      MatchCollection matches = regex.Matches(Expression);
      _mode = BindingMode.OneWay;
      for (int i = 0; i < matches.Count; ++i)
      {
        //setter format is : bindingPropertyName1=value
        string setter = matches[i].Value;
        Regex regex2 = new Regex(@"[:!a-zA-Z0-9.\[\]\(\)]+");
        MatchCollection matches2 = regex2.Matches(setter);
        bool done = false;
        if (matches2.Count == 2)
        {
          string bindingPropertyName = matches2[0].Value;
          string bindingValue = matches2[1].Value;
          if (bindingPropertyName == "Mode")
          {
            if (bindingValue == "OneWay") _mode = BindingMode.OneWay;
            else if (bindingValue == "TwoWay") _mode = BindingMode.TwoWay;
          }
          if (bindingPropertyName == "ElementName")
          {
            elementName = bindingValue;
          }
          if (bindingPropertyName == "Path")
          {
            done = true;
            if (elementName != "")
              SetupDatabinding(bindingDestinationObject, elementName + "." + bindingValue);
            else
              SetupDatabinding(bindingDestinationObject, bindingValue);
          }
        }
        if (!done && !String.IsNullOrEmpty(elementName))
        {
          object vis = VisualTreeHelper.Instance.FindElement(elementName);
          if (vis == null) return;

          object obj;
          PropertyInfo info = GetPropertyOnObject(bindingDestinationObject, this.PropertyInfo.Name, true, out obj);
          if (info == null) return;
          if (info.PropertyType == typeof(Property))
          {
            //get the destination property
            MethodInfo methodInfo = info.GetGetMethod();
            if (methodInfo == null) return;
            Property destinationProperty = (Property)methodInfo.Invoke(obj, null);

            destinationProperty.SetValue(vis);
          }

        }
      }
    }

    void SetupDatabinding(object bindingDestinationObject, string bindingSourcePropertyName)
    {
      bool negate = false;
      if (bindingSourcePropertyName.StartsWith("!"))
      {
        negate = true;
        bindingSourcePropertyName = bindingSourcePropertyName.Substring(1);
      }
      object bindingSourceProperty;
      UIElement sourceElement = bindingDestinationObject as UIElement;
      if (sourceElement != null)
      {
        bindingSourceProperty = GetBindingSourceObject(sourceElement, bindingSourcePropertyName);
        if (bindingSourceProperty == null)
        {
          bindingSourceProperty = VisualTreeHelper.Instance.FindElement(sourceElement, bindingSourcePropertyName + "Property");
          if (bindingSourceProperty == null)
          {
            bindingSourceProperty = VisualTreeHelper.Instance.FindElement(sourceElement, bindingSourcePropertyName);
            if (bindingSourceProperty == null)
            {
              ServiceScope.Get<ILogger>().Warn("Binding:'{0}' cannot find binding source element '{1}' on {2}",
                Expression, bindingSourcePropertyName, sourceElement);
              return;
            }
          }
        }
      }
      else
      {
        bindingSourceProperty = GetBindingSourceObject(_context, bindingSourcePropertyName);
        if (bindingSourceProperty == null)
        {
          bindingSourceProperty = VisualTreeHelper.Instance.FindElement(_context, bindingSourcePropertyName + "Property");
          if (bindingSourceProperty == null)
          {
            bindingSourceProperty = VisualTreeHelper.Instance.FindElement(_context, bindingSourcePropertyName);
            if (bindingSourceProperty == null)
            {
              ServiceScope.Get<ILogger>().Warn("Binding:'{0}' cannot find binding source element '{1}' on {2}",
                Expression, bindingSourcePropertyName, sourceElement);
              return;
            }
          }
        }
      }

      object obj;
      if (bindingSourceProperty is Property)
      {
        Property sourceProperty = (Property)bindingSourceProperty;
        PropertyInfo info = GetPropertyOnObject(bindingDestinationObject, this.PropertyInfo.Name, true, out obj);
        if (info == null) return;
        if (info.PropertyType == typeof(Property))
        {
          //get the destination property
          MethodInfo methodInfo = info.GetGetMethod();
          if (methodInfo == null) return;
          Property destinationProperty = (Property)methodInfo.Invoke(obj, null);

          //create a new dependency..
          _dependency = new BindingDependency(sourceProperty, destinationProperty, _mode, negate);
        }
        else
        {
          info = GetPropertyOnObject(bindingDestinationObject, this.PropertyInfo.Name, false, out obj);
          if (info == null) return;
          MethodInfo methodInfo = info.GetSetMethod();
          if (methodInfo == null) return;
          _dependency = new BindingDependency(sourceProperty, methodInfo, obj, _mode,negate);
        }
      }
      else
      {
        PropertyInfo info = GetPropertyOnObject(bindingDestinationObject, this.PropertyInfo.Name, false, out obj);
        if (info == null) return;
        MethodInfo methodInfo = info.GetSetMethod();
        if (methodInfo == null) return;
        methodInfo.Invoke(obj, new object[] { bindingSourceProperty });
      }
    }

    /// <summary>
    /// Gets the binding source object.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="bindingSourcePropertyName">Name of the binding source property.</param>
    /// <returns></returns>
    protected object GetBindingSourceObject(UIElement element, string bindingSourcePropertyName)
    {
      if (element.Context == null)
      {
        if (element.VisualParent == null) return null;
        return GetBindingSourceObject(element.VisualParent, bindingSourcePropertyName);
      }
      Object obj;
      PropertyInfo info = GetPropertyOnObject(element.Context, bindingSourcePropertyName, true, out obj);
      if (info == null)
      {
        if (element.Context is ListItem)
        {
          ListItem listItem = (ListItem)element.Context;
          if (listItem.Contains(bindingSourcePropertyName))
          {
            return listItem.Label(bindingSourcePropertyName).Evaluate(null, null);
          }
        }
        //check if its a method
        MethodInfo infoM = GetMethodOnObject(element.Context, bindingSourcePropertyName, out obj);
        if (infoM != null)
        {
          Command cmd = new Command();
          cmd.Method = infoM;
          cmd.Object = obj;
          return cmd;
        }
        Command newCmd = GetMethodInfo(bindingSourcePropertyName);
        if (newCmd != null)
        {
          return newCmd;
        }
        return null;
      }
      MethodInfo methodInfo = info.GetGetMethod();
      if (methodInfo == null) return null;
      object bindingObject = methodInfo.Invoke(obj, null);
      return bindingObject;
    }

    protected object GetBindingSourceObject(object element, string bindingSourcePropertyName)
    {
      object obj;
      PropertyInfo info = GetPropertyOnObject(element, bindingSourcePropertyName, true, out obj);
      if (info == null)
      {
        Command newCmd = GetMethodInfo(bindingSourcePropertyName);
        if (newCmd != null)
        {
          return newCmd;
        }
        return null;
      }
      MethodInfo methodInfo = info.GetGetMethod();
      if (methodInfo == null) return null;
      object bindingObject = methodInfo.Invoke(obj, null);
      return bindingObject;
    }

    Command GetMethodInfo(string command)
    {
      IWindow window = ServiceScope.Get<IWindowManager>().CurrentWindow;
      string[] parts = command.Split(new char[] { '.' });
      if (parts.Length < 2)
      {
        return null;
      }

      object control = SkinEngine.Commands.ObjectFactory.GetObject(null, window, parts[0]);
      if (control == null)
      {
        return null;
      }

      Type classType;
      int partNr = 1;
      while (partNr < parts.Length - 1)
      {
        classType = control.GetType();
        MethodInfo info =
          classType.GetProperty(parts[partNr],
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
        if (info == null)
        {
          ServiceScope.Get<ILogger>().Error("cannot get object for {0}", command);
          return null;
        }
        object obj = info.Invoke(control, null);
        partNr++;
        if (partNr < parts.Length)
        {
          control = obj;
          if (control == null)
          {
            break;
          }
        }
      }
      string memberName = parts[parts.Length - 1];

      if (control != null)
      {
        classType = control.GetType();
        MethodInfo info =
          classType.GetMethod(memberName,
                              BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                              BindingFlags.InvokeMethod | BindingFlags.ExactBinding);
        if (info != null)
        {
          Command cmd = new Command();
          cmd.Method = info;
          cmd.Object = control;
          return cmd;
        }
      }
      return null;
    }

    protected PropertyInfo GetPropertyOnObject(object element, string propertyName, bool checkForProperty, out object context)
    {
      context = null;
      PropertyInfo info;
      string[] parts = propertyName.Split('.');
      if (parts.Length == 1)
      {
        if (checkForProperty)
        {
          info = element.GetType().GetProperty(propertyName + "Property");
          if (info != null)
          {
            context = element;
            return info;
          }
        }
        info = element.GetType().GetProperty(propertyName);
        if (info != null)
        {
          context = element;
        }
        return info;
      }
      ///----
      object model = element;
      int partNr = 0;
      object obj = null;
      while (partNr < parts.Length - 1)
      {
        Type classType = model.GetType();
        info = classType.GetProperty(parts[partNr],
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.InvokeMethod | BindingFlags.ExactBinding);
        if (info == null)
          return null;
        MethodInfo methodInfo = info.GetGetMethod();
        if (methodInfo == null)
          return null;
        obj = methodInfo.Invoke(model, null);

        partNr++;
        if (partNr < parts.Length)
        {
          model = obj;
          if (model == null)
          {
            return null;
          }
        }
      }

      if (checkForProperty)
      {
        info = model.GetType().GetProperty(parts[parts.Length - 1] + "Property");
        if (info != null)
        {
          context = model;
          return info;
        }
      }
      info = model.GetType().GetProperty(parts[parts.Length - 1]);
      context = model;
      return info;
      //----
    }

    protected MethodInfo GetMethodOnObject(object element, string propertyName, out object context)
    {
      context = null;
      MethodInfo info;
      string[] parts = propertyName.Split('.');
      if (parts.Length == 1)
      {
        info = element.GetType().GetMethod(propertyName);
        if (info != null)
        {
          context = element;
          return info;
        }
        return null;
      }
      ///----
      object model = element;
      int partNr = 0;
      object obj = null;
      while (partNr < parts.Length - 1)
      {
        Type classType = model.GetType();
        PropertyInfo inf = classType.GetProperty(parts[partNr],
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.InvokeMethod | BindingFlags.ExactBinding);
        if (inf == null)
          return null;
        MethodInfo methodInfo = inf.GetGetMethod();
        if (methodInfo == null)
          return null;
        obj = methodInfo.Invoke(model, null);

        partNr++;
        if (partNr < parts.Length)
        {
          model = obj;
          if (model == null)
          {
            return null;
          }
        }
      }

      info = model.GetType().GetMethod(parts[parts.Length - 1]);
      context = model;
      return info;
      //----
    }
  }
}
