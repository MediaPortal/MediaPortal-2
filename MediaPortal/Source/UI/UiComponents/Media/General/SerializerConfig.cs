#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MediaPortal.UiComponents.Media.General
{
  /// <summary>
  /// <see cref="SerializerConfig"/> holds default settings for the Json.Net serializer. The serialization is optimized to be 
  /// compatible with the existing [XmlIgnore] attributes of properties.
  /// </summary>
  public static class SerializerConfig
  {
    private static readonly JsonSerializerSettings SETTING;
    static SerializerConfig()
    {
      SETTING = new JsonSerializerSettings
      {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        TypeNameHandling = TypeNameHandling.Auto,
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new XmlIngoreContractResolver()
      };
    }
    /// <summary>
    /// Gets the default setting for JSON serialization.
    /// </summary>
    public static JsonSerializerSettings Default { get { return SETTING; } }
  }

  /// <summary>
  /// <see cref="XmlIngoreContractResolver"/> filters properties that are marked with the <see cref="XmlIgnoreAttribute"/>.
  /// </summary>
  public class XmlIngoreContractResolver : DefaultContractResolver
  {
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
      var properties = base.CreateProperties(type, memberSerialization);
      var filtered = properties.Where(p => !type.GetProperty(p.PropertyName).GetCustomAttributes(typeof(XmlIgnoreAttribute), false).Any()).ToList();
      return filtered;
    }
  }
}
