#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Backend.Services.SystemResolver;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;

namespace MediaPortal.Mock
{
  public class MockCore
  {
    private static MockMIA_Management MANAGEMENT;
    private static MockMediaLibrary LIBRARY;
    private static MockMessageBroker BROKER;

    private static ILogger logger = null;

    public static MockMIA_Management Management
    {
      get { return MANAGEMENT; }
    }

    public static MockMediaLibrary Library
    {
      get { return LIBRARY; }
    }

    static MockCore()
    {
      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Get<IPathManager>().SetPath("LOG", "_Mock/log");
      ServiceRegistration.Get<IPathManager>().SetPath("CONFIG", "_Mock/config");
      ServiceRegistration.Set<ILogger>(new ConsoleLogger(LogLevel.All, true));

      logger = ServiceRegistration.Get<ILogger>();

      logger.Debug("Registering IMessageBroker service");
      ServiceRegistration.Set<IMessageBroker>(BROKER = new MockMessageBroker());

      logger.Debug("Registering ISettingsManager service");
      ServiceRegistration.Set<ISettingsManager>(new SettingsManager());

      logger.Debug("Registering ISystemResolver service");
      ServiceRegistration.Set<ISystemResolver>(new SystemResolver());

      logger.Debug("Creating MIA management");
      MANAGEMENT = new MockMIA_Management();
    }

    public static void Reset()
    {
      MANAGEMENT.Reset();
    }

    public static void SetupLibrary()
    {
      logger.Debug("Creating test media library");
      LIBRARY = new MockMediaLibrary();
    }

    public static void ShutdownLibrary()
    {
      LIBRARY.Dispose();
      LIBRARY = null;
    }

    public static void AddMediaItemAspectStorage(MediaItemAspectMetadata meta)
    {
      MANAGEMENT.AddMediaItemAspectStorage(meta);
    }
  }
}
