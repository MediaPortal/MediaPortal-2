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
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Visuals.Styles
{
  public class ControlTemplate : FrameworkTemplate
  {
    Property _triggerProperty;
    Property _targetTypeProperty;

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
      _triggerProperty = new Property(typeof(TriggerCollection), new TriggerCollection());
      _targetTypeProperty = new Property(typeof(Type), null);
    }

    public override object Clone()
    {
      ControlTemplate result = new ControlTemplate(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }

    #endregion

    #region Public properties
    /// <summary>
    /// Gets or sets the target type property.
    /// </summary>
    public Property TargetTypeProperty
    {
      get { return _targetTypeProperty; }
      set { _targetTypeProperty = value; }
    }

    /// <summary>
    /// Gets or sets the type of the target (we dont use it in our xaml engine, but real xaml requires it)
    /// FIXME: New XAML engine uses it!
    /// </summary>
    /// <value>The type of the target.</value>
    public Type TargetType
    {
      get { return _targetTypeProperty.GetValue() as Type; }
      set { _targetTypeProperty.SetValue(value); }
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
