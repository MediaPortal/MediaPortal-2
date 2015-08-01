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
using System.Globalization;
using System.IO;
using System.Reflection;
using MediaPortal.Common.Logging;
using MediaPortal.PackageManager.Options.Shared;

namespace MediaPortal.PackageManager.Core
{
  internal class OtherCmd
  {
    private readonly ILogger _log;

    public OtherCmd(ILogger log)
    {
      _log = log;
    }

    public static bool Dispatch(ILogger log, Operation operation, object options)
    {
      if (options == null)
        return false;

      var core = new OtherCmd(log);
      switch (operation)
      {
        case Operation.ListAssemblies:
          return core.ListAssemblies();

        default:
          return false;
      }
    }

    private bool ListAssemblies()
    {
      var winFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
      if (!winFolder.EndsWith("\\"))
      {
        winFolder += "\\";
      }
      var entryAssemblyFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
      if (entryAssemblyFolder != null)
      {
        if (!entryAssemblyFolder.EndsWith("\\"))
        {
          entryAssemblyFolder += "\\";
        }

        // go through all assemblies and log all that are not inside windows folder
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
          var assemblyPath = assembly.Location;
          if (!assemblyPath.StartsWith(winFolder))
          {
            if (assemblyPath.StartsWith(entryAssemblyFolder))
            {
              _log.Info(assemblyPath);
            }
            else
            {
              _log.Warn(assemblyPath);
            }

            var assemblyFolder = Path.GetDirectoryName(assemblyPath);
            var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

            // also log the config file if exists
            if (assemblyFolder != null)
            {
              LogFileIfExists(assemblyFolder, assemblyName, ".exe.config");

              // and also all satelite assemblies
              foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
              {
                LogFileIfExists(Path.Combine(assemblyFolder, culture.Name), assemblyName, ".resources.dll");
              }
            }
          }
        }
      }
      return true;
    }

    private void LogFileIfExists(string assemblyFolder, string assemblyName, string extension)
    {
      var path = Path.Combine(assemblyFolder, assemblyName + extension);
      if (File.Exists(path))
      {
        _log.Debug(path);
      }
    }
  }
}