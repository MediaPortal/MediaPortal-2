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
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.ResourceAccess.VirtualResourceProvider
{
  public class VirtualResourceAccessor : IResourceAccessor
  {
    protected VirtualResourceProvider _provider;
    protected Guid _virtualId;

    public VirtualResourceAccessor(VirtualResourceProvider provider, Guid virtualId)
    {
      _provider = provider;
      _virtualId = virtualId;
    }

    public void Dispose() { }

    #region IResourceAccessor implementation

    public IResourceProvider ParentProvider
    {
      get { return _provider; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(VirtualResourceProvider.VIRTUAL_RESOURCE_PROVIDER_ID, 
        VirtualResourceProvider.BuildProviderPath(_virtualId)); }
    }

    public DateTime LastChanged
    {
      get { return DateTime.MinValue; }
    }

    public long Size
    {
      get { return 0; }
    }

    public void PrepareStreamAccess()
    {
      // Nothing to do
    }

    public Stream OpenRead()
    {
      return null; // No direct stream access supported.
    }

    public Stream OpenWrite()
    {
      return null; // No direct stream access supported.
    }

    public bool Exists
    {
      get { return true; }
    }

    public bool IsFile
    {
      get { return false; }
    }

    public string Path
    {
      get { return VirtualResourceProvider.BuildProviderPath(_virtualId); }
    }

    public string ResourceName
    {
      get { return string.Format("Virtual Resource {0}", _virtualId); }
    }

    public string ResourcePathName
    {
      get { return string.Format("Virtual Resource {0}:", _virtualId); }
    }

    public IResourceAccessor Clone()
    {
      return new VirtualResourceAccessor(_provider, _virtualId);
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return ResourcePathName;
    }

    #endregion
  }
}
