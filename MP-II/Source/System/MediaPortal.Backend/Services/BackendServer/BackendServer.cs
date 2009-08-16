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
using HttpServer;
using HttpServer.HttpModules;
using MediaPortal.BackendServer;
using MediaPortal.Core;
using MediaPortal.Core.Logging;

namespace MediaPortal.Services.BackendServer
{
  public class BackendServer : IBackendServer, IDisposable
  {
    protected readonly HttpServer.HttpServer _server;

    internal class HttpLogWriter : ILogWriter
    {
      public void Write(object source, LogPrio priority, string message)
      {
        string msg = source + ": " + message;
        ILogger logger = ServiceScope.Get<ILogger>();
        switch (priority)
        {
          case LogPrio.Trace:
            // Don't write trace messages (we don't support a trace level in MP - would have to map it to debug level)
            break;
          case LogPrio.Debug:
            logger.Debug(msg);
            break;
          case LogPrio.Info:
            logger.Info(msg);
            break;
          case LogPrio.Warning:
            logger.Warn(msg);
            break;
          case LogPrio.Error:
            logger.Error(msg);
            break;
          case LogPrio.Fatal:
            logger.Critical(msg);
            break;
        }
      }
    }

    public BackendServer()
    {
      _server = new HttpServer.HttpServer(new HttpLogWriter());
      // TODO: Start UPnP server
    }

    public void Dispose()
    {
      _server.Stop();
      // TODO: Shutdown UPnP server
    }

    #region IBackendServer implementation

    public void AddHttpModule(HttpModule module)
    {
      _server.Add(module);
    }

    public void RemoveHttpModule(HttpModule module)
    {
      _server.Remove(module);
    }

    #endregion
  }
}