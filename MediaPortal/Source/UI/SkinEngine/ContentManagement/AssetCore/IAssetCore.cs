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

namespace MediaPortal.UI.SkinEngine.ContentManagement.AssetCore
{
  public delegate void AssetAllocationHandler(int allocation);

  /// <summary>
  /// Extended interface for the direct access of asset resources. Provides properties and methods for freeing 
  /// unmanaged resources and determining if those resources are still needed.
  /// </summary>
  /// <remarks>
  /// Implementation note for subclasses:
  /// Asset core objects are maybe shared between different client objects, potentially from multiple threads. So implementations must be
  /// thread-safe. Rendering and disposal is always done by a single thread. So it is sufficient to protect all other methods - particularly
  /// allocation and helper/support methods.
  /// </remarks>
  public interface IAssetCore : IAsset
  {
    /// <summary>
    /// Event for notifying managing classes of changes in allocation quantity/state.
    /// </summary>
    event AssetAllocationHandler AllocationChanged;

    /// <summary>
    /// Gets a value indicating whether this asset can be deleted.
    /// </summary>
    /// <value>
    /// <c>true</c> if this asset can be deleted; otherwise, <c>false</c>.
    /// </value>
    bool CanBeDeleted { get; }

    /// <summary>
    /// Frees this asset. Should only ever be called by the ContentManager.Instance.
    /// </summary>
    void Free();

    /// <summary>
    /// Gets a value indicating the estimated current VRAM usage of this asset.
    /// </summary>
    /// <value>
    /// The estimated VRAM usage of the asset in bytes.
    /// </value>
    int AllocationSize { get; }
  }
}