#region Copyright (C) 2007-xxCurrentYear Team MediaPortal

/*
    Copyright (C) 2007-xxCurrentYear Team MediaPortal
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
using MediaPortal.Extensions.BassLibraries;

namespace MediaPortal.Extensions.ResourceProviders.xxPluginName
{
  public class xxShortNameResourceAccessor : IResourceAccessor
  {
    protected xxPluginName _provider;
    
    public xxShortNameResourceAccessor(xxPluginName provider)
    {
      _provider = provider;
      
    }

    public void Dispose() { }

    #region IResourceAccessor implementation

    public IResourceProvider ParentProvider
    {
      get { return _provider; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(xxPluginName.RESOURCE_PROVIDER_ID, xxPluginName.BuildProviderPath()); }
    }

    public DateTime LastChanged
    {
      get { return DateTime.MinValue; }
    }

    public long Size
    {
      get { return ; }
    }

    public void PrepareStreamAccess()
    {
      
    }

    public Stream OpenRead()
    {
      return ; 
    }

    public Stream OpenWrite()
    {
      return ;
    }

    public bool Exists
    {
      get { return ; }
    }

    public bool IsFile
    {
      get { return ; }
    }

    public string Path
    {
      get { return xxPluginName.BuildProviderPath(); }
    }

    public string ResourceName
    {
      get { return ; }
    }

    public string ResourcePathName
    {
      get { return ; }
    }

    public IResourceAccessor Clone()
    {
      return new xxShortNameResourceAccessor(_provider);
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
