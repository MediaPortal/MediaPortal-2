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
  public class MessageBroker : IMessageBroker
  {
    #region Protected fields

    protected IDictionary<string, IMessageQueue> _queues;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBroker"/> class.
    /// </summary>
    public MessageBroker()
    {
      _queues = new Dictionary<string, IMessageQueue>();
    }

    #region IMessageBroker implementation


    public IMessageQueue GetOrCreate(string queueName)
    {
      if (!_queues.ContainsKey(queueName))
      {
        Queue q = new Queue();
        _queues[queueName] = q;
      }
      return _queues[queueName];
    }

    public IList<string> Queues
    {
      get 
      {
        List<string> queueNames = new List<string>();
        IEnumerator<KeyValuePair<string, IMessageQueue>> enumer = _queues.GetEnumerator();
        while (enumer.MoveNext())
          queueNames.Add(enumer.Current.Key);
        return queueNames;
      }
    }

    #endregion
  }
}
