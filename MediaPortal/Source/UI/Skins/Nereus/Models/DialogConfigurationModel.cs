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
  /// Workflow model for the dialog configuration.
  /// </summary>
  public class DialogConfigurationModel : IWorkflowModel
  {
    public const string DIALOG_CONFIGURATION_MODEL_ID_STR = "5C314F18-6A81-47b7-91DB-02DBCF5176F0";
    public const double DEFAULT_DIALOG_BACKGROUND_TRANSPARENCY = 0.85;
    

    #region Private fields

    protected AbstractProperty _dialogBackgroundOpacityProperty = new WProperty(typeof(string), string.Empty);
    protected AbstractProperty _useRoundedDialogCornersProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _useNoColorProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _useWhiteColorProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _useFocusColorProperty = new WProperty(typeof(bool), true);
    protected AbstractProperty _useTransparencyProperty = new WProperty(typeof(bool), true);

    #endregion

    #region Public fields (can be used by the GUI)

    public AbstractProperty UseRoundedDialogCornersProperty
    {
      get { return _useRoundedDialogCornersProperty; }
    }
    public bool UseRoundedDialogCorners
    {
      get { return (bool)_useRoundedDialogCornersProperty.GetValue(); }
      set { _useRoundedDialogCornersProperty.SetValue(value); }
    }

    public AbstractProperty UseTransparencyProperty
    {
      get { return _useTransparencyProperty; }
    }
    public bool UseTransparency
    {
      get { return (bool)_useTransparencyProperty.GetValue(); }
      set { _useTransparencyProperty.SetValue(value); }
    }

    public AbstractProperty UseNoColorProperty
    {
      get { return _useNoColorProperty; }
    }
    public bool UseNoColor
    {
      get { return (bool)_useNoColorProperty.GetValue(); }
      set { _useNoColorProperty.SetValue(value); }
    }

    public AbstractProperty UseWhiteColorProperty
    {
      get { return _useWhiteColorProperty; }
    }
    public bool UseWhiteColor
    {
      get { return (bool)_useWhiteColorProperty.GetValue(); }
      set { _useWhiteColorProperty.SetValue(value); }
    }

    public AbstractProperty UseFocusColorProperty
    {
      get { return _useFocusColorProperty; }
    }
    public bool UseFocusColor
    {
      get { return (bool)_useFocusColorProperty.GetValue(); }
      set { _useFocusColorProperty.SetValue(value); }
    }

    public AbstractProperty DialogBackgroundOpacityProperty
    {
      get { return _dialogBackgroundOpacityProperty; }
    }
    public string DialogBackgroundOpacity
    {
      get { return (string)_dialogBackgroundOpacityProperty.GetValue(); }
      set { _dialogBackgroundOpacityProperty.SetValue(value); }
    }

    #endregion

    #region Private methods

    private void GetConfigFromSettings()
    {
      NereusSkinSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NereusSkinSettings>();
      UseRoundedDialogCorners = settings.UseRoundedDialogCorners;
      UseTransparency = settings.UseTransparency;
      UseNoColor = settings.UseNoColor;
      UseWhiteColor = settings.UseWhiteColor;
      UseFocusColor = settings.UseFocusColor;
      DialogBackgroundOpacity = Convert.ToDouble(settings.DialogBackgroundOpacity).ToString();
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

      settings.UseRoundedDialogCorners = UseRoundedDialogCorners;
      settings.UseTransparency = UseTransparency;
      settings.UseNoColor = UseNoColor;
      settings.UseWhiteColor = UseWhiteColor;
      settings.UseFocusColor = UseFocusColor;

      if (double.TryParse(DialogBackgroundOpacity, out var opacity) && opacity > 0.7 && opacity < 1.0)
        settings.DialogBackgroundOpacity = opacity;
      else
        settings.DialogBackgroundOpacity = DEFAULT_DIALOG_BACKGROUND_TRANSPARENCY;

      settingsManager.Save(settings);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(DIALOG_CONFIGURATION_MODEL_ID_STR); }
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
