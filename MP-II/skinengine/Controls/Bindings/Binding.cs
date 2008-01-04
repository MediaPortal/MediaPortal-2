using System;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace SkinEngine.Controls.Bindings
{
  public class Binding : ICloneable
  {
    public string _expression;
    PropertyInfo _propertyInfo;

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
    public void Initialize(object obj)
    {
      //{Binding LastName}
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
        MatchCollection matches2 = regex.Matches(setter);
        if (matches2.Count == 2)
        {
          string bindingPropertyName = matches[0].Value;
          string bindingValue = matches[1].Value;

        }
      }

      //handle special case: {Binding LastName}
      if (matches.Count == 0)
      {
      }
    }

  }
}
