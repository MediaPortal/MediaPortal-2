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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;

namespace MediaPortal.UI.Shares
{
  /// <summary>
  /// This class provides an interface for all shares related messages. This class is part of the
  /// shares management API.
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
      /// <summary>
      /// A new share was created. The SHARE will contain the share instance which is affected.
      /// </summary>
      ShareAdded,
      /// <summary>
      /// An existing share was removed. The SHARE will contain the share instance which is affected.
      /// </summary>
      ShareRemoved,
      /// <summary>
      /// An existing share was changed. The SHARE will contain the share instance which is affected. Parameter RELOCATION_MODE will be additionally set.
      /// </summary>
      ShareChanged,
      /// <summary>
      /// An existing share should be reimported. The SHARE will contain the share instance which is affected.
      /// </summary>
      ReImportShare,
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
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a message that a share was changed (<see cref="MessageType.ShareChanged"/>).
    /// </summary>
    /// <param name="share">Share which was changed.</param>
    /// <param name="relocationMode">Controls how the data of the changed share should be adapted at the server.</param>
    public static void SendShareChangedMessage(Share share, RelocationMode relocationMode)
    {
      SystemMessage msg = new SystemMessage(MessageType.ShareChanged);
      msg.MessageData[SHARE] = share;
      msg.MessageData[RELOCATION_MODE] = relocationMode;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }

    /// <summary>
    /// Sends a message that tells clients that a local share should be re-imported (<see cref="MessageType.ReImportShare"/>).
    /// </summary>
    /// <param name="share">Local share that should be re-imported.</param>
    public static void SendShareReimportMessage(Share share)
    {
      SystemMessage msg = new SystemMessage(MessageType.ReImportShare);
      msg.MessageData[SHARE] = share;
      ServiceRegistration.Get<IMessageBroker>().Send(CHANNEL, msg);
    }
  }
}