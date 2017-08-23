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
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Newtonsoft.Json;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Extension
{
  /// <summary>
  /// Methods used to transform to and from JSON
  /// </summary>
  public static class JSONExtensions
  {
    /// <summary>
    /// Creates a list based on a JSON Array
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="jsonArray"></param>
    /// <returns></returns>
    public static IEnumerable<T> FromJSONArray<T>(this string jsonArray)
    {
      if (string.IsNullOrEmpty(jsonArray)) return new List<T>();

      try
      {
        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray)))
        {
          var ser = new DataContractJsonSerializer(typeof(IEnumerable<T>));
          var result = (IEnumerable<T>)ser.ReadObject(ms);

          if (result == null)
          {
            return new List<T>();
          }
          else
          {
            return result;
          }
        }
      }
      catch (Exception)
      {
        return new List<T>();
      }
    }

    /// <summary>
    /// Creates an object from JSON
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="json"></param>
    /// <returns></returns>
    public static T FromJSON<T>(this string json)
    {
      if (string.IsNullOrEmpty(json)) return default(T);

      try
      {
        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json.ToCharArray())))
        {
          var ser = new DataContractJsonSerializer(typeof(T));
          return (T)ser.ReadObject(ms);
        }
      }
      catch (Exception)
      {
        return default(T);
      }
    }

    /// <summary>
    /// Creates a Dictionary based on the JSON string
    /// </summary>
    public static T FromJSONDictionary<T>(this string json)
    {
      if (string.IsNullOrEmpty(json)) return default(T);

      try
      {
        return JsonConvert.DeserializeObject<T>(json);
      }
      catch
      {
        return default(T);
      }
    }

    /// <summary>
    /// Turns an object into JSON
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToJSON(this object obj)
    {
      if (obj == null) return string.Empty;
      using (var ms = new MemoryStream())
      {
        var ser = new DataContractJsonSerializer(obj.GetType());
        ser.WriteObject(ms, obj);
        return Encoding.UTF8.GetString(ms.ToArray());
      }
    }
  }
}
