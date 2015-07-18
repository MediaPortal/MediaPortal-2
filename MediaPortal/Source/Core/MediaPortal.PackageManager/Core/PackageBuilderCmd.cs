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
  internal class PackageBuilderCmd : PackageBuilder
  {
    public PackageBuilderCmd(ILogger log) : 
      base(log ?? new BasicConsoleLogger(LogLevel.All))
    { }

    public static bool Dispatch(ILogger log, Operation operation, object options)
    {
      if (options == null)
        return false;

      var builder = new PackageBuilderCmd(log);
      switch (operation)
      {
        case Operation.Create:
          return builder.CreatePackage(options as CreateOptions);
        default:
          return false;
      }
    }

    public bool CreatePackage(CreateOptions options)
    {
      VerifyOptions(options);

      return base.CreatePackage(options.SourceFolder, options.TargetFolder, options.OverwriteExistingTarget);
    }

    private static void VerifyOptions(CreateOptions options)
    {
      if (options == null)
        throw new ArgumentNullException("options");

      // make sure source and target are specified with absolute paths
      if (!Path.IsPathRooted(options.SourceFolder))
        options.SourceFolder = Path.Combine(Environment.CurrentDirectory, options.SourceFolder);
      if (options.TargetFolder == null)
        options.TargetFolder = Environment.CurrentDirectory;
      else if (!Path.IsPathRooted(options.TargetFolder))
        options.TargetFolder = Path.Combine(Environment.CurrentDirectory, options.TargetFolder);

      // make sure source and target paths exist
      if (!Directory.Exists(options.SourceFolder))
        throw new ArgumentException("The specified source folder does not exist.");
      if (!Directory.Exists(options.TargetFolder))
        throw new ArgumentException("The specified target folder does not exist.");

      // make sure target is not inside source
      if (options.TargetFolder.StartsWith(options.SourceFolder))
        throw new ArgumentException("The target folder cannot be inside the source folder.");

      // make sure plugin descriptor file exists
      //options.SourceFolder.VerifyIsPluginDirectory();
    }
  }
}