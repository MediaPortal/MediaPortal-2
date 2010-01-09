#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Core.Services.PluginManager;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;

namespace MediaPortal.Core.Services.Localization
{
  /// <summary>
  /// Base class for localization management classes.
  /// </summary>
  /// <remarks>
  /// This class is multithreading-safe.
  /// </remarks>
  public class StringManagerBase : IDisposable
  {
    public const string LANGUAGE_RESOURCES_REGISTRATION_PATH = "/Resources/Language";

    protected class LanguagePluginItemStateTracker : IPluginItemStateTracker
    {
      protected StringManagerBase _parent;

      public LanguagePluginItemStateTracker(StringManagerBase parent)
      {
        _parent = parent;
      }

      #region IPluginItemStateTracker implementation

      public string UsageDescription
      {
        get { return "Localization system: Language resources"; }
      }

      public bool RequestEnd(PluginItemRegistration itemRegistration)
      {
        // We don't care about strings which are currently in use; we don't have an overview which strings are
        // still needed
        return true;
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        _parent.RemoveLanguageResource(itemRegistration);
      }

      public void Continue(PluginItemRegistration itemRegistration)
      { }

      #endregion
    }

    protected class LanguageResourcesRegistrationChangeListener : IItemRegistrationChangeListener
    {
      protected StringManagerBase _parent;

      public LanguageResourcesRegistrationChangeListener(StringManagerBase parent)
      {
        _parent = parent;
      }

      #region IItemRegistrationChangeListener implementation

      public void ItemsWereAdded(string location, ICollection<PluginItemMetadata> items)
      {
        _parent.AddLanguageResources(items);
      }

      public void ItemsWereRemoved(string location, ICollection<PluginItemMetadata> items)
      {
        // Item removals are handled by the SkinResourcesPluginItemStateTracker
      }

      #endregion
    }

    #region Protected fields

    protected IPluginItemStateTracker _languagePluginStateTracker;
    protected IItemRegistrationChangeListener _languageResourcesRegistrationChangeListener;
    protected ICollection<CultureInfo> _availableLanguages = null;
    protected ICollection<string> _languageDirectories = null;
    protected object _syncObj = new object();

    #endregion

    #region Constructors/Destructors

    public StringManagerBase()
    {
      _languagePluginStateTracker = new LanguagePluginItemStateTracker(this);
      _languageResourcesRegistrationChangeListener = new LanguageResourcesRegistrationChangeListener(this);
    }

    public virtual void Dispose()
    {
      ServiceScope.Get<IPluginManager>().RevokeAllPluginItems(LANGUAGE_RESOURCES_REGISTRATION_PATH,
          _languagePluginStateTracker);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when the plugin manager notifies the system about its events.
    /// Adds plugin language resource folders to the directory list when all plugins are initialized.
    /// </summary>
    /// <param name="queue">Queue which sent the message.</param>
    /// <param name="message">Message containing the notification data.</param>
    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PluginManagerMessaging.CHANNEL)
      {
        if (((PluginManagerMessaging.MessageType) message.MessageType) ==
            PluginManagerMessaging.MessageType.PluginsInitialized)
          InitializeLanguageResources();
      }
    }

    #endregion

    #region Protected methods

    protected void InitializeLanguageResources()
    {
      try
      {
        // Add language directories
        IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
        ICollection<PluginResource> languageResources = pluginManager.RequestAllPluginItems<PluginResource>(
            LANGUAGE_RESOURCES_REGISTRATION_PATH, _languagePluginStateTracker);
        pluginManager.AddItemRegistrationChangeListener(
            LANGUAGE_RESOURCES_REGISTRATION_PATH, _languageResourcesRegistrationChangeListener);
  
        lock (_syncObj)
        {
          _languageDirectories = new List<string>();
  
          ILogger logger = ServiceScope.Get<ILogger>();
          foreach (PluginResource resource in languageResources)
          {
            logger.Debug("{0}: Adding language directory '{1}'", GetType().Name, resource.Path);
            if (Directory.Exists(resource.Path))
              _languageDirectories.Add(resource.Path);
            else
              logger.Error("{0}: Language directory doesn't exist: {1}", GetType().Name, resource.Path);
          }
        }
  
        ReLoad();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("{0}: Error initializing language resources", e, GetType().Name);
      }
    }

    protected void AddLanguageResources(ICollection<PluginItemMetadata> items)
    {
      lock (_syncObj)
        foreach (PluginItemMetadata item in items)
        {
          PluginResource resource = ServiceScope.Get<IPluginManager>().RequestPluginItem<PluginResource>(
                item.RegistrationLocation, item.Id, _languagePluginStateTracker);
          if (resource != null && Directory.Exists(resource.Path))
            _languageDirectories.Add(resource.Path);
        }
      ReLoad();
    }

    protected void RemoveLanguageResource(PluginItemRegistration itemRegistration)
    {
      PluginResource languageResource = (PluginResource) itemRegistration.Item;
      lock (_syncObj)
        _languageDirectories.Remove(languageResource.Path);
      ReLoad();
    }

    /// <summary>
    /// Loads or reloads all localization resources based on the current <see cref="_languageDirectories"/>.
    /// </summary>
    protected virtual void ReLoad()
    {
      lock (_syncObj)
        _availableLanguages = LocalizationStrings.FindAvailableLanguages(_languageDirectories);
    }

    #endregion

    #region Properties

    public ICollection<CultureInfo> AvailableLanguages
    {
      get { return _availableLanguages; }
    }

    #endregion

  }
}
