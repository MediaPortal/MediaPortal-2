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
using System.IO;
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Settings;
using MediaPortal.UI.SkinEngine.Settings;
using MediaPortal.UiComponents.BlueVision.Settings;

namespace MediaPortal.UiComponents.BlueVision.Models
{
  public class SetupModel : IWorkflowModel
  {
    #region Consts

    public const string SETUP_MODEL_ID_STR = "92A16CDF-480B-4A40-9C76-7F9B0779319F";
    public const string SPLASH_SCREEN_NAME = "SplashScreen.jpg";

    public readonly static Guid SETUP_MODEL_ID = new Guid(SETUP_MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected readonly AbstractProperty _disableAutoSelectionProperty = new WProperty(typeof(bool), false);
    protected readonly AbstractProperty _disableHomeTabProperty = new WProperty(typeof(bool), false);
    protected readonly AbstractProperty _useAlternativeSplashscreenProperty = new WProperty(typeof(bool), false);
    protected readonly AbstractProperty _resetGroupLayoutProperty = new WProperty(typeof(bool), false);

    #endregion

    #region Public properties - Bindable Data

    public AbstractProperty DisableAutoSelectionProperty
    {
      get { return _disableAutoSelectionProperty; }
    }

    public bool DisableAutoSelection
    {
      get { return (bool)_disableAutoSelectionProperty.GetValue(); }
      set { _disableAutoSelectionProperty.SetValue(value); }
    }

    public AbstractProperty DisableHomeTabProperty
    {
      get { return _disableHomeTabProperty; }
    }

    public bool DisableHomeTab
    {
      get { return (bool)_disableHomeTabProperty.GetValue(); }
      set { _disableHomeTabProperty.SetValue(value); }
    }

    public AbstractProperty UseAlternativeSplashscreenProperty
    {
      get { return _useAlternativeSplashscreenProperty; }
    }

    public bool UseAlternativeSplashscreen
    {
      get { return (bool)_useAlternativeSplashscreenProperty.GetValue(); }
      set { _useAlternativeSplashscreenProperty.SetValue(value); }
    }

    public AbstractProperty ResetGroupLayoutProperty
    {
      get { return _resetGroupLayoutProperty; }
    }

    public bool ResetGroupLayout
    {
      get { return (bool)_resetGroupLayoutProperty.GetValue(); }
      set { _resetGroupLayoutProperty.SetValue(value); }
    }

    #endregion

    #region Public methods - Commands

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      var settings = ServiceRegistration.Get<ISettingsManager>().Load<MenuSettings>();
      settings.DisableHomeTab = DisableHomeTab;
      settings.DisableAutoSelection = DisableAutoSelection;
      settings.UseAlternativeSplashscreen = UseAlternativeSplashscreen;

      // Used to restore the default layout from settings. When the MenuItems are cleared, the defaults will be applied.
      if (ResetGroupLayout)
        settings.MenuItems.Clear();

      // Save
      settingsManager.Save(settings);

      var skinSettings = ServiceRegistration.Get<ISettingsManager>().Load<UI.SkinEngine.Settings.SkinSettings>();
      var startupSettings = ServiceRegistration.Get<ISettingsManager>().Load<StartupSettings>();
      List<string> paths = new List<string>
      {
        string.Format("Plugins\\BlueVision\\Skin\\BlueVision\\Themes\\{0}\\Images\\{1}", skinSettings.Theme, SPLASH_SCREEN_NAME),
        string.Format("Plugins\\BlueVision\\Skin\\BlueVision\\Images\\{0}", SPLASH_SCREEN_NAME),
      };

      startupSettings.AlternativeSplashScreen = string.Empty;
      if (UseAlternativeSplashscreen)
      {
        string startupPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        foreach (string path in paths)
        {
          string testPath = Path.Combine(startupPath, path);
          if (File.Exists(testPath))
          {
            startupSettings.AlternativeSplashScreen = path;
            break;
          }
        }
      }
      settingsManager.Save(startupSettings);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return SETUP_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      var settings = ServiceRegistration.Get<ISettingsManager>().Load<UI.SkinEngine.Settings.SkinSettings>();
      return settings.Skin == "BlueVision";
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Load settings
      var settings = ServiceRegistration.Get<ISettingsManager>().Load<MenuSettings>();
      DisableHomeTab = settings.DisableHomeTab;
      DisableAutoSelection = settings.DisableAutoSelection;
      UseAlternativeSplashscreen = settings.UseAlternativeSplashscreen;
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
