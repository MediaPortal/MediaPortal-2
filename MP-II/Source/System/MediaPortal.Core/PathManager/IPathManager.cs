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

using System;

namespace MediaPortal.Core.PathManager
{
  /// <summary>
  /// Registration for local file path locations.
  /// </summary>
  /// <remarks>
  /// This class helps to resolve complete file paths for the local system by specifying
  /// a path string with placeholders for special registered path parts, like the path
  /// where the application was started (APPLICATION_ROOT), for example.
  /// Its also possible to add/remove/replace path registrations manually.
  /// A path pattern to be resolved by the PathManager can contain any path labels to be replaced
  /// with the registered values. All labels in the form &lt;[LABLE]&gt; will be replaced
  /// by their corresponding registered path.
  /// Registered path patterns can also contain references to other path registrations.
  /// <example>A path pattern will look like this:
  /// <c>&lt;LOG&gt;\MediaPortal.log</c>, where <c>&lt;LOG&gt;</c> is a reference to the path registered
  /// for the label <i>LOG</i>. If the label <i>LOG</i> was registered with the path
  /// <i>C:\Temp</i> for example, the pattern would be resolved to <i>C:\Temp\MediaPortal.log</i>.</example>
  /// </remarks>
  public interface IPathManager
  {
    /// <summary>
    /// Checks if a path with the specified label is registered.
    /// </summary>
    /// <param name="label">Label to lookup in the registration.</param>
    /// <returns>True, if the specified label exists in our path registration, else false.</returns>
    bool Exists(string label);

    /// <summary>
    /// Registers the specified <paramref name="pathPattern"/> for the specified
    /// <paramref name="label"/>. If the label is already registered, it will be replaced.
    /// </summary>
    /// <param name="label">The lookup label for the new path.</param>
    /// <param name="pathPattern">The path pattern to be registered with the <paramref name="label"/>.
    /// This pattern may contain references to other path registrations.</param>
    void SetPath(string label, string pathPattern);

    /// <summary>
    /// Resolves the specified pathPattern, as described in the class documentation.
    /// </summary>
    /// <param name="pathPattern">The path pattern to be resolved.</param>
    /// <returns>The resolved path as a string.</returns>
    /// <exception cref="ArgumentException">When the specified <paramref name="pathPattern"/>
    /// contains labels that are not registered.</exception>
    string GetPath(string pathPattern);

    /// <summary>
    /// Removes the path registration for the specified <paramref name="label"/>.
    /// </summary>
    /// <param name="label">The label of the path registration to be removed.</param>
    void RemovePath(string label);
  }
}
