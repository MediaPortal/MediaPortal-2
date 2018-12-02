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

namespace MediaPortal.Common.TaskScheduler
{
  public class TaskSchedulerMessaging
  {
    // Message channel name
    public const string CHANNEL = "TaskScheduler";

    // Message type
    public enum MessageType
    {
      DUE,
      CHANGED,
      DELETED,
      EXPIRED
    }

    // Message data
    public const string TASK = "Task"; // Stores the task of this message

    /// <summary>
    /// Sends a message in the <see cref="CHANNEL"/>.
    /// </summary>
    /// <param name="type">The type of the message to send.</param>
    /// <param name="task">The task of the message to send.</param>
    public static void SendTaskSchedulerMessage(MessageType type, Task task)
    {
      SystemMessage msg = new SystemMessage(type);
      msg.MessageData[TASK] = task;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}