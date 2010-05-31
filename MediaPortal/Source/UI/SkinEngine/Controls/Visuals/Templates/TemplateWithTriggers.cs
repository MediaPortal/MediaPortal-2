#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
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
      _triggerProperty = new SProperty(typeof(IList<TriggerBase>), new List<TriggerBase>());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      TemplateWithTriggers twt = (TemplateWithTriggers) source;
      IList<TriggerBase> triggers = Triggers;
      foreach (TriggerBase t in twt.Triggers)
        triggers.Add(copyManager.GetCopy(t));
    }

    #endregion

    #region Public properties

    public AbstractProperty TriggersProperty
    {
      get { return _triggerProperty; }
    }

    public IList<TriggerBase> Triggers
    {
      get { return (IList<TriggerBase>)_triggerProperty.GetValue(); }
    }

    #endregion

    #region Public methods

    public UIElement LoadContent(out IList<TriggerBase> triggers, out FinishBindingsDlgt finishBindings)
    {
      triggers = new List<TriggerBase>(Triggers.Count);
      if (_templateElement == null)
      {
        finishBindings = () => { };
        return null;
      }
      MpfCopyManager cm = new MpfCopyManager();
      cm.AddIdentity(this, null);
      UIElement result = cm.GetCopy(_templateElement);
      NameScope ns =  (NameScope) cm.GetCopy(_templateElement.TemplateNameScope);
      result.Resources.Merge(Resources);
      if (_names != null)
        foreach (KeyValuePair<string, object> nameRegistration in _names)
          ns.RegisterName(nameRegistration.Key, cm.GetCopy(nameRegistration.Value));
      foreach (TriggerBase t in Triggers)
      {
        TriggerBase trigger = cm.GetCopy(t);
        trigger.LogicalParent = result;
        trigger.Setup(result);
        triggers.Add(trigger);
      }
      cm.FinishCopy();
      IEnumerable<IBinding> deferredBindings = cm.GetDeferredBindings();
      finishBindings = () =>
        {
          MpfCopyManager.ActivateBindings(deferredBindings);
        };
      return result;
    }

    #endregion
  }
}