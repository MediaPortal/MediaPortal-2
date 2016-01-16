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
using MediaPortal.Common.MediaManagement;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.AspNetWebApi.Json
{
  public class MediaItemJsonConverter : JsonConverter
  {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      var mi = (MediaItem)value;
      writer.Formatting = Formatting.Indented;

      writer.WriteStartObject();

      writer.WritePropertyName("MediaItemId");
      writer.WriteValue(mi.MediaItemId);

      writer.WritePropertyName("Aspects");
      writer.WriteStartArray();
      foreach (var mia in mi.Aspects.Values)
      {
        writer.WriteStartObject();
        writer.WritePropertyName("MediaItemAspectId");
        writer.WriteValue(mia.Metadata.AspectId);
        writer.WritePropertyName("MediaItemAspectName");
        writer.WriteValue(mia.Metadata.Name);

        writer.WritePropertyName("Attributes");
        writer.WriteStartObject();
        foreach (var attributeSpecification in mia.Metadata.AttributeSpecifications.Values)
        {
          writer.WritePropertyName(attributeSpecification.AttributeName);
          var attribute = mia[attributeSpecification];
          if (attribute is byte[])
            writer.WriteValue("byte[]");
          else
            serializer.Serialize(writer, mia[attributeSpecification]);
        }
        writer.WriteEndObject();

        writer.WriteEndObject();
      }
      writer.WriteEndArray();

      writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }

    public override bool CanConvert(Type objectType)
    {
      return typeof(MediaItem).IsAssignableFrom(objectType);
    }
  }
}
