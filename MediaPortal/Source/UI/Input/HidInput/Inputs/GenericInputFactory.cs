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

using System;

namespace HidInput.Inputs
{
  /// <summary>
  /// Interface for a factory that can create a specific <see cref="GenericInput{T}"/> for a usage type T.
  /// Used internally by <see cref="GenericInput.TryDecodeEvent(SharpLib.Hid.Event, InputCollection)"/>.
  /// </summary>
  internal interface IGenericInputFactory
  {
    /// <summary>
    /// Gets the specific type of <see cref="GenericInput{T}"/> that will be created by <see cref="TryCreateInput(ushort, out GenericInput)"/>.
    /// </summary>
    Type InputType { get; }

    /// <summary>
    /// Tries to create a <see cref="GenericInput{T}"/> of type <see cref="InputType"/> for the specified <paramref name="usage"/>.
    /// </summary>
    /// <param name="usage">The integral value of a usage enum.</param>
    /// <param name="input">If successful, a <see cref="GenericInput{T}"/> of type <see cref="InputType"/>.</param>
    /// <returns><c>true</c> if <paramref name="usage"/> is a valid value for the enum T and a <see cref="GenericInput{T}"/> was created; else <c>false</c>.</returns>
    bool TryCreateInput(ushort usage, out GenericInput input);
  }

  /// <summary>
  /// Implementation of <see cref="IGenericInputFactory"/> that creates a type specific <see cref="GenericInput{T}"/>.
  /// </summary>
  /// <typeparam name="T">The enum type of the usages that will be passed to <see cref="TryCreateInput(ushort, out GenericInput)"/>.</typeparam>
  internal class GenericInputFactory<T> : IGenericInputFactory where T : Enum
  {
    protected static readonly Type _usageType = typeof(T);
    protected static readonly Type _inputType = typeof(GenericInput<T>);

    public Type InputType
    {
      get { return _inputType; }
    }

    public bool TryCreateInput(ushort usage, out GenericInput input)
    {
      if (Enum.IsDefined(_usageType, usage))
      {
        input = new GenericInput<T>((T)Enum.ToObject(_usageType, usage));
        return true;
      }
      input = null;
      return false;
    }
  }
}
