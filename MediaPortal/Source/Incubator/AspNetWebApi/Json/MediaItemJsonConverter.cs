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
  /// <summary>
  /// Converts <see cref="MediaItem"/>s into Json and vice versa
  /// </summary>
  public class MediaItemJsonConverter : JsonConverter
  {
    #region Consts

    private const string PROPERTY_NAME_MEDIA_ITEM_ID = "MediaItemId";
    private const string PROPERTY_NAME_ASPECTS = "Aspects";
    private const string PROPERTY_NAME_MEDIA_ITEM_ASPECT_ID = "MediaItemAspectId";
    private const string PROPERTY_NAME_MEDIA_ITEM_ASPECT_NAME = "MediaItemAspectName";
    private const string PROPERTY_NAME_ATTRIBUTES = "Attributes";

    // This string is sent for Attributes of type byte[] instead of the actual binary data.
    private const string BINARY_REPLACEMENT_STRING = "%%BinaryData%%";

    #endregion

    #region JsonConverter implementation

    /// <summary>
    /// Converts a <see cref="MediaItem"/> into Json
    /// </summary>
    /// <param name="writer">JsonWriter to use for the conversion</param>
    /// <param name="value"><see cref="MediaItem"/> to convert</param>
    /// <param name="serializer">JsonSerializer to use for the conversion</param>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      // This is to make debugging easier but wasts bandwidth;
      // ToDo: Remove this once the service is stable
      writer.Formatting = Formatting.Indented;

      var mi = (MediaItem)value;

      writer.WriteStartObject(); // MediaItem

      writer.WritePropertyName(PROPERTY_NAME_MEDIA_ITEM_ID);
      writer.WriteValue(mi.MediaItemId);

      writer.WritePropertyName(PROPERTY_NAME_ASPECTS);
      writer.WriteStartArray(); // Array of MIAs
      foreach (var mia in mi.Aspects.Values)
      {
        writer.WriteStartObject(); // MIA

        writer.WritePropertyName(PROPERTY_NAME_MEDIA_ITEM_ASPECT_ID);
        writer.WriteValue(mia.Metadata.AspectId);

        // The AspectName is for debugging purposes onle; a MIA can be uniquely identified by its MiaId
        // ToDo: Remove this once the service is stable
        writer.WritePropertyName(PROPERTY_NAME_MEDIA_ITEM_ASPECT_NAME);
        writer.WriteValue(mia.Metadata.Name);

        writer.WritePropertyName(PROPERTY_NAME_ATTRIBUTES);
        writer.WriteStartObject(); // Attribute
        foreach (var attributeSpecification in mia.Metadata.AttributeSpecifications.Values)
        {
          writer.WritePropertyName(attributeSpecification.AttributeName);
          var attribute = mia[attributeSpecification];
          if (attribute is byte[])
            writer.WriteValue(BINARY_REPLACEMENT_STRING);
          else
            serializer.Serialize(writer, mia[attributeSpecification]);
        }
        writer.WriteEndObject(); // Attribute

        writer.WriteEndObject(); // MIA
      }
      writer.WriteEndArray(); // Array of MIAs

      writer.WriteEndObject(); // MediaItem
    }

    /// <summary>
    /// Converts Json into a <see cref="MediaItem"/>
    /// </summary>
    /// <param name="reader">JsonReader to read from</param>
    /// <param name="objectType">Type of the object (typeof(MediaItem))</param>
    /// <param name="existingValue">Existing value of object being read</param>
    /// <param name="serializer">The calling serializer</param>
    /// <returns>Deserialized <see cref="MediaItem"/></returns>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Determined if this <see cref="JsonConverter"/> can convert a specific type into Json
    /// </summary>
    /// <param name="objectType">Type to convert</param>
    /// <returns><c>true</c> if <paramref name="objectType"/> can be assigned to a <see cref="MediaItem"/>; else <c>false</c></returns>
    public override bool CanConvert(Type objectType)
    {
      return typeof(MediaItem).IsAssignableFrom(objectType);
    }

    #endregion
  }
}
