#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MediaPortal.Common.Services.ResourceAccess.Auth;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The endpoint a client sends its username and password to and gets an access token back from, so that it can
  /// use the endpoints which are not open to strangers.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This closes the gap that kept <c>MediaPortalAuthorizeAttribute</c> off every other endpoint. On .NET
  /// Framework the token was handed out by <c>OAuthAuthorizationServerMiddleware</c> from
  /// <c>Microsoft.Owin.Security.OAuth</c>; that library targets .NET Framework and there is no ASP.NET Core
  /// equivalent of its provider callbacks, so on the new target the endpoint had no implementation at all and the
  /// server had no way to identify anybody. The URL, the form encoding, the parameter names, the answer fields and
  /// the error strings are those of the old middleware, so a client written against MP2 2.3 keeps working; what is
  /// new is only what is inside the token, described at <see cref="MediaPortalAccessToken"/>.
  /// </para>
  /// <para>
  /// Everything it decides is decided in <see cref="MediaPortalCredentials"/>, which is not an ASP.NET type and is
  /// unit-tested without a listener; this class adds the HTTP shape and nothing else.
  /// </para>
  /// <para>
  /// Like the old one, it answers over plain HTTP. A token sent in the clear can be read by anyone on the path
  /// between the client and this machine, which was acceptable for a media server on a home LAN and is still the
  /// situation this server is usually deployed in - but it is the reason the endpoints that expose a household's
  /// data check the token, and the reason a deployment reachable from outside should be behind TLS.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("Token")]
  public class TokenController : ControllerBase
  {
    private const string GRANT_TYPE_PASSWORD = "password";

    /// <summary>
    /// The answer of <c>POST Token</c>.
    /// </summary>
    /// <remarks>
    /// The field names are the ones RFC 6749 §5.1 defines plus the two the Owin middleware added
    /// (<c>.issued</c>, <c>.expires</c>), which the MP2 client of the 2.3 series reads. <c>expires_in</c> is a
    /// number here, as the RFC specifies; the Owin middleware serialized it as a string.
    /// </remarks>
    public class TokenResult
    {
      public string access_token { get; set; }
      public string token_type { get; set; }
      public long expires_in { get; set; }
      public string issued { get; set; }
      public string expires { get; set; }
    }

    /// <summary>
    /// The answer of a failed request, in the shape RFC 6749 §5.2 defines.
    /// </summary>
    public class TokenErrorResult
    {
      public string error { get; set; }
      public string error_description { get; set; }
    }

    /// <summary>
    /// Exchanges credentials for an access token.
    /// </summary>
    /// <remarks>
    /// Reads the parameters itself instead of model-binding them: an OAuth password grant arrives as
    /// <c>application/x-www-form-urlencoded</c>, which is what the existing clients send, but a JSON body is what a
    /// new client will send, and the endpoint accepts both without either of them having to announce which.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Post()
    {
      TokenRequest request = await TokenRequest.ReadFromAsync(Request).ConfigureAwait(false);
      if (request.Error != null)
        return Error(StatusCodes.Status400BadRequest, MediaPortalCredentials.ERROR_INVALID_GRANT, request.Error);

      string grantType = request.GrantType;
      string userName = request.UserName;
      string password = request.Password;

      if (!string.Equals(grantType, GRANT_TYPE_PASSWORD, StringComparison.OrdinalIgnoreCase))
        return Error(StatusCodes.Status400BadRequest, MediaPortalCredentials.ERROR_UNSUPPORTED_GRANT_TYPE,
            "Only the '" + GRANT_TYPE_PASSWORD + "' grant is supported.");

      MediaPortalCredentials.Result result =
          await MediaPortalCredentials.GrantResourceOwnerCredentialsAsync(userName, password).ConfigureAwait(false);
      if (!result.Granted)
        return Error(StatusCodes.Status400BadRequest, result.Error, result.Description);

      IMediaPortalAccessTokenProtector protector =
          HttpContext.RequestServices.GetService(typeof(IMediaPortalAccessTokenProtector))
          as IMediaPortalAccessTokenProtector;
      if (protector == null)
        return Error(StatusCodes.Status503ServiceUnavailable, "server_error",
            "Token signing is not available on this server.");

      DateTimeOffset now = DateTimeOffset.UtcNow;
      string token = protector.Create(result.Identity);
      return new JsonResult(new TokenResult
      {
        access_token = token,
        token_type = "bearer",
        expires_in = (long)protector.Lifetime.TotalSeconds,
        issued = now.ToString("o"),
        expires = now.Add(protector.Lifetime).ToString("o")
      }) { StatusCode = StatusCodes.Status200OK };
    }

    /// <summary>
    /// Pulls <c>grant_type</c>, <c>username</c> and <c>password</c> out of the request.
    /// </summary>
    /// <summary>
    /// The three parameters of a token request, or the reason the request could not be read.
    /// </summary>
    /// <remarks>
    /// A type rather than out parameters because reading a body is asynchronous and C# does not allow
    /// <c>out</c> on an async method.
    /// </remarks>
    private class TokenRequest
    {
      public string GrantType;
      public string UserName;
      public string Password;

      /// <summary>
      /// Null when the request carried something usable.
      /// </summary>
      public string Error;

      public static async Task<TokenRequest> ReadFromAsync(HttpRequest request)
      {
        TokenRequest result = new TokenRequest();

        if (request.HasFormContentType)
        {
          IFormCollection form = await request.ReadFormAsync().ConfigureAwait(false);
          result.GrantType = FromForm(form, "grant_type");
          result.UserName = FromForm(form, "username");
          result.Password = FromForm(form, "password");
        }
        else if (request.ContentType != null &&
            request.ContentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0)
        {
          try
          {
            using (JsonDocument document = await JsonDocument.ParseAsync(request.Body).ConfigureAwait(false))
            {
              JsonElement root = document.RootElement;
              result.GrantType = ReadString(root, "grant_type");
              result.UserName = ReadString(root, "username");
              result.Password = ReadString(root, "password");
            }
          }
          catch (JsonException)
          {
            result.Error = "The request body is not valid JSON.";
            return result;
          }
          catch (ArgumentException)
          {
            result.Error = "The request body is not valid JSON.";
            return result;
          }
        }
        else
        {
          result.Error = "Send the credentials as application/x-www-form-urlencoded or application/json.";
          return result;
        }

        if (string.IsNullOrEmpty(result.GrantType))
          result.Error = "The 'grant_type' parameter is missing.";
        else if (string.IsNullOrEmpty(result.UserName))
          result.Error = "The 'username' parameter is missing.";
        // An empty password is a value: a profile with no password set exists and is allowed in, so only a
        // missing parameter is refused here.
        else if (result.Password == null)
          result.Error = "The 'password' parameter is missing.";
        return result;
      }

      private static string FromForm(IFormCollection form, string name)
      {
        StringValues value;
        return form.TryGetValue(name, out value) ? value.ToString() : null;
      }

      private static string ReadString(JsonElement root, string name)
      {
        JsonElement value;
        if (!root.TryGetProperty(name, out value))
          return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
      }
    }

    private JsonResult Error(int statusCode, string error, string description)
    {
      return new JsonResult(new TokenErrorResult { error = error, error_description = description })
      { StatusCode = statusCode };
    }
  }
}
