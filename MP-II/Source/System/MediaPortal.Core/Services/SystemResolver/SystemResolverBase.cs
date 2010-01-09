#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core.General;
using MediaPortal.Core.Services.SystemResolver.Settings;
using MediaPortal.Core.Settings;
using MediaPortal.Core.SystemResolver;

namespace MediaPortal.Core.Services.SystemResolver
{
  public abstract class SystemResolverBase : ISystemResolver
  {
    protected string _localSystemId;

    protected SystemResolverBase()
    {
      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
      SystemResolverSettings settings = settingsManager.Load<SystemResolverSettings>();
      if (string.IsNullOrEmpty(settings.SystemId))
      {
        // Create a new id for our local system
        settings.SystemId = _localSystemId = Guid.NewGuid().ToString("D");
        settingsManager.Save(settings);
      }
      else
        _localSystemId = settings.SystemId;
    }

    #region ISystemResolver implementation

    public string LocalSystemId
    {
      get { return _localSystemId; }
    }

    public abstract SystemName GetSystemNameForSystemId(string sytemId);

    #endregion
  }
}