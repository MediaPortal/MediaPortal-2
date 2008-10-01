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

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Configuration;

namespace Components.Configuration.Builders
{
  // FIXME: replace this class by direct access to the plugin manager's settings API.
  [Obsolete("Will be replaced by settings access via the plugin manager's settings API.")]
  public class SettingBuilder : IPluginItemBuilder
  {
    #region IPluginBuilder methods

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      //ConfigBase setting;
      //if (itemData.Attributes.ContainsKey("ClassName"))
      //{
      //  string className = itemData.Attributes["ClassName"];
      //  try
      //  {
      //    setting = (ConfigBase) plugin.InstanciatePluginObject(itemData.Attributes["ClassName"]);
      //  }
      //  catch (Exception e)
      //  {
      //    ServiceScope.Get<ILogger>().Error("Can't create instance for class '{0}'", e, className);
      //    setting = new ConfigBase();
      //  }
      //}
      //else
      //  setting = new ConfigBase();
      //// All .plugin files should only contain english characters.
      //setting.Id = itemData.Id;

      //if (itemData.Attributes.ContainsKey("Text"))
      //  setting.Text = new StringId(itemData.Attributes["Text"]);
      //else
      //  setting.Text = new StringId();

      //if (itemData.Attributes.ContainsKey("Help"))
      //  setting.Help = new StringId(itemData.Attributes["Help"]);
      //else
      //  setting.Help = new StringId();

      //if (itemData.Attributes.ContainsKey("IconSmall"))
      //  setting.IconSmall = plugin.Metadata.GetAbsolutePath(itemData.Attributes["IconSmall"]);

      //if (itemData.Attributes.ContainsKey("IconLarge"))
      //  setting.IconLarge = plugin.Metadata.GetAbsolutePath(itemData.Attributes["IconLarge"]);

      //int width = -1;
      //if (itemData.Attributes.ContainsKey("Width"))
      //  Int32.TryParse(itemData.Attributes["Width"], out width);
      //setting.Width = width;

      //int height = -1;
      //if (itemData.Attributes.ContainsKey("Height"))
      //  Int32.TryParse(itemData.Attributes["Height"], out height);
      //setting.Height = height;

      //if (itemData.Attributes.ContainsKey("Type"))
      //{
      //  try
      //  {
      //    setting.Type = (SettingType) Enum.Parse(typeof(SettingType), itemData.Attributes["Type"], true);
      //  }
      //  catch (Exception)
      //  {
      //    setting.Type = SettingType.Unknown;
      //  }
      //}
      //else
      //{
      //  setting.Type = SettingType.Unknown;
      //}

      //if (itemData.Attributes.ContainsKey("ListenTo"))
      //  // Very easy parser for a list of items of this kind: [Item1] [Item2] [Item3] ...
      //  setting.ListenItems = new List<string>(itemData.Attributes["ListenTo"].Replace(" ", "").Split(new char[] { '[', ']', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
      //else
      //  setting.ListenItems = new List<string>(0);

      //return setting;
      return new object();
    }

    public bool NeedsPluginActive
    {
      get { return true; }
    }

    #endregion
  }
}
