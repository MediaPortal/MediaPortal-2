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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Templates
{
  public abstract class TemplateWithTriggers : FrameworkTemplate
  {
    #region Protected fields

    protected AbstractProperty _triggerProperty;

    #endregion

    #region Ctor

    protected TemplateWithTriggers()
    {
      Init();
    }

    void Init()
    {
      _triggerProperty = new SProperty(typeof(TriggerCollection), new TriggerCollection());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      TemplateWithTriggers twt = (TemplateWithTriggers) source;
      TriggerCollection triggers = Triggers;
      foreach (TriggerBase t in twt.Triggers)
        triggers.Add(copyManager.GetCopy(t));
    }

    public override void Dispose()
    {
      foreach (TriggerBase triggerBase in Triggers)
        triggerBase.Dispose();
      base.Dispose();
    }

    #endregion

    #region Public properties

    public AbstractProperty TriggersProperty
    {
      get { return _triggerProperty; }
    }

    public TriggerCollection Triggers
    {
      get { return (TriggerCollection)_triggerProperty.GetValue(); }
    }

    #endregion

    #region Public methods

    public UIElement LoadContent(UIElement triggerParent)
    {
      if (_templateElement == null)
        return null;
      MpfCopyManager cm = new MpfCopyManager();
      cm.AddIdentity(this, null);
      FrameworkElement result = cm.GetCopy(_templateElement);
      NameScope ns =  (NameScope) cm.GetCopy(_templateElement.TemplateNameScope);
      result.Resources.Merge(Resources);
      if (_names != null)
        foreach (KeyValuePair<string, object> nameRegistration in _names)
          ns.RegisterName(nameRegistration.Key, cm.GetCopy(nameRegistration.Value));
      triggerParent.UninitializeTriggers();
      ICollection<TriggerBase> triggers = triggerParent.Triggers;
      foreach (TriggerBase t in Triggers)
      {
        TriggerBase trigger = cm.GetCopy(t);
        triggers.Add(trigger);
        // Trigger will automatically be set-up (_initializeTriggers is initially set to true in result)
      }
      cm.FinishCopy();
      // Setting the logical parent has to be done after the copy process has finished - else the logical parent will be overridden
      foreach (TriggerBase t in triggers)
        t.LogicalParent = result;
      return result;
    }

    #endregion
  }
}
