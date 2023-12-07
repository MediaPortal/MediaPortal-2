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
  /// Workflow model for the header configuration.
  /// </summary>
  public class HeaderConfigurationModel : IWorkflowModel
  {
    public const string HEADER_CONFIGURATION_MODEL_ID_STR = "B5FE25BA-BDAC-44ea-BF82-F059C00052DC";

    #region Private fields

    protected AbstractProperty _showTimeProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _showDateProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _showTemperatureProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _showWeatherConditionProperty = new WProperty(typeof(bool), true);

    #endregion

    #region Public fields (can be used by the GUI)

    public AbstractProperty ShowTimeProperty
    {
      get { return _showTimeProperty; }
    }
    public bool ShowTime
    {
      get { return (bool)_showTimeProperty.GetValue(); }
      set { _showTimeProperty.SetValue(value); }
    }

    public AbstractProperty ShawDateProperty
    {
      get { return _showDateProperty; }
    }
    public bool ShowDate
    {
      get { return (bool)_showDateProperty.GetValue(); }
      set { _showDateProperty.SetValue(value); }
    }

    public AbstractProperty ShowTemperatureProperty
    {
      get { return _showTemperatureProperty; }
    }
    public bool ShowTemperature
    {
      get { return (bool)_showTemperatureProperty.GetValue(); }
      set { _showTemperatureProperty.SetValue(value); }
    }

    public AbstractProperty ShowWeatherConditionProperty
    {
      get { return _showWeatherConditionProperty; }
    }
    public bool ShowWeatherCondition
    {
      get { return (bool)_showWeatherConditionProperty.GetValue(); }
      set { _showWeatherConditionProperty.SetValue(value); }
    }

    #endregion

    #region Private methods

    private void GetConfigFromSettings()
    {
      NereusSkinSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NereusSkinSettings>();
      ShowTime = settings.ShowTime;
      ShowDate = settings.ShowDate;
      ShowTemperature = settings.ShowTemperature;
      ShowWeatherCondition = settings.ShowWeatherCondition;
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

      settings.ShowTime = ShowTime;
      settings.ShowDate = ShowDate;
      settings.ShowTemperature = ShowTemperature;
      settings.ShowWeatherCondition = ShowWeatherCondition;
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(HEADER_CONFIGURATION_MODEL_ID_STR); }
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
