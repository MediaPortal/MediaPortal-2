#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;

namespace MediaPortal.Media.ClientMediaManager
{
  internal interface ITidyUpExecutor
  {
    void Execute();
  }

  public class MediaItemAccessorBase : IDisposable
  {
    protected MediaItemLocator _locator;
    internal ITidyUpExecutor _tidyUpExecutor;

    internal MediaItemAccessorBase(MediaItemLocator locator, ITidyUpExecutor tidyUpExecutor)
    {
      _locator = locator;
      _tidyUpExecutor = tidyUpExecutor;
    }

    ~MediaItemAccessorBase()
    {
      Dispose();
    }

    public void Dispose()
    {
      if (_tidyUpExecutor != null)
        _tidyUpExecutor.Execute();
      _tidyUpExecutor = null;
    }

    public MediaItemLocator Locator
    {
      get { return _locator; }
    }
  }
}