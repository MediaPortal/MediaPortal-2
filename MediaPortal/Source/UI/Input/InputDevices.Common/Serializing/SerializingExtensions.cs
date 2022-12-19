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

using InputDevices.Common.Inputs;
using InputDevices.Common.Mapping;
using System.Linq;

namespace InputDevices.Common.Serializing
{
  public static class SerializingExtensions
  {
    /// <summary>
    /// Converts a <see cref="SerializedMapping"/> instance to an <see cref="InputDeviceMapping"/>.
    /// </summary>
    /// <param name="mapping">The mapping to convert.</param>
    /// <returns>An <see cref="InputDeviceMapping"/> with the same device and mappings as <paramref name="mapping"/>.</returns>
    public static InputDeviceMapping ToDeviceMapping(this SerializedMapping mapping)
    {
      return new InputDeviceMapping(mapping.DeviceId,
        mapping.Mappings.Select(m =>
          new MappedAction(
            new InputAction(m.Type, m.Action),
            m.Inputs.Select(i => new Input(i.Id, i.Name))
          )
        ));
    }

    /// <summary>
    /// Converts an <see cref="InputDeviceMapping"/> instance to a <see cref="SerializedMapping"/>.
    /// </summary>
    /// <param name="mapping">The mapping to convert</param>
    /// <returns>A <see cref="SerializedMapping"/> with the same device and mappings as <paramref name="mapping"/>.</returns>
    public static SerializedMapping ToSerializedMapping(this InputDeviceMapping mapping)
    {
      return new SerializedMapping
      {
        DeviceId = mapping.DeviceId,
        Mappings = mapping.MappedActions.Select(m =>
          new SerializedActionMap
          {
            Type = m.Action.Type,
            Action = m.Action.Action,
            Inputs = m.Inputs.Select(i => new SerializedInput
            {
              Id = i.Id,
              Name = i.Name
            }).ToList()
          }).ToList()
      };
    }
  }
}
