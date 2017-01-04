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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers
{
  public class MultiTrigger : TriggerBase, IAddChild<Setter>
  {
    #region Protected fields

    protected AbstractProperty _conditionsProperty;

    #endregion

    #region Ctor

    public MultiTrigger()
    {
      Init();
    }

    void Init()
    {
      _conditionsProperty = new SProperty(typeof(ConditionCollection), new ConditionCollection());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      MultiTrigger mt = (MultiTrigger)source;
      foreach (Condition c in mt.Conditions)
        Conditions.Add(copyManager.GetCopy(c));
    }

    public override void Dispose()
    {
      DetachFromConditions();
      foreach (Condition c in Conditions)
        c.Dispose();
      base.Dispose();
    }

    #endregion

    #region Public properties

    public AbstractProperty ConditionsProperty
    {
      get { return _conditionsProperty; }
    }

    public ConditionCollection Conditions
    {
      get { return (ConditionCollection)_conditionsProperty.GetValue(); }
    }

    #endregion

    public override void Setup(UIElement element)
    {
      DetachFromConditions();
      base.Setup(element);
      foreach (Condition c in Conditions)
        c.Setup(element);
      AttachToConditions();
      TriggerIfConditionsMet();
    }

    public override void Reset()
    {
      DetachFromConditions();
      foreach (Condition c in Conditions)
        c.Reset();
      base.Reset();
    }

    protected void AttachToConditions()
    {
      foreach (Condition condition in Conditions)
        condition.TriggeredProperty.Attach(OnConditionChanged);
    }

    protected void DetachFromConditions()
    {
      foreach (Condition condition in Conditions)
        condition.TriggeredProperty.Detach(OnConditionChanged);
    }

    private void OnConditionChanged(AbstractProperty property, object oldValue)
    {
      TriggerIfConditionsMet();
    }

    protected void TriggerIfConditionsMet()
    {
      bool triggerState = false;
      foreach (Condition condition in Conditions)
      {
        triggerState = condition.Triggered;
        if (!triggerState)
          break;
      }

      if (triggerState)
        ExecuteTriggerStartActions();
      else
        ExecuteTriggerEndActions();
    }

    #region IAddChild Members

    public void AddChild(Setter s)
    {
      Setters.Add(s);
    }

    #endregion
  }
}