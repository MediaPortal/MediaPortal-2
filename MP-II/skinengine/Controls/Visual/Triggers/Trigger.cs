using System;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;
namespace SkinEngine.Controls.Visuals.Triggers
{
  public class Trigger : ICloneable, IList
  {
    Property _propertyProperty;
    Property _valueProperty;
    Property _enterActionsProperty;
    Property _exitActionsProperty;
    Property _property;
    PropertyChangedHandler _handler;
    UIElement _element;

    public Trigger()
    {
      Init();
    }

    public Trigger(Trigger trig)
    {
      Init();
      Property = trig.Property;
      Value = trig.Value;
      foreach (TriggerAction ac in trig.EnterActions)
      {
        EnterActions.Add((TriggerAction)ac.Clone());
      }
      foreach (TriggerAction ac in trig.ExitActions)
      {
        ExitActions.Add((TriggerAction)ac.Clone());
      }
    }

    public virtual object Clone()
    {
      return new Trigger(this);
    }

    void Init()
    {
      _propertyProperty = new Property("");
      _valueProperty = new Property(false);
      _enterActionsProperty = new Property(new TriggerActionCollection());
      _exitActionsProperty = new Property(new TriggerActionCollection());
      _handler = new PropertyChangedHandler(OnPropertyChanged);
    }

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

    public bool Value
    {
      get
      {
        return (bool)_valueProperty.GetValue();
      }
      set
      {
        _valueProperty.SetValue(value);
      }
    }


    public Property EnterActionsProperty
    {
      get
      {
        return _enterActionsProperty;
      }
      set
      {
        _enterActionsProperty = value;
      }
    }

    public TriggerActionCollection EnterActions
    {
      get
      {
        return (TriggerActionCollection)_enterActionsProperty.GetValue();
      }
    }


    public Property ExitActionsProperty
    {
      get
      {
        return _exitActionsProperty;
      }
      set
      {
        _exitActionsProperty = value;
      }
    }

    public TriggerActionCollection ExitActions
    {
      get
      {
        return (TriggerActionCollection)_exitActionsProperty.GetValue();
      }
    }

    public void Setup(UIElement element)
    {
      if (_property != null)
      {
        _property.Detach(_handler);
        _property = null;
      }
      if (String.IsNullOrEmpty(Property))
      {
        return;
      }
      if (element as ControlTemplate != null)
      {
        element = element.VisualParent;
      }
      _element = element;
      Type t = element.GetType();
      PropertyInfo pinfo = t.GetProperty(Property + "Property");
      if (pinfo == null)
      {
        Trace.WriteLine(String.Format("trigger property {0} not found on {1}", this.Property, element));
        return;
      }
      MethodInfo minfo = pinfo.GetGetMethod();
      _property = minfo.Invoke(element, null) as Property;
      _property.Attach(_handler);
    }

    void OnPropertyChanged(Property p)
    {
      if ((bool)_property.GetValue() == Value)
      {
        //execute start actions
        foreach (TriggerAction action in EnterActions)
        {
          action.Execute(_element, this);
        }
      }
      else
      {
        //execute stop actions
        foreach (TriggerAction action in ExitActions)
        {
          action.Execute(_element, this);
        }
      }
    }

    #region IList Members

    public int Add(object value)
    {
      EnterActions.Add((TriggerAction)value);
      return EnterActions.Count;
    }

    public void Clear()
    {
      EnterActions.Clear();
    }

    public bool Contains(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int IndexOf(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Insert(int index, object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public bool IsFixedSize
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsReadOnly
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public void Remove(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void RemoveAt(int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public object this[int index]
    {
      get
      {
        throw new Exception("The method or operation is not implemented.");
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    #region ICollection Members

    public void CopyTo(Array array, int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int Count
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsSynchronized
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public object SyncRoot
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    #endregion

    #region IEnumerable Members

    public IEnumerator GetEnumerator()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion
  }
}
