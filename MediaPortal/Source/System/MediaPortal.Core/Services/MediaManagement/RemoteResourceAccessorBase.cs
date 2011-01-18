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
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Core.Services.MediaManagement
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

    public SystemName NativeSystem
    {
      get { return _resourceLocator.NativeSystem; }
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
      if (!_isFile)
        return;
      IRemoteResourceInformationService rris;
      string resourceURL = null;
      if (_underlayingStream == null)
      {
        rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
        resourceURL = rris.GetFileHttpUrl(_resourceLocator.NativeSystem, _resourceLocator.NativeResourcePath);
      }
      lock (_syncObj)
        if (_underlayingStream == null)
          _underlayingStream = new CachedHttpResourceStream(resourceURL, Size);
    }

    public Stream OpenRead()
    {
      if (!IsFile)
        throw new IllegalCallException("Only files provide stream access");
      PrepareStreamAccess();
      return new SynchronizedMasterStreamClient(_underlayingStream, _syncObj);
    }

    public Stream OpenWrite()
    {
      if (!IsFile)
        throw new IllegalCallException("Only files provide stream access");
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