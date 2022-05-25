#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

namespace InputDevices.Common.Serializing
{
  /// <summary>
  /// A DTO for an <see cref="Mapping.InputDeviceMapping"/> that is suitable for serializing.
  /// The extention methods <see cref="SerializingExtensions.ToDeviceMapping(SerializedMapping)"/>
  /// and <see cref="SerializingExtensions.ToSerializedMapping(Mapping.InputDeviceMapping)"/> can
  /// be used to convert between the types.
  /// </summary>
  public class SerializedMapping
  {
    public string DeviceId { get; set; }
    public List<SerializedActionMap> Mappings { get; set; }
  }

  public class SerializedActionMap
  {
    public string Type { get; set; }
    public string Action { get; set; }
    public List<SerializedInput> Inputs { get; set; }
  }

  public class SerializedInput
  {
    public string Id { get; set; }
    public string Name { get; set; }
  }
}
