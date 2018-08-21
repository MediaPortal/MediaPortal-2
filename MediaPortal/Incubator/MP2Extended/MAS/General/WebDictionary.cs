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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaPortal.Plugins.MP2Extended.MAS.General
{
  [Serializable]
  public class WebDictionary<TValue> : ISerializable,
    IEnumerable<KeyValuePair<string, TValue>>, IEnumerable
  {
    private Dictionary<string, TValue> dict = new Dictionary<string, TValue>();

    public WebDictionary()
    {
    }

    public WebDictionary(Dictionary<string, TValue> input)
    {
      dict = input;
    }

    public WebDictionary(SerializationInfo info, StreamingContext context)
    {
      foreach (SerializationEntry item in info)
      {
        dict.Add(item.Name, (TValue)item.Value);
      }
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      foreach (string key in dict.Keys)
      {
        info.AddValue(key.ToString(), dict[key]);
      }
    }

    public TValue this[string key]
    {
      get { return dict[key]; }
      set { dict[key] = value; }
    }

    public void Add(string key, TValue value)
    {
      dict[key] = value;
    }

    public bool ContainsKey(string key)
    {
      return dict.ContainsKey(key);
    }

    public ICollection<string> Keys
    {
      get { return dict.Keys; }
    }

    public ICollection<TValue> Values
    {
      get { return dict.Values; }
    }

    public bool Remove(string key)
    {
      return dict.Remove(key);
    }

    public void Clear()
    {
      dict.Clear();
    }

    public int Count
    {
      get { return dict.Count(); }
    }

    public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
    {
      return dict.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return dict.GetEnumerator();
    }
  }
}
