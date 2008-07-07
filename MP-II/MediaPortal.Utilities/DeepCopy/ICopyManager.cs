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

using System.Collections.Generic;

namespace MediaPortal.Utilities.DeepCopy
{
  /// <summary>
  /// Interface for providing access to instances copied from a source instance
  /// in a two-step deep copying process.
  /// </summary>
  public interface ICopyManager
  {
    /// <summary>
    /// Returns the object which was copied from the specified <paramref name="source"/>
    /// object.
    /// </summary>
    /// <returns>Copy of the specified <paramref name="source"/> object. The
    /// returned instance may not have finished its copying process, so it is not
    /// save to access fields on the returned object.</returns>
    T GetCopy<T>(T source);

    /// <summary>
    /// Returns a map of to-be-copied objects mapped to their copied couterpart.
    /// </summary>
    IDictionary<object, object> Identities { get; }
  }
}
