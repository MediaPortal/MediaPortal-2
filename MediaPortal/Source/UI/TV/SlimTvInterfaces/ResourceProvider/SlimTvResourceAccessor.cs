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
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider
{
  public class SlimTvResourceAccessor : INetworkResourceAccessor
  {
    private readonly string _path;
    private readonly int _slotIndex;

    public SlimTvResourceAccessor(int slotIndex, string path)
    {
      _path = path;
      _slotIndex = slotIndex;
    }

    #region IResourceAccessor Member

    public IResourceProvider ParentProvider
    {
      get { return null; }
    }

    public string Path
    {
      get { return _path; }
    }

    public string URL
    {
      get { return _path; }
    }

    public string ResourceName
    {
      get { return System.IO.Path.GetFileName(_path); }
    }

    public string ResourcePathName
    {
      get { return _path; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get
      {
        // format the path with the slotindex as prefix.
        return ResourcePath.BuildBaseProviderPath(SlimTvResourceProvider.SLIMTV_RESOURCE_PROVIDER_ID, String.Format("{0}|{1}", _slotIndex, _path));
      }
    }

    public IResourceAccessor Clone()
    {
      return new SlimTvResourceAccessor(_slotIndex, _path);
    }

    #endregion

    #region IDisposable Member

    public void Dispose()
    {
      ITvHandler tv = ServiceRegistration.Get<ITvHandler>(false);
      if (tv != null)
        tv.DisposeSlot(_slotIndex);
    }

    #endregion
  }
}