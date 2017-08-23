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
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Extensions.ResourceProviders.ZipResourceProvider
{
  internal class ZipResourceProxy : IDisposable
  {
    protected ZipFile _zipFile;
    protected string _key;
    protected int _usageCount = 0;
    protected IFileSystemResourceAccessor _zipFileResourceAccessor;
    protected Stream _zipFileStream;
    protected IDictionary<string, string> _tempFilePaths = new Dictionary<string, string>(); // ZipEntry names to temp file paths
    protected object _syncObj = new object();

    public ZipResourceProxy(string key, IFileSystemResourceAccessor zipFileAccessor)
    {
      _key = key;
      _zipFileResourceAccessor = zipFileAccessor;
      _zipFileStream = _zipFileResourceAccessor.OpenRead(); // Not sure if the ZipFile closes the stream appropriately, so we keep a reference to it
      try
      {
        _zipFile = new ZipFile(_zipFileStream);
      }
      catch
      {
        _zipFileStream.Dispose();
        throw;
      }
    }

    public void Dispose()
    {
      foreach (string tempFilePath in _tempFilePaths.Values)
        try
        {
          File.Delete(tempFilePath);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("ZipResourceProxy: Unable to delete temp file '{0}'", e, tempFilePath);
        }
      _tempFilePaths.Clear();
      CloseZipFile();
      if (_zipFileResourceAccessor != null)
      {
        _zipFileResourceAccessor.Dispose();
        _zipFileResourceAccessor= null;
      }
    }

    protected void CloseZipFile()
    {
      lock (_syncObj)
      {
        if (_zipFile != null)
        {
          if (_usageCount > 0)
            ServiceRegistration.Get<ILogger>().Warn("ZipResourceProxy: Closing ZIP file which is still accessed by {0} resource accessors", _usageCount);
          _zipFile.Close();
          _zipFileStream.Dispose();
          _zipFile = null;
          _zipFileStream = null;
        }
      }
    }

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

    public IResourceAccessor ZipFileResourceAccessor
    {
      get { return _zipFileResourceAccessor; }
    }

    public ZipFile ZipFile
    {
      get { return _zipFile; }
    }

    public int UsageCount
    {
      get { return _usageCount; }
    }

    public delegate void OrphanedDlgt(ZipResourceProxy proxy);

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

    public string GetTempFile(ZipEntry entry)
    {
      lock (_syncObj)
      {
        string result;
        if (_tempFilePaths.TryGetValue(entry.Name, out result))
          return result;
        result = FileUtils.CreateHumanReadableTempFilePath(entry.Name);
        using (FileStream streamWriter = File.Create(result))
        {
          byte[] buffer = new byte[4096]; // 4K is optimum
          lock (_syncObj)
            StreamUtils.Copy(_zipFile.GetInputStream(entry), streamWriter, buffer);
        }
        _tempFilePaths.Add(new KeyValuePair<string, string>(entry.Name, result));
        return result;
      }
    }

    public override string ToString()
    {
      return string.Format("ZIP file proxy object for file '{0}'", _zipFileResourceAccessor.ResourceName);
    }
  }
}
