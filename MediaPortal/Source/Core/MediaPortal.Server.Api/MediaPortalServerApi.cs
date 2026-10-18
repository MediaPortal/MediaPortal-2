#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The door through which the server's HTTP API enters a server process.
  /// </summary>
  /// <remarks>
  /// <para>
  /// MVC is told which assemblies to search for controllers, and the set has to be named before the resource
  /// server starts (the pipeline is rebuilt by adding an endpoint, which the shared listener of the platforms
  /// without http.sys refuses once other endpoints have joined its port). So the decision belongs to the code that
  /// composes a server - the headless host and the Windows server each call <see cref="Register"/> while they
  /// register their services - and not to this assembly, which has no way to notice that it was loaded.
  /// </para>
  /// <para>
  /// This type, rather than a controller type, is what the composition roots name. The registration is about the
  /// assembly, so naming the assembly's own type means that adding, renaming or deleting a controller never
  /// touches a launcher again.
  /// </para>
  /// </remarks>
  public static class MediaPortalServerApi
  {
    /// <summary>
    /// Tells the resource server of this process to search this assembly for API controllers.
    /// </summary>
    /// <remarks>
    /// Calling it twice is harmless - the resource server keeps the set free of duplicates - so a host that
    /// restarts its services can call it again from its registration path.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// No resource server is registered, which means the call came before <c>ApplicationCore.RegisterCoreServices()</c>
    /// or after the core services were disposed.
    /// </exception>
    public static void Register()
    {
      IResourceServer resourceServer = ServiceRegistration.Get<IResourceServer>(false);
      if (resourceServer == null)
        throw new InvalidOperationException(
            "MediaPortal.Server.Api.Register() needs the resource server, which the core services register; call it " +
            "after ApplicationCore.RegisterCoreServices() and before ApplicationCore.StartCoreServices().");
      resourceServer.AddHttpControllerAssembly(typeof(MediaPortalServerApi));
    }
  }
}
