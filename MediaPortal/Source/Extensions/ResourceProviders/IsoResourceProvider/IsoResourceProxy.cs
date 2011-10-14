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
using ISOReader;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;

namespace MediaPortal.Extensions.ResourceProviders.IsoResourceProvider
{
  internal class IsoResourceProxy : IDisposable
  {
    #region Protected fields

    protected IsoReader _isoReader;
    protected string _key;
    protected int _usageCount = 0;
    protected IResourceAccessor _isoFileResourceAccessor;
    protected ILocalFsResourceAccessor _baseLocalFsIsoResourceAccessor;
    protected object _syncObj = new object();

    #endregion

    #region Ctor

    public IsoResourceProxy(string key, IResourceAccessor isoFileResourceAccessor)
    {
      _key = key;
      _isoFileResourceAccessor = isoFileResourceAccessor;
      _baseLocalFsIsoResourceAccessor = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(isoFileResourceAccessor.Clone()); // The StreamedResourceToLocalFsAccessBridge might dispose the given RA
      try
      {
        _isoReader = new IsoReader();
        _isoReader.Open(_baseLocalFsIsoResourceAccessor.LocalFileSystemPath);

      }
      catch
      {
        _isoReader.Close();
        _baseLocalFsIsoResourceAccessor.Dispose();
        throw;
      }
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      if (_isoReader != null)
      {
        _isoReader.Dispose();
        _isoReader = null;
      }
      if (_baseLocalFsIsoResourceAccessor != null)
      {
        _baseLocalFsIsoResourceAccessor.Dispose();
        _baseLocalFsIsoResourceAccessor = null;
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

    public IsoReader IsoReader
    {
      get { return _isoReader; }
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
      return string.Format("ISO file proxy object for file '{0}'", _baseLocalFsIsoResourceAccessor.LocalFileSystemPath);
    }
  }
}
