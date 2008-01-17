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
using SkinEngine.Controls.Visuals;
using MediaPortal.Core;
using MediaPortal.Core.Properties;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Collections;
using MediaPortal.Core.WindowManager;

namespace SkinEngine.Controls.Bindings
{
  public class Binding : ICloneable
  {
    public string _expression;
    PropertyInfo _propertyInfo;
    protected BindingDependency _dependency;

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

    /// <summary>
    /// Initializes the binding to the object specified
    /// </summary>
    /// <param name="obj">The object.</param>
    public virtual void Initialize(object bindingDestinationObject)
    {
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
      Regex regex = new Regex(@"[a-zA-Z0-9.\[\]\(\)]+=[a-zA-Z0-9.\[\]\(\)]+");
      MatchCollection matches = regex.Matches(Expression);
      for (int i = 0; i < matches.Count; ++i)
      {
        //setter format is : bindingPropertyName1=value
        string setter = matches[i].Value;
        Regex regex2 = new Regex(@"[a-zA-Z0-9.\[\]\(\)]+");
        MatchCollection matches2 = regex2.Matches(setter);
        bool done = false;
        if (matches2.Count == 2)
        {
          string bindingPropertyName = matches2[0].Value;
          string bindingValue = matches2[1].Value;
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

          PropertyInfo info = GetPropertyOnObject(bindingDestinationObject, this.PropertyInfo.Name, true);
          if (info == null) return;
          if (info.PropertyType == typeof(Property))
          {
            //get the destination property
            MethodInfo methodInfo = info.GetGetMethod();
            if (methodInfo == null) return;
            Property destinationProperty = (Property)methodInfo.Invoke(bindingDestinationObject, null);

            destinationProperty.SetValue(vis);


          }

        }
      }
    }

    void SetupDatabinding(object bindingDestinationObject, string bindingSourcePropertyName)
    {
      UIElement sourceElement = bindingDestinationObject as UIElement;
      if (sourceElement == null)
      {
        return;
      }
      object bindingSourceProperty = GetBindingSourceObject(sourceElement, bindingSourcePropertyName);
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
      if (bindingSourceProperty is Property)
      {
        Property sourceProperty = (Property)bindingSourceProperty;
        PropertyInfo info = GetPropertyOnObject(bindingDestinationObject, this.PropertyInfo.Name, true);
        if (info == null) return;
        if (info.PropertyType == typeof(Property))
        {
          //get the destination property
          MethodInfo methodInfo = info.GetGetMethod();
          if (methodInfo == null) return;
          Property destinationProperty = (Property)methodInfo.Invoke(bindingDestinationObject, null);

          //create a new dependency..
          _dependency = new BindingDependency(sourceProperty, destinationProperty);


        }
        else
        {
          info = GetPropertyOnObject(bindingDestinationObject, this.PropertyInfo.Name, false);
          if (info == null) return;
          MethodInfo methodInfo = info.GetSetMethod();
          if (methodInfo == null) return;
          _dependency = new BindingDependency(sourceProperty, methodInfo, bindingDestinationObject);
        }

      }
      else
      {
        PropertyInfo info = GetPropertyOnObject(bindingDestinationObject, this.PropertyInfo.Name, false);
        if (info == null) return;
        MethodInfo methodInfo = info.GetSetMethod();
        if (methodInfo == null) return;
        methodInfo.Invoke(bindingDestinationObject, new object[] { bindingSourceProperty });
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
      PropertyInfo info = GetPropertyOnObject(element.Context, bindingSourcePropertyName, true);
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
        MethodInfo infoM = element.Context.GetType().GetMethod(bindingSourcePropertyName);
        if (infoM != null)
        {
          Command cmd = new Command();
          cmd.Method = infoM;
          cmd.Object = element.Context;
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
      object bindingObject = methodInfo.Invoke(element.Context, null);
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


    protected PropertyInfo GetPropertyOnObject(object obj, string propertyName, bool checkForProperty)
    {
      PropertyInfo info;
      if (checkForProperty)
      {
        info = obj.GetType().GetProperty(propertyName + "Property");
        if (info != null) return info;
      }
      info = obj.GetType().GetProperty(propertyName);
      return info;
    }
  }

}
