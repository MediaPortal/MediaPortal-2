#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Configuration;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.UserManagement;

namespace MediaPortal.Configuration.Builders
{
  /// <summary>
  /// Plugin item builder for building <i>Section</i>, <i>Group</i> and <i>Setting</i> plugin items.
  /// </summary>
  public class ConfigBuilder : IPluginItemBuilder
  {
    #region Protected methods

    protected static ConfigSectionMetadata BuildSection(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      string location = ConfigBaseMetadata.ConcatLocations(itemData.RegistrationLocation, itemData.Id);
      string text = null;
      string sort = null;
      string iconSmallPath = null;
      string iconLargePath = null;
      string restrictionGroup = null;
      foreach (KeyValuePair<string, string> attr in itemData.Attributes)
      {
        switch (attr.Key)
        {
          case "Text":
            text = attr.Value;
            break;
          case "Sort":
            sort = attr.Value;
            break;
          case "IconSmallPath":
            iconSmallPath = attr.Value;
            break;
          case "IconLargePath":
            iconLargePath = attr.Value;
            break;
          case "RestrictionGroup":
            SetValueAndRegister(ref restrictionGroup, attr.Value);
            break;
          default:
            throw new ArgumentException("'ConfigSection' builder doesn't define an attribute '" + attr.Key + "'");
        }
      }
      if (text == null)
        throw new ArgumentException("'ConfigSection' item needs an attribute 'Text'");
      return new ConfigSectionMetadata(location, text, sort,
                                       plugin.Metadata.GetAbsolutePath(iconSmallPath),
                                       plugin.Metadata.GetAbsolutePath(iconLargePath),
                                       restrictionGroup);
    }

    protected static ConfigGroupMetadata BuildGroup(
      PluginItemMetadata itemData, PluginRuntime plugin)
    {
      string location = ConfigBaseMetadata.ConcatLocations(itemData.RegistrationLocation, itemData.Id);
      string text = null;
      string sort = null;
      string restrictionGroup = null;
      foreach (KeyValuePair<string, string> attr in itemData.Attributes)
      {
        switch (attr.Key)
        {
          case "Text":
            text = attr.Value;
            break;
          case "Sort":
            sort = attr.Value;
            break;
          case "RestrictionGroup":
            SetValueAndRegister(ref restrictionGroup, attr.Value);
            break;
          default:
            throw new ArgumentException("'ConfigGroup' builder doesn't define an attribute '" + attr.Key + "'");
        }
      }
      if (text == null)
        throw new ArgumentException("'ConfigGroup' item needs an attribute 'Text'");
      return new ConfigGroupMetadata(location, text, sort, restrictionGroup);
    }

    protected static ConfigSettingMetadata BuildSetting(
      PluginItemMetadata itemData, PluginRuntime plugin)
    {
      string location = ConfigBaseMetadata.ConcatLocations(itemData.RegistrationLocation, itemData.Id);
      string text = null;
      string sort = null;
      string className = null;
      string helpText = null;
      ICollection<string> listenTo = null;
      string restrictionGroup = null;
      foreach (KeyValuePair<string, string> attr in itemData.Attributes)
      {
        switch (attr.Key)
        {
          case "Text":
            text = attr.Value;
            break;
          case "Sort":
            sort = attr.Value;
            break;
          case "ClassName":
            className = attr.Value;
            break;
          case "HelpText":
            helpText = attr.Value;
            break;
          case "ListenTo":
            listenTo = ParseListenTo(attr.Value);
            break;
          case "RestrictionGroup":
            SetValueAndRegister(ref restrictionGroup, attr.Value);
            break;
          default:
            throw new ArgumentException("'ConfigSetting' builder doesn't define an attribute '" + attr.Key+ "'");
        }
      }
      if (text == null)
        throw new ArgumentException("'ConfigSetting' item needs an attribute 'Text'");
      return new ConfigSettingMetadata(location, text, sort, className, helpText, listenTo, restrictionGroup);
    }

