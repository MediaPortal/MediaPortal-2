#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

namespace MediaPortal.SkinEngine.Xaml.Interfaces
{
  /// <summary>
  /// Interface to mark controls to constitute an own nameing scope.
  /// Inside a name scope, all element names are guaranteed to be unique.
  /// The name scope may have a parent name scope, whose names merge into
  /// this name scope, if not existing here.
  /// </summary>
  public interface INameScope
  {
    /// <summary>
    /// Searches the specified <paramref name="name"/> in this naming scope.
    /// If registered, the method returns the object mapped to that name.
    /// If the name is not present in this name scope, the method will search
    /// in the parent name scope, if present.
    /// </summary>
    /// <param name="name">Name of the object to search.</param>
    /// <returns>Object registered to the specified <paramref name="name"/>,
    /// either in this name scope or in one of the parent name scopes.
    /// The method returns <c>null</c>, if the name was not found.</returns>
    object FindName(string name);

    /// <summary>
    /// Registeres the specified <paramref name="instance"/> with the specified
    /// <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Name to register.</param>
    /// <param name="instance">Instance to map to the specified
    /// <paramref name="name"/>.</param>
    void RegisterName(string name, object instance);

    /// <summary>
    /// Unregisteres the specified <paramref name="name"/> in this naming scope.
    /// </summary>
    /// <param name="name">Name to unregister.</param>
    void UnregisterName(string name);
  }
}
