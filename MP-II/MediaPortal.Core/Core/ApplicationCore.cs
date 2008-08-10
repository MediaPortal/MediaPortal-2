#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.Logging;
using MediaPortal.Interfaces.Core.PluginManager;

namespace MediaPortal.Core
{
  /// <summary>
  /// This is the MediaPortal Core
  /// </summary>
  public class ApplicationCore
  {
    public void Start()
    {
      //Start the plugins
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      pluginManager.Startup();

      ServiceScope.Get<ILogger>().Info("Application: Starting Autostart plugins");
      foreach (IAutoStart plugin in pluginManager.GetAllPluginItems<IAutoStart>("/AutoStart"))
      {
        plugin.Startup();
      }
    }
  }
}
