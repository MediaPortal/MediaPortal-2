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
using System.Net;

namespace MediaPortal.Common.ResourceAccess
{
  public interface IResourceServer
  {
    /// <summary>
    /// Gets the service base url.
    /// </summary>
    string GetServiceUrl(IPAddress ipAddress);

    void Startup();

    void Shutdown();

    /// <summary>
    /// Restarts the HTTP servers (IPv4 and IPv6).
    /// </summary>
    /// <remarks>
    /// This has to be done when the settings about IPv4/IPv6 or about the HTTP server port changed.
    /// </remarks>
    void RestartHttpServers();

    /// <summary>
    /// Adds a new HTTP middleware to the HTTP server.
    /// </summary>
    /// <remarks>
    /// The HTTP module approach is implemented by Owin self host which allows to add a OwinMiddleware to the HTTP server.
    /// </remarks>
    /// <param name="moduleType">Type of OwinMiddleware</param>
    void AddHttpModule(Type moduleType);

    /// <summary>
    /// Removes an HTTP module from the HTTP server.
    /// </summary>
    /// <param name="moduleType">Type of OwinMiddleware to remove.</param>
    void RemoveHttpModule(Type moduleType);
  }
}
