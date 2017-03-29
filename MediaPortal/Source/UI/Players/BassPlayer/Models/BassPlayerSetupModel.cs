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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UI.Players.BassPlayer.OutputDevices;
using MediaPortal.UI.Players.BassPlayer.Settings;
using Un4seen.Bass;
using Un4seen.BassWasapi;

namespace MediaPortal.UI.Players.BassPlayer.Models
{
  /// <summary>
  /// Workflow model for the video background manager setup.
  /// </summary>
  public class BassPlayerSetupModel : IWorkflowModel
  {
    public const string BASS_PLAYER_SETUP_MODEL_ID_STR = "0EB2A664-5CA1-4B1E-B661-527004D6019B";
    public static Guid BASS_PLAYER_SETUP_MODEL_ID = new Guid(BASS_PLAYER_SETUP_MODEL_ID_STR);
    public const string KEY_GROUP = "Group";

    protected AbstractProperty _enableWASAPIExclusiveProperty;
    protected AbstractProperty _useDirectSoundProperty;
    protected AbstractProperty _directSoundDeviceProperty;
    protected AbstractProperty _useWASAPIProperty;
    protected AbstractProperty _WASAPIDeviceProperty;
    protected ItemsList _directSoundDevices = new ItemsList();
    protected ItemsList _WASAPIDevices = new ItemsList();

    public AbstractProperty UseWASAPIProperty
    {
      get { return _useWASAPIProperty; }
    }

    public bool UseWASAPI
    {
      get { return (bool)_useWASAPIProperty.GetValue(); }
      set { _useWASAPIProperty.SetValue(value); }
    }

    public AbstractProperty EnableWASAPIExclusiveProperty
    {
      get { return _enableWASAPIExclusiveProperty; }
    }

    public bool EnableWASAPIExclusive
    {
      get { return (bool)_enableWASAPIExclusiveProperty.GetValue(); }
      set { _enableWASAPIExclusiveProperty.SetValue(value); }
    }

    public AbstractProperty WASAPIDeviceProperty
    {
      get { return _WASAPIDeviceProperty; }
    }

    public string WASAPIDevice
    {
      get { return (string)_WASAPIDeviceProperty.GetValue(); }
      set { _WASAPIDeviceProperty.SetValue(value); }
    }

    public ItemsList WASAPIDevices
    {
      get { return _WASAPIDevices; }
    }

    public AbstractProperty UseDirectSoundProperty
    {
      get { return _useDirectSoundProperty; }
    }

    public bool UseDirectSound
    {
      get { return (bool)_useDirectSoundProperty.GetValue(); }
      set { _useDirectSoundProperty.SetValue(value); }
    }

    public AbstractProperty DirectSoundDeviceProperty
    {
      get { return _directSoundDeviceProperty; }
    }

    public string DirectSoundDevice
    {
      get { return (string)_directSoundDeviceProperty.GetValue(); }
      set { _directSoundDeviceProperty.SetValue(value); }
    }

    public ItemsList DirectSoundDevices
    {
      get { return _directSoundDevices; }
    }

    public BassPlayerSetupModel()
    {
      // Note: the BassLibraryManager is needed here to initalize the BassPlayer and plugins. The instance is not Disposed here, as this would lead to stopping current playback.
      var bassLibraryManager = BassLibraryManager.Get();
      _enableWASAPIExclusiveProperty = new SProperty(typeof(bool), false);
      _useDirectSoundProperty = new SProperty(typeof(bool), false);
      _useWASAPIProperty = new SProperty(typeof(bool), false);
      _directSoundDeviceProperty = new SProperty(typeof(string), string.Empty);
      _WASAPIDeviceProperty = new SProperty(typeof(string), string.Empty);
    }

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      BassPlayerSettings settings = settingsManager.Load<BassPlayerSettings>();
      settings.OutputMode = UseDirectSound ? OutputMode.DirectSound : OutputMode.WASAPI;
      settings.WASAPIExclusiveMode = EnableWASAPIExclusive;
      settings.WASAPIDevice = WASAPIDevice;
      settings.DirectSoundDevice = DirectSoundDevice;
      settingsManager.Save(settings);
    }

    private void InitModel()
    {
      BassPlayerSettings settings = Controller.GetSettings();
      EnableWASAPIExclusive = settings.WASAPIExclusiveMode;
      DirectSoundDevice = settings.DirectSoundDevice;
      WASAPIDevice = settings.WASAPIDevice;
      UseDirectSound = settings.OutputMode == OutputMode.DirectSound;
      UseWASAPI = settings.OutputMode == OutputMode.WASAPI;
      FillDirectSoundDeviceList();
      FillWASAPIDeviceList();
    }

    private void FillWASAPIDeviceList()
    {
      _WASAPIDevices.Clear();
      BassPlayerSettings settings = Controller.GetSettings();
      BASS_WASAPI_DEVICEINFO[] deviceDescriptions = BassWasapi.BASS_WASAPI_GetDeviceInfos();
      for (int i = 0; i < deviceDescriptions.Length; i++)
      {
        // Skip input devices, they have same name as output devices.
        BASS_WASAPI_DEVICEINFO deviceInfo = deviceDescriptions[i];
        bool skip = !WASAPIOutputDevice.IsValidDevice(deviceInfo);

        ServiceRegistration.Get<ILogger>().Debug("{5} WASAPI Device {0}: '{1}' Flags: {2} Device path: [{3}] Ch: {4}", i, deviceInfo.name, deviceInfo.flags, deviceInfo.id, deviceInfo.mixchans,
          skip ? "Skip" : "Use ");
        if (skip)
          continue;

        ListItem deviceItem = new ListItem(Consts.KEY_NAME, deviceInfo.name)
        {
          Selected = deviceInfo.name == settings.WASAPIDevice
        };
        deviceItem.SelectedProperty.Attach(delegate
        {
          var selected = _WASAPIDevices.FirstOrDefault(d => d.Selected);
          WASAPIDevice = selected != null ? selected.Labels[Consts.KEY_NAME].ToString() : string.Empty;
        });
        deviceItem.SetLabel(KEY_GROUP, "WASAPI");
        _WASAPIDevices.Add(deviceItem);
      }
      _WASAPIDevices.FireChange();
    }

    private void FillDirectSoundDeviceList()
    {
      _directSoundDevices.Clear();
      BassPlayerSettings settings = Controller.GetSettings();
      BASS_DEVICEINFO[] deviceDescriptions = Bass.BASS_GetDeviceInfos();
      for (int i = 0; i < deviceDescriptions.Length; i++)
      {
        BASS_DEVICEINFO deviceInfo = deviceDescriptions[i];

        ListItem deviceItem = new ListItem(Consts.KEY_NAME, deviceInfo.name)
        {
          Selected = deviceInfo.name == settings.DirectSoundDevice
        };
        deviceItem.SelectedProperty.Attach(delegate
        {
          var selected = DirectSoundDevices.FirstOrDefault(d => d.Selected);
          DirectSoundDevice = selected != null ? selected.Labels[Consts.KEY_NAME].ToString() : string.Empty;
        });
        deviceItem.SetLabel(KEY_GROUP, "DirectSound");
        _directSoundDevices.Add(deviceItem);
      }
      _directSoundDevices.FireChange();

    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return BASS_PLAYER_SETUP_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      InitModel();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Nothing to do here
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
