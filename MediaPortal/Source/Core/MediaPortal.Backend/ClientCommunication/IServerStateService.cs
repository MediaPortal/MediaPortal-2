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

namespace MediaPortal.Backend.ClientCommunication
{
  /// <summary>
  /// Interface for sending arbitary state objects to connected clients.
  /// </summary>
  public interface IServerStateService
  {
    /// <summary>
    /// Updates the state with the given <paramref name="stateId"/> and notifies connected clients of the change.
    /// Any state object previously associated with tne <paramref name="stateId"/> will be replaced by the new state object.
    /// </summary>
    /// <param name="stateId">The unique id of the state.</param>
    /// <param name="state">An object representing the state. This must be serializable by the XmlSerializer.</param>
    void UpdateState(Guid stateId, object state);
  }
}