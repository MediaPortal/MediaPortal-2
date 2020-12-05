#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Nereus.Settings;

namespace MediaPortal.UiComponents.Nereus.Models
{
  /// <summary>
  /// Workflow model for the auto scroll configuration.
  /// </summary>
  public class AutoScrollConfigurationModel : IWorkflowModel
  {
    public const string AUTO_SCROLL_CONFIGURATION_MODEL_ID_STR = "AB34B067-DDA7-4D1C-A50E-A7BBFBBD2925";
    public const double DEFAULT_SCROLL_SPEED = 20.0;
    public const double DEFAULT_SCROLL_DELAY = 2.0;

    #region Private fields

    protected AbstractProperty _scrollSpeedProperty = new WProperty(typeof(string), string.Empty);
    protected AbstractProperty _scrollDelayProperty = new WProperty(typeof(string), string.Empty);
    protected AbstractProperty _autoScrollProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _manualScrollProperty = new WProperty(typeof(bool), true);

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

    public AbstractProperty ScrollSpeedProperty
    {
      get { return _scrollSpeedProperty; }
    }
    public string ScrollSpeed
    {
      get { return (string)_scrollSpeedProperty.GetValue(); }
      set { _scrollSpeedProperty.SetValue(value); }
    }

    public AbstractProperty ScrollDelayProperty
    {
      get { return _scrollDelayProperty; }
    }
    public string ScrollDelay
    {
      get { return (string)_scrollDelayProperty.GetValue(); }
      set { _scrollDelayProperty.SetValue(value); }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Loads SleepTimer-related configuration from the settings.
    /// </summary>
    private void GetConfigFromSettings()
    {
      NereusSkinSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NereusSkinSettings>();
      UseAutoScroll = settings.EnableAutoScrolling;
      UseManualScroll = !settings.EnableAutoScrolling;
      ScrollSpeed = Convert.ToInt32(settings.AutoScrollSpeed).ToString();
      ScrollDelay = Convert.ToInt32(settings.AutoScrollDelay).ToString();
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

      if (int.TryParse(ScrollSpeed, out var speed) && speed > 0)
        settings.AutoScrollSpeed = speed;
      else
        settings.AutoScrollSpeed = DEFAULT_SCROLL_SPEED;

      if (int.TryParse(ScrollDelay, out var delay) && delay > 0)
        settings.AutoScrollDelay = delay;
      else
        settings.AutoScrollDelay = DEFAULT_SCROLL_DELAY;

      settingsManager.Save(settings);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(AUTO_SCROLL_CONFIGURATION_MODEL_ID_STR); }
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
