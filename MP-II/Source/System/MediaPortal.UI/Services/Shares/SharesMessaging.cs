#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Shares;

namespace MediaPortal.UI.Services.Shares
{
  /// <summary>
  /// This class provides an interface for all shares related messages.
  /// </summary>
  public class SharesMessaging
  {
    // Message channel name
    public const string CHANNEL = "Shares";

    /// <summary>
    /// Messages of this type are sent by the media manager and its components.
    /// </summary>
    public enum MessageType
    {
      // Share related messages. The SHARE will contain the share instance which is affected.
      ShareAdded,
      ShareRemoved,
      ShareChanged, // Parameter RELOCATION_MODE will be set
    }

    // Message data
    public const string SHARE = "Share"; // The affected share
    public const string RELOCATION_MODE = "RelocationMode"; // Contains a variable of type RelocationMode

    /// <summary>
    /// Sends a message concerning a share.
    /// </summary>
    /// <param name="messageType">Type of the message to send.</param>
    /// <param name="share">Share which is affected.</param>
    public static void SendShareMessage(MessageType messageType, Share share)
    {
      SystemMessage msg = new SystemMessage(messageType);
      msg.MessageData[SHARE] = share;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    public static void SendShareChangedMessage(Share share, RelocationMode relocationMode)
    {
      SystemMessage msg = new SystemMessage(MessageType.ShareChanged);
      msg.MessageData[SHARE] = share;
      msg.MessageData[RELOCATION_MODE] = relocationMode;
      ServiceScope.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}