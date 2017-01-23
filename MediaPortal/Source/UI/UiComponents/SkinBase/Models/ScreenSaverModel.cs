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
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// This model provides information about the screen saver and mouse controls state. It provides a copy of the
  /// <see cref="IScreenControl.IsScreenSaverActive"/> and <see cref="IInputManager.IsMouseUsed"/> data, but as
  /// <see cref="AbstractProperty"/> to enable the screen controls to bind to the data.
  /// </summary>
  public class ScreenSaverModel : BaseTimerControlledModel
  {
    public const string STR_SCREENSAVER_MODEL_ID = "D4B7FEDD-243F-4afc-A8BE-28BBBF17D799";
    public static readonly Guid SCREENSAVER_MODEL_ID = new Guid(STR_SCREENSAVER_MODEL_ID);

    public const string RES_MEDIAPORTAL_2 = "[ScreenSaver.MediaPortal2]";

    protected AbstractProperty _isScreenSaverActiveProperty;
    protected AbstractProperty _isMouseUsedProperty;
    protected AbstractProperty _screenSaverTextProperty;

    public ScreenSaverModel() : base(true, 100)
    {
      _isScreenSaverActiveProperty = new WProperty(typeof(bool), false);
      _isMouseUsedProperty = new WProperty(typeof(bool), false);
      _screenSaverTextProperty = new WProperty(typeof(string), RES_MEDIAPORTAL_2); // Might be made configurable

      Update();
    }

    public Guid ModelId
    {
      get { return SCREENSAVER_MODEL_ID; }
    }

    protected override void Update()
    {
      IScreenControl screenControl = ServiceRegistration.Get<IScreenControl>();
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      IsScreenSaverActive = screenControl.IsScreenSaverActive;
      IsMouseUsed = inputManager.IsMouseUsed;
    }

    public AbstractProperty IsScreenSaverActiveProperty
    {
      get { return _isScreenSaverActiveProperty; }
    }

    public bool IsScreenSaverActive
    {
      get { return (bool) _isScreenSaverActiveProperty.GetValue(); }
      internal set { _isScreenSaverActiveProperty.SetValue(value); }
    }

    public AbstractProperty IsMouseUsedProperty
    {
      get { return _isMouseUsedProperty; }
    }

    public bool IsMouseUsed
    {
      get { return (bool) _isMouseUsedProperty.GetValue(); }
      internal set { _isMouseUsedProperty.SetValue(value); }
    }

    public AbstractProperty ScreenSaverTextProperty
    {
      get { return _screenSaverTextProperty; }
    }

    public string ScreenSaverText
    {
      get { return (string) _screenSaverTextProperty.GetValue(); }
      set { _screenSaverTextProperty.SetValue(value); }
    }
  }
}