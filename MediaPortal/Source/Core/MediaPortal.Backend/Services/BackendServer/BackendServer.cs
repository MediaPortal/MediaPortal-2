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
using HttpServer;
using MediaPortal.Backend.BackendServer;
using MediaPortal.Backend.Services.ClientCommunication;
using MediaPortal.Common;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Utilities.Network;
using UPnP.Infrastructure;
using UPnP.Infrastructure.Dv;
using ILogger = MediaPortal.Common.Logging.ILogger;
using UPnPLogger = UPnP.Infrastructure.ILogger;

namespace MediaPortal.Backend.Services.BackendServer
{
  public class BackendServer : IBackendServer, IDisposable
  {
    public const string MP2SERVER_DEVICEVERSION = "MediaPortal-2-Server/1.0";
    public const string MP2_HTTP_SERVER_NAME = "MediaPortal 2 (Server) Web Server";

    internal class UPnPLoggerDelegate : UPnPLogger
    {
      public void Debug(string format, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Debug(format, args);
      }

      public void Debug(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Debug(format, ex, args);
      }

      public void Info(string format, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Info(format, args);
      }

      public void Info(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Info(format, ex, args);
      }

      public void Warn(string format, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Warn(format, args);
      }

      public void Warn(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Warn(format, ex, args);
      }

      public void Error(string format, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Error(format, args);
      }

      public void Error(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Error(format, ex, args);
      }

      public void Error(Exception ex)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Error(ex);
      }

      public void Critical(string format, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Critical(format, args);
      }

      public void Critical(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Critical(format, ex, args);
      }

      public void Critical(Exception ex)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Critical(ex);
      }
    }

    protected readonly UPnPBackendServer _upnpServer;

    public BackendServer()
    {
      ServerSettings serverSettings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      UPnPConfiguration.PRODUCT_VERSION = MP2SERVER_DEVICEVERSION;
      UPnPConfiguration.LOGGER = new UPnPLoggerDelegate();
      UPnPConfiguration.USE_IPV4 = serverSettings.UseIPv4;
      UPnPConfiguration.USE_IPV6 = serverSettings.UseIPv6;
      UPnPConfiguration.IP_ADDRESS_BINDINGS = serverSettings.IPAddressBindingsList;
      NetworkUtils.LimitIPEndpoints = serverSettings.LimitIPEndpoints;
      HttpResponse.HTTP_SERVER_NAME = MP2_HTTP_SERVER_NAME;

      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      _upnpServer = new UPnPBackendServer(systemResolver.LocalSystemId);
    }

    public void Dispose()
    {
      _upnpServer.Dispose();
    }

    #region IBackendServer implementation

    public UPnPServer UPnPBackendServer
    {
      get { return _upnpServer; }
    }

    public void Startup()
    {
      _upnpServer.Start();
    }

    public void Shutdown()
    {
      _upnpServer.Stop();
    }

    public void UpdateUPnPConfiguration()
    {
      _upnpServer.UpdateConfiguration();
    }

    #endregion
  }
}
