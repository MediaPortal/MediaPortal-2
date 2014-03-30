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
using System.Linq;
using System.Xml.XPath;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager.Models;
using MediaPortal.Common.Services.PluginManager;
using MediaPortal.Utilities;

namespace MediaPortal.Common.PluginManager.Discovery
{
  /// <summary>
  /// Class providing logic to enumerate all installed plugins by scanning a local directory.
  /// </summary>
  class DirectoryScanner
  {
    private string _pluginsPath;

    public DirectoryScanner( string pluginsPath )
    {
      _pluginsPath = pluginsPath;
    }

    public IDictionary<Guid, PluginMetadata> PerformDiscovery()
    {
      var result = new Dictionary<Guid, PluginMetadata>();
      foreach (string pluginDirectoryPath in Directory.GetDirectories( _pluginsPath ))
      {
        if ((Path.GetFileName(pluginDirectoryPath) ?? string.Empty).StartsWith("."))
          continue;
        try
        {
          PluginMetadata pm;
          if( pluginDirectoryPath.TryParsePluginDefinition( out pm ) )
          {
            if (result.ContainsKey(pm.PluginId))
              throw new ArgumentException(string.Format(
                "Duplicate: plugin '{0}' has the same plugin id as {1}", pm.Name, result[pm.PluginId]));
            result.Add(pm.PluginId, pm);
          }
          else
          {
            ServiceRegistration.Get<ILogger>().Error("Error loading plugin in directory '{0}'", pluginDirectoryPath);
          }
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("Error loading plugin in directory '{0}'", e, pluginDirectoryPath);
        }
      }
      return result;
    }
  }
}
