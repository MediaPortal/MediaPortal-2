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
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.Utilities.DeepCopy;
using System.Collections.Generic;

namespace MediaPortal.SkinEngine.Controls.Visuals.Templates
{
  /// <summary>
  /// Specifies the visual structure and behavioral aspects of a Control that
  /// can be shared across multiple instances of the control.
  /// </summary>
  public class ControlTemplate : FrameworkTemplate
  {
    #region Private fields

    Property _triggerProperty;
    Property _targetTypeProperty;

    #endregion

    #region Ctor

    public ControlTemplate()
    {
      Init();
    }

    void Init()
    {
      _triggerProperty = new Property(typeof(IList<TriggerBase>), new List<TriggerBase>());
      _targetTypeProperty = new Property(typeof(Type), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ControlTemplate ct = (ControlTemplate) source;
      TargetType = copyManager.GetCopy(ct.TargetType);
      foreach (TriggerBase t in ct.Triggers)
        Triggers.Add(copyManager.GetCopy(t));
    }

    #endregion

    #region Public properties

    public Property TargetTypeProperty
    {
      get { return _targetTypeProperty; }
    }

    public Type TargetType
    {
      get { return _targetTypeProperty.GetValue() as Type; }
      set { _targetTypeProperty.SetValue(value); }
    }

    public Property TriggersProperty
    {
      get { return _triggerProperty; }
    }

    public IList<TriggerBase> Triggers
    {
      get { return (IList<TriggerBase>)_triggerProperty.GetValue(); }
    }

    #endregion
  }
}
