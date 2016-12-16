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

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  /// <summary>
  /// Identifies an unmanaged DirectX resource
  /// </summary>
  /// <remarks>
  /// Asset objects are maybe shared between different client objects, potentially from multiple threads. So implementations must be
  /// thread-safe.
  /// </remarks>
  public interface IAsset
  {
    /// <summary>
    /// Gets a value indicating the asset is allocated.
    /// </summary>
    /// <value>
    /// <c>true</c> if this asset is allocated; otherwise, <c>false</c>.
    /// </value>
    bool IsAllocated { get; }
  }
}