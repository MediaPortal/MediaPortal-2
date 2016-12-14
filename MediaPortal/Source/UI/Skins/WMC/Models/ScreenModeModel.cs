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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.WMCSkin.Models
{
  public class ScreenModeModel
  {
    public static readonly Guid MODEL_ID = new Guid("0E621AA5-0733-4E88-A0F0-887F8D73A055");

    protected AbstractProperty _currentScreenModeProperty;
    protected SettingsChangeWatcher<AppSettings> _settings;

    public ScreenModeModel()
    {
      Init();
      Attach();
    }

    public AbstractProperty CurrentScreenModeProperty
    {
      get { return _currentScreenModeProperty; }
    }

    public ScreenMode CurrentScreenMode
    {
      get { return (ScreenMode)_currentScreenModeProperty.GetValue(); }
      set { _currentScreenModeProperty.SetValue(value); }
    }

    void Init()
    {
      _currentScreenModeProperty = new WProperty(typeof(ScreenMode));
      _settings = new SettingsChangeWatcher<AppSettings>();
      _settings.SettingsChanged += OnSettingsChanged;
      CurrentScreenMode = _settings.Settings.ScreenMode;
    }

    void Attach()
    {
      _currentScreenModeProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _currentScreenModeProperty.Detach(OnPropertyChanged);
    }

    private void OnPropertyChanged(AbstractProperty property, object oldValue)
    {
      SetScreenMode(CurrentScreenMode);
    }

    void OnSettingsChanged(object sender, EventArgs e)
    {
      Detach();
      CurrentScreenMode = _settings.Settings.ScreenMode;
      Attach();
    }

    public void ToggleScreenMode()
    {
      ScreenMode currentMode = CurrentScreenMode;
      int nextMode = ((int)currentMode) + 1;
      int totalModes = Enum.GetNames(typeof(ScreenMode)).Length;
      ScreenMode newMode = (ScreenMode)(nextMode % totalModes);
      ServiceRegistration.Get<ILogger>().Info("ScreenModeModel: Switching screen mode from current '{0}' to '{1}'", currentMode, newMode);
      SetScreenMode(newMode);
    }

    protected void SetScreenMode(ScreenMode mode)
    {
      IScreenControl sc = ServiceRegistration.Get<IScreenControl>();
      sc.SwitchMode(mode);
    }
  }
}
