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
using Newtonsoft.Json;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Utilities
{
  /// <summary>
  /// <see cref="JsonConverter"/> for byte arrays
  /// </summary>
  /// <remarks>
  /// The NfoMetadataExtractors can, for debugging purposes, write the filled stub objects of each imported
  /// MediaItem into the debug log in form of its Json representation. Binary data in these stub objects
  /// (in particular images) are represented by byte arrays. This <see cref="JsonConverter"/> prevents
  /// large amounts of binary data in byte arrays to be written into the debug log and instead just writes
  /// "[{0:N0} bytes of binary data]".
  /// </remarks>
  public class JsonByteArrayConverter : JsonConverter
  {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      if (value == null)
      {
        writer.WriteNull();
        return;
      }
      var array = (byte[])value;
      writer.WriteValue(String.Format("[{0:N0} bytes of binary data]", array.Length));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }

    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(byte[]);
    }
  }
}
