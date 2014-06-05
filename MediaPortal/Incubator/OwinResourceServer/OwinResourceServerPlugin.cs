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

using MediaPortal.Common;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Plugins.WebServices.OwinResourceServer
{
  /// <summary>
  /// PluginStateStracker for the <see cref="OwinResourceServer"/>.
  /// </summary>
  /// <remarks>
  /// Creates an instance of <see cref="OwinResourceServer"/>, registers it in the <see cref="ServiceRegistration"/>
  /// and thereby replaces the default ResourceServer when the plugin is activated.
  /// </remarks>
  public class OwinResourceServerPlugin : IPluginStateTracker
  {

    #region IPluginStateStracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      ServiceRegistration.RemoveAndDispose<IResourceServer>();
      ServiceRegistration.Set<IResourceServer>(new OwinResourceServer());
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Continue()
    {
    }

    public void Shutdown()
    {
    }

    #endregion

  }
}
