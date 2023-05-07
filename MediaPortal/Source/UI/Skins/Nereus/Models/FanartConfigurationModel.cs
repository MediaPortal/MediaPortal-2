#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Nereus.Settings;

namespace MediaPortal.UiComponents.Nereus.Models
{
  /// <summary>
  /// Workflow model for the fanart configuration.
  /// </summary>
  public class FanartConfigurationModel : IWorkflowModel
  {
    public const string FANART_CONFIGURATION_MODEL_ID_STR = "543CFC2F-816D-48ae-BEE3-BB3B85876505";
    public const double DEFAULT_FANART_OVERLAY_OPACITY = 0.85;
    

    #region Private fields

    protected AbstractProperty _fanartOverlayOpacityProperty = new WProperty(typeof(string), string.Empty);
    protected AbstractProperty _enableFanartProperty = new WProperty(typeof(bool), true);

    #endregion

    #region Public fields (can be used by the GUI)

    public AbstractProperty EnableFanartProperty
    {
      get { return _enableFanartProperty; }
    }
    public bool EnableFanart
    {
      get { return (bool)_enableFanartProperty.GetValue(); }
      set { _enableFanartProperty.SetValue(value); }
    }

    public AbstractProperty FanartOverlayOpacityProperty
    {
      get { return _fanartOverlayOpacityProperty; }
    }
    public string FanartOverlayOpacity
    {
      get { return (string)_fanartOverlayOpacityProperty.GetValue(); }
      set { _fanartOverlayOpacityProperty.SetValue(value); }
    }

    #endregion

    #region Private methods

    private void GetConfigFromSettings()
    {
      NereusSkinSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NereusSkinSettings>();
      EnableFanart = settings.EnableFanart;
      FanartOverlayOpacity = Convert.ToDouble(settings.FanartOverlayOpacity).ToString();
    }

    #endregion

    #region Public methods (can be used by the GUI)

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      NereusSkinSettings settings = settingsManager.Load<NereusSkinSettings>();

      settings.EnableFanart = EnableFanart;

      if (double.TryParse(FanartOverlayOpacity, out var opacity) && opacity >= 0.7 && opacity <= 1.0)
        settings.FanartOverlayOpacity = opacity;
      else
        settings.FanartOverlayOpacity = DEFAULT_FANART_OVERLAY_OPACITY;

      settingsManager.Save(settings);
    }

    public void Refresh()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.Reload();
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(FANART_CONFIGURATION_MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Load settings
      GetConfigFromSettings();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do here
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
