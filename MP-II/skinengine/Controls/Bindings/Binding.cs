using System;
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
    }

  }
}
