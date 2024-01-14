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

using MediaPortal.Common;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Configuration.ConfigurationControllers;
using MediaPortal.UiComponents.Nereus.Settings;
using System;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Nereus.Models
{
  /// <summary>
  /// Workflow model for the  scroll configuration.
  /// </summary>
  public class ScrollConfigurationModel : IWorkflowModel
  {
    // Used to initialize the NumberSelectControllers that are used to
    // manage the AutoScrollSpeed and AutoScrollDelay settings
    protected class CustomNumberSetting : LimitedNumberSelect
    {
      public CustomNumberSetting(NumberType type, double value, double step, double lowerLimit, double upperLimit)
      {
        _type = type;
        _value = value;
        _step = step;
        _lowerLimit = lowerLimit;
        _upperLimit = upperLimit;
      }
    }

    public const string SCROLL_CONFIGURATION_MODEL_ID_STR = "AB34B067-DDA7-4D1C-A50E-A7BBFBBD2925";
    //private const double DEFAULT_SCROLL_SPEED = 20.0;
    //private const double DEFAULT_SCROLL_DELAY = 2.0;
    private const int MAX_SCROLL_SPEED = 60;
    private const int MAX_SCROLL_DELAY = 10;
    private const int SCROLL_SPEED_STEP = 5; // Step size for ScrollSpeed
    private const int SCROLL_DELAY_STEP = 1; // Step size for ScrollDelay

    #region Private fields

    protected AbstractProperty _autoScrollProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _manualScrollProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _enableLoopScrollingProperty = new WProperty(typeof(bool), true);

    protected NumberSelectController _scrollSpeedController;
    protected NumberSelectController _scrollDelayController;

    #endregion

    #region Public fields (can be used by the GUI)

    public AbstractProperty UseAutoScrollProperty
    {
      get { return _autoScrollProperty; }
    }
    public bool UseAutoScroll
    {
      get { return (bool)_autoScrollProperty.GetValue(); }
      set { _autoScrollProperty.SetValue(value); }
    }

    public AbstractProperty UseManualScrollProperty
    {
      get { return _manualScrollProperty; }
    }
    public bool UseManualScroll
    {
      get { return (bool)_manualScrollProperty.GetValue(); }
      set { _manualScrollProperty.SetValue(value); }
    }

    public AbstractProperty EnableLoopScrollingProperty
    {
      get { return _enableLoopScrollingProperty; }
    }
    public bool EnableLoopScrolling
    {
      get { return (bool)_enableLoopScrollingProperty.GetValue(); }
      set { _enableLoopScrollingProperty.SetValue(value); }
    }

    public NumberSelectController ScrollSpeedController
    {
      get { return _scrollSpeedController; }
    }

    public NumberSelectController ScrollDelayController
    {
      get { return _scrollDelayController; }
    }

    #endregion

    #region Private methods

    private void GetConfigFromSettings()
    {
      NereusSkinSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NereusSkinSettings>();
      EnableLoopScrolling = settings.EnableLoopScrolling;
      UseAutoScroll = settings.EnableAutoScrolling;
      UseManualScroll = !settings.EnableAutoScrolling;

      // Use a NumberSelectController to validate the settings, it contains all
      // required properties for input validation and up/down button enabling.
      _scrollSpeedController = new NumberSelectController();
      _scrollSpeedController.Initialize(
        new CustomNumberSetting(NumberSelect.NumberType.Integer, settings.AutoScrollSpeed, SCROLL_SPEED_STEP, 0, MAX_SCROLL_SPEED)
      );

      _scrollDelayController = new NumberSelectController();
      _scrollDelayController.Initialize(
        new CustomNumberSetting(NumberSelect.NumberType.Integer, settings.AutoScrollDelay, SCROLL_DELAY_STEP, 0, MAX_SCROLL_DELAY)
      );
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

      settings.EnableAutoScrolling = UseAutoScroll;
      settings.EnableLoopScrolling = EnableLoopScrolling;
      if (_scrollDelayController.IsValueValid)
        settings.AutoScrollSpeed = Convert.ToDouble(_scrollSpeedController.Value);
      if (_scrollDelayController.IsValueValid)
        settings.AutoScrollDelay = Convert.ToDouble(_scrollDelayController.Value);

      settingsManager.Save(settings);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(SCROLL_CONFIGURATION_MODEL_ID_STR); }
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
