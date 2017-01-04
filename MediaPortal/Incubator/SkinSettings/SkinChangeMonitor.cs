#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.Configuration;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinSettings
{
  public class SkinChangeMonitor
  {
    public static readonly SkinChangeMonitor Instance = new SkinChangeMonitor();

    protected readonly object _syncObj = new object();
    protected AsynchronousMessageQueue _messageQueue;
    protected HashSet<string> _loadedSkins;
    protected Dictionary<string, List<ConfigBase>> _configsDictionary;

    public SkinChangeMonitor()
    {
      _loadedSkins = GetLoadedSkins();
      _configsDictionary = new Dictionary<string, List<ConfigBase>>();
      SubscribeToMessages();
    }

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           SkinResourcesMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    protected void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SkinResourcesMessaging.CHANNEL)
      {
        SkinResourcesMessaging.MessageType messageType = (SkinResourcesMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SkinResourcesMessaging.MessageType.SkinOrThemeChanged:
            UpdateConfigItems();
            break;
        }
      }
    }

    public void RegisterConfiguration(string skinName, ConfigBase config)
    {
      bool visible;
      lock (_syncObj)
      {
        visible = _loadedSkins.Contains(skinName);
        List<ConfigBase> configs;
        if (!_configsDictionary.TryGetValue(skinName, out configs))
        {
          configs = new List<ConfigBase>();
          _configsDictionary[skinName] = configs;
        }
        configs.Add(config);
      }
      config.Visible = visible;
    }

    public void UnregisterConfiguration(string skinName, ConfigBase config)
    {
      lock (_syncObj)
      {
        List<ConfigBase> configs;
        if (_configsDictionary.TryGetValue(skinName, out configs))
          configs.Remove(config);
      }
    }

    protected void UpdateConfigItems()
    {
      List<ConfigBase> visibleItems = new List<ConfigBase>();
      List<ConfigBase> nonVisibleItems = new List<ConfigBase>();
      HashSet<string> loadedSkins = GetLoadedSkins();
      lock (_syncObj)
      {
        _loadedSkins = loadedSkins;
        foreach (var kvp in _configsDictionary)
        {
          if (loadedSkins.Contains(kvp.Key))
            visibleItems.AddRange(kvp.Value);
          else
            nonVisibleItems.AddRange(kvp.Value);
        }
      }

      foreach (var config in visibleItems)
        config.Visible = true;
      foreach (var config in nonVisibleItems)
        config.Visible = false;
    }

    protected static HashSet<string> GetLoadedSkins()
    {
      HashSet<string> loadedSkins = new HashSet<string>();
      var srb = ServiceRegistration.Get<IScreenManager>().CurrentSkinResourceBundle;
      while (srb != null)
      {
        loadedSkins.Add(srb.Name);
        srb = srb.InheritedSkinResources;
      }
      return loadedSkins;
    }
  }
}