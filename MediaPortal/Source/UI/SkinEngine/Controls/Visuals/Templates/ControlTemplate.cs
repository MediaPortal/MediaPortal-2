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

using System;
using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Templates
{
  /// <summary>
  /// Specifies the visual structure and behavioral aspects of a Control that
  /// can be shared across multiple instances of the control.
  /// </summary>
  public class ControlTemplate : TemplateWithTriggers
  {
    #region Protected fields

    protected AbstractProperty _targetTypeProperty;

    #endregion

    #region Ctor

    public ControlTemplate()
    {
      Init();
    }

    void Init()
    {
      _targetTypeProperty = new SProperty(typeof(Type), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ControlTemplate ct = (ControlTemplate) source;
      TargetType = ct.TargetType;
    }

    #endregion

    #region Public properties

    public AbstractProperty TargetTypeProperty
    {
      get { return _targetTypeProperty; }
    }

    public Type TargetType
    {
      get { return (Type) _targetTypeProperty.GetValue(); }
      set { _targetTypeProperty.SetValue(value); }
    }

    #endregion
  }
}
