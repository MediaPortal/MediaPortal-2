#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.Common.Services.ResourceAccess.RemoteResourceProvider
{
  public class RemoteFileResourceAccessor : RemoteResourceAccessorBase
  {
    protected DateTime _lastChanged;
    protected long _size;

    protected RemoteFileResourceAccessor(string nativeSystemId, ResourcePath nativeResourcePath,
        string resourcePathName, string resourceName, DateTime lastChanged, long size) :
        base(nativeSystemId, nativeResourcePath, true, resourcePathName, resourceName)
    {
      _lastChanged = lastChanged;
      _size = size;
    }

    public static bool ConnectFile(string nativeSystemId, ResourcePath nativeResourcePath, out IResourceAccessor result)
    {
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      result = null;
      bool isFileSystemResource;
      bool isFile;
      string resourcePathName;
      string resourceName;
      DateTime lastChanged;
      long size;
      if (!rris.GetResourceInformation(nativeSystemId, nativeResourcePath,
          out isFileSystemResource, out isFile, out resourcePathName, out resourceName, out lastChanged, out size) ||
              !isFile)
        return false;
      result = new RemoteFileResourceAccessor(nativeSystemId, nativeResourcePath, resourcePathName, resourceName, lastChanged, size);
      return true;
    }

    public override long Size
    {
      get { return _size; }
    }

    #region IResourceAccessor implementation

    public override bool Exists
    {
      get
      {
        IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
        return rris.ResourceExists(_nativeSystemId, _nativeResourcePath);
      }
    }

    public override DateTime LastChanged
    {
      get { return _lastChanged; }
    }

    public override IResourceAccessor Clone()
    {
      return new RemoteFileResourceAccessor(_nativeSystemId, _nativeResourcePath, _resourcePathName, _resourceName, _lastChanged, _size);
    }

    #endregion
  }
}