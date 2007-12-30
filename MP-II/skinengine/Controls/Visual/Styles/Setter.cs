using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Visuals.Styles
{
  public class Setter : ICloneable
  {
    Property _propertyProperty;
    Property _valueProperty;

    public Setter()
    {
      Init();
    }

    public Setter(Setter s)
    {
      Init();
      Property = s.Property;
      ICloneable clone=s.Value as ICloneable;
      if (clone != null)
      {
        Value = clone.Clone();
      }
      else
      {
        Value = s.Value;
        Trace.WriteLine(String.Format("type:{0} is not clonable", s.Value));
      }
    }

    public object Clone()
    {
      return new Setter(this);
    }
    void Init()
    {
      _propertyProperty = new Property("");
      _valueProperty = new Property(null);
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
  }
}
