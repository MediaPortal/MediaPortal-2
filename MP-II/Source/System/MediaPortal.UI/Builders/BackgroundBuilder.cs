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
using MediaPortal.Core.PluginManager;
using MediaPortal.Presentation.Screens;

namespace MediaPortal.Builders
{
  public class BackgroundBuilder : IPluginItemBuilder
  {
    #region IPluginItemBuilder implementation

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      string value;
      if (itemData.Attributes.TryGetValue("StaticScreen", out value))
        return new StaticBackgroundManager(value);
      if (itemData.Attributes.TryGetValue("BackgroundManagerClassName", out value))
        // The cast is necessary here to ensure the returned instance is an IBackgroundManager
        return (IBackgroundManager) plugin.InstanciatePluginObject(value);
      if (itemData.Attributes.TryGetValue("RemoveBackground", out value))
      {
        bool bVal;
        if (!bool.TryParse(value, out bVal) || !bVal)
          throw new ArgumentException("Background builder: Attribute 'RemoveBackground' needs to be set to 'true'");
        return new StaticBackgroundManager();
      }
      throw new ArgumentException("Background builder needs one of the attributes 'StaticScreen', 'RemoveBackground' or 'BackgroundManagerClassName'");
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      if (itemData.Attributes.ContainsKey("StaticScreen") || itemData.Attributes.ContainsKey("RemoveBackground"))
        return false;
      if (itemData.Attributes.ContainsKey("BackgroundManagerClassName"))
        return true;
      throw new ArgumentException("Background builder needs one of the attributes 'StaticScreen', 'RemoveBackground' or 'BackgroundManagerClassName'");
    }

    #endregion
  }
}