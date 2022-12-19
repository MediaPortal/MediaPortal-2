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
using System;
using System.Collections.Generic;
using System.Linq;

namespace InputDevices.Common.Mapping
{
  /// <summary>
  /// Class that connects an <see cref="InputAction"/> to a mapped collection of <see cref="Input"/>.
  /// </summary>
  public class MappedAction
  {
    protected static readonly IReadOnlyList<Input> EMPTY_INPUTS = new List<Input>().AsReadOnly();

    /// <summary>
    /// Creates a new instance of <see cref="MappedAction"/>.
    /// </summary>
    /// <param name="action">The action that has been mapped.</param>
    /// <param name="inputs">The input that is mapped to <paramref name="action"/>.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MappedAction(InputAction action, Input input)
    : this(action, new[] { input })
    { }

    /// <summary>
    /// Creates a new instance of <see cref="MappedAction"/>.
    /// </summary>
    /// <param name="action">The action that has been mapped.</param>
    /// <param name="inputs">The inputs that are mapped to <paramref name="action"/>.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MappedAction(InputAction action, IEnumerable<Input> inputs)
    {
      Action = action ?? throw new ArgumentNullException(nameof(action));
      Inputs = inputs?.ToList().AsReadOnly() ?? EMPTY_INPUTS;
    }

    public InputAction Action { get; }
    public IReadOnlyList<Input> Inputs { get; }
  }
}
