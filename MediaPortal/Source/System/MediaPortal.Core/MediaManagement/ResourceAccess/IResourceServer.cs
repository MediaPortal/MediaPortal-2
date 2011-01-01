#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using HttpServer.HttpModules;

namespace MediaPortal.Core.MediaManagement.ResourceAccess
{
  public interface IResourceServer
  {
    int PortIPv4 { get; }

    int PortIPv6 { get; }

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
    /// Adds a new HTTP module to the HTTP server.
    /// </summary>
    /// <remarks>
    /// The HTTP module approach is implemented by our <see cref="HttpServer.HttpServer"/> and fits very well into
    /// the MediaPortal concept: Plugins simply can add a module to the HTTP server.
    /// </remarks>
    /// <param name="module"></param>
    void AddHttpModule(HttpModule module);

    /// <summary>
    /// Removes an HTTP module from the HTTP server.
    /// </summary>
    /// <param name="module">Module to remove.</param>
    void RemoveHttpModule(HttpModule module);
  }
}