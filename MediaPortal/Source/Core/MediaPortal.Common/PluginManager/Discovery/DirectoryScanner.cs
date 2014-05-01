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
using System.IO;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager.Models;

namespace MediaPortal.Common.PluginManager.Discovery
{
  /// <summary>
  /// Class providing logic to enumerate all installed plugins by scanning a local directory.
  /// </summary>
  internal class DirectoryScanner
  {
    private readonly string _pluginsPath;

    public DirectoryScanner(string pluginsPath)
    {
      _pluginsPath = pluginsPath;
    }

    public IDictionary<Guid, PluginMetadata> PerformDiscovery()
    {
      var result = new Dictionary<Guid, PluginMetadata>();
      foreach (string pluginDirectoryPath in Directory.GetDirectories(_pluginsPath))
      {
        if ((Path.GetFileName(pluginDirectoryPath) ?? string.Empty).StartsWith("."))
          continue;
        try
        {
          PluginMetadata pm;
          if (pluginDirectoryPath.TryParsePluginDefinition(out pm))
          {
            if (result.ContainsKey(pm.PluginId))
              throw new ArgumentException(
                string.Format("DirectoryScanner: Duplicate identifier (plugin {0} has the same plugin id as {1}).",
                  pm.LogName, result[pm.PluginId].LogName));
            result.Add(pm.PluginId, pm);
          }
          else
          {
            Log.Error("DirectoryScanner: Error parsing plugin definition file in directory '{0}'", pluginDirectoryPath);
          }
        }
        catch (Exception e)
        {
          Log.Error("DirectoryScanner: Error loading plugin in directory '{0}'", e, pluginDirectoryPath);
        }
      }
      return result;
    }

    #region Static Helpers

    private static ILogger Log
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    #endregion
  }
}