    protected static ConfigSettingMetadata BuildCustomSetting(
      PluginItemMetadata itemData, PluginRuntime plugin)
    {
      string location = ConfigBaseMetadata.ConcatLocations(itemData.RegistrationLocation, itemData.Id);
      string text = null;
      string sort = null;
      string className = null;
      string helpText = null;
      IDictionary<string, string> additionalData = null;
      IDictionary<string, Type> additionalTypes = null;
      ICollection<string> listenTo = null;
      string restrictionGroup = null;
      foreach (KeyValuePair<string, string> attr in itemData.Attributes)
      {
        switch (attr.Key)
        {
          case "Text":
            text = attr.Value;
            break;
          case "Sort":
            sort = attr.Value;
            break;
          case "ClassName":
            className = attr.Value;
            break;
          case "HelpText":
            helpText = attr.Value;
            break;
          case "ListenTo":
            listenTo = ParseListenTo(attr.Value);
            break;
          case "AdditionalData":
            additionalData = ParseAdditionalData(attr.Value);
            break;
          case "AdditionalTypes":
            additionalTypes = ParseAdditionalTypes(attr.Value, plugin);
            break;
          case "RestrictionGroup":
            SetValueAndRegister(ref restrictionGroup, attr.Value);
            break;
          default:
            throw new ArgumentException("'ConfigSetting' builder doesn't define an attribute '" + attr.Key + "'");
        }
      }
      if (text == null)
        throw new ArgumentException("'ConfigSetting' item needs an attribute 'Text'");
      ConfigSettingMetadata result = new ConfigSettingMetadata(location, text, sort, className, helpText, listenTo, restrictionGroup)
        {
            AdditionalData = additionalData,
            AdditionalTypes = additionalTypes
        };
      return result;
    }

    protected static ICollection<string> ParseListenTo(string listenTo)
    {
      return listenTo == null ? null : new List<string>(
                                         listenTo.Replace(" ", string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
    }

    protected static IDictionary<string, string> ParseAdditionalData(string additionalData)
    {
      IDictionary<string, string> result = new Dictionary<string, string>();
      if (additionalData == null)
        return result;
      string[] entries = additionalData.Replace(" ", string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
      foreach (string entry in entries)
      {
        int index = entry.IndexOf('=');
        if (index == -1)
          result.Add(entry, null);
        else
          result.Add(entry.Substring(0, index).Trim(), entry.Substring(index+1, entry.Length-index-1));
      }
      return result;
    }

    protected static IDictionary<string, Type> ParseAdditionalTypes(string additionalTypesList, PluginRuntime plugin)
    {
      IDictionary<string, Type> result = new Dictionary<string, Type>();
      if (additionalTypesList == null)
        return result;
      string[] entries = additionalTypesList.Replace(" ", string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
      foreach (string entry in entries)
      {
        int index = entry.IndexOf('=');
        if (index == -1)
          throw new ArgumentException("AdditionTypes must contain entries in the form 'entry=typename'");
        else
        {
          string typeName = entry.Substring(index + 1, entry.Length - index - 1).Trim();
          Type type = plugin.GetPluginType(typeName);
          result.Add(entry.Substring(0, index).Trim(), type);
        }
      }
      return result;
    }

    private static void SetValueAndRegister(ref string restrictionGroup, string attrValue)
    {
      restrictionGroup = attrValue;
      var userManagement = ServiceRegistration.Get<IUserManagement>(false);
      userManagement?.RegisterRestrictionGroup(restrictionGroup);
    }

    #endregion

    #region IPluginBuilder methods

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      switch (itemData.BuilderName)
      {
        case "ConfigSection":
          return BuildSection(itemData, plugin);
        case "ConfigGroup":
          return BuildGroup(itemData, plugin);
        case "ConfigSetting":
          return BuildSetting(itemData, plugin);
        case "CustomConfigSetting":
          return BuildCustomSetting(itemData, plugin);
      }
      throw new ArgumentException(string.Format("{0} builder cannot build setting of type '{1}'",
                                                typeof(ConfigBuilder).Name, itemData.BuilderName));
    }

    public void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin)
    {
      // Nothing to do here
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      return itemData.BuilderName != "ConfigSection" && itemData.BuilderName != "ConfigGroup";
    }

    #endregion
  }
}
