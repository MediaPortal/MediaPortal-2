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
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls;
using Presentation.SkinEngine.Controls.Visuals.Styles;
using Presentation.SkinEngine.XamlParser;
using Presentation.SkinEngine.Controls.Bindings;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Visuals.Triggers
{
  public class Trigger: DependencyObject, ICloneable, IAddChild
  {
    Property _propertyProperty;
    Property _valueProperty;
    Property _enterActionsProperty;
    Property _exitActionsProperty;
    Property _property;
    PropertyChangedHandler _handler;
    UIElement _element;

    public Trigger(): base()
    {
      Init();
    }

    public Trigger(Trigger trig): base(trig)
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
      Trigger result = new Trigger(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }

    void Init()
    {
      _propertyProperty = new Property(typeof(string), "");
      _valueProperty = new Property(typeof(bool), false);
      _enterActionsProperty = new Property(typeof(TriggerActionCollection), new TriggerActionCollection());
      _exitActionsProperty = new Property(typeof(TriggerActionCollection), new TriggerActionCollection());
      _handler = OnPropertyChanged;
      _propertyProperty.Attach(OnPropChanged);
    }

    public Property PropertyProperty
    {
      get
      {
        return _propertyProperty;
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

    /// Albert78: FIXME: Remove properties for List members? It doesn't make sense
    /// to attach change handlers to them
    public Property EnterActionsProperty
    {
      get
      {
        return _enterActionsProperty;
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
            // FIXME Albert78: Remove this
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
            // FIXME Albert78: Remove this
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
            // FIXME Albert78: Remove this
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
      EnterActions.Add((TriggerAction) o);
    }

    #endregion
  }
}
