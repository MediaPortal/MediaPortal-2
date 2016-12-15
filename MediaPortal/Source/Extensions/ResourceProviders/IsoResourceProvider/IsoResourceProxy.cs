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
using System.IO;
using DiscUtils;
using MediaPortal.Common.ResourceAccess;
using DiscUtils.Iso9660;
using DiscUtils.Udf;

namespace MediaPortal.Extensions.ResourceProviders.IsoResourceProvider
{
  internal class IsoResourceProxy : IDisposable
  {
    #region Protected fields

    protected IFileSystem _diskFileSystem;
    protected string _key;
    protected int _usageCount = 0;
    protected IFileSystemResourceAccessor _isoFileResourceAccessor;
    protected Stream _underlayingStream;
    protected object _syncObj = new object();

    #endregion

    #region Ctor

    public IsoResourceProxy(string key, IFileSystemResourceAccessor isoFileResourceAccessor)
    {
      _key = key;
      _isoFileResourceAccessor = isoFileResourceAccessor;

      _underlayingStream = Stream.Synchronized(_isoFileResourceAccessor.OpenRead());
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
      lock (SyncObj)
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
      if (_isoFileResourceAccessor != null)
      {
        _isoFileResourceAccessor.Dispose();
        _isoFileResourceAccessor = null;
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

    public IFileSystemResourceAccessor IsoFileResourceAccessor
    {
      get { return _isoFileResourceAccessor; }
    }

    public IFileSystem DiskFileSystem
    {
      get { return _diskFileSystem; }
    }

    public int UsageCount
    {
      get { return _usageCount; }
    }

    public delegate void OrphanedDlgt(IsoResourceProxy proxy);

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
      return string.Format("ISO file proxy object for file '{0}', using {1} as file system", _isoFileResourceAccessor.CanonicalLocalResourcePath, _diskFileSystem);
    }

    public static IFileSystem GetFileSystem(Stream underlayingStream)
    {
      // Try UDF access first; if that doesn't work, try iso9660
      try
      {
        if (!UdfReader.Detect(underlayingStream))
          throw new ArgumentException("The given stream does not contain a valid UDF filesystem");
        return new UdfReader(underlayingStream);
      }
      catch
      {
        if (!CDReader.Detect(underlayingStream))
          throw new ArgumentException("The given stream does neither contain a valid UDF nor a valid ISO9660 filesystem");
        return new CDReader(underlayingStream, true, true);
      }
    }
  }
}
