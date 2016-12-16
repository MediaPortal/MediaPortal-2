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

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Resource accessor interface to access resources which are available in the local network and
  /// which are represented by an URL. The access to resources of this interface is not managed by the
  /// MediaPortal infrastructure, i.e. there are no methods providing streams or similar accessibility
  /// components. The access to the referenced resource has to be provided by the module which uses this
  /// resource accessor.
  /// </summary>
  public interface INetworkResourceAccessor : IResourceAccessor
  {
    /// <summary>
    /// URL to the resource in the local network.
    /// </summary>
    string URL { get; }
  }
}