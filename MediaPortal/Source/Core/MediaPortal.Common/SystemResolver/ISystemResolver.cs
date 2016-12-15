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

using MediaPortal.Common.General;

namespace MediaPortal.Common.SystemResolver
{
  public enum SystemType
  {
    Client,
    Server
  }

  public interface ISystemResolver
  {
    string LocalSystemId { get; }

    /// <summary>
    /// Gets the name of the system with the given <paramref name="systemId"/>.
    /// </summary>
    /// <param name="systemId">Id of the system to resolve.</param>
    /// <returns>System name or <c>null</c>, if the system could not resolved (i.e. if the server is not connected or
    /// the system with the given <paramref name="systemId"/> is not known.</returns>
    SystemName GetSystemNameForSystemId(string systemId);

    /// <summary>
    /// Returns the type of this system.
    /// </summary>
    SystemType SystemType { get; }
  }
}