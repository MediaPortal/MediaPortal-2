using System;
using System.ComponentModel;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Triggers;

namespace SkinEngine.Controls.Visuals.Styles
{
  public class Setter : TriggerAction
  {
    Property _propertyProperty;
    Property _valueProperty;
    Property _targetNameProperty;

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
      if (s.Value != null)
      {
        ICloneable clone = s.Value as ICloneable;
        if (clone != null)
        {
          Value = clone.Clone();
        }
        else
        {
          Value = s.Value;
          Trace.WriteLine(String.Format("setter type:{0} is not clonable", s.Value));
        }
      }
    }

    public override object Clone()
    {
      return new Setter(this);
    }
    void Init()
    {
      _propertyProperty = new Property("");
      _valueProperty = new Property(null);
      _targetNameProperty = new Property("");
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

      object obj = Value;
      if (obj as String != null)
      {
        obj = TypeDescriptor.GetConverter(pinfo2.PropertyType).ConvertFromString((string)obj);
      }
      ICloneable clone = obj as ICloneable;
      if (clone != null)
      {
        property.SetValue(clone.Clone());
      }
      else
      {
        property.SetValue(obj);
      }
    }
  }
}
