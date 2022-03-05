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

namespace MP2BootstrapperApp.BootstrapperWrapper
{
  /// <summary>
  /// Interface for a class that can get and set variables from the installation engine.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public interface IVariables<T>
  {
    /// <summary>
    /// Gets or sets the variable with the specified name.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <returns>The value of the variable.</returns>
    /// <exception cref="System.Exception">Thrown if an error occurs getting the variable.</exception>
    T this[string name] { get; set; }

    /// <summary>
    /// Gets whether the variable with the specified name exists.
    /// </summary>
    /// <param name="name">The value of the variable.</param>
    /// <returns><c>true</c> if the variable exists; otherwise, <c>false</c>.</returns>
    bool Contains(string name);
  }
}
