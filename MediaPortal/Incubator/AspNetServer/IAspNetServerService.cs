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
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MediaPortal.Plugins.AspNetServer
{
  /// <summary>
  /// Provides methods to start and stop ASP.NET 5 WebApplications
  /// </summary>
  /// <remarks>
  /// For details on ASP.NET see https://github.com/aspnet
  /// </remarks>
  public interface IAspNetServerService
  {
    /// <summary>
    /// Starts a WebApplication with the name <param name="webApplicationName"></param> on the given TCP <param name="port"></param> and <param name="basePath"></param>.
    /// </summary>
    /// <param name="webApplicationName">Unique name to identify the WebApplication</param>
    /// <param name="configureServices">Action that uses the <see cref="IServiceCollection"/> parameter to configure the dependencies</param>
    /// <param name="configureApp">Action that uses the <see cref="IApplicationBuilder"/> parameter to configure the WebApplication</param>
    /// <param name="port">TCP port on which the WebApplication is supposed to listen</param>
    /// <param name="basePath">Base path on which the WebApplication is supposed to listen</param>
    /// <returns>
    /// A Task that completes when the WebApplication has started or failed to start.
    /// The Task's result is <c>true</c> if the WebApplication started successfully, else <c>false</c>.
    /// </returns>
    Task<bool> TryStartWebApplicationAsync(string webApplicationName, Action<IServiceCollection> configureServices, Action<IApplicationBuilder> configureApp, int port, string basePath);

    /// <summary>
    /// Stops a WebApplication with the given <param name="webApplicationName"></param>.
    /// </summary>
    /// <param name="webApplicationName">Name that was used when starting the WebApplication</param>
    /// <returns>
    /// A Task that completes when the WebApplication has stopped or failed to stop.
    /// The Task's result is <c>true</c> if the WebApplication was stopped successfully, else <c>false</c>.
    /// <c>false</c> can also mean that there was no WebApplication started with the given <param name="webApplicationName"></param>.
    /// </returns>
    Task<bool> TryStopWebApplicationAsync(string webApplicationName);
  }
}
