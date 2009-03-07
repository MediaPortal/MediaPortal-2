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

using System.Collections.Generic;
using MediaPortal.Core.Messaging;

namespace MediaPortal.Core.Services.Messaging
{
  public class Queue : IMessageQueue
  {
    #region Protected fields

    protected IList<IMessageFilter> _filters;

    #endregion

    public Queue()
    {
      _filters = new List<IMessageFilter>();
    }

    #region IMessageQueue implementation

    public event MessageReceivedHandler MessageReceived;

    public IList<IMessageFilter> Filters
    {
      get { return _filters; }
    }

    public void Send(QueueMessage message)
    {
      message.MessageQueue = this;
      foreach (IMessageFilter filter in _filters)
      {
        message = filter.Process(message);
        if (message == null) return;
      }
      if (MessageReceived != null)
      {
        MessageReceived(message);
      }
    }

    public bool HasSubscribers
    {
      get { return (MessageReceived != null); }
    }

    #endregion
  }
}
