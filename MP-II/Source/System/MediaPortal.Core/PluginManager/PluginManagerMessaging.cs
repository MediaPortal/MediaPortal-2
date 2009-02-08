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

using MediaPortal.Core.Messaging;

namespace MediaPortal.Core.PluginManager
{
  /// <summary>
  /// This class provides an interface for the messages sent by the plugin manager.
  /// This class is part of the plugin manager API.
  /// </summary>
  public static class PluginManagerMessaging
  {
    // Message Queue name
    public const string QUEUE = "Plugin";

    // Message data
    public const string NOTIFICATION = "Notification"; // Notification stored as NotificationType

    public enum NotificationType
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

    public static void SendPluginManagerMessage(PluginManagerMessaging.NotificationType notificationType)
    {
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(QUEUE);
      QueueMessage msg = new QueueMessage();
      msg.MessageData[NOTIFICATION] = notificationType;
      queue.Send(msg);
    }
  }
}
