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
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Geometry;
using MediaPortal.UI.SkinEngine.GUI;
using MediaPortal.Common.PluginManager;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.UI.SkinEngine
{
  public class SkinEnginePlugin: IPluginStateTracker, ISkinEngine
  {
    #region Protected fields

    protected const string HOME_STATE_STR = "{7F702D9C-F2DD-42da-9ED8-0BA92F07787F}";

    protected MainForm _mainForm = null;
    protected ScreenManager _screenManager = null;
    protected bool _screenSaverWasEnabled = false;

    #endregion

    protected static void RegisterGlobalKeyBindings()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.AddKeyBinding(Key.Escape, () =>
        {
          // Close dialog or switch to previous workflow state
          IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
          if (screenManager.IsDialogVisible)
            screenManager.CloseTopmostDialog();
          else
            ServiceRegistration.Get<IWorkflowManager>().NavigatePop(1);
        });
      inputManager.AddKeyBinding(Key.Fullscreen, () =>
        {
          //switch to fullscreen
          IScreenControl sc = ServiceRegistration.Get<IScreenControl>();
          int nextMode = ((int)sc.CurrentScreenMode) + 1;
          int totalModes = Enum.GetNames(typeof(ScreenMode)).Length;
          ScreenMode newMode = (ScreenMode)(nextMode % totalModes);
          ServiceRegistration.Get<ILogger>().Info("SkinEngine: Switching screen mode from current '{0}' to '{1}'", sc.CurrentScreenMode, newMode);
          sc.SwitchMode(newMode);
        });
    }

    protected static void UnregisterGlobalKeyBindings()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.RemoveKeyBinding(Key.Escape);
      inputManager.RemoveKeyBinding(Key.Back);
      inputManager.RemoveKeyBinding(Key.Fullscreen);
    }

    #region ISkinEngine implementation

    public void Initialize()
    {
      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Registering IGeometryManager service");
      IGeometryManager geometryManager = new GeometryManager();
      ServiceRegistration.Set<IGeometryManager>(geometryManager);

      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Registering IInputManager service");
      ServiceRegistration.Set<IInputManager>(InputManager.Instance);

      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Registering IClipboardManager service");
      ServiceRegistration.Set<IClipboardManager>(ClipboardManager.Instance);

      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Registering ISuperLayerManager service");
      ServiceRegistration.Set<ISuperLayerManager>(SuperLayerManager.Instance);

      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Registering IScreenManager service");
      _screenManager = new ScreenManager();
      ServiceRegistration.Set<IScreenManager>(_screenManager);
      GraphicsDevice.ScreenManager = _screenManager;
      _screenManager.Startup();

      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Registering ISkinResourceManager service");
      ServiceRegistration.Set<ISkinResourceManager>(_screenManager.SkinResourceManager);
    }

    public void Startup()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();
      logger.Info("SkinEnginePlugin: Startup");
#if DEBUG
      // Enable object tracking only in debug builds, as it adds overhead to each object creation.
      SharpDX.Configuration.EnableObjectTracking = true;
#endif

      logger.Debug("SkinEnginePlugin: Create DirectX main window");
      _mainForm = new MainForm(_screenManager);
      // Install background manager before the mainform gets visible
      _screenManager.InstallBackgroundManager();
      _mainForm.Visible = true;
      _mainForm.Start();

      logger.Debug("SkinEnginePlugin: Switching workflow manager to home state");
      ServiceRegistration.Get<IWorkflowManager>().NavigatePush(new Guid(HOME_STATE_STR));

      logger.Debug("SkinEnginePlugin: Registering default key bindings");
      RegisterGlobalKeyBindings();
    }

    void ISkinEngine.Shutdown()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();

      InputManager.Instance.Dispose();
      SuperLayerManager.Instance.Dispose();

      logger.Debug("SkinEnginePlugin: Unregistering default key bindings");
      UnregisterGlobalKeyBindings();

      logger.Debug("SkinEnginePlugin: Uninstalling background manager");
      _screenManager.UninstallBackgroundManager();

      _mainForm.StopRenderThread();
      _screenManager.Shutdown();

      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Removing ISkinResourceManager service");
      ServiceRegistration.RemoveAndDispose<ISkinResourceManager>();

      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Removing IScreenManager service");
      ServiceRegistration.RemoveAndDispose<IScreenManager>();
      _screenManager = null;

      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Removing IClipboardManager service");
      ServiceRegistration.RemoveAndDispose<IClipboardManager>();

      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Removing IInputManager service");
      ServiceRegistration.RemoveAndDispose<IInputManager>();

      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Removing IGeometryManager service");
      ServiceRegistration.RemoveAndDispose<IGeometryManager>();

      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Clearing BrushCache");
      Controls.Brushes.BrushCache.Instance.Clear();

      ServiceRegistration.Get<ILogger>().Debug("SkinEnginePlugin: Clearing ContentManager");
      ContentManager.Instance.Clear();
    }

    public void Dispose()
    {
      if (_mainForm == null)
        return;
      _mainForm.DisposeDirectX();
      _mainForm.Dispose();
      _screenManager = null;
      _mainForm = null;
    }

    #endregion

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      ServiceRegistration.Set<ISkinEngine>(this);
      _screenSaverWasEnabled = WindowsAPI.ScreenSaverEnabled;
      WindowsAPI.ScreenSaverEnabled = false;
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      ServiceRegistration.Remove<ISkinEngine>();
      Dispose();
    }

    public void Continue() { }

    void IPluginStateTracker.Shutdown()
    {
      WindowsAPI.ScreenSaverEnabled = _screenSaverWasEnabled;
      Dispose();
    }

    #endregion
  }
}
