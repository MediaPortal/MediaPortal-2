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
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;
using MyXaml.Core;
using SkinEngine.Controls.Bindings;
namespace SkinEngine.Controls.Visuals.Triggers
{
  public class Trigger : ICloneable, IAddChild, IBindingCollection
  {
    Property _propertyProperty;
    Property _valueProperty;
    Property _enterActionsProperty;
    Property _exitActionsProperty;
    Property _property;
    PropertyChangedHandler _handler;
    UIElement _element;
    BindingCollection _bindings;

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
      foreach (Binding binding in trig._bindings)
      {
        _bindings.Add((Binding)binding.Clone());
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
      _bindings = new BindingCollection();
      _propertyProperty.Attach(new PropertyChangedHandler(OnPropChanged));
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
        return _propertyProperty.GetValue() as string;
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

    public virtual void Setup(UIElement element)
    {
      _element = element;
      InitializeBindings(_element);
      //Trace.WriteLine("Setup trigger for " + element.Name + " " + element.GetType().ToString() + " " + Property);
      if (_property != null)
      {
        _property.Detach(_handler);
        _property = null;
      }
      if (!String.IsNullOrEmpty(Property))
      {

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
      foreach (TriggerAction action in EnterActions)
      {
        if (action is Setter)
        {
          Setter s = (Setter)action;
          if (s.TargetName == "lbl11")
          {
          }
        }
        action.Setup(element);
      }
      foreach (TriggerAction action in ExitActions)
      {
        action.Setup(element);
      }
      OnPropertyChanged(_property);
    }

    void OnPropChanged(Property p)
    {
      if (_property != null) return;
      if (_propertyProperty == null) return;
      if (_propertyProperty.GetValue().GetType() != typeof(bool)) return;
      if ((bool)_propertyProperty.GetValue() == Value)
      {
        //execute start actions
        foreach (TriggerAction action in EnterActions)
        {
          if (action is Setter)
          {
          }
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
        foreach (TriggerAction action in EnterActions)
        {
          Setter s = action as Setter;
          if (s != null)
          {
            s.Restore(_element, this);
          }
        }
      }
    }
    
    void OnPropertyChanged(Property p)
    {
      if (_property == null) return;
      if ((bool)_property.GetValue() == Value)
      {
        //execute start actions
        foreach (TriggerAction action in EnterActions)
        {
          if (action is Setter)
          {
          }
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
        foreach (TriggerAction action in EnterActions)
        {
          Setter s = action as Setter;
          if (s != null)
          {
            s.Restore(_element, this);
          }
        }
      }
    }

    #region IAddChild Members

    public void AddChild(object o)
    {
      if (o is Setter)
      {
        Setter s = (Setter)o;
        if (s.TargetName == "lbl11")
        {
        }
      }
      EnterActions.Add((TriggerAction)o);
    }

    #endregion

    #region IBindingCollection Members

    public void Add(Binding binding)
    {
      _bindings.Add(binding);
    }

    public virtual void InitializeBindings(UIElement element)
    {
      if (_bindings.Count == 0) return;
      foreach (Binding binding in _bindings)
      {
        binding.Initialize(this, element);
      }
    }

    #endregion
  }
}
