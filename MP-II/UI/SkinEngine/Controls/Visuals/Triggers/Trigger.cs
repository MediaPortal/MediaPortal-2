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
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.SkinEngine.Xaml;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals.Triggers
{
  public class Trigger: TriggerBase, IAddChild<Setter>
  {
    #region Private fields

    protected Property _propertyProperty;
    protected Property _valueProperty;
    protected Property _enterActionsProperty;
    protected Property _exitActionsProperty;
    protected Property _settersProperty;
    protected IDataDescriptor _dataDescriptor;

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
      _settersProperty = new Property(typeof(IList<Setter>), new List<Setter>());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Trigger t = (Trigger) source;
      Property = copyManager.GetCopy(t.Property);
      Value = copyManager.GetCopy(t.Value);
      foreach (TriggerAction ac in t.EnterActions)
        EnterActions.Add(copyManager.GetCopy(ac));
      foreach (TriggerAction ac in t.ExitActions)
        ExitActions.Add(copyManager.GetCopy(ac));
      foreach (Setter s in t.Setters)
        Setters.Add(copyManager.GetCopy(s));
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

    public Property SettersProperty
    {
      get { return _settersProperty; }
    }

    public IList<Setter> Setters
    {
      get { return (IList<Setter>) _settersProperty.GetValue(); }
    }

    #endregion

    public override void Setup(UIElement element)
    {
      base.Setup(element);
      if (_dataDescriptor != null)
      {
        _dataDescriptor.Detach(OnPropertyChanged);
        _dataDescriptor = null;
      }
      if (!String.IsNullOrEmpty(Property))
      {
        if (ReflectionHelper.FindMemberDescriptor(element, Property, out _dataDescriptor))
          _dataDescriptor.Attach(OnPropertyChanged);
      }
      foreach (TriggerAction action in EnterActions)
        action.Setup(element);
      foreach (TriggerAction action in ExitActions)
        action.Setup(element);
      OnPropertyChanged(_dataDescriptor);
    }

    /// <summary>
    /// Listens for changes of our trigger property data descriptor.
    /// </summary>
    void OnPropertyChanged(IDataDescriptor dd)
    {
      if (_dataDescriptor == null) return;
      if ((bool) _dataDescriptor.Value == Value)
      {
        //execute start actions
        foreach (TriggerAction action in EnterActions)
          action.Execute(_element);
        foreach (Setter s in Setters)
          s.Set(_element);
      }
      else
      {
        //execute stop actions
        foreach (TriggerAction action in ExitActions)
          action.Execute(_element);
        foreach (Setter s in Setters)
          s.Restore(_element);
      }
    }

    #region IAddChild Members

    public void AddChild(Setter s)
    {
      Setters.Add(s);
    }

    #endregion
  }
}
