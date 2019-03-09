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
using MediaPortal.Common.General;
using MediaPortal.Common.Services.SystemResolver.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;

namespace MediaPortal.Common.Services.SystemResolver
{
  public abstract class SystemResolverBase : ISystemResolver
  {
    protected string _localSystemId;

    protected SystemResolverBase()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
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

    public abstract SystemName GetSystemNameForSystemId(string systemId);

    public abstract SystemType SystemType { get; }

    #endregion
  }
}