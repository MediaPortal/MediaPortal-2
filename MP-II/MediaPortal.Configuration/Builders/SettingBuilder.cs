
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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Localisation;

namespace MediaPortal.Configuration.Builders
{
  public class SettingBuilder : IPluginBuilder
  {
    #region IPluginBuilder methods
    public object BuildItem(object caller, INodeItem item, ArrayList subItems)
    {
      SettingBase setting;

      try
      {
        if (item.Contains("class"))
          setting = (SettingBase)item.CreateObject(item["class"]);
        else
          setting = new SettingBase();
      }
      catch (Exception)
      {
        //log
        setting = new SettingBase();
      }

      setting.Id = item.Id;
      
      if (item.Contains("text"))
        setting.Text = new StringId(item["text"]);
      else
        setting.Text = new StringId();

      if (item.Contains("help"))
        setting.Help = new StringId(item["help"]);
      else
        setting.Help = new StringId();

      if (item.Contains("iconsmall"))
        setting.IconSmall = Path.Combine(item.Plugin.PluginPath, item["iconsmall"]).ToString();

      if (item.Contains("iconlarge"))
        setting.IconLarge = Path.Combine(item.Plugin.PluginPath, item["iconlarge"]).ToString();

      if (item.Contains("type"))
      {
        try
        {
          setting.Type = (SettingType)Enum.Parse(typeof(SettingType), item["type"], true);
        }
        catch (Exception)
        {
          setting.Type = SettingType.Unknown;
        }
      }
      else
      {
        setting.Type = SettingType.Unknown;
      }

      return setting;
    }
    #endregion
  }
}
