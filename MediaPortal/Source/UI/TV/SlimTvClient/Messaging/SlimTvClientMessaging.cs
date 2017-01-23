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

using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Client.Messaging
{
  public class SlimTvClientMessaging
  {
    // Message channel name
    public const string CHANNEL = "SlimTvClient";
    public const string KEY_PROGRAM = "PROGRAM";

    // Message type
    public enum MessageType
    {
      /// <summary>
      /// Indicates that the active channel group was changed.
      /// </summary>
      GroupChanged,
      /// <summary>
      /// Indicates that the programs were changed. This usually happens when scrolling through the EPG.
      /// </summary>
      ProgramsChanged,
      /// <summary>
      /// Indicates that a program's information was changed. This usually happens after changing its recording status.
      /// The message data contains the affected <see cref="IProgram"/> under key <see cref="SlimTvClientMessaging.KEY_PROGRAM"/>.
      /// </summary>
      ProgramStatusChanged,
    }

    /// <summary>
    /// Sends a message which announces changes on the given <paramref name="program"/>.
    /// </summary>
    /// <param name="program">Program</param>
    public static void SendSlimTvProgramChangedMessage(IProgram program)
    {
      SystemMessage msg = new SystemMessage(MessageType.ProgramStatusChanged);
      msg.MessageData[KEY_PROGRAM] = program;
      SendMessage(msg);
    }

    /// <summary>
    /// Sends a message which announces a change in TV client.
    /// </summary>
    /// <param name="type">Type of the message.</param>
    public static void SendSlimTvClientMessage(MessageType type)
    {
      SendMessage(new SystemMessage(type));
    }

    private static void SendMessage(SystemMessage msg)
    {
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}
