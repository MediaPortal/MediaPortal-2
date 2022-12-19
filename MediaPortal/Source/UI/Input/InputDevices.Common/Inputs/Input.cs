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
using System.Linq;

namespace InputDevices.Common.Inputs
{
  /// <summary>
  /// Class that represents a generic device input.
  /// </summary>
  public class Input
  {
    /// <summary>
    /// Creates a new instance of an input.
    /// </summary>
    /// <param name="id">The unique id of the input, this should be unique across all plugins and devices.</param>
    /// <param name="name">The display name of the input.</param>
    public Input(string id, string name)
      : this(id, name, false)
    {
    }

    /// <summary>
    /// Creates a new instance of an input.
    /// </summary>
    /// <param name="id">The unique id of the input, this should be unique across all plugins and devices.</param>
    /// <param name="name">The display name of the input.</param>
    /// <param name="isModifier">Whether this input is a modifier and can therefore be used as part of an input combination.</param>
    public Input(string id, string name, bool isModifier)
    {
      Id = id;
      Name = name;
      IsModifier = isModifier;
    }

    /// <summary>
    /// The unique identifier for this input.
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// The display name of the input.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Whether this input is a modifier and can therefore be used as part of an input combination.
    /// </summary>
    public bool IsModifier { get; private set; }

    /// <summary>
    /// Utility method to format an enumeration of inputs into a human readable string for display.
    /// </summary>
    /// <param name="inputs">Enumeration of inputs to display.</param>
    /// <returns>A human readable string containing the input names or <see cref="string.Empty"/> if inputs is <c>null</c> or empty.</returns>
    public static string GetInputString(IEnumerable<Input> inputs)
    {
      return inputs != null ? string.Join(", ", inputs.Select(i => i.Name)) : string.Empty;
    }
  }
}
