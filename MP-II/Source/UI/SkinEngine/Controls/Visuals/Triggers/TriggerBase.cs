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

using System.Collections.Generic;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls;
using MediaPortal.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals.Triggers
{
  public class TriggerBase: DependencyObject
  {
    #region Protected fields

    protected UIElement _element;
    protected Property _enterActionsProperty;
    protected Property _exitActionsProperty;
    protected Property _settersProperty;

    #endregion

    #region Ctor

    public TriggerBase()
    {
      Init();
    }

    void Init()
    {
      _enterActionsProperty = new Property(typeof(IList<TriggerAction>), new List<TriggerAction>());
      _exitActionsProperty = new Property(typeof(IList<TriggerAction>), new List<TriggerAction>());
      _settersProperty = new Property(typeof(IList<Setter>), new List<Setter>());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      TriggerBase tb = (TriggerBase) source;
      foreach (TriggerAction ac in tb.EnterActions)
        EnterActions.Add(copyManager.GetCopy(ac));
      foreach (TriggerAction ac in tb.ExitActions)
        ExitActions.Add(copyManager.GetCopy(ac));
      foreach (Setter s in tb.Setters)
        Setters.Add(copyManager.GetCopy(s));
    }

    #endregion

    public virtual void Setup(UIElement element)
    {
      _element = element;
      foreach (TriggerAction action in EnterActions)
        action.Setup(element);
      foreach (TriggerAction action in ExitActions)
        action.Setup(element);
    }

    #region Public properties

    public bool IsInitialized
    {
      get { return _element != null; }
    }

    public Property EnterActionsProperty
    {
      get { return _enterActionsProperty; }
    }

    public IList<TriggerAction> EnterActions
    {
      get { return (IList<TriggerAction>) _enterActionsProperty.GetValue(); }
    }

    public Property ExitActionsProperty
    {
      get { return _exitActionsProperty; }
    }

    public IList<TriggerAction> ExitActions
    {
      get { return (IList<TriggerAction>) _exitActionsProperty.GetValue(); }
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

    protected void Initialize(object triggerValue, object checkValue)
    {
      object obj;
      if (triggerValue == null || !TypeConverter.Convert(checkValue, triggerValue.GetType(), out obj) ||
          !Equals(triggerValue, obj)) return;
      // Execute start actions
      foreach (TriggerAction action in EnterActions)
        action.Execute(_element);
      foreach (Setter s in Setters)
        s.Set(_element);
    }

    protected void TriggerIfValuesEqual(object triggerValue, object checkValue)
    {
      object obj;
      if (triggerValue != null && TypeConverter.Convert(checkValue, triggerValue.GetType(), out obj) &&
          Equals(triggerValue, obj))
      {
        // Execute start actions
        foreach (TriggerAction action in EnterActions)
          action.Execute(_element);
        foreach (Setter s in Setters)
          s.Set(_element);
      }
      else
      {
        // Execute stop actions
        foreach (TriggerAction action in ExitActions)
          action.Execute(_element);
        foreach (Setter s in Setters)
          s.Restore(_element);
      }
    }
  }
}
