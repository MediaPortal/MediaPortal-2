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
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;

namespace MediaPortal.PackageServer.Utility.Security
{
  public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
  {
    private readonly string _publicClientId;
    private readonly Func<UserManager<IdentityUser>> _userManagerFactory;

    public ApplicationOAuthProvider(string publicClientId, Func<UserManager<IdentityUser>> userManagerFactory)
    {
      if (publicClientId == null)
      {
        throw new ArgumentNullException("publicClientId");
      }

      if (userManagerFactory == null)
      {
        throw new ArgumentNullException("userManagerFactory");
      }

      _publicClientId = publicClientId;
      _userManagerFactory = userManagerFactory;
    }

    public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
    {
      using (UserManager<IdentityUser> userManager = _userManagerFactory())
      {
        IdentityUser user = await userManager.FindAsync(context.UserName, context.Password);

        if (user == null)
        {
          context.SetError("invalid_grant", "The user name or password is incorrect.");
          return;
        }

        ClaimsIdentity oAuthIdentity = await userManager.CreateIdentityAsync(user,
          context.Options.AuthenticationType);
        ClaimsIdentity cookiesIdentity = await userManager.CreateIdentityAsync(user,
          CookieAuthenticationDefaults.AuthenticationType);
        AuthenticationProperties properties = CreateProperties(user.UserName);
        AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
        context.Validated(ticket);
        context.Request.Context.Authentication.SignIn(cookiesIdentity);
      }
    }

    public override Task TokenEndpoint(OAuthTokenEndpointContext context)
    {
      foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
      {
        context.AdditionalResponseParameters.Add(property.Key, property.Value);
      }

      return Task.FromResult<object>(null);
    }

    public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
    {
      // Resource owner password credentials does not provide a client ID.
      if (context.ClientId == null)
      {
        context.Validated();
      }

      return Task.FromResult<object>(null);
    }

    public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
    {
      if (context.ClientId == _publicClientId)
      {
        Uri expectedRootUri = new Uri(context.Request.Uri, "/");

        if (expectedRootUri.AbsoluteUri == context.RedirectUri)
        {
          context.Validated();
        }
      }

      return Task.FromResult<object>(null);
    }

    public static AuthenticationProperties CreateProperties(string userName)
    {
      IDictionary<string, string> data = new Dictionary<string, string>
      {
        { "userName", userName }
      };
      return new AuthenticationProperties(data);
    }
  }
}