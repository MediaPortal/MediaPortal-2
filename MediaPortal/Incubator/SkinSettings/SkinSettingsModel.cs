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
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinSettings
{
  public class SkinSettingsModel : IObservable
  {
    #region SettingsLoader

    protected class SettingsLoader
    {
      Type _type;
      object _value;

      public SettingsLoader(Type type)
      {
        _type = type;
      }

      public object Value
      {
        get
        {
          object value = _value;
          if (value == null)
            _value = value = ServiceRegistration.Get<ISettingsManager>().Load(_type);
          return value;
        }
        set { _value = value; }
      }
    }

    #endregion

    protected readonly object _syncObj = new object();
    protected IPluginItemStateTracker _pluginItemStateTracker;
    protected Dictionary<Type, string> _registeredTypes;
    protected IDictionary<string, SettingsLoader> _registeredNames;
    protected WeakEventMulticastDelegate _objectChanged = new WeakEventMulticastDelegate();
    protected AsynchronousMessageQueue _messageQueue;

    public SkinSettingsModel()
    {
      InitRegisteredSettings();
      _messageQueue = new AsynchronousMessageQueue(this, new string[] { SettingsManagerMessaging.CHANNEL, PluginManagerMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SettingsManagerMessaging.CHANNEL)
      {
        SettingsManagerMessaging.MessageType messageType = (SettingsManagerMessaging.MessageType)message.MessageType;
        if (messageType == SettingsManagerMessaging.MessageType.SettingsChanged)
        {
          Type settingsType = (Type)message.MessageData[SettingsManagerMessaging.SETTINGSTYPE];
          Update(settingsType);
        }
      }
      else if (message.ChannelName == PluginManagerMessaging.CHANNEL)
      {
        PluginManagerMessaging.MessageType messageType = (PluginManagerMessaging.MessageType)message.MessageType;
        if (messageType == PluginManagerMessaging.MessageType.PluginsInitialized)
          InitRegisteredSettings();
      }
    }

    public object this[string name]
    {
      get { return GetSettings(name); }
    }

    public event ObjectChangedDlgt ObjectChanged
    {
      add { _objectChanged.Attach(value); }
      remove { _objectChanged.Detach(value); }
    }

    public void FireChange()
    {
      _objectChanged.Fire(new object[] { this });
    }

    protected object GetSettings(string name)
    {
      var names = _registeredNames;
      SettingsLoader settings;
      if (names == null || !names.TryGetValue(name, out settings))
        return null;
      return settings.Value;
    }

    protected void Update(Type settingsType)
    {
      var types = _registeredTypes;
      var names = _registeredNames;
      string name;
      SettingsLoader settings;
      if (!types.TryGetValue(settingsType, out name) || !names.TryGetValue(name, out settings))
        return;
      //Settings will be lazily loaded the next time they are requested
      settings.Value = null;
      FireChange();
    }

    protected void InitRegisteredSettings()
    {
      lock (_syncObj)
      {
        if (_pluginItemStateTracker == null)
          _pluginItemStateTracker = new FixedItemStateTracker("Skin Settings - Type registration");
        var types = new Dictionary<Type, string>();
        var names = new Dictionary<string, SettingsLoader>();

        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(SkinSettingsBuilder.SKIN_SETTINGS_PROVIDER_PATH))
        {
          try
          {
            SkinSettingsRegistration providerRegistration = pluginManager.RequestPluginItem<SkinSettingsRegistration>(
              SkinSettingsBuilder.SKIN_SETTINGS_PROVIDER_PATH, itemMetadata.Id, _pluginItemStateTracker);
            if (providerRegistration == null)
              ServiceRegistration.Get<ILogger>().Warn("Could not instantiate Skin Settings registration with id '{0}'", itemMetadata.Id);
            else
            {
              if (names.ContainsKey(providerRegistration.Name))
              {
                ServiceRegistration.Get<ILogger>().Warn("Could not add Skin Settings type '{0}' with name '{1}' (Id '{2}'). The name is already in use.",
                  itemMetadata.Attributes["ClassName"], providerRegistration.Name, itemMetadata.Id);
                continue;
              }
              types[providerRegistration.ProviderClass] = providerRegistration.Name;
              names[providerRegistration.Name] = new SettingsLoader(providerRegistration.ProviderClass);
              ServiceRegistration.Get<ILogger>().Info("Successfully added Skin Settings type '{0}' with name '{1}' (Id '{2}')",
                itemMetadata.Attributes["ClassName"], providerRegistration.Name, itemMetadata.Id);
            }
          }
          catch (PluginInvalidStateException e)
          {
            ServiceRegistration.Get<ILogger>().Warn("Cannot add Skin Settings registration with id '{0}'", e, itemMetadata.Id);
          }
        }
        _registeredTypes = types;
        _registeredNames = names;
      }
      FireChange();
    }
  }
}