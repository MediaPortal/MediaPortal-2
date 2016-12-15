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
using System.Collections.Generic;

namespace MediaPortal.UI.ServerCommunication
{
  /// <summary>
  /// Interface for managing server states
  /// </summary>
  public interface IServerStateManager
  {
    /// <summary>
    /// Gets all currently known states. 
    /// </summary>
    /// <returns>Mapping of state guids to states</returns>
    IDictionary<Guid, object> GetAllStates();
    
    /// <summary>
    /// Tries to get the state of the given type with the given id.
    /// </summary>
    /// <typeparam name="T">The type of the state object.</typeparam>
    /// <param name="stateId">The unique id of the state.</param>
    /// <param name="state">If successful, the state object, otherwise undefined.</param>
    /// <returns>True if the state of the given type was found</returns>
    bool TryGetState<T>(Guid stateId, out T state);
  }
}
