#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.UI.Players.BassPlayer.InputSources
{
  public abstract class AbstractBassResourceInputSource : IDisposable
  {
    #region Protected fields

    protected IFileSystemResourceAccessor _accessor = null;

    #endregion

    protected AbstractBassResourceInputSource(IFileSystemResourceAccessor resourceAccessor)
    {
      _accessor = resourceAccessor;
    }

    public IResourceAccessor ResourceAccessor
    {
      get { return _accessor; }
    }

    #region IDisposable implementation

    public virtual void Dispose()
    {
      if (_accessor != null)
        _accessor.Dispose();
    }

    #endregion

    public override string ToString()
    {
      return GetType().Name + (_accessor == null ? string.Empty : (": " + _accessor.ResourcePathName));
    }
  }
}
