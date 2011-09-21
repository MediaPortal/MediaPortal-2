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
using System.IO;
using MediaPortal.Common.MediaManagement.ResourceAccess;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Common.Services.MediaManagement
{
  /// <summary>
  /// Multithreading-safe base class for remote resource accessors. Provides metadata and a remote access stream.
  /// </summary>
  public abstract class RemoteResourceAccessorBase : IResourceAccessor
  {
    protected object _syncObj = new object();
    protected IResourceLocator _resourceLocator;
    protected bool _isFile;
    protected string _resourcePathName;
    protected string _resourceName;
    protected Stream _underlayingStream = null; // Lazy initialized

    protected RemoteResourceAccessorBase(IResourceLocator resourceLocator,
        bool isFile, string resourcePathName, string resourceName)
    {
      _resourceLocator = resourceLocator;
      _isFile = isFile;
      _resourcePathName = resourcePathName;
      _resourceName = resourceName;
    }

    public void Dispose()
    {
      lock (_syncObj)
        if (_underlayingStream != null)
        {
          _underlayingStream.Dispose();
          _underlayingStream = null;
        }
    }

    public string NativeSystemId
    {
      get { return _resourceLocator.NativeSystemId; }
    }

    public IResourceLocator ResourceLocator
    {
      get { return _resourceLocator; }
    }

    public abstract long Size { get; }

    #region IResourceAccessor implementation

    public IMediaProvider ParentProvider
    {
      get { return null; }
    }

    public abstract bool Exists { get; }

    public bool IsFile
    {
      get { return _isFile; }
    }

    public string ResourceName
    {
      get { return _resourceName; }
    }

    public string ResourcePathName
    {
      get { return _resourcePathName; }
    }

    public ResourcePath LocalResourcePath
    {
      get { return _resourceLocator.NativeResourcePath; }
    }

    public abstract DateTime LastChanged { get; }

    public void PrepareStreamAccess()
    {
      if (!_isFile || _underlayingStream != null)
        return;
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      string resourceURL = rris.GetFileHttpUrl(_resourceLocator.NativeSystemId, _resourceLocator.NativeResourcePath);
      lock (_syncObj)
      {
        _underlayingStream = new CachedMultiSegmentHttpStream(resourceURL, Size);
      }
    }

    public Stream OpenRead()
    {
      if (!_isFile)
        throw new IllegalCallException("Only files can provide stream access");
      PrepareStreamAccess();
      return new SynchronizedMasterStreamClient(_underlayingStream, _syncObj);
    }

    public Stream OpenWrite()
    {
      if (!_isFile)
        throw new IllegalCallException("Only files can provide stream access");
      return null;
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("Remote resource accessor for '{0}'", _resourceLocator);
    }

    #endregion
  }
}