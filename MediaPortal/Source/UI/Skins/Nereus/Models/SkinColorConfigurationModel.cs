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
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Nereus.Settings;

namespace MediaPortal.UiComponents.Nereus.Models
{
  /// <summary>
  /// Workflow model for the skin color configuration.
  /// </summary>
  public class SkinColorConfigurationModel : IWorkflowModel
  {
    public const string SKIN_COLOR_CONFIGURATION_MODEL_ID_STR = "03C73355-E01C-4d30-AE69-14408ADB2174";

    #region Private fields

    protected AbstractProperty _torquoiseProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _yellowProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _orangeProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _redProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _purpleProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _blueProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _greyProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _greenProperty = new WProperty(typeof(bool), true);

    #endregion

    #region Public fields (can be used by the GUI)

    public AbstractProperty UseTorquoiseProperty
    {
      get { return _torquoiseProperty; }
    }
    public bool UseTorquoise
    {
      get { return (bool)_torquoiseProperty.GetValue(); }
      set { _torquoiseProperty.SetValue(value); }
    }

    public AbstractProperty UseYellowProperty
    {
      get { return _yellowProperty; }
    }
    public bool UseYellow
    {
      get { return (bool)_yellowProperty.GetValue(); }
      set { _yellowProperty.SetValue(value); }
    }

    public AbstractProperty UseOrangeProperty
    {
      get { return _orangeProperty; }
    }
    public bool UseOrange
    {
      get { return (bool)_orangeProperty.GetValue(); }
      set { _orangeProperty.SetValue(value); }
    }

    public AbstractProperty UseRedProperty
    {
      get { return _redProperty; }
    }
    public bool UseRed
    {
      get { return (bool)_redProperty.GetValue(); }
      set { _redProperty.SetValue(value); }
    }

    public AbstractProperty UsePurpleProperty
    {
      get { return _purpleProperty; }
    }
    public bool UsePurple
    {
      get { return (bool)_purpleProperty.GetValue(); }
      set { _purpleProperty.SetValue(value); }
    }

    public AbstractProperty UseGreenProperty
    {
      get { return _greenProperty; }
    }
    public bool UseGreen
    {
      get { return (bool)_greenProperty.GetValue(); }
      set { _greenProperty.SetValue(value); }
    }

    public AbstractProperty UseGreyProperty
    {
      get { return _greyProperty; }
    }
    public bool UseGrey
    {
      get { return (bool)_greyProperty.GetValue(); }
      set { _greyProperty.SetValue(value); }
    }

    public AbstractProperty UseBlueProperty
    {
      get { return _blueProperty; }
    }
    public bool UseBlue
    {
      get { return (bool)_blueProperty.GetValue(); }
      set { _blueProperty.SetValue(value); }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Loads SleepTimer-related configuration from the settings.
    /// </summary>
    private void GetConfigFromSettings()
    {
      NereusSkinSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NereusSkinSettings>();
      UseTorquoise = settings.UseTorquoise;
      UseYellow = settings.UseYellow;
      UseOrange = settings.UseOrange;
      UseRed = settings.UseRed;
      UsePurple = settings.UsePurple;
      UseGreen = settings.UseGreen;
      UseBlue = settings.UseBlue;
      UseGrey = settings.UseGrey;
      //UseAutoScroll = settings.EnableAutoScrolling;
      //UseManualScroll = !settings.EnableAutoScrolling;
      //ScrollSpeed = Convert.ToInt32(settings.AutoScrollSpeed).ToString();
      //ScrollDelay = Convert.ToInt32(settings.AutoScrollDelay).ToString();
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

      settings.UseTorquoise = UseTorquoise;
      settings.UseYellow = UseYellow;
      settings.UseOrange = UseOrange;
      settings.UseRed = UseRed;
      settings.UsePurple = UsePurple;
      settings.UseGreen = UseGreen;
      settings.UseBlue = UseBlue;
      settings.UseGrey = UseGrey;

      //if (int.TryParse(ScrollSpeed, out var speed) && speed > 0)
      //  settings.AutoScrollSpeed = speed;
      //else
      //  settings.AutoScrollSpeed = DEFAULT_SCROLL_SPEED;

      //if (int.TryParse(ScrollDelay, out var delay) && delay > 0)
      //   settings.AutoScrollDelay = delay;
      //else
      //  settings.AutoScrollDelay = DEFAULT_SCROLL_DELAY;

      settingsManager.Save(settings);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(SKIN_COLOR_CONFIGURATION_MODEL_ID_STR); }
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
