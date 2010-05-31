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

using System.Collections.Generic;

namespace MediaPortal.Core.Messaging
{
  /// <summary>
  /// Message to be sent via <see cref="IMessageBroker"/>
  /// </summary>
  public class SystemMessage
  {
    #region Protected fields

    protected object _messageType;
    protected string _channelName = null;
    protected IDictionary<string, object> _messageData = new Dictionary<string, object>();

    #endregion

    #region Ctor

    public SystemMessage(object messageType)
    {
      _messageType = messageType;
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets the type of this message.
    /// </summary>
    public object MessageType
    {
      get { return _messageType; }
    }

    /// <summary>
    /// Gets or sets the name of the message channel this message is being sent.
    /// </summary>
    public string ChannelName
    {
      get { return _channelName; }
      set { _channelName = value; }
    }

    /// <summary>
    /// Gets or sets the message data. The message data is a generic dictionary special
    /// data entries defined by the message queue.
    /// </summary>
    public IDictionary<string, object> MessageData
    {
      get { return _messageData; }
      set { _messageData = value; }
    }

    #endregion
  }
}
