#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Threading.Tasks;
using Owin;

namespace MediaPortal.Plugins.WebServices.OwinServer
{
  /// <summary>
  /// IOwinServer provides the possibility to start Owin WebApps.
  /// </summary>
  /// <remarks>
  /// Owin is the Open Web Interface for .NET. For details see http://owin.org
  /// </remarks> 
  public interface IOwinServer
  {
    /// <summary>
    /// Starts an Owin WebApp on the given TCP <param name="port"></param> and <param name="basePath"></param>.
    /// </summary>
    /// <param name="startup">Delegate that uses the <see cref="IAppBuilder"/> parameter to configure the WebApp.</param>
    /// <param name="port">TCP port on which the WebApp is supposed to listen.</param>
    /// <param name="basePath">Base path at which the WebApp is supposed to listen.</param>
    /// <returns>
    /// A Task that completes when the WebApp has started or failed to start.
    /// The Task's result is <c>true</c> if the WebApp started successfully, else <c>false</c>.
    /// </returns>
    Task<bool> TryStartWebAppAsync(Action<IAppBuilder> startup, int port, String basePath);

    /// <summary>
    /// Stops an Owin WebApp on the given TCP <param name="port"></param>port and <param name="basePath"></param>.
    /// </summary>
    /// <param name="port">TCP port on which the WebApp to be stopped is listening.</param>
    /// <param name="basePath"></param>
    /// <returns>
    /// A Task that completes when the WebApp has stopped or failed to stop.
    /// THe Task's result is <c>true</c> if the WebApp was stopped successfully, else <c>false</c>.
    /// <c>false</c> can also mean that there was no WebApp listening on the given
    /// <param name="port"></param> and <param name="basePath"></param>.
    /// </returns>    
    Task<bool> TryStopWebAppAsync(int port, String basePath);

    /// <summary>
    /// Stops all the WebApps that were previously started with <see cref="TryStartWebAppAsync"/>
    /// and prevents new WebApps to be started.
    /// </summary>
    /// <returns>
    /// A Task that completes when alls WebApps have stopped
    /// </returns>
    Task ShutdownAsync();
  }
}
