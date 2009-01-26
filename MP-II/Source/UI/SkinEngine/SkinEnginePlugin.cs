#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using MediaPortal.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation;
using MediaPortal.Presentation.Screens;
using MediaPortal.Presentation.SkinResources;
using MediaPortal.Presentation.Workflow;
using MediaPortal.SkinEngine.GUI;
using MediaPortal.Core.PluginManager;
using MediaPortal.SkinEngine.InputManagement;
using MediaPortal.SkinEngine.ScreenManagement;

namespace MediaPortal.SkinEngine
{
  public class SkinEnginePlugin: IPluginStateTracker, ISkinEngine
  {
    #region Protected fields

    protected const string HOME_STATE_STR = "{7F702D9C-F2DD-42da-9ED8-0BA92F07787F}";

    protected MainForm _mainForm = null;
    protected ScreenManager _screenManager = null;

    #endregion

    #region ISkinEngine implementation

    public void Initialize()
    {
      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Create IInputManager service");
      InputManager inputManager = new InputManager();
      ServiceScope.Add<IInputManager>(inputManager);

      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Create IScreenManager service");
      _screenManager = new ScreenManager();
      ServiceScope.Add<IScreenManager>(_screenManager);
      ServiceScope.Add<ISkinResourceManager>(_screenManager.SkinResourceManager);
    }

    public void Startup()
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      logger.Info("SkinEnginePlugin: Startup");
      SlimDX.Configuration.EnableObjectTracking = true;

      logger.Debug("SkinEnginePlugin: Create DirectX main window");
      _mainForm = new MainForm(_screenManager);
      _mainForm.Visible = true;
      _mainForm.Start();

      logger.Debug("SkinEnginePlugin: Switching workflow manager to home state");
      ServiceScope.Get<IWorkflowManager>().NavigatePush(new Guid(HOME_STATE_STR));
    }

    public void Dispose()
    {
      _mainForm.Dispose();
      _screenManager.Dispose();
    }

    #endregion

    #region IPluginStateTracker implementation

    public void Activated()
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

    public void Shutdown()
    {
      Dispose();
    }

    #endregion
  }
}
