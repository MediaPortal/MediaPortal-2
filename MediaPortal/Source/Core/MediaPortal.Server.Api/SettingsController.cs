/*
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

using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Services.MediaManagement;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Utilities.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The server's own settings, over the same HTTP API the library is read from.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference configured its server through a window on the server's own desktop. A headless server has no
  /// desktop - the household reaches it from a television - so what the reference's server configuration window
  /// offered has to be reachable here. Not everything, and deliberately so: this endpoint speaks the settings a
  /// living room plausibly changes (which folders are watched by when the importer runs, whether strangers have
  /// to sign in, which address the server listens on), and the answer says plainly which of them only take
  /// effect after a restart, because promising a live change the server cannot make is worse than saying so.
  /// </para>
  /// <para>
  /// The writing is guarded the same way registering a share is: when the owner turned web authorization on,
  /// a token is required for this too; when they did not, the LAN is trusted with its own server, which is the
  /// bargain that switch offers.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/media")]
  [MediaPortalAuthorize]
  public class SettingsController : ControllerBase
  {
    /// <summary>
    /// What the server is configured to do and to listen to, plus the importer's schedule.
    /// </summary>
    public class ServerSettingsResult
    {
      /// <summary>Whether the API asks strangers for a token. Turning it on takes effect immediately.</summary>
      public bool WebAuthorizationEnabled { get; set; }

      /// <summary>
      /// The port stored in the server's settings. The stored value changes nothing in this
      /// build - it listens on the shared port 55555 either way - but the reference's
      /// settings object holds the field, so it is carried and can be kept as it is.
      /// </summary>
      public int HttpServerPort { get; set; }

      /// <summary>Whether the server binds IPv4 sockets. A change needs a server restart.</summary>
      public bool UseIPv4 { get; set; }

      /// <summary>Whether the server binds IPv6 sockets. A change needs a server restart.</summary>
      public bool UseIPv6 { get; set; }

      /// <summary>Whether only the addresses in <see cref="IpAddressBindings"/> are bound. Restart to apply.</summary>
      public bool LimitIpEndpoints { get; set; }

      /// <summary>The bound addresses, comma separated, as the setting stores them. Restart to apply.</summary>
      public string IpAddressBindings { get; set; }

      /// <summary>Whether the importer runs its scheduled pass at all.</summary>
      public bool ImporterEnabled { get; set; }

      /// <summary>The hour of day the scheduled import pass starts, 0-24, fractions allowed.</summary>
      public double ImporterStartTime { get; set; }

      /// <summary>Whether the household has to say who is watching. Off means nobody is asked.</summary>
      public bool EnableUserLogin { get; set; }

      /// <summary>Whether the screen that asks is shown when a person is expected.</summary>
      public bool EnableUserLoginScreen { get; set; }

      /// <summary>The person signed in without being asked, or empty for nobody.</summary>
      public string AutoLoginUser { get; set; }

      /// <summary>Whether an untouched television gives its user back to the household.</summary>
      public bool AutoLogoutEnabled { get; set; }

      /// <summary>How many minutes of nothing it takes.</summary>
      public int AutoLogoutIdleTimeoutInMin { get; set; }

      /// <summary>The settings whose change the server only picks up after a restart.</summary>
      public List<string> RestartNeededFor { get; set; }
    }

    /// <summary>
    /// The body of an answer that refused: the reason, in words a screen can show.
    /// </summary>
    public class ErrorResponse
    {
      public string Error { get; set; }
    }

    /// <summary>
    /// What a caller asks to change. Every field is optional; what is left out stays as it is.
    /// </summary>
    public class ServerSettingsRequest
    {
      public bool? WebAuthorizationEnabled { get; set; }

      public int? HttpServerPort { get; set; }

      public bool? UseIPv4 { get; set; }

      public bool? UseIPv6 { get; set; }

      public bool? LimitIpEndpoints { get; set; }

      public string IpAddressBindings { get; set; }

      public bool? ImporterEnabled { get; set; }

      public double? ImporterStartTime { get; set; }

      public bool? EnableUserLogin { get; set; }

      public bool? EnableUserLoginScreen { get; set; }

      /// <summary>The person to sign in without asking. Empty means nobody; anything that is not a name the
      /// store has is refused, because a half-typed id would leave the house asking nobody.</summary>
      public string AutoLoginUser { get; set; }

      public bool? AutoLogoutEnabled { get; set; }

      public int? AutoLogoutIdleTimeoutInMin { get; set; }
    }

    /// <summary>
    /// Reads the server's settings.
    /// </summary>
    [HttpGet("settings")]
    public ActionResult<ServerSettingsResult> GetSettings()
    {
      try
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>(false);
        if (settingsManager == null)
          return StatusCode(StatusCodes.Status503ServiceUnavailable);

        return Ok(Of(settingsManager.Load<ServerSettings>(), settingsManager.Load<ImporterWorkerSettings>(),
            ReadLoginSettings(settingsManager), new List<string>()));
      }
      catch (IllegalCallException ex)
      {
        return new ObjectResult(new ErrorResponse { Error = ex.Message })
        {
          StatusCode = StatusCodes.Status503ServiceUnavailable
        };
      }
    }

    /// <summary>
    /// Changes the server's settings. What is sent is applied and saved; the answer is the whole state, so the
    /// screen that sent a value can show what the server now holds - and <see cref="ServerSettingsResult.RestartNeededFor"/>
    /// says which of those values the running server has not picked up yet.
    /// </summary>
    [HttpPut("settings")]
    public ActionResult<ServerSettingsResult> PutSettings([FromBody] ServerSettingsRequest request)
    {
      if (request == null)
        return BadRequest(new ErrorResponse { Error = "no settings were sent" });

      // A port outside the usable range is a typo, and a typo here means a server nobody can reach until it is
      // restarted. Refusing the whole request is kinder than saving it.
      if (request.HttpServerPort is int port && (port < 1 || port > 65535))
        return BadRequest(new ErrorResponse { Error = "the port has to be between 1 and 65535" });
      if (request.ImporterStartTime is double hour && (hour < 0 || hour >= 24))
        return BadRequest(new ErrorResponse { Error = "the importer's start hour has to be between 0 and 24" });
      // A timeout the household cannot wait out is a typo too, and a name that does not belong to anybody
      // would be saved as a person nobody can ever become.
      if (request.AutoLogoutIdleTimeoutInMin is int minutes && (minutes < 1 || minutes > 24 * 60))
        return BadRequest(new ErrorResponse { Error = "the idle timeout has to be between 1 and 1440 minutes" });
      Guid autoLoginUser = Guid.Empty;
      if (!string.IsNullOrWhiteSpace(request.AutoLoginUser) && !Guid.TryParse(request.AutoLoginUser, out autoLoginUser))
        return BadRequest(new ErrorResponse { Error = "the auto-login user has to be the id of a user" });

      try
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>(false);
        if (settingsManager == null)
          return StatusCode(StatusCodes.Status503ServiceUnavailable);

        ServerSettings server = settingsManager.Load<ServerSettings>();
        bool serverChanged = false;
        // Which of the restart-needed fields this request actually moved. The answer names
        // only those - saying "restart needed" for five settings when one was touched would
        // send people rebooting over values that took effect the moment the save returned.
        List<string> restartNeededFor = new List<string>();

        if (request.WebAuthorizationEnabled.HasValue && server.WebAutorizationEnabled != request.WebAuthorizationEnabled.Value)
        {
          server.WebAutorizationEnabled = request.WebAuthorizationEnabled.Value;
          serverChanged = true;
        }
        // The port is stored - the reference's settings object holds it - but this build
        // listens on the shared port 55555 whatever is stored here, and says so in its
        // docs. Listing it as "restart needed" would promise a change no restart brings.
        if (request.HttpServerPort.HasValue && server.HttpServerPort != request.HttpServerPort.Value)
        {
          server.HttpServerPort = request.HttpServerPort.Value;
          serverChanged = true;
        }
        if (request.UseIPv4.HasValue && server.UseIPv4 != request.UseIPv4.Value)
        {
          server.UseIPv4 = request.UseIPv4.Value;
          restartNeededFor.Add("use_ipv4");
          serverChanged = true;
        }
        if (request.UseIPv6.HasValue && server.UseIPv6 != request.UseIPv6.Value)
        {
          server.UseIPv6 = request.UseIPv6.Value;
          restartNeededFor.Add("use_ipv6");
          serverChanged = true;
        }
        if (request.LimitIpEndpoints.HasValue && server.LimitIPEndpoints != request.LimitIpEndpoints.Value)
        {
          server.LimitIPEndpoints = request.LimitIpEndpoints.Value;
          restartNeededFor.Add("limit_ip_endpoints");
          serverChanged = true;
        }
        if (!string.IsNullOrEmpty(request.IpAddressBindings) && server.IPAddressBindings != request.IpAddressBindings)
        {
          server.IPAddressBindings = request.IpAddressBindings;
          restartNeededFor.Add("ip_address_bindings");
          serverChanged = true;
        }
        if (serverChanged)
          settingsManager.Save(server);

        ImporterWorkerSettings importer = settingsManager.Load<ImporterWorkerSettings>();
        bool importerChanged = false;
        if (request.ImporterEnabled.HasValue && importer.EnableAutoRefresh != request.ImporterEnabled.Value)
        {
          importer.EnableAutoRefresh = request.ImporterEnabled.Value;
          importerChanged = true;
        }
        if (request.ImporterStartTime.HasValue && Math.Abs(importer.ImporterStartTime - request.ImporterStartTime.Value) > 0.001)
        {
          importer.ImporterStartTime = request.ImporterStartTime.Value;
          importerChanged = true;
        }
        if (importerChanged)
          settingsManager.Save(importer);

        // The login policy travels with the household, not with the television, so it is kept here where every
        // client reads it. A name the store has forgotten is dropped rather than kept, because the reference's
        // own rule is that an unknown auto-login name is nobody.
        UserLoginSettings login = ReadLoginSettings(settingsManager);
        bool loginChanged = false;
        if (request.EnableUserLogin.HasValue && login.EnableUserLogin != request.EnableUserLogin.Value)
        {
          login.EnableUserLogin = request.EnableUserLogin.Value;
          loginChanged = true;
        }
        if (request.EnableUserLoginScreen.HasValue && login.EnableUserLoginScreen != request.EnableUserLoginScreen.Value)
        {
          login.EnableUserLoginScreen = request.EnableUserLoginScreen.Value;
          loginChanged = true;
        }
        if (request.AutoLoginUser != null && login.AutoLoginUser != autoLoginUser)
        {
          login.AutoLoginUser = autoLoginUser;
          loginChanged = true;
        }
        if (request.AutoLogoutEnabled.HasValue && login.AutoLogoutEnabled != request.AutoLogoutEnabled.Value)
        {
          login.AutoLogoutEnabled = request.AutoLogoutEnabled.Value;
          loginChanged = true;
        }
        if (request.AutoLogoutIdleTimeoutInMin is int wantedMinutes && login.AutoLogoutIdleTimeoutInMin != wantedMinutes)
        {
          login.AutoLogoutIdleTimeoutInMin = wantedMinutes;
          loginChanged = true;
        }
        if (loginChanged)
          settingsManager.Save(login);

        return Ok(Of(server, importer, login, restartNeededFor));
      }
      catch (IllegalCallException ex)
      {
        return new ObjectResult(new ErrorResponse { Error = ex.Message })
        {
          StatusCode = StatusCodes.Status503ServiceUnavailable
        };
      }
    }

    /// <summary>
    /// The household's five rules. A settings manager that has never heard of them - a stand-in that only
    /// knows the two settings this controller was written for, or a store older than the rules themselves -
    /// is answered with what a household that never set its rules holds, rather than taking the whole
    /// settings answer down with it: every other field of this endpoint is worth more than a stack trace.
    /// </summary>
    private static UserLoginSettings ReadLoginSettings(ISettingsManager settingsManager)
    {
      return settingsManager.Load<UserLoginSettings>() ?? new UserLoginSettings();
    }

    private static ServerSettingsResult Of(ServerSettings server, ImporterWorkerSettings importer,
        UserLoginSettings login, List<string> restartNeededFor)
    {
      return new ServerSettingsResult
      {
        WebAuthorizationEnabled = server.WebAutorizationEnabled,
        HttpServerPort = server.HttpServerPort,
        UseIPv4 = server.UseIPv4,
        UseIPv6 = server.UseIPv6,
        LimitIpEndpoints = server.LimitIPEndpoints,
        IpAddressBindings = server.IPAddressBindings ?? string.Empty,
        ImporterEnabled = importer.EnableAutoRefresh,
        ImporterStartTime = importer.ImporterStartTime,
        EnableUserLogin = login.EnableUserLogin,
        EnableUserLoginScreen = login.EnableUserLoginScreen,
        AutoLoginUser = login.AutoLoginUser.ToString(),
        AutoLogoutEnabled = login.AutoLogoutEnabled,
        AutoLogoutIdleTimeoutInMin = login.AutoLogoutIdleTimeoutInMin,
        RestartNeededFor = restartNeededFor
      };
    }
  }
}