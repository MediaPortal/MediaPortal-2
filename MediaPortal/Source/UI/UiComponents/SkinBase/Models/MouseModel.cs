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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Models;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// Model which provides data about the current mouse usage.
  /// </summary>
  public class MouseModel : BaseTimerControlledModel
  {
    public const string MOUSE_MODEL_ID_STR = "CA6428A7-A6E2-4dd3-9661-F89CEBAF8E62";
    public static readonly Guid MOUSE_MODEL_ID = new Guid(MOUSE_MODEL_ID_STR);

    #region Protected fields

    protected AbstractProperty _isMouseUsedProperty = new WProperty(typeof(bool), false);

    #endregion

    public MouseModel() : base(true, 50)
    {
      Update();
    }

    public Guid ModelId
    {
      get { return MOUSE_MODEL_ID; }
    }

    protected override void Update()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      IsMouseUsed = inputManager.IsMouseUsed;
    }

    public AbstractProperty IsMouseUsedProperty
    {
      get { return _isMouseUsedProperty; }
    }

    public bool IsMouseUsed
    {
      get { return (bool) _isMouseUsedProperty.GetValue(); }
      set { _isMouseUsedProperty.SetValue(value); }
    }
  }
}