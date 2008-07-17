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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Visuals.Styles;
using Presentation.SkinEngine.XamlParser.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Visuals.Triggers
{
  public class Trigger: TriggerBase, IAddChild<TriggerAction>
  {
    #region Private fields

    Property _propertyProperty;
    Property _valueProperty;
    Property _enterActionsProperty;
    Property _exitActionsProperty;
    Property _property;
    PropertyChangedHandler _handler;

    #endregion

    #region Ctor

    public Trigger()
    {
      Init();
    }

    void Init()
    {
      _propertyProperty = new Property(typeof(string), "");
      _valueProperty = new Property(typeof(bool), false);
      _enterActionsProperty = new Property(typeof(IList<TriggerAction>), new List<TriggerAction>());
      _exitActionsProperty = new Property(typeof(IList<TriggerAction>), new List<TriggerAction>());
      _handler = OnPropertyChanged;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Trigger t = source as Trigger;
      Property = copyManager.GetCopy(t.Property);
      Value = copyManager.GetCopy(t.Value);
      foreach (TriggerAction ac in t.EnterActions)
        EnterActions.Add(copyManager.GetCopy(ac));
      foreach (TriggerAction ac in t.ExitActions)
        ExitActions.Add(copyManager.GetCopy(ac));
    }

    #endregion

    #region Public properties

    public Property PropertyProperty
    {
      get { return _propertyProperty; }
    }

    public string Property
    {
      get { return _propertyProperty.GetValue() as string; }
      set { _propertyProperty.SetValue(value); }
    }

    public Property ValueProperty
    {
      get { return _valueProperty; }
    }

    public bool Value
    {
      get { return (bool)_valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    public Property EnterActionsProperty
    {
      get { return _enterActionsProperty; }
    }

    public IList<TriggerAction> EnterActions
    {
      get { return (IList<TriggerAction>)_enterActionsProperty.GetValue(); }
    }

    public Property ExitActionsProperty
    {
      get { return _exitActionsProperty; }
    }

    public IList<TriggerAction> ExitActions
    {
      get { return (IList<TriggerAction>)_exitActionsProperty.GetValue(); }
    }

    #endregion

    public override void Setup(UIElement element)
    {
      base.Setup(element);
      if (_property != null)
      {
        _property.Detach(_handler);
        _property = null;
      }
      if (!String.IsNullOrEmpty(Property))
      {
        // FIXME: Use ReflectionHelper here
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
        action.Setup(element);
      foreach (TriggerAction action in ExitActions)
        action.Setup(element);
      OnPropertyChanged(_property);
    }

    /// <summary>
    /// Listens for changes of our trigger property.
    /// </summary>
    void OnPropertyChanged(Property p)
    {
      if (_property == null) return;
      if ((bool)_property.GetValue() == Value)
      {
        //execute start actions
        foreach (TriggerAction action in EnterActions)
          action.Execute(_element, this);
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
            s.Restore(_element, this);
        }
      }
    }

    #region IAddChild Members

    public void AddChild(TriggerAction o)
    {
      EnterActions.Add(o);
    }

    #endregion
  }
}
