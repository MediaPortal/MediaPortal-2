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

using HttpServer.HttpModules;

namespace MediaPortal.Backend.BackendServer
{
  /// <summary>
  /// Control interface for the backend's server components - HTTP, UPnP, ...
  /// </summary>
  public interface IBackendServer
  {
    void Startup();
    void Shutdown();

    /// <summary>
    /// Adds a new HTTP module to the backend HTTP server.
    /// </summary>
    /// <remarks>
    /// The HTTP module approach is implemented by our <see cref="HttpServer.HttpServer"/> and fits very well into
    /// the MediaPortal concept: Plugins simply can add a module to the HTTP server.
    /// </remarks>
    /// <param name="module"></param>
    void AddHttpModule(HttpModule module);

    /// <summary>
    /// Removes an HTTP module from the backend HTTP server.
    /// </summary>
    /// <param name="module">Module to remove.</param>
    void RemoveHttpModule(HttpModule module);
  }
}