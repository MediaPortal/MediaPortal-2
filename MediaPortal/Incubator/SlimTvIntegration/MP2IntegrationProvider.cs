#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Logging;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Integration;
using ILogger = Mediaportal.TV.Server.TVLibrary.Interfaces.Integration.ILogger;

namespace MediaPortal.Plugins.SlimTv.Integration
{
  /// <summary>
  /// Integration provider to run TVE35 components in MP2 context. This provider will be used for TvLibrary.Services and the SetupTV GUI.
  /// For the SetupTV GUI (which runs outside of MP2 scope) it provides default PathManager and Logger.
  /// </summary>
  public class MP2IntegrationProvider : IIntegrationProvider
  {
    private PathManagerWrapper _pathManagerWrapper;
    private LoggerWrapper _loggerWrapper;

    protected void InitDefaults()
    {
      // Check if the required service is already present, then no action is required
      bool pathManagerAvailable = ServiceRegistration.Get<Common.PathManager.IPathManager>(false) != null;
      if (pathManagerAvailable && _pathManagerWrapper != null && _loggerWrapper != null)
        return;

      if (_pathManagerWrapper != null && _loggerWrapper != null) 
        return;

      // If running outside of MP2 scope
      if (!pathManagerAvailable)
      {
        ServiceRegistration.Set<Common.Logging.ILogger>(new NoLogger());

        Common.Services.PathManager.PathManager pathManager = new Common.Services.PathManager.PathManager();
        pathManager.InitializeDefaults();

        ServiceRegistration.Set<Common.PathManager.IPathManager>(pathManager);

        Common.Logging.ILogger logger = new Log4NetLogger(pathManager.GetPath(@"<LOG>"));
        logger.Info("MP2IntegrationProvider: Launching in AppDomain {0}...", AppDomain.CurrentDomain.FriendlyName);
      }

      _pathManagerWrapper = new PathManagerWrapper();
      _loggerWrapper = new LoggerWrapper();
    }

    public IPathManager PathManager
    {
      get
      {
        InitDefaults();
        return _pathManagerWrapper;
      }
    }

    public ILogger Logger
    {
      get
      {
        InitDefaults();
        return _loggerWrapper;
      }
    }
  }
}
