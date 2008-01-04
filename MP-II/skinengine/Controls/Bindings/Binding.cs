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

namespace SkinEngine.Controls.Bindings
{
  public class Binding : ICloneable
  {
    public string _expression;
    PropertyInfo _propertyInfo;
    BindingDependency _dependency;

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
    public string Expression
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
    public void Initialize(object bindingDestinationObject)
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

      Regex regex = new Regex(@"[a-zA-Z0-9.\[\]\(\)]+=[a-zA-Z0-9.\[\]\(\)]+");
      MatchCollection matches = regex.Matches(Expression);
      for (int i = 0; i < matches.Count; ++i)
      {
        //setter format is : bindingPropertyName1=value
        string setter = matches[i].Value;
        Regex regex2 = new Regex(@"[a-zA-Z0-9.\[\]\(\)]+");
        MatchCollection matches2 = regex2.Matches(setter);
        if (matches2.Count == 2)
        {
          string bindingPropertyName = matches2[0].Value;
          string bindingValue = matches2[1].Value;
          if (bindingPropertyName == "Path")
          {
            SetupDatabinding(bindingDestinationObject, bindingValue);
          }
        }
      }
    }

    void SetupDatabinding(object bindingDestinationObject, string bindingSourcePropertyName)
    {
      UIElement sourceElement = bindingDestinationObject as UIElement;
      if (sourceElement == null) return;
      object bindingSourceProperty = GetBindingSourceObject(sourceElement, bindingSourcePropertyName);
      if (bindingSourceProperty == null)
      {
        ServiceScope.Get<ILogger>().Warn("Binding:'{0}' cannot find binding source element '{1}' on {2}",
          Expression, bindingSourcePropertyName, sourceElement);
        return;
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
          methodInfo.Invoke(bindingDestinationObject, new object[] { sourceProperty.GetValue() });
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
    object GetBindingSourceObject(UIElement element, string bindingSourcePropertyName)
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
        return null;
      }
      MethodInfo methodInfo = info.GetGetMethod();
      if (methodInfo == null) return null;
      object bindingObject = methodInfo.Invoke(element.Context, null);
      return bindingObject;
    }

    PropertyInfo GetPropertyOnObject(object obj, string propertyName, bool checkForProperty)
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
