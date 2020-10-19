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
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UiComponents.CECRemote.Settings;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UiComponents.CECRemote.Models
{
  /// <summary>
  /// Workflow model for the shutdown menu configuration.
  /// </summary>
  /// 
  public class CECPowerConfigurationModel : IWorkflowModel
  {
        public const string CECAR_CONFIGURATION_MODEL_ID_STR = "1e088edf-4f4f-4c45-bce1-6e49d2088f18";

        #region Private fields

        protected readonly AbstractProperty _isEnabledWakeDeviceProperty = new WProperty(typeof(bool), false);
        protected readonly AbstractProperty _isEnabledSendActiveProperty = new WProperty(typeof(bool), false);
        protected readonly AbstractProperty _isEnabledSleepDeviceProperty = new WProperty(typeof(bool), false);
        protected readonly AbstractProperty _isEnabledSendInactiveProperty = new WProperty(typeof(bool), false);
        #endregion

        #region Private methods

        /// <summary>
        /// Loads shutdown actions from the settings.
        /// </summary>



        #endregion

        #region Public methods (can be used by the GUI)

        public AbstractProperty IsEnabledWakeDeviceProperty
        {
            get { return _isEnabledWakeDeviceProperty; }
        }

        public AbstractProperty IsEnabledSendActiveProperty
        {
            get { return _isEnabledSendActiveProperty; }
        }

        public AbstractProperty IsEnabledSleepDeviceProperty
        {
            get { return _isEnabledSleepDeviceProperty; }
        }

        public AbstractProperty IsEnabledSendInactiveProperty
        {
            get { return _isEnabledSendInactiveProperty; }
        }


        public bool WakeDevice
        {
            get { return (bool)_isEnabledWakeDeviceProperty.GetValue(); }
            set { _isEnabledWakeDeviceProperty.SetValue(value); }
        }

        public bool SendActive
        {
            get { return (bool)_isEnabledSendActiveProperty.GetValue(); }
            set { _isEnabledSendActiveProperty.SetValue(value); }
        }

        public bool SleepDevice
        {
            get { return (bool)_isEnabledSleepDeviceProperty.GetValue(); }
            set { _isEnabledSleepDeviceProperty.SetValue(value); }
        }

        public bool SendInactive
        {
            get { return (bool)_isEnabledSendInactiveProperty.GetValue(); }
            set { _isEnabledSendInactiveProperty.SetValue(value); }
        }


        public void SaveSettings()
        {
            ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
            CECRemoteSettings settings = settingsManager.Load<CECRemoteSettings>();

            settings.WakeDevicesOnResume = WakeDevice;
            settings.ActivateSourceOnResume = SendActive;
            settings.StandbyDevicesOnSleep = SleepDevice;
            settings.InactivateSourceOnSleep = SendInactive;
            settingsManager.Save(settings);
        }


        #endregion

        #region IWorkflowModel implementation

        public Guid ModelId
    {
      get { return new Guid(CECAR_CONFIGURATION_MODEL_ID_STR); }
    }

       

        public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
            // Load settings
            ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
            CECRemoteSettings settings = settingsManager.Load<CECRemoteSettings>();

            WakeDevice = settings.WakeDevicesOnResume;
            SendActive = settings.ActivateSourceOnResume;
            SleepDevice = settings.StandbyDevicesOnSleep;
            SendInactive = settings.InactivateSourceOnSleep;

        }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
            ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
            CECRemoteSettings settings = settingsManager.Load<CECRemoteSettings>();

            settings.WakeDevicesOnResume = WakeDevice;
            settings.ActivateSourceOnResume = SendActive;
            settings.StandbyDevicesOnSleep = SleepDevice;
            settings.InactivateSourceOnSleep = SendInactive;
            settingsManager.Save(settings);

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
