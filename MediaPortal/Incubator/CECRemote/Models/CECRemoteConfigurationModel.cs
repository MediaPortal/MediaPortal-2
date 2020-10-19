#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common.Settings;
using MediaPortal.UiComponents.CECRemote.Settings;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UiComponents.CECRemote.Models
{
  /// <summary>
  /// Workflow model for the shutdown menu configuration.
  /// </summary>
  public class CECRemoteConfigurationModel : IWorkflowModel
  {
    public const string CECREMOTE_CONFIGURATION_MODEL_ID_STR = "14428A8D-3831-42A4-8A53-8BE3120054EA";

    #region Private fields

    private int _hdmiport;
        private string _deviceName;

    #endregion

    #region Private methods

    /// <summary>
    /// Loads shutdown actions from the settings.
    /// </summary>
    private void LoadSettings()
    {
      CECRemoteSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<CECRemoteSettings>();
      _hdmiport = settings.HDMIPort;
            _deviceName = settings.DeviceName;
    }


    #endregion

    #region Public methods (can be used by the GUI)

    public string HDMIPort
    {
      get { return _hdmiport.ToString(); }
      set { _hdmiport = Convert.ToInt16(value); }
    }

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      CECRemoteSettings settings = settingsManager.Load<CECRemoteSettings>();
      settings.HDMIPort = _hdmiport;
            settings.DeviceName = _deviceName;
      settingsManager.Save(settings);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(CECREMOTE_CONFIGURATION_MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Load settings
      LoadSettings();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // TODO
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
