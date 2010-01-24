#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.Presentation;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Geometry;
using MediaPortal.UI.SkinEngine.GUI;
using MediaPortal.Core.PluginManager;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.ScreenManagement;

namespace MediaPortal.UI.SkinEngine
{
  public class SkinEnginePlugin: IPluginStateTracker, ISkinEngine
  {
    #region Protected fields

    protected const string HOME_STATE_STR = "{7F702D9C-F2DD-42da-9ED8-0BA92F07787F}";

    protected MainForm _mainForm = null;
    protected ScreenManager _screenManager = null;

    #endregion

    protected static void RegisterGlobalKeyBindings()
    {
      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      inputManager.AddKeyBinding(Key.Escape, () =>
        {
          // Close dialog or switch to previous workflow state
          IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
          if (screenManager.IsDialogVisible)
            screenManager.CloseDialog();
          else
            ServiceScope.Get<IWorkflowManager>().NavigatePop(1);
          return true;
        });
      inputManager.AddKeyBinding(Key.Fullscreen, () =>
        {
          //switch to fullscreen
          IScreenControl sc = ServiceScope.Get<IScreenControl>();
          if (sc.IsFullScreen)
            sc.SwitchMode(ScreenMode.NormalWindowed);
          else
            sc.SwitchMode(ScreenMode.FullScreenWindowed);
          return true;
        });
    }

    protected static void UnregisterGlobalKeyBindings()
    {
      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      inputManager.RemoveKeyBinding(Key.Escape);
      inputManager.RemoveKeyBinding(Key.Back);
      inputManager.RemoveKeyBinding(Key.Fullscreen);
    }

    #region ISkinEngine implementation

    public void Initialize()
    {
      ContentManager.Initialize();

      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Registering IGeometryManager service");
      IGeometryManager geometryManager = new GeometryManager();
      ServiceScope.Add<IGeometryManager>(geometryManager);

      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Registering IInputManager service");
      ServiceScope.Add<IInputManager>(InputManager.Instance);

      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Registering IScreenManager service");
      _screenManager = new ScreenManager();
      ServiceScope.Add<IScreenManager>(_screenManager);

      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Registering ISkinResourceManager service");
      ServiceScope.Add<ISkinResourceManager>(_screenManager.SkinResourceManager);
    }

    public void Startup()
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      logger.Info("SkinEnginePlugin: Startup");
      SlimDX.Configuration.EnableObjectTracking = true;

      logger.Debug("SkinEnginePlugin: Create DirectX main window");
      _mainForm = new MainForm(_screenManager);
      // Install background manager before the mainform gets visible
      _screenManager.InstallBackgroundManager();
      _mainForm.Visible = true;
      _mainForm.Start();

      logger.Debug("SkinEnginePlugin: Switching workflow manager to home state");
      ServiceScope.Get<IWorkflowManager>().NavigatePush(new Guid(HOME_STATE_STR), null);

      logger.Debug("SkinEnginePlugin: Registering default key bindings");
      RegisterGlobalKeyBindings();
    }

    void ISkinEngine.Shutdown()
    {
      ILogger logger = ServiceScope.Get<ILogger>();

      logger.Debug("SkinEnginePlugin: Unregistering default key bindings");
      UnregisterGlobalKeyBindings();

      logger.Debug("SkinEnginePlugin: Uninstalling background manager");
      _screenManager.UninstallBackgroundManager();

      _mainForm.StopRenderThread();
      _screenManager.Shutdown();

      ContentManager.Uninitialize();

      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Removing ISkinResourceManager service");
      ServiceScope.Remove<ISkinResourceManager>();

      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Removing IScreenManager service");
      ServiceScope.Remove<IScreenManager>();

      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Removing IInputManager service");
      ServiceScope.Remove<IInputManager>();

      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Removing IGeometryManager service");
      ServiceScope.Remove<IGeometryManager>();
    }

    public void Dispose()
    {
      _mainForm.DisposeDirectX();
      _mainForm.Dispose();
      _screenManager = null;
      _mainForm = null;
    }

    #endregion

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      ServiceScope.Add<ISkinEngine>(this);
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      Dispose();
    }

    public void Continue() { }

    void IPluginStateTracker.Shutdown()
    {
      Dispose();
    }

    #endregion
  }
}
