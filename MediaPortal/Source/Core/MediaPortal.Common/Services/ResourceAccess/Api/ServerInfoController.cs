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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;

namespace MediaPortal.Common.Services.ResourceAccess.Api
{
  /// <summary>
  /// What the server is and what it runs on: the first endpoint of the HTTP API that the new front end is
  /// built on.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The class exists as much for the seam as for the answer. The resource server adds the assembly of every
  /// registered resource access module as an MVC application part, and <c>ResourceAccessModule</c> lives in
  /// <c>MediaPortal.Common</c>, so a controller in this assembly is discovered without any further wiring. That
  /// makes this the smallest possible thing which proves the whole chain works on both HTTP hosts: attribute
  /// routing below the URL prefix, JSON serialization of a server type, and an MP2 service resolved on an
  /// ASP.NET Core request thread. The API stays in <c>MediaPortal.Common</c> only while that holds; once it has
  /// several controllers it should move to a portable project of its own, which is a file move plus one
  /// <c>AddApplicationPart</c>.
  /// </para>
  /// <para>
  /// Deliberately <em>not</em> decorated with <see cref="MediaPortalAuthorizeAttribute"/>: that filter asks for
  /// an identity whose authentication type is <see cref="ResourceServer.MEDIAPORTAL_AUTHENTICATION_TYPE"/>, and
  /// the component which hands out such identities - the OAuth token endpoint - belongs to the Owin stack that
  /// only the .NET Framework target of this project still builds. Requiring it here would answer 403 to every
  /// client on every platform. The unauthenticated surface is not new (the resource access module below this
  /// one serves files without credentials), but it is a debt, and exposing user data through the API is blocked
  /// on implementing the token endpoint for .NET first.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/[controller]")]
  public class ServerInfoController : ControllerBase
  {
    /// <summary>
    /// The answer of <c>GET api/serverinfo</c>.
    /// </summary>
    /// <remarks>
    /// A plain data object rather than the server's own types: this shape is a contract that clients will be
    /// built against, while the types behind it are free to change.
    /// </remarks>
    public class ServerInfoResult
    {
      public string Name { get; set; }

      /// <summary>
      /// Version of the server's own assembly - the same number the health endpoint reports.
      /// </summary>
      public string Version { get; set; }

      /// <summary>
      /// The .NET the server process runs on, e.g. <c>.NET 10.0.x</c>.
      /// </summary>
      public string Framework { get; set; }

      public string OperatingSystem { get; set; }

      /// <summary>
      /// The two architectures that matter: the one the machine has and the one the server process was built
      /// for. A 32-bit process on a 64-bit system shows up here, which the built images alone do not.
      /// </summary>
      public string OsArchitecture { get; set; }

      public string ProcessArchitecture { get; set; }

      /// <summary>
      /// Process id, so that a service manager or a test can tell two servers on one machine apart.
      /// </summary>
      public int ProcessId { get; set; }

      public string ServerUtc { get; set; }

      /// <summary>
      /// The URL prefix this request was served below, as the server saw it. A client needs it to build its
      /// other URLs, and reporting it back is how the gates measure that both HTTP hosts hand the prefix over
      /// as <c>PathBase</c>.
      /// </summary>
      public string PathBase { get; set; }

      /// <summary>
      /// The IP endpoints the server was configured to listen on. An empty list means every address of the
      /// machine, which is what a fresh install does.
      /// </summary>
      public IReadOnlyList<string> IpAddressBindings { get; set; }

      public bool UseIPv4 { get; set; }

      public bool UseIPv6 { get; set; }
    }

    /// <summary>
    /// Answers with what this process is. Every field is read from the running process or from the server's own
    /// settings, so the answer describes the server rather than a build of it.
    /// </summary>
    [HttpGet]
    public ActionResult<ServerInfoResult> GetServerInfo()
    {
      // The same resource server also runs inside the client process, below "/MediaPortal/Client", so without
      // this guard a client would answer questions about a server - and would widen its own HTTP surface with
      // endpoints it does not use. This is the same fact that decides the base path in
      // ResourceHttpAccessUrlUtils; without a resolver the endpoint answers, which is what the tests do.
      ISystemResolver resolver = ServiceRegistration.Get<ISystemResolver>(false);
      if (resolver != null && resolver.SystemType != SystemType.Server)
        return NotFound();

      ServerInfoResult info = new ServerInfoResult
      {
        Name = "MediaPortal 2 Server",
        Version = typeof(ServerInfoController).Assembly.GetName().Version.ToString(),
        Framework = RuntimeInformation.FrameworkDescription,
        OperatingSystem = RuntimeInformation.OSDescription,
        OsArchitecture = RuntimeInformation.OSArchitecture.ToString(),
        ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
        ProcessId = Environment.ProcessId,
        ServerUtc = DateTime.UtcNow.ToString("O"),
        PathBase = HttpContext.Request.PathBase.Value ?? string.Empty,
        // Reported until the settings below can be read; a server without the settings service answers with
        // what its defaults are rather than with a 500.
        UseIPv4 = true
      };

      // The settings service is registered with the core services, so any request the running server answers
      // finds it. Asking without throwing keeps the endpoint answering in a host started without them - which
      // is what the tests do - instead of failing the request over a missing service.
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>(false);
      if (settingsManager != null)
      {
        ServerSettings settings = settingsManager.Load<ServerSettings>();
        info.UseIPv4 = settings.UseIPv4;
        info.UseIPv6 = settings.UseIPv6;
        List<string> bindings = settings.IPAddressBindingsList;
        info.IpAddressBindings = bindings ?? new List<string>();
      }

      return info;
    }
  }
}
