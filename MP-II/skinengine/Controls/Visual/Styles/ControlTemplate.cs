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
using System.Text;
using SkinEngine.Controls.Visuals;
using SkinEngine.Controls.Panels;
using SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Visuals.Styles
{
  public class ControlTemplate : FrameworkTemplate
  {
    Property _triggerProperty;

    #region ctor
    /// <summary>
    /// pecifies the visual structure and behavioral aspects of a Control that can be shared across multiple instances of the control.
    /// </summary>
    public ControlTemplate()
    {
      Init();
    }

    public ControlTemplate(ControlTemplate ct)
      : base(ct)
    {
      Init();
      foreach (Trigger t in ct.Triggers)
      {
        Triggers.Add((Trigger)t.Clone());
      }
    }

    void Init()
    {
      _triggerProperty = new Property(new TriggerCollection());
    }

    public override object Clone()
    {
      return new ControlTemplate(this);
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the type of the target (not used here, but required for real xaml)
    /// </summary>
    /// <value>The type of the target.</value>
    public string TargetType
    {
      get
      {
        return "";
      }
      set
      {
      }
    }

    /// <summary>
    /// Gets or sets the triggers property.
    /// </summary>
    /// <value>The triggers property.</value>
    public Property TriggersProperty
    {
      get
      {
        return _triggerProperty;
      }
      set
      {
        _triggerProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the triggers.
    /// </summary>
    /// <value>The triggers.</value>
    public TriggerCollection Triggers
    {
      get
      {
        return (TriggerCollection)_triggerProperty.GetValue();
      }
    }
    #endregion

  }
}
