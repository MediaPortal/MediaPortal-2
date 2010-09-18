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
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement.ResourceAccess;

namespace MediaPortal.Core.Services.MediaManagement
{
  public class RemoteFileResourceAccessor : RemoteResourceAccessorBase, IResourceAccessor
  {
    protected DateTime _lastChanged;
    protected long _size;

    protected RemoteFileResourceAccessor(SystemName nativeSystem, ResourcePath nativeResourcePath,
        string resourcePathName, string resourceName, DateTime lastChanged, long size) :
        base(nativeSystem, nativeResourcePath, true, resourcePathName, resourceName)
    {
      _lastChanged = lastChanged;
      _size = size;
    }

    public static bool ConnectFile(SystemName nativeSystem, ResourcePath nativeResourcePath, out IResourceAccessor result)
    {
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      result = null;
      bool isFileSystemResource;
      bool isFile;
      string resourcePathName;
      string resourceName;
      DateTime lastChanged;
      long size;
      if (!rris.GetResourceInformation(nativeSystem, nativeResourcePath, out isFileSystemResource, out isFile,
          out resourcePathName, out resourceName, out lastChanged, out size) || !isFile)
        return false;
      result = new RemoteFileResourceAccessor(nativeSystem, nativeResourcePath,
          resourcePathName, resourceName, lastChanged, size);
      return true;
    }

    public override long Size
    {
      get { return _size; }
    }

    #region IResourceAccessor implementation

    public override DateTime LastChanged
    {
      get { return _lastChanged; }
    }

    #endregion
  }
}