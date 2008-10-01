#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System;
using System.Collections.Generic;

using MediaPortal.Core.PluginManager;
using MediaPortal.Core.PluginManager.Settings;
using MediaPortal.Configuration;
using MediaPortal.Presentation.Localisation;

namespace MediaPortal.Configuration
{

  /// <summary>
  /// LoaderHelper provides a structured interface for all settings
  /// exposed by the added instances of PluginRuntime.
  /// </summary>
  internal class LoaderHelper
  {

    #region Variables

    private IList<LoaderItem> _items;

    #endregion

    #region Constructors

    public LoaderHelper()
    {
      _items = new List<LoaderItem>();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a PluginRuntime to the current helper.
    /// </summary>
    /// <param name="runtime"></param>
    public void Add(PluginRuntime runtime)
    {
      foreach (SettingRegistrationBase setting in runtime.Metadata.PluginSettingsMetadata)
      {
        string[] location = setting.Location.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (location.Length == 0)
          continue;
        if (location.Length == 1) // RootNode
        {
          LoaderItem item = new LoaderItem(setting, runtime);
          int index = _items.IndexOf(item);
          if (index != -1)
            _items[index] = item;
          else
            _items.Add(item);
        }
        else
        {
          IList<LoaderItem> items = _items;
          int index;
          for (int i = 0; i < location.Length - 1; i++)
          {
            index = items.IndexOf(new LoaderItem(location[i]));
            if (index == -1)
            {
              items.Add(new LoaderItem(location[i]));
              index = items.Count - 1;
            }
            items = items[index].Items;
          }
          index = items.IndexOf(new LoaderItem(setting.Id));
          if (index == -1)
            items.Add(new LoaderItem(setting, runtime));
          else
            items[index] = new LoaderItem(setting, runtime);
        }
      }
    }
    
    /// <summary>
    /// Initializes all items which are member of the specified location.
    /// </summary>
    /// <param name="location">Location to build all members from.</param>
    /// <returns></returns>
    public IList<ConfigBase> InitializeItems(string location)
    {
      string[] locs = location.Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
      IList<LoaderItem> items = _items;
      foreach (string loc in locs)
      {
        bool found = false;
        foreach (LoaderItem item in items)
        {
          if (item.Id == loc)
          {
            if (!item.Instantiable)
              throw new NodeNotFoundException("Can't create an instance of the requested item: \"" + item.Id + "\" in location \"" + location + "\"");
            items = item.Items;
            found = true;
            break;
          }
        }
        if (!found)
          throw new NodeNotFoundException();
      }
      List<ConfigBase> output = new List<ConfigBase>(items.Count);
      foreach (LoaderItem item in items)
        output.Add(item.Instantiate());
      return output;
    }

    #endregion

    #region Private Classes

    /// <summary>
    /// LoaderItem represents an uninitialized ConfigBase-object.
    /// It's able to initialize such instance by using a PluginRuntime and a classname.
    /// It contains the items which are member of the location created by the ConfigBase-object.
    /// </summary>
    private class LoaderItem : IEquatable<LoaderItem>
    {

      #region Variables

      /// <summary>
      /// The ID of the current LoaderItem.
      /// </summary>
      private string _id;
      /// <summary>
      /// The PluginRuntime to use when building the instance of ConfigBase.
      /// </summary>
      private PluginRuntime _runtime;
      /// <summary>
      /// The SettingRegistrationBase containing the classname to use in order to build the instance of ConfigBase.
      /// </summary>
      private SettingRegistrationBase _setting;
      /// <summary>
      /// All members of the current LoaderItem.
      /// </summary>
      private IList<LoaderItem> _items;

      #endregion

      #region Properties

      /// <summary>
      /// Gets the ID of the current LoaderItem.
      /// </summary>
      public string Id
      {
        get { return _id; }
      }

      /// <summary>
      /// Gets all LoaderItems which are a member of the current LoaderItems.
      /// </summary>
      public IList<LoaderItem> Items
      {
        get { return _items; }
      }

      /// <summary>
      /// Gets whether its possible to create an instance of ConfigBase from the current LoaderItem.
      /// </summary>
      public bool Instantiable
      {
        get { return _runtime != null && _setting != null; }
      }

      #endregion

      #region Constructors

      /// <summary>
      /// Initializes a new instance of LoaderItem.
      /// </summary>
      /// <param name="id"></param>
      public LoaderItem(string id)
      {
        _id = id;
        _items = new List<LoaderItem>();
      }

      /// <summary>
      /// Initializes a new instance of LoaderItem.
      /// </summary>
      /// <param name="settingRegistrationBase"></param>
      /// <param name="runtime"></param>
      public LoaderItem(SettingRegistrationBase settingRegistrationBase, PluginRuntime runtime)
        : this(settingRegistrationBase.Id)
      {
        _runtime = runtime;
        _setting = settingRegistrationBase;
      }

      #endregion

      #region Public Methods

      /// <summary>
      /// Instantiates the current LoaderItem as a ConfigBase.
      /// </summary>
      /// <returns></returns>
      public ConfigBase Instantiate()
      {
        if (_setting is Setting)
        {
          Setting setting = (Setting) _setting;
          ConfigItem configItem = (ConfigItem)_runtime.InstanciatePluginObject(setting.ClassName);
          configItem.Text = new StringId(setting.Text);
          configItem.Help = new StringId(setting.HelpText);
          configItem.Hidden = setting.Hidden;
          configItem.Disabled = setting.Disabled;
          configItem.Location = _setting.Location;
          return configItem;
        }
        if (_setting is Group)
          return new ConfigGroup(_setting.Location, new StringId(_setting.Text));
        if (_setting is Section)
          return new ConfigSection(_setting.Location, new StringId(_setting.Text),
            _runtime.Metadata.GetAbsolutePath(((Section)_setting).IconSmall), _runtime.Metadata.GetAbsolutePath(((Section)_setting).IconLarge));
        throw new OperationCanceledException("Can't instantiate an object for the current LoaderItem.");
      }

      /// <summary>
      /// Returns a string which represents the current LoaderItem.
      /// </summary>
      /// <returns></returns>
      public override string ToString()
      {
        return _id + "  - " + _items.Count + " subitems";
      }

      #endregion

      #region IEquatable<LoaderNode> Members

      /// <summary>
      /// Determines if the current and other LoaderItem are equal.
      /// </summary>
      /// <param name="other"></param>
      /// <returns></returns>
      public bool Equals(LoaderItem other)
      {
        return _id == other._id;
      }

      #endregion

    }

    #endregion

  }
}
