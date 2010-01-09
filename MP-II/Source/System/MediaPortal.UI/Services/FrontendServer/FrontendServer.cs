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
using MediaPortal.Core.SystemResolver;
using MediaPortal.UI.FrontendServer;
using MediaPortal.Core;
using MediaPortal.UI.Services.ServerCommunication;
using UPnP.Infrastructure;
using ILogger=MediaPortal.Core.Logging.ILogger;
using UPnPLogger = UPnP.Infrastructure.ILogger;

namespace MediaPortal.UI.Services.FrontendServer
{
  public class FrontendServer : IFrontendServer, IDisposable
  {
    public const string MP2SERVER_DEVICEVERSION = "MediaPortal-II/1.0";

    public class UPnPLoggerDelegate : UPnPLogger
    {
      public void Debug(string format, params object[] args)
      {
        ILogger logger = ServiceScope.Get<ILogger>(false);
        if (logger != null)
          logger.Debug(format, args);
      }

      public void Debug(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceScope.Get<ILogger>(false);
        if (logger != null)
          logger.Debug(format, ex, args);
      }

      public void Info(string format, params object[] args)
      {
        ILogger logger = ServiceScope.Get<ILogger>(false);
        if (logger != null)
          logger.Info(format, args);
      }

      public void Info(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceScope.Get<ILogger>(false);
        if (logger != null)
          logger.Info(format, ex, args);
      }

      public void Warn(string format, params object[] args)
      {
        ILogger logger = ServiceScope.Get<ILogger>(false);
        if (logger != null)
          logger.Warn(format, args);
      }

      public void Warn(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceScope.Get<ILogger>(false);
        if (logger != null)
          logger.Warn(format, ex, args);
      }

      public void Error(string format, params object[] args)
      {
        ILogger logger = ServiceScope.Get<ILogger>(false);
        if (logger != null)
          logger.Error(format, args);
      }

      public void Error(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceScope.Get<ILogger>(false);
        if (logger != null)
          logger.Error(format, ex, args);
      }

      public void Error(Exception ex)
      {
        ILogger logger = ServiceScope.Get<ILogger>(false);
        if (logger != null)
          logger.Error(ex);
      }

      public void Critical(string format, params object[] args)
      {
        ILogger logger = ServiceScope.Get<ILogger>(false);
        if (logger != null)
          logger.Critical(format, args);
      }

      public void Critical(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceScope.Get<ILogger>(false);
        if (logger != null)
          logger.Critical(format, ex, args);
      }

      public void Critical(Exception ex)
      {
        ILogger logger = ServiceScope.Get<ILogger>(false);
        if (logger != null)
          logger.Critical(ex);
      }
    }

    protected readonly UPnPFrontendServer _upnpServer;

    public FrontendServer()
    {
      Configuration.PRODUCT_VERSION = MP2SERVER_DEVICEVERSION;
      Configuration.LOGGER = new UPnPLoggerDelegate();

      ISystemResolver systemResolver = ServiceScope.Get<ISystemResolver>();
      _upnpServer = new UPnPFrontendServer(systemResolver.LocalSystemId);
    }

    public void Dispose()
    {
      _upnpServer.Dispose();
    }

    #region IBackendServer implementation

    public void Startup()
    {
      _upnpServer.Start();
    }

    public void Shutdown()
    {
      _upnpServer.Stop();
    }

    #endregion
  }
}