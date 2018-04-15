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

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MediaPortal.Utilities.Xml
{
  public class XmlDictionarySerializer
  {
    public struct Pair<TKey, TValue>
    {
      public TKey Key { get; set; }
      public TValue Value { get; set; }
      public Pair(KeyValuePair<TKey, TValue> pair)
        : this()
      {
        Key = pair.Key;
        Value = pair.Value;
      }
    }

    public static void WriteXml<TKey, TValue>(XmlWriter writer, IDictionary<TKey, TValue> dict)
    {
      var list = new List<Pair<TKey, TValue>>(dict.Count);
      list.AddRange(dict.Select(pair => new Pair<TKey, TValue>(pair)));

      var serializer = new XmlSerializer(list.GetType());
      serializer.Serialize(writer, list);
    }

    public static void ReadXml<TKey, TValue>(XmlReader reader, IDictionary<TKey, TValue> dict)
    {
      reader.Read();

      var serializer = new XmlSerializer(typeof(List<Pair<TKey, TValue>>));
      var list = (List<Pair<TKey, TValue>>) serializer.Deserialize(reader);

      foreach (var pair in list)
        dict.Add(pair.Key, pair.Value);

      reader.Read();
    }
  }

  public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
  {

    public virtual void WriteXml(XmlWriter writer)
    {
      XmlDictionarySerializer.WriteXml(writer, this);
    }

    public virtual void ReadXml(XmlReader reader)
    {
      XmlDictionarySerializer.ReadXml(reader, this);
    }

    public virtual XmlSchema GetSchema()
    {
      return null;
    }
  }
}
