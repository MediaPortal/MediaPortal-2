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

using MediaPortal.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.Commands;
using MediaPortal.SkinEngine.Commands;
using MediaPortal.SkinEngine.GUI;
using MediaPortal.Core.PluginManager;
using MediaPortal.SkinEngine.InputManagement;

namespace MediaPortal.SkinEngine
{
  public class SkinEnginePlugin: IPluginStateTracker
  {
    #region Protected fields

    protected MainForm _mainForm = null;
    protected ScreenManager _screenManager = null;

    #endregion

    #region Private & protected methods

    protected void Initialize()
    {
      SlimDX.Configuration.EnableObjectTracking = true;
      InitializeServices();

      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      if (pluginManager.State == PluginManagerState.Starting)
      {
        // The main form will be created when all plugins are loaded
        IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(PluginManagerMessaging.Queue);
        queue.OnMessageReceive += OnPluginManagerMessageReceived;
      }
      else
        // The plugin manager is already running, this means the skin engine was started during
        // the runtime. We do not need to wait for other plugins to be loaded and we can start immediately.
        Start();
    }

    /// <summary>
    /// Called when the plugin manager notifies the system about its events.
    /// Creates the main screen when all plugins are initialized.
    /// </summary>
    /// <param name="message">Message containing the notification data.</param>
    private void OnPluginManagerMessageReceived(QueueMessage message)
    {
      if (((PluginManagerMessaging.NotificationType)message.MessageData[PluginManagerMessaging.Notification]) == PluginManagerMessaging.NotificationType.PluginsInitialized)
        Start();
    }

    protected void Start()
    {
      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Create IScreenManager service");
      _screenManager = new ScreenManager();

      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Create DirectX main window");
      _mainForm = new MainForm(_screenManager);
      _mainForm.Visible = true;
      _mainForm.Start();
    }

    protected void Dispose()
    {
      _mainForm.Dispose();
      _screenManager.Dispose();
    }

    protected void InitializeServices()
    {
      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Create ICommandBuilder service");
      CommandBuilder cmdBuilder = new CommandBuilder();
      ServiceScope.Add<ICommandBuilder>(cmdBuilder);

      ServiceScope.Get<ILogger>().Debug("SkinEnginePlugin: Create IInputManager service");
      InputManager inputManager = new InputManager();
      ServiceScope.Add<IInputManager>(inputManager);
    }

    #endregion

    #region IPluginStateTracker implementation

    public void Activated()
    {
      Initialize();
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
