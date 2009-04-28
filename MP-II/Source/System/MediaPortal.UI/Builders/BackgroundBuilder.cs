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
using System.Collections.Generic;
using MediaPortal.Core.PluginManager;
using MediaPortal.Presentation.Screens;

namespace MediaPortal.Builders
{
  public class BackgroundBuilder : IPluginItemBuilder
  {
    protected enum BackgroundType
    {
      None,
      Static,
      Manager,
      Remove
    }

    protected static string GetBackgroundAndType(IDictionary<string, string> attributes, out BackgroundType type)
    {
      type = BackgroundType.None;
      string result = null;
      foreach (KeyValuePair<string, string> attribute in attributes)
      {
        if (type != BackgroundType.None)
          throw new ArgumentException("Background builder: Only one of the attributes 'StaticScreen', 'RemoveBackground' and 'BackgroundManagerClassName' must be set");
        if (attribute.Key == "StaticScreen")
        {
          type = BackgroundType.Static;
          result = attribute.Value;
        }
        else if (attribute.Key == "BackgroundManagerClassName")
        {
          type = BackgroundType.Manager;
          result = attribute.Value;
        }
        else if (attribute.Key == "RemoveBackground")
        {
          bool bVal;
          if (!bool.TryParse(attribute.Value, out bVal) || !bVal)
            throw new ArgumentException("Background builder: Attribute 'RemoveBackground' needs to be set to 'true'");
          type = BackgroundType.Remove;
        }
      }
      if (type == BackgroundType.None)
        throw new ArgumentException("Background builder needs one of the attributes 'StaticScreen', 'RemoveBackground' or 'BackgroundManagerClassName'");
      return result;
    }

    #region IPluginItemBuilder implementation

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BackgroundType type;
      string value = GetBackgroundAndType(itemData.Attributes, out type);
      switch (type)
      {
        case BackgroundType.Static:
          return new StaticBackgroundManager(value);
        case BackgroundType.Manager:
          // The cast is necessary here to ensure the returned instance is an IBackgroundManager
          return (IBackgroundManager) plugin.InstanciatePluginObject(value);
        case BackgroundType.Remove:
          return new StaticBackgroundManager();
        default:
          throw new NotImplementedException(string.Format(
              "Background builder: Background type '{0}' is not implemented", type));
      }
    }

    public void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BackgroundType type;
      string typeName = GetBackgroundAndType(itemData.Attributes, out type);
      switch (type)
      {
        case BackgroundType.Manager:
          plugin.RevokePluginObject(typeName);
          break;
      }
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BackgroundType type;
      string value = GetBackgroundAndType(itemData.Attributes, out type);
      if (type == BackgroundType.Static || type == BackgroundType.Remove)
        return false;
      if (type == BackgroundType.Manager)
        return true;
      throw new NotImplementedException(string.Format(
          "Background builder: Background type '{0}' is not implemented", type));
    }

    #endregion
  }
}