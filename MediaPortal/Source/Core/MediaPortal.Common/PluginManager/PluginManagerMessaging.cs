#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.Messaging;

namespace MediaPortal.Common.PluginManager
{
  /// <summary>
  /// This class provides an interface for the messages sent by the plugin manager.
  /// This class is part of the plugin manager API.
  /// </summary>
  public static class PluginManagerMessaging
  {
    // Message channel name
    public const string CHANNEL = "Plugin";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// This message will be sent before the plugin manager performs its startup tasks.
      /// When this message is sent, the plugin manager is in state
      /// <see cref="PluginManagerState.Initializing"/>.
      /// </summary>
      Startup,

      /// <summary>
      /// This message will be sent after all plugins were loaded, enabled and auto-started.
      /// After this message is sent, the plugin manager will change its state to
      /// <see cref="PluginManagerState.Running"/>.
      /// </summary>
      PluginsInitialized,

      /// <summary>
      /// This message will be sent before the plugin manager shuts down.
      /// When this message is sent, the plugin manager is in state
      /// <see cref="PluginManagerState.ShuttingDown"/>.
      /// </summary>
      Shutdown
    }

    public static void SendPluginManagerMessage(MessageType messageType)
    {
      SystemMessage msg = new SystemMessage(messageType);
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
