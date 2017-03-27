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
using System.Globalization;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.UiComponents.Configuration.ConfigurationControllers;
using MediaPortal.UI.Players.BassPlayer.Settings;
using MediaPortal.UI.Players.BassPlayer.Settings.Configuration;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UI.Players.BassPlayer.Models
{
  public class BassPlayerPlaybackBufferSetupModel : IWorkflowModel
  {
    public const string BASS_PLAYER_PLAYBACK_BUFFER__SETUP_MODEL_ID_STR = "1EA86870-CB4D-4B24-AEC1-13D018C92410";
    public static Guid BASS_PLAYER_PLAYBACK_BUFFER_SETUP_MODEL_ID = new Guid(BASS_PLAYER_PLAYBACK_BUFFER__SETUP_MODEL_ID_STR);

    protected AbstractProperty _directSoundBufferSizeProperty;
    protected AbstractProperty _wasapiBufferSizeProperty;
    protected AbstractProperty _dsErrorTextProperty;
    protected AbstractProperty _wasapiErrorTextProperty;
    protected AbstractProperty _isDsValidProperty;
    protected AbstractProperty _isWasapiValidProperty;

    private NumberSelectController _directSoundBufferSizeController;
    private NumberSelectController _wasapiBufferSizeController;

    public AbstractProperty DirectSoundBufferSizeProperty
    {
      get { return _directSoundBufferSizeController.ValueProperty; }
    }

    public AbstractProperty WasapiBufferSizeProperty
    {
      get { return _wasapiBufferSizeController.ValueProperty; }
    }

    public AbstractProperty DsErrorTextProperty
    {
      get { return _directSoundBufferSizeController.ErrorTextProperty; }
    }

    public AbstractProperty WasapiErrorTextProperty
    {
      get { return _wasapiBufferSizeController.ErrorTextProperty; }
    }

    public AbstractProperty IsDsValidProperty
    {
      get { return _directSoundBufferSizeController.IsValueValidProperty; }
    }

    public AbstractProperty IsWasapiValidProperty
    {
      get { return _wasapiBufferSizeController.IsValueValidProperty; }
    }

    public bool IsDirectSoundBufferSizeUpEnabled
    {
      get { return _directSoundBufferSizeController.IsUpEnabled; }
    }

    public bool IsWasapiBufferSizeUpEnabled
    {
       get { return _wasapiBufferSizeController.IsUpEnabled; }
    }

    public bool IsDirectSoundBufferSizeDownEnabled
    {
      get { return _directSoundBufferSizeController.IsDownEnabled; }
    }

    public bool IsWasapiBufferSizeDownEnabled
    {
      get { return _wasapiBufferSizeController.IsDownEnabled; }
    }

    public void DirectSoundBufferSizeUp()
    {
      _directSoundBufferSizeController.Up();
    }

    public void WasapiBufferSizeUp()
    {
      _wasapiBufferSizeController.Up();
    }

    public void DirectSoundBufferSizeDown()
    {
      _directSoundBufferSizeController.Down();
    }

    public void WasapiBufferSizeDown()
    {
     _wasapiBufferSizeController.Down();
    }

    public string DsErrorText
    {
      get { return _directSoundBufferSizeController.ErrorText; }
    }

    public string WasapiErrorText
    {
      get { return _wasapiBufferSizeController.ErrorText; }
    }

    public bool IsDsValid
    {
      get { return _directSoundBufferSizeController.IsValueValid; }
    }

    public bool IsWasapiValid
    {
      get { return _wasapiBufferSizeController.IsValueValid; }
    }

    public int DirectSoundBufferSize
    {
      get { return Int32.Parse(_directSoundBufferSizeController.Value, CultureInfo.InvariantCulture); }
      set { _directSoundBufferSizeController.Value = Convert.ToString(value, CultureInfo.InvariantCulture); }
    }

    public int WasapiBufferSize
    {
      get { return Int32.Parse(_wasapiBufferSizeController.Value, CultureInfo.InvariantCulture); }
      set { _wasapiBufferSizeController.Value = Convert.ToString(value, CultureInfo.InvariantCulture); }
    }

    public BassPlayerPlaybackBufferSetupModel()
    {
      _directSoundBufferSizeProperty = new SProperty(typeof(int));
      _wasapiBufferSizeProperty = new SProperty(typeof(int));
      _dsErrorTextProperty = new WProperty(typeof(string), string.Empty);
      _wasapiErrorTextProperty = new WProperty(typeof(string), string.Empty);
      _isDsValidProperty = new WProperty(typeof(bool), true);
      _isWasapiValidProperty = new WProperty(typeof(bool), true);
    }

    public void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      BassPlayerSettings settings = settingsManager.Load<BassPlayerSettings>();

      settings.DirectSoundBufferSizeMilliSecs = DirectSoundBufferSize;
      settings.PlaybackBufferSizeMilliSecs = WasapiBufferSize;
      settingsManager.Save(settings);
    }

    private void InitModel()
    {
      BassPlayerSettings settings = Controller.GetSettings();

      DirectSoundBuffer directSoundBuffer = new DirectSoundBuffer();
      directSoundBuffer.Load();
      _directSoundBufferSizeController = new NumberSelectController();
      _directSoundBufferSizeController.Initialize(directSoundBuffer);

      PlaybackBufferSize wasapiBuffer = new PlaybackBufferSize();
      wasapiBuffer.Load();
      _wasapiBufferSizeController = new NumberSelectController();
      _wasapiBufferSizeController.Initialize(wasapiBuffer);
      
      DirectSoundBufferSize = settings.DirectSoundBufferSizeMilliSecs;
      WasapiBufferSize = settings.PlaybackBufferSizeMilliSecs;
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return BASS_PLAYER_PLAYBACK_BUFFER_SETUP_MODEL_ID; }
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
      return ScreenUpdateMode.AutoWorkflowManager; ;
    }

    #endregion
  }
}
