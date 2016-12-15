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
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Dokan;
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Common.Services.Dokan.Native;

#pragma warning disable 649,169

namespace MediaPortal.Common.Services.Dokan
{
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public sealed class DokanFileInfo
  {
    private ulong _context;
    private readonly ulong _dokanContext;
    private readonly IntPtr _dokanOptions;
    private readonly uint _processId;

    [MarshalAs(UnmanagedType.U1)]
    private bool _isDirectory;

    [MarshalAs(UnmanagedType.U1)]
    private bool _deleteOnClose;

    [MarshalAs(UnmanagedType.U1)]
    private bool _pagingIo;

    [MarshalAs(UnmanagedType.U1)]
    private bool _synchronousIo;

    [MarshalAs(UnmanagedType.U1)]
    private bool _nocache;

    [MarshalAs(UnmanagedType.U1)]
    private bool _writeToEndOfFile;

    public object Context
    {
      get { return _context != 0 ? ((GCHandle)((IntPtr)_context)).Target : null; }
      set
      {
        if (_context != 0)
        {
          ((GCHandle)((IntPtr)_context)).Free();
          _context = 0;
        }
        if (value != null)
        {
          _context = (ulong)(IntPtr)GCHandle.Alloc(value);
        }
      }
    }

    public int ProcessId
    {
      get { return (int)_processId; }
    }

    public bool IsDirectory
    {
      get { return _isDirectory; }
      set { _isDirectory = value; }
    }

    public bool DeleteOnClose
    {
      get { return _deleteOnClose; }
      set { _deleteOnClose = value; }
    }

    public bool PagingIo
    {
      get { return _pagingIo; }
    }

    public bool SynchronousIo
    {
      get { return _synchronousIo; }
    }

    public bool NoCache
    {
      get { return _nocache; }
    }

    public bool WriteToEndOfFile
    {
      get { return _writeToEndOfFile; }
    }

    public WindowsIdentity GetRequestor()
    {
      try
      {
        return new WindowsIdentity(DokanNativeMethods.DokanOpenRequestorToken(this));
      }
      catch
      {
        return null;
      }
    }

    public bool TryResetTimeout(int milliseconds)
    {
      return DokanNativeMethods.DokanResetTimeout((uint)milliseconds, this);
    }

    private DokanFileInfo()
    {
    }
  }
}

#pragma warning restore 649, 169
