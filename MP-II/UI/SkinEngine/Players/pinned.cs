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
using System.Runtime.InteropServices;

namespace Clifton.Tools.Interop
{
  /// <summary>
  /// A helper class for pinning a managed structure so that it is suitable for
  /// unmanaged calls. A pinned object will not be collected and will not be moved
  /// by the GC until explicitly freed.
  /// </summary>
  public class PinnedObject<T> : IDisposable where T : struct
  {
    protected T managedObject;
    protected GCHandle handle;
    protected IntPtr ptr;
    protected bool disposed;

    public T ManangedObject
    {
      get 
      {
        return (T)handle.Target;
      }
      set
      {
        Marshal.StructureToPtr(value, ptr, false);
      }
    }

    public IntPtr Pointer
    {
      get { return ptr; }
    }

    public PinnedObject()
    {
      handle = GCHandle.Alloc(managedObject, GCHandleType.Pinned);
      ptr = handle.AddrOfPinnedObject();
    }

    ~PinnedObject()
    {
      Dispose();
    }

    public void Dispose()
    {
      if (!disposed)
      {
        handle.Free();
        ptr = IntPtr.Zero;
        disposed = true;
      }
    }
  }
}
