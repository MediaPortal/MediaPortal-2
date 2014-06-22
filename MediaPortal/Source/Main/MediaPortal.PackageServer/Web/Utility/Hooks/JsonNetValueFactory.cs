#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MediaPortal.PackageServer.Utility.Hooks
{
  public class JsonNetValueProviderFactory : ValueProviderFactory
  {
    public override IValueProvider GetValueProvider(ControllerContext controllerContext)
    {
      if (controllerContext == null)
        throw new ArgumentNullException("controllerContext");
      if (!controllerContext.HttpContext.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
        return null;

      // use JSON.NET to deserialize object to a dynamic (expando) object
      object jsonQueryModel;
      using (var streamReader = new StreamReader(controllerContext.HttpContext.Request.InputStream))
      {
        var jsonReader = new JsonTextReader(streamReader);
        if (!jsonReader.Read())
          return null;

        var serializer = new JsonSerializer();
        serializer.Converters.Add(new ExpandoObjectConverter());

        // if we start with a "[", treat this as an array
        if (jsonReader.TokenType == JsonToken.StartArray)
          jsonQueryModel = serializer.Deserialize<List<ExpandoObject>>(jsonReader);
        else
          jsonQueryModel = serializer.Deserialize<ExpandoObject>(jsonReader);
      }

      // create a backing store to hold all properties for this deserialization
      var backingStore = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
      AddToBackingStore(backingStore, string.Empty, jsonQueryModel);
      
      // return the object in a dictionary value provider so the MVC understands it
      return new DictionaryValueProvider<object>(backingStore, CultureInfo.CurrentCulture);
    }

    private static void AddToBackingStore(Dictionary<string, object> backingStore, string prefix, object value)
    {
      var d = value as IDictionary<string, object>;
      if (d != null)
      {
        foreach (var entry in d)
        {
          AddToBackingStore(backingStore, MakePropertyKey(prefix, entry.Key), entry.Value);
        }
        return;
      }

      var l = value as IList;
      if (l != null)
      {
        for (var i = 0; i < l.Count; i++)
        {
          AddToBackingStore(backingStore, MakeArrayKey(prefix, i), l[i]);
        }
        return;
      }

      // primitive
      backingStore[prefix] = value;
    }

    private static string MakeArrayKey(string prefix, int index)
    {
      return prefix + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
    }

    private static string MakePropertyKey(string prefix, string propertyName)
    {
      return (String.IsNullOrEmpty(prefix)) ? propertyName : prefix + "." + propertyName;
    }
  }
}
