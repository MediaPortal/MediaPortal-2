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
using System.Linq;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider
{
  /// <summary>
  /// Simple <see cref="INetworkResourceAccessor"/> implementation that handles a raw url.
  /// Bound to the <see cref="RawUrlResourceProvider"/>.
  /// </summary>
  public class RawUrlResourceAccessor : INetworkResourceAccessor
  {
    protected string _rawUrl = string.Empty;

    public RawUrlResourceAccessor(string url)
    {
      _rawUrl = url;
    }

    public string URL
    {
      get { return _rawUrl; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get { return RawUrlResourceProvider.ToProviderResourcePath(_rawUrl); }
    }

    public IResourceAccessor Clone()
    {
      return new RawUrlResourceAccessor(_rawUrl);
    }

    public IResourceProvider ParentProvider
    {
      get { return null; }
    }

    public string Path
    {
      get { return ResourcePath.BuildBaseProviderPath(RawUrlResourceProvider.RAW_URL_RESOURCE_PROVIDER_ID, _rawUrl).Serialize(); }
    }

    public string ResourceName
    {
      get { return new Uri(_rawUrl).Segments.Last(); }
    }

    public string ResourcePathName
    {
      get { return _rawUrl; }
    }

    public void Dispose ()
    {
    }
  }
}

