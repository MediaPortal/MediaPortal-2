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

using MediaPortal.UI.SkinEngine.ContentManagement.AssetCore;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  /// <summary>
  /// In order to properly manage resources, all Assets are actually two components: A wrapper that 
  /// can GC'd freely and a core object that holds the actual DirectX resource and is managed by the 
  /// <see cref="ContentManager"/>. This is a base class for all asset wrappers.
  /// </summary>
  /// <typeparam name="T">The asset core type.</typeparam>
  public class AssetWrapper<T> : IAsset where T : IAssetCore
  {
    protected T _assetCore;

    public event AssetAllocationHandler AllocationChanged
    {
      add { _assetCore.AllocationChanged += value; }
      remove { _assetCore.AllocationChanged -= value; }
    }

    public AssetWrapper(T assetCore)
    {
      _assetCore = assetCore;
    }

    public bool IsAllocated 
    {
      get { return _assetCore.IsAllocated; } 
    }

    public int AllocationSize 
    {
      get { return _assetCore.AllocationSize; } 
    }
  }
}