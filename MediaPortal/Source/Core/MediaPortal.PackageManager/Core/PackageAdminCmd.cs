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
using MediaPortal.Common.Logging;
using MediaPortal.PackageCore;
using MediaPortal.PackageManager.Options.Admin;
using MediaPortal.PackageManager.Options.Shared;

namespace MediaPortal.PackageManager.Core
{
  internal class PackageAdminCmd : PackageAdmin
  {
    public PackageAdminCmd(ILogger log)
      : base(log ?? new BasicConsoleLogger(LogLevel.All))
    { }

    public static bool Dispatch(ILogger log, Operation operation, object options)
    {
      if (options == null)
        return false;

      var core = new PackageAdminCmd(log);
      switch (operation)
      {
        case Operation.CreateUser:
          return core.CreateUser(options as CreateUserOptions);
        case Operation.RevokeUser:
          return core.RevokeUser(options as RevokeUserOptions);
        default:
          return false;
      }
    }

    public bool CreateUser(CreateUserOptions options)
    {
      VerifyOptions(options);
      return base.CreateUser(options.UserName, options.Password, options.Login, options.Secret, options.Name, options.Email, null);
    }

    public bool RevokeUser(RevokeUserOptions options)
    {
      VerifyOptions(options);
      return base.RevokeUser(options.UserName, options.Password, options.Login, options.Reason);
    }

    #region VerifyOptions (and QueryUserForPassword)

    private static void VerifyOptions(CreateUserOptions options)
    {
      if (string.IsNullOrWhiteSpace(options.Login))
        throw new ArgumentException("Unable to create user (the users login is invalid or unspecified).");

      if (string.IsNullOrWhiteSpace(options.Secret) || options.Secret.Length < 6)
        throw new ArgumentException("Unable to create user (the users secret (password) must be at least 6 characters).");

      if (string.IsNullOrWhiteSpace(options.Name))
        throw new ArgumentException("Unable to create user (the user name is invalid or unspecified).");

      VerifyAuthOptions(options);
    }

    private static void VerifyOptions(RevokeUserOptions options)
    {
      if (string.IsNullOrWhiteSpace(options.Login))
        throw new ArgumentException("Unable to revoke user (login is invalid or unspecified).");

      if (string.IsNullOrWhiteSpace(options.Reason) || options.Reason.Length < 5)
        throw new ArgumentException("Unable to revoke user (a proper reason must be supplied).");

      VerifyAuthOptions(options);
    }

    private static void VerifyAuthOptions(AuthOptions options)
    {
      // ask user for password if one was not specified
      if (options.Password == null)
        options.Password = QueryUserForPassword(options.UserName);

      // verify credentials
      if (string.IsNullOrWhiteSpace(options.UserName) || string.IsNullOrWhiteSpace(options.Password))
        throw new ArgumentException("Unable to authenticate at the MediaPortal package server using the given credentials.");
    }

    private static string QueryUserForPassword(string userName)
    {
      Console.Write("Please specify the password for user '{0}' at the MediaPortal package server: ", userName);
      return Console.ReadLine();
    }

    #endregion
  }
}
