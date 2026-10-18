#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

    /// <summary>
    /// Adds the assembly of <paramref name="controllerType"/> to the assemblies that MVC searches for
    /// API controllers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Until now the only way for an assembly to be searched was to contain a resource access module, because
    /// the module's assembly was added as an application part on the way. That silently excluded assemblies
    /// with controllers but without middleware, which is the normal shape of an API assembly, and the resource
    /// server's own comment at the time called the shortcut a hack.
    /// </para>
    /// <para>
    /// Call it with any controller type of the assembly, from the code that composes the server, before
    /// <see cref="Startup"/>. Calling it later rebuilds the HTTP pipeline, which the shared listener of the
    /// platforms without http.sys refuses once other endpoints have joined the port - so the API of the server
    /// itself is registered while the services are registered, not while they start.
    /// </para>
    /// </remarks>
    /// <param name="controllerType">Any controller type in the assembly to search.</param>
    void AddHttpControllerAssembly(Type controllerType);
  }
}
