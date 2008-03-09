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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;

namespace MediaPortal.Services.Messaging
{
  public class Queue : IQueue
  {
    #region IQueue Members
    public event MessageReceivedHandler OnMessageReceive;
    List<IMessageFilter> _filters;
    public Queue()
    {
      _filters = new List<IMessageFilter>();
    }

    /// <summary>
    /// Gets the message filters.
    /// </summary>
    /// <value>The message filters.</value>
    public List<IMessageFilter> Filters
    {
      get
      {
        return _filters;
      }
    }

    /// <summary>
    /// Sends the specified message.
    /// </summary>
    /// <param name="message">The message.</param>
    public void Send(MPMessage message)
    {
      message.Queue = this;
      foreach (IMessageFilter filter in _filters)
      {
        message = filter.Process(message);
        if (message == null) return;
      }
      if (OnMessageReceive != null)
      {
        OnMessageReceive(message);
      }
    }

    /// <summary>
    /// Gets a value indicating whether this queue has subscribers.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this queue has subscribers; otherwise, <c>false</c>.
    /// </value>
    public bool HasSubscribers
    {
      get
      {
        return (OnMessageReceive != null);
      }
    }
    #endregion
  }
}
