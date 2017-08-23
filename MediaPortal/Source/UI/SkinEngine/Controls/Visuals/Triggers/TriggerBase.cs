#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Windows.Markup;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers
{
  [ContentProperty("Setters")]
  public class TriggerBase : DependencyObject
  {
    #region Protected fields

    protected UIElement _element;
    protected bool _triggerState;
    protected AbstractProperty _enterActionsProperty;
    protected AbstractProperty _exitActionsProperty;
    protected AbstractProperty _settersProperty;

    #endregion

    #region Ctor

    public TriggerBase()
    {
      Init();
    }

    void Init()
    {
      _enterActionsProperty = new SProperty(typeof(TriggerActionCollection), new TriggerActionCollection());
      _exitActionsProperty = new SProperty(typeof(TriggerActionCollection), new TriggerActionCollection());
      _settersProperty = new SProperty(typeof(List<Setter>), new List<Setter>());
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

    public override void Dispose()
    {
      base.Dispose();
      foreach (TriggerAction ac in EnterActions)
        ac.Dispose();
      foreach (TriggerAction ac in ExitActions)
        ac.Dispose();
      foreach (Setter s in Setters)
        s.Dispose();
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

    public virtual void Reset()
    {
      if (_element == null)
        return;
      foreach (TriggerAction action in EnterActions)
        action.Reset(_element);
      foreach (TriggerAction action in ExitActions)
        action.Reset(_element);
      foreach (Setter s in Setters)
        s.Restore(_element);
    }

    #region Public properties

    public bool IsInitialized
    {
      get { return _element != null; }
    }

    public AbstractProperty EnterActionsProperty
    {
      get { return _enterActionsProperty; }
    }

    public TriggerActionCollection EnterActions
    {
      get { return (TriggerActionCollection) _enterActionsProperty.GetValue(); }
    }

    public AbstractProperty ExitActionsProperty
    {
      get { return _exitActionsProperty; }
    }

    public TriggerActionCollection ExitActions
    {
      get { return (TriggerActionCollection) _exitActionsProperty.GetValue(); }
    }

    public AbstractProperty SettersProperty
    {
      get { return _settersProperty; }
    }

    public List<Setter> Setters
    {
      get { return (List<Setter>) _settersProperty.GetValue(); }
    }

    #endregion

    protected void ExecuteTriggerStartActions()
    {
      if (_triggerState)
        return;
      _triggerState = true;
      foreach (TriggerAction action in EnterActions)
        action.Execute(_element);
      foreach (Setter s in Setters)
        s.Set(_element);
    }

    protected void ExecuteTriggerEndActions()
    {
      if (!_triggerState)
        return;
      _triggerState = false;
      foreach (TriggerAction action in ExitActions)
        action.Execute(_element);
      foreach (Setter s in Setters)
        s.Restore(_element);
    }

    protected void TriggerIfValuesEqual(object triggerValue, object checkValue)
    {
      object obj = null;
      try
      {
        if ((triggerValue == null && checkValue == null) || (triggerValue != null && TypeConverter.Convert(checkValue, triggerValue.GetType(), out obj) &&
            Equals(triggerValue, obj)))
          ExecuteTriggerStartActions();
        else
          ExecuteTriggerEndActions();
      }
      finally
      {
        if (!ReferenceEquals(obj, checkValue))
          // If the conversion created a copy of the object, dispose it
          MPF.TryCleanupAndDispose(obj);
      }
    }
  }
}
