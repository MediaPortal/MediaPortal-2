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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Utilities;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.BackgroundManager.Helper;
using MediaPortal.UiComponents.BackgroundManager.Settings;

namespace MediaPortal.UiComponents.BackgroundManager.Models
{
  /// <summary>
  /// Workflow model for the video background manager setup.
  /// </summary>
  public class BackgroundManagerSetupModel : IWorkflowModel
  {
    public const string BACKGROUND_SETUP_MODEL_ID_STR = "5054832A-C20D-448E-AA08-A8B2826D1C31";
    public const string RES_HEADER_CHOOSE_VIDEO = "[Settings.Appearance.Skin.Background.Setup.SelectVideo]";

    protected AbstractProperty _backgroundVideoFilenameProperty;
    protected AbstractProperty _isEnabledProperty;

    protected PathBrowserCloseWatcher _pathBrowserCloseWatcher = null;

    public AbstractProperty BackgroundVideoFilenameProperty
    {
      get { return _backgroundVideoFilenameProperty; }
    }

    public string BackgroundVideoFilename
    {
      get { return (string) _backgroundVideoFilenameProperty.GetValue(); }
      set { _backgroundVideoFilenameProperty.SetValue(value); }
    }

    public AbstractProperty IsEnabledProperty
    {
      get { return _isEnabledProperty; }
    }

    public bool IsEnabled
    {
      get { return (bool) _isEnabledProperty.GetValue(); }
      set { _isEnabledProperty.SetValue(value); }
    }

    public BackgroundManagerSetupModel()
    {
      _backgroundVideoFilenameProperty = new SProperty(typeof(string), string.Empty);
      _isEnabledProperty = new SProperty(typeof(bool), false);
    }

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      BackgroundManagerSettings settings = settingsManager.Load<BackgroundManagerSettings>();
      settings.VideoBackgroundFileName = BackgroundVideoFilename;
      settings.EnableVideoBackground = IsEnabled;
      settingsManager.Save(settings);
    }

    public void ChooseBackgroundVideo()
    {
      string videoFilename = BackgroundVideoFilename;
      string initialPath = string.IsNullOrEmpty(videoFilename) ? null : DosPathHelper.GetDirectory(videoFilename);
      Guid dialogHandle = ServiceRegistration.Get<IPathBrowser>().ShowPathBrowser(RES_HEADER_CHOOSE_VIDEO, true, false,
          string.IsNullOrEmpty(initialPath) ? null : LocalFsResourceProviderBase.ToResourcePath(initialPath),
          path =>
          {
            string choosenPath = LocalFsResourceProviderBase.ToDosPath(path.LastPathSegment.Path);
            if (string.IsNullOrEmpty(choosenPath))
              return false;

            return MediaItemHelper.IsValidVideo(MediaItemHelper.CreateMediaItem(choosenPath));
          });

      if (_pathBrowserCloseWatcher != null)
        _pathBrowserCloseWatcher.Dispose();

      _pathBrowserCloseWatcher = new PathBrowserCloseWatcher(this, dialogHandle, choosenPath =>
          {
            BackgroundVideoFilename = LocalFsResourceProviderBase.ToDosPath(choosenPath);
          }, 
          null);
    }

    private void InitModel()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      BackgroundManagerSettings settings = settingsManager.Load<BackgroundManagerSettings>();
      BackgroundVideoFilename = settings.VideoBackgroundFileName;
      IsEnabled = settings.EnableVideoBackground;
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(BACKGROUND_SETUP_MODEL_ID_STR); }
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