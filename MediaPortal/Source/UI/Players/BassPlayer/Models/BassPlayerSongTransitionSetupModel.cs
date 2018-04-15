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
using MediaPortal.UI.Players.BassPlayer.Settings;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Configuration.ConfigurationControllers;
using MediaPortal.UI.Players.BassPlayer.Settings.Configuration;

namespace MediaPortal.UI.Players.BassPlayer.Models
{
  public class BassPlayerSongTransitionSetupModel: IWorkflowModel
  {
    public const string BASS_PLAYER_SONG_TRANSITION_SETUP_MODEL_ID_STR = "33BA5457-F2C1-4CD6-A443-9E150C90100E";
    public static Guid BASS_PLAYER_SONG_TRANSITION_SETUP_MODEL_ID = new Guid(BASS_PLAYER_SONG_TRANSITION_SETUP_MODEL_ID_STR);

    protected AbstractProperty _isNormalModeProperty;
    protected AbstractProperty _isGaplessModeProperty;
    protected AbstractProperty _isCrossFadingModeProperty;
    protected AbstractProperty _crossFadeDurationPropery;
    protected AbstractProperty _valueErrorTextProperty;
    protected AbstractProperty _isValidValueProperty;

    private NumberSelectController _numberSelectController;

    public AbstractProperty IsNormalModeProperty
    {
      get { return _isNormalModeProperty; }
    }

    public AbstractProperty IsGaplessModeProperty
    {
      get { return _isGaplessModeProperty; }
    }

    public AbstractProperty IsCrossFadingModeProperty
    {
      get { return _isCrossFadingModeProperty; }
    }

    public AbstractProperty CrossFadeDurationProperty
    {
      get { return _numberSelectController.ValueProperty; }
    }

    public AbstractProperty ValueErrorTextProperty
    {
      get { return _numberSelectController.ErrorTextProperty; }
    }

    public AbstractProperty IsValidValueProperty
    {
      get { return _numberSelectController.IsValueValidProperty; }
    }

    public bool IsNormalMode
    {
      get { return (bool)_isNormalModeProperty.GetValue(); }
      set { _isNormalModeProperty.SetValue(value);}
    }

    public bool IsGaplessMode
    {
      get { return (bool)_isGaplessModeProperty.GetValue(); }
      set { _isGaplessModeProperty.SetValue(value); }
    }

    public bool IsCrossFadingMode
    {
      get { return (bool)_isCrossFadingModeProperty.GetValue(); }
      set { _isCrossFadingModeProperty.SetValue(value); }
    }

    public bool IsUpEnabled
    {
      get { return _numberSelectController.IsUpEnabled; }
    }

    public bool IsDownEnabled
    {
      get { return _numberSelectController.IsDownEnabled; }
    }

    public void Up()
    {
      _numberSelectController.Up();
    }

    public void Down()
    {
      _numberSelectController.Down();
    }

    public double CrossFadeDuration
    {
      get { return double.Parse(_numberSelectController.Value, CultureInfo.InvariantCulture); }
      set { _numberSelectController.Value = Convert.ToString(value, CultureInfo.InvariantCulture); }
    }

    public string ValueErrorText
    {
      get { return _numberSelectController.ErrorText; }
    }

    public bool IsValidValue
    {
      get { return _numberSelectController.IsValueValid; }
    }

    public BassPlayerSongTransitionSetupModel()
    {
      _isNormalModeProperty = new SProperty(typeof(bool), true);
      _isGaplessModeProperty = new SProperty(typeof(bool), false);
      _isCrossFadingModeProperty = new SProperty(typeof(bool), false);
      _crossFadeDurationPropery = new SProperty(typeof(double));
      _valueErrorTextProperty = new WProperty(typeof(string), string.Empty);
      _isValidValueProperty = new WProperty(typeof(bool), true);
    }

    public void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      BassPlayerSettings settings = settingsManager.Load<BassPlayerSettings>();
    
      if (IsNormalMode)
      {
        settings.SongTransitionMode = PlaybackMode.Normal;
      }
      if (IsGaplessMode)
      {
        settings.SongTransitionMode = PlaybackMode.Gapless;
      }
      if (IsCrossFadingMode)
      {
        settings.SongTransitionMode = PlaybackMode.CrossFading;
      }

      settings.CrossFadeDurationSecs = CrossFadeDuration;
      settingsManager.Save(settings);
    }

    private void InitModel()
    {
      BassPlayerSettings settings = Controller.GetSettings();
      IsNormalMode = settings.SongTransitionMode == PlaybackMode.Normal;
      IsGaplessMode = settings.SongTransitionMode == PlaybackMode.Gapless;
      IsCrossFadingMode = settings.SongTransitionMode == PlaybackMode.CrossFading;

      CrossFadeDuration crossFadeDuration = new CrossFadeDuration();
      crossFadeDuration.Load();
      _numberSelectController = new NumberSelectController();
      _numberSelectController.Initialize(crossFadeDuration);
      CrossFadeDuration = settings.CrossFadeDurationSecs;
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return BASS_PLAYER_SONG_TRANSITION_SETUP_MODEL_ID; }
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
      return ScreenUpdateMode.AutoWorkflowManager;;
    }

    #endregion
  }
}
