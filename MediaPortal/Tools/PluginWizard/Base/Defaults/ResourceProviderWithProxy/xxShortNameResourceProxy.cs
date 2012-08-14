#region Copyright (C) 2007-xxCurrentYear Team MediaPortal

/*
    Copyright (C) 2007-xxCurrentYear Team MediaPortal
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
using System.IO;
using DiscUtils;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Extensions.ResourceProviders.xxShortNameResourceProvider
{
  internal class xxShortNameResourceProxy : IDisposable
  {
    #region Protected fields

    protected IFileSystem _diskFileSystem;
    protected string _key;
    protected int _usageCount = 0;
    protected IResourceAccessor _xxShortNameFileResourceAccessor;
    protected Stream _underlayingStream;
    protected object _syncObj = new object();

    #endregion

    #region Ctor

    public xxShortNameResourceProxy(string key, IResourceAccessor xxShortNameFileResourceAccessor)
    {
      _key = key;
      _xxShortNameFileResourceAccessor = xxShortNameFileResourceAccessor;

      _underlayingStream = _xxShortNameFileResourceAccessor.OpenRead();
      try
      {
        _diskFileSystem = GetFileSystem(_underlayingStream);
      }
      catch
      {
        _underlayingStream.Dispose();
        throw;
      }
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      if (_diskFileSystem != null)
      {
        IDisposable d = _diskFileSystem as IDisposable;
        if (d != null)
          d.Dispose();
        _diskFileSystem = null;
      }
      if (_underlayingStream != null)
      {
        _underlayingStream.Dispose();
        _underlayingStream = null;
      }
      if (_xxShortNameFileResourceAccessor != null)
      {
        _xxShortNameFileResourceAccessor.Dispose();
        _xxShortNameFileResourceAccessor = null;
      }
    }

    #endregion

    protected void FireOrphaned()
    {
      OrphanedDlgt dlgt = Orphaned;
      if (dlgt != null)
        dlgt(this);
    }

    public object SyncObj
    {
      get { return _syncObj; }
    }

    public string Key
    {
      get { return _key; }
    }

    public IResourceAccessor xxShortNameFileResourceAccessor
    {
      get { return _xxShortNameFileResourceAccessor; }
    }

    public IFileSystem DiskFileSystem
    {
      get { return _diskFileSystem; }
    }

    public int UsageCount
    {
      get { return _usageCount; }
    }

    public delegate void OrphanedDlgt(xxShortNameResourceProxy proxy);

    public OrphanedDlgt Orphaned;

    public void DecUsage()
    {
      lock (_syncObj)
      {
        _usageCount--;
        if (_usageCount > 0)
          return;
      }
      // Outside the lock:
      FireOrphaned();
    }

    public void IncUsage()
    {
      lock (_syncObj)
        _usageCount++;
    }

    public override string ToString()
    {
      return string.Format("xxShortName file proxy object for file '{0}', using {1} as file system", _xxShortNameFileResourceAccessor.CanonicalLocalResourcePath, _diskFileSystem);
    }

    public static IFileSystem GetFileSystem(Stream underlayingStream)
    {
      // Add your code here
      
    }
  }
}
