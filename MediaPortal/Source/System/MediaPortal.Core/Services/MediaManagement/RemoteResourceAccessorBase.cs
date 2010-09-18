#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Core.Services.MediaManagement
{
  public abstract class RemoteResourceAccessorBase : ResourceAccessorBase, IResourceAccessor
  {
    protected SystemName _nativeSystem;
    protected ResourcePath _nativeResourcePath;
    protected bool _isFile;
    protected string _resourcePathName;
    protected string _resourceName;

    protected RemoteResourceAccessorBase(SystemName nativeSystem, ResourcePath nativeResourcePath,
        bool isFile, string resourcePathName, string resourceName)
    {
      _nativeSystem = nativeSystem;
      _nativeResourcePath = nativeResourcePath;
      _isFile = isFile;
      _resourcePathName = resourcePathName;
      _resourceName = resourceName;
    }

    public SystemName NativeSystem
    {
      get { return _nativeSystem; }
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
      get { return _nativeResourcePath; }
    }

    public abstract DateTime LastChanged { get; }

    public Stream OpenRead()
    {
      if (!IsFile)
        throw new IllegalCallException("Only files provide stream access");
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      string resourceURL = rris.GetFileHttpUrl(_nativeSystem, _nativeResourcePath);
      return new CachedHttpResourceStream(resourceURL, Size);
    }

    public Stream OpenWrite()
    {
      if (!IsFile)
        throw new IllegalCallException("Only files provide stream access");
      return null;
    }

    #endregion
  }
}