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

using System;
using System.IO;
using System.Collections;
using MediaPortal.Interfaces.Core.PluginManager;

namespace MediaPortal.Services.PluginManager.Builders
{
  /// <summary>
  /// Builds a resource item
  /// </summary>
  public class ResourceBuilder : IPluginItemBuilder
  {
    #region IPluginItemBuilder Members
    public object BuildItem(IPluginRegisteredItem item)
    {
      PluginResourceDescriptor resource = new PluginResourceDescriptor();

      resource.PluginName = item.Plugin.Name;
      resource.Location = Path.Combine(item.Plugin.PluginPath.FullName, item["location"]);

      // test to see if it is a relative path
      if (!Directory.Exists(resource.Location))
      {
        // Location string returned as is
        resource.Location = item["location"];
      }
      return resource;
    }
    #endregion
  }
}
