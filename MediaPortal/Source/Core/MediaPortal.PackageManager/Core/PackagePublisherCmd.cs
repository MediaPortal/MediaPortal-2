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
using System.IO;
using MediaPortal.Common.Logging;
using MediaPortal.PackageCore;
using MediaPortal.PackageManager.Options.Authors;
using MediaPortal.PackageManager.Options.Shared;

namespace MediaPortal.PackageManager.Core
{
  internal class PackagePublisherCmd : PackagePublisher
  {
    public PackagePublisherCmd(ILogger log)
      : base(log ?? new BasicConsoleLogger(LogLevel.All))
    { }

    public static bool Dispatch(ILogger log, Operation operation, object options)
    {
      if (options == null)
        return false;

      var manager = new PackagePublisherCmd(log);
      switch (operation)
      {
        case Operation.Publish:
          return manager.Publish(options as PublishOptions);
        case Operation.Recall:
          return manager.Recall(options as RecallOptions);
        default:
          return false;
      }
    }

    public bool Publish(PublishOptions options)
    {
      VerifyOptions(options);
      return base.Publish(options.UserName, options.Password, options.Host, options.PackageFilePath, options.PackageType, options.CategoryTags);
    }

    public bool Recall(RecallOptions options)
    {
      VerifyOptions(options);
      return base.Recall(options.UserName, options.Password, options.Host, options.Name, options.Version);
    }

    #region VerifyOptions (and QueryUserForPassword)

    private static void VerifyOptions(PublishOptions options)
    {
      // make sure source and target are specified with absolute paths
      if (!Path.IsPathRooted(options.PackageFilePath))
        options.PackageFilePath = Path.Combine(Environment.CurrentDirectory, options.PackageFilePath);

      // verify that package exists
      if (!File.Exists(options.PackageFilePath))
        throw new InvalidOperationException(string.Format("Could not find a package to publish at '{0}'.", options.PackageFilePath));

      VerifyAuthOptions(options);
    }

    private static void VerifyOptions(RecallOptions options)
    {
      // make sure we have both name and version
      if (string.IsNullOrWhiteSpace(options.Name) || string.IsNullOrWhiteSpace(options.Version))
        throw new ArgumentException("Unable to recall package (name or version is invalid or unspecified).");

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
