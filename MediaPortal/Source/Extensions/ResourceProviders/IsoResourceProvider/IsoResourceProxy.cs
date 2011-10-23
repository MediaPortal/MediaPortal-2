#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Common.ResourceAccess;
using DiscUtils.Iso9660;
using DiscUtils.Udf;

namespace MediaPortal.Extensions.ResourceProviders.IsoResourceProvider
{
  internal class IsoResourceProxy : IDisposable
  {
    #region Protected fields

    protected UdfReader _udfReader;
    protected CDReader _iso9660Reader;
    protected string _key;
    protected int _usageCount = 0;
    protected IResourceAccessor _isoFileResourceAccessor;
    protected object _syncObj = new object();

    #endregion

    #region Ctor

    public IsoResourceProxy(string key, IResourceAccessor isoFileResourceAccessor)
    {
      _key = key;
      _isoFileResourceAccessor = isoFileResourceAccessor;
      try
      {
        _udfReader = new UdfReader(_isoFileResourceAccessor.OpenRead());
      }
      catch
      {
        _udfReader = null;
      }

      try
      {
        _iso9660Reader = new CDReader(_isoFileResourceAccessor.OpenRead(), true, true);
      }
      catch
      {
        _iso9660Reader = null;
      }

    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      if (_udfReader != null)
      {
        _udfReader.Dispose();
        _udfReader = null;
      }
      if (_iso9660Reader != null)
      {
        _iso9660Reader.Dispose();
        _iso9660Reader = null;
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

    public IResourceAccessor IsoFileResourceAccessor
    {
      get { return _isoFileResourceAccessor; }
    }

    public UdfReader IsoUdfReader
    {
      get { return _udfReader; }
    }

    public CDReader Iso9660Reader
    {
      get { return _iso9660Reader; }
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
      return string.Format("ISO file proxy object for file '{0}'", _isoFileResourceAccessor.CanonicalLocalResourcePath);
    }
  }
}
