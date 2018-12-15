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

using MediaPortal.Common;
using MediaPortal.Common.Configuration;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.SkinEngine.SkinManagement;
using System.Collections.Generic;
using System.Linq;

namespace HomeEditor.Extensions
{
  /// <summary>
  /// Class that monitors for skin changes and updates the visibility of registered config items
  /// based on whether the current skin supports the home menu editor.
  /// </summary>
  public class SkinChangeMonitor
  {
    #region Instance

    public static readonly SkinChangeMonitor Instance = new SkinChangeMonitor();

    #endregion

    #region Protected fields

    protected readonly object _syncObj = new object();
    protected AsynchronousMessageQueue _messageQueue;

    protected IPluginItemStateTracker _pluginItemStateTracker;
    protected HashSet<string> _currentSkins;
    protected HashSet<string> _supportedSkins;
    protected List<ConfigBase> _configs;

    #endregion

    #region Ctor / Init

    public SkinChangeMonitor()
    {
      _configs = new List<ConfigBase>();
      SubscribeToMessages();
    }

    protected void Init(bool reloadCurrentSkins)
    {
      //Get the currently loaded skins
      if (reloadCurrentSkins || _currentSkins == null)
        _currentSkins = GetCurrentSkins();

      //Only load the plugin items once
      if (_pluginItemStateTracker != null)
        return;

      //Get all registered plugin items
      _pluginItemStateTracker = new FixedItemStateTracker("Home Editor - skin registration");
      _supportedSkins = GetSupportedSkins();
    }

    #endregion

    #region Message handling

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
          //Listen for skin and theme changes
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
            //Update current skin and the visibility of the config items
            UpdateConfigurations();
            break;
        }
      }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Registers the specified <paramref name="config"/>. The item will be hidden if
    /// the current skin does not supports the home menu editor.
    /// </summary>
    /// <param name="config">The configuration item to register.</param>
    public void RegisterConfiguration(ConfigBase config)
    {
      bool visible;
      lock (_syncObj)
      {
        //Init if necessary
        Init(false);
        visible = IsCurrentSkinSupported();
        _configs.Add(config);
      }
      //Update outside lock just in case
      config.Visible = visible;
    }

    /// <summary>
    /// Unregisters the specified <paramref name="config"/>.
    /// </summary>
    /// <param name="config">The configuration item to unregister.</param>
    public void UnregisterConfiguration(ConfigBase config)
    {
      lock (_syncObj)
        _configs.Remove(config);
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Updates the visibility of all registered config items
    /// </summary>
    protected void UpdateConfigurations()
    {
      bool visible;
      List<ConfigBase> configs;
      lock (_syncObj)
      {
        //Update current skin
        Init(true);
        visible = IsCurrentSkinSupported();
        configs = new List<ConfigBase>(_configs);
      }

      //Update outside lock just in case
      foreach (var config in configs)
        config.Visible = visible;
    }

    /// <summary>
    /// Whether the currently loaded skin or one of its parents supports the home menu editor.
    /// </summary>
    /// <returns></returns>
    protected bool IsCurrentSkinSupported()
    {
      return _currentSkins.Any(s => _supportedSkins.Contains(s));
    }

    /// <summary>
    /// Gets the currently loaded skin and all of its parents.
    /// </summary>
    /// <returns>A collection of all currently loaded skin names.</returns>
    protected static HashSet<string> GetCurrentSkins()
    {
      HashSet<string> loadedSkins = new HashSet<string>();
      var sr = SkinContext.SkinResources;
      //walk up the skin inheritance and get all loaded skins
      while (sr != null)
      {
        loadedSkins.Add(sr.Name);
        sr = sr.InheritedSkinResources;
      }
      return loadedSkins;
    }

    /// <summary>
    /// Loads all <see cref="HomeEditorRegistration"/>s and gets a collection of supported skins. 
    /// </summary>
    /// <returns>A collection of supported skin names.</returns>
    protected HashSet<string> GetSupportedSkins()
    {
      HashSet<string> registrations = new HashSet<string>();
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(HomeEditorRegistrationBuilder.HOME_EDITOR_PROVIDER_PATH))
      {
        HomeEditorRegistration registration = GetSkinRegistration(itemMetadata, pluginManager);
        if (registration != null)
          registrations.Add(registration.SkinName);
      }
      return registrations;
    }

    /// <summary>
    /// Loads a <see cref="HomeEditorRegistration"/> for the specified <paramref name="itemMetadata"/>.
    /// </summary>
    /// <param name="itemMetadata"></param>
    /// <param name="pluginManager"></param>
    /// <returns></returns>
    protected HomeEditorRegistration GetSkinRegistration(PluginItemMetadata itemMetadata, IPluginManager pluginManager)
    {
      try
      {
        HomeEditorRegistration providerRegistration = pluginManager.RequestPluginItem<HomeEditorRegistration>(
          HomeEditorRegistrationBuilder.HOME_EDITOR_PROVIDER_PATH, itemMetadata.Id, _pluginItemStateTracker);

        if (providerRegistration != null)
        {
          ServiceRegistration.Get<ILogger>().Info("Successfully added Home Editor skin registration for skin '{0}' (Id '{1}')",
            itemMetadata.Attributes["SkinName"], itemMetadata.Id);
          return providerRegistration;
        }

        ServiceRegistration.Get<ILogger>().Warn("Could not instantiate Home Editor skin registration with id '{0}'", itemMetadata.Id);
      }
      catch (PluginInvalidStateException e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Cannot add Home Editor skin registration with id '{0}'", e, itemMetadata.Id);
      }
      return null;
    }

    #endregion
  }
}