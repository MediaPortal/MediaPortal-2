#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using HttpServer;
using HttpServer.Authentication;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.Plugins.MP2Extended.Authentication
{
  public class AuthRequestHandler
  {
    private static string _resourceAccessPath;

    public AuthRequestHandler(string resourceAccessPath)
    {
      _resourceAccessPath = resourceAccessPath;
      ServiceRegistration.Get<IResourceServer>().AddAuthenticationModule(new DigestAuthentication(OnAuthenticate, OnAuthenticationRequired));
    }

    /// <summary>
    /// Checks based on the URL if an Authentication is required
    /// </summary>
    /// <param name="request"></param>
    /// <returns>true = Required, false = not Required</returns>
    internal static bool OnAuthenticationRequired(IHttpRequest request)
    {
      return request.Uri.AbsolutePath.StartsWith(_resourceAccessPath);
    }

    /// <summary>
    /// Delegate used to let authentication modules authenticate the user name and password.
    /// </summary>
    /// <param name="realm">Realm that the user want to authenticate in</param>
    /// <param name="userName">User name specified by client</param>
    /// <param name="password">Password supplied by the delegate</param>
    /// <param name="login">object that will be stored in a session variable called <see cref="AuthenticationModule.AuthenticationTag"/> if authentication was successful.</param>
    /// <exception cref="ForbiddenException">throw forbidden exception if too many attempts have been made.</exception>
    internal static void OnAuthenticate(string realm, string userName, ref string password, out object login)
    {
      // digest authentication encrypts password which means that
      // we need to provide the authenticator with a stored password.
      
      // you should really query a DB or something
      if (userName == "test")
      {
        password = "123";

        // login can be fetched from IHttpSession in all modules
        login = new User(1, "test");
      }
      else
      {
        password = string.Empty;
        login = null;
      }
    }


    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }

  internal class User
  {
    public int Id;
    public string UserName;

    public User(int id, string userName)
    {
      Id = id;
      UserName = userName;
    }
  }
}