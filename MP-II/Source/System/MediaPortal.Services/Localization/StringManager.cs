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
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.Services.PluginManager;
using MediaPortal.Presentation.Localization;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;

namespace MediaPortal.Services.Localization
{
  /// <summary>
  /// This class manages localisation strings.
  /// </summary>
  public class StringManager : ILocalization
  {
    protected class LanguagePluginItemStateTracker : IPluginItemStateTracker
    {
      protected StringManager _parent;

      public LanguagePluginItemStateTracker(StringManager parent)
      {
        _parent = parent;
      }

      #region IPluginItemStateTracker implementation

      public bool RequestEnd(PluginItemRegistration itemRegistration)
      {
        // We don't care about strings in use, because we don't have an overview which strings are
        // still needed.
        return true;
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        _parent.RemovePlugin(itemRegistration);
      }

      public void Continue(PluginItemRegistration itemRegistration)
      { }

      #endregion
    }

    #region Protected fields

    protected LocalizationStrings _strings;
    protected IPluginItemStateTracker _languagePluginStateTracker;

    #endregion

    #region Constructors/Destructors

    public StringManager()
    {
      _languagePluginStateTracker = new LanguagePluginItemStateTracker(this);
      ServiceScope.Get<ILogger>().Debug("StringManager: Loading settings");
      RegionSettings settings = ServiceScope.Get<ISettingsManager>().Load<RegionSettings>();

      if (settings.Culture == string.Empty)
      {
        ServiceScope.Get<ILogger>().Info("StringManager: Culture not found in settings");
        _strings = new LocalizationStrings("Language", null);
        settings.Culture = _strings.CurrentCulture.Name;

        ServiceScope.Get<ILogger>().Info("StringManager: Culture set to: " + _strings.CurrentCulture.Name);
        ServiceScope.Get<ISettingsManager>().Save(settings);
        ServiceScope.Get<ILogger>().Info("StringsManager: Saving settings");
      }
      else
      {
        ServiceScope.Get<ILogger>().Debug("StringManager: Using culture: " + settings.Culture);
        _strings = new LocalizationStrings("Language", settings.Culture);
      }

      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(PluginManagerMessaging.Queue);
      queue.OnMessageReceive += OnPluginManagerMessageReceived;
    }

    #endregion

    #region Protected methods

    protected void InitializeLanguageResources()
    {
      try
      {
        ICollection<PluginResource> languageResources = ServiceScope.Get<IPluginManager>().RequestAllPluginItems<PluginResource>(
            "/Resources/Language", new FixedItemStateTracker());

        ILogger logger = ServiceScope.Get<ILogger>();
        foreach (PluginResource resource in languageResources)
        {
          logger.Debug("StringManager: Adding language directory '{0}'", resource.Path);
          if (Directory.Exists(resource.Path))
            AddDirectory(resource.Path);
          else
            logger.Error("StringManager: Language directory doesn't exist: {0}", resource.Path);
        }
      }
      catch (Exception)
      {
        ServiceScope.Get<ILogger>().Error("StringManager: Error initializing language resources");
      }
    }

    protected void RemovePlugin(PluginItemRegistration itemRegistration)
    {
      PluginResource languageResource = (PluginResource) itemRegistration.Item;
      _strings.RemoveDirectory(languageResource.Path);
    }

    public void Dispose()
    {
      ServiceScope.Get<IPluginManager>().RevokeAllPluginItems("/Resources/Language", _languagePluginStateTracker);
    }

    #endregion

    #region ILocalization implementation

    public event LanguageChangeHandler LanguageChange;

    public CultureInfo CurrentCulture
    {
      get { return _strings.CurrentCulture; }
    }

    public void ChangeLanguage(string cultureName)
    {
      _strings.ChangeLanguage(cultureName);
      RegionSettings settings = ServiceScope.Get<ISettingsManager>().Load<RegionSettings>();
      settings.Culture = cultureName;
      ServiceScope.Get<ISettingsManager>().Save(settings);

      //send language change event
      LanguageChange(this);
    }

    public string ToString(string section, string name, object[] parameters)
    {
      return _strings.ToString(section, name, parameters);
    }

    public string ToString(string section, string name)
    {
      return _strings.ToString(section, name);
    }

    public string ToString(StringId id)
    {
      return _strings.ToString(id.Section, id.Name);
    }

    public bool IsLocaleSupported(string cultureName)
    {
      return _strings.IsLocaleSupported(cultureName);
    }

    public ICollection<CultureInfo> AvailableLanguages
    {
      get { return _strings.AvailableLanguages; }
    }

    public CultureInfo GuessBestLanguage()
    {
      return _strings.GetBestLanguage();
    }

    public void AddDirectory(string stringsDirectory)
    {
      _strings.AddDirectory(stringsDirectory);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when the plugin manager notifies the system about its events.
    /// Adds Plugin language resource folders to the directory list when all plugins are initialized.
    /// </summary>
    /// <param name="message">Message containing the notification data.</param>
    private void OnPluginManagerMessageReceived(QueueMessage message)
    {
      if (((PluginManagerMessaging.NotificationType) message.MessageData[PluginManagerMessaging.Notification]) ==
          PluginManagerMessaging.NotificationType.PluginsInitialized)
        InitializeLanguageResources();
    }

    #endregion
  }
}
