#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Core.Services.MediaManagement
{
  public abstract class ResourceAccessorBase : IDisposable
  {
    internal ICollection<ITidyUpExecutor> _tidyUpExecutors = null;

    ~ResourceAccessorBase()
    {
      Dispose();
    }

    public virtual void Dispose()
    {
      if (_tidyUpExecutors == null)
        return;
      foreach (ITidyUpExecutor executor in _tidyUpExecutors)
        executor.Execute();
      _tidyUpExecutors.Clear();
    }

    public void AddTidyUpExecutor(ITidyUpExecutor executor)
    {
      if (_tidyUpExecutors == null)
        _tidyUpExecutors = new List<ITidyUpExecutor>();
      _tidyUpExecutors.Add(executor);
    }
  }
}