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
  /// This class manages localization strings.
  /// </summary>
  /// TODO: Make this class multithreading safe
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

      public string UsageDescription
      {
        get { return "StringManager: Language plugins"; }
      }

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

      ServiceScope.Get<IMessageBroker>().Register_Async(PluginManagerMessaging.QUEUE, OnPluginManagerMessageReceived);
    }

    #endregion

    #region Protected methods

    protected void InitializeLanguageResources()
    {
      try
      {
        ServiceScope.Get<ILogger>().Debug("StringManager: Loading settings");
        RegionSettings settings = ServiceScope.Get<ISettingsManager>().Load<RegionSettings>();

        ICollection<string> languageDirectories = new List<string>();
        // Add language directories
        ICollection<PluginResource> languageResources = ServiceScope.Get<IPluginManager>().RequestAllPluginItems<PluginResource>(
            "/Resources/Language", _languagePluginStateTracker);

        ILogger logger = ServiceScope.Get<ILogger>();
        foreach (PluginResource resource in languageResources)
        {
          logger.Debug("StringManager: Adding language directory '{0}'", resource.Path);
          if (Directory.Exists(resource.Path))
            languageDirectories.Add(resource.Path);
          else
            logger.Error("StringManager: Language directory doesn't exist: {0}", resource.Path);
        }

        string currentCulture = settings.Culture;

        if (string.IsNullOrEmpty(currentCulture))
        {
          currentCulture = CultureInfo.CurrentUICulture.Name;
          ServiceScope.Get<ILogger>().Info("StringManager: Culture not set. Using culture: '{0}'", currentCulture);
        }
        else
          ServiceScope.Get<ILogger>().Info("StringManager: Using culture: " + currentCulture);

        _strings = new LocalizationStrings(currentCulture);
        foreach (string languageDirectory in languageDirectories)
          _strings.AddDirectory(languageDirectory);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("StringManager: Error initializing language resources", e);
      }
    }

    protected static CultureInfo GetBestLanguage(ICollection<CultureInfo> availableLanguages)
    {
      // Try current local language
      if (availableLanguages.Contains(CultureInfo.CurrentUICulture))
        return CultureInfo.CurrentUICulture;

      // Try Language Parent if it has one
      if (CultureInfo.CurrentUICulture.Parent != CultureInfo.InvariantCulture &&
        availableLanguages.Contains(CultureInfo.CurrentUICulture.Parent))
        return CultureInfo.CurrentUICulture.Parent;

      // Default to English
      CultureInfo englishCulture = CultureInfo.GetCultureInfo("en");
      if (availableLanguages.Contains(englishCulture))
        return englishCulture;

      return null;
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

    public void ChangeLanguage(CultureInfo culture)
    {
      _strings.ChangeLanguage(culture);
      RegionSettings settings = ServiceScope.Get<ISettingsManager>().Load<RegionSettings>();
      settings.Culture = culture.Name;
      ServiceScope.Get<ISettingsManager>().Save(settings);

      //send language change event
      LanguageChange(this, culture);
    }

    public string ToString(string section, string name, params object[] parameters)
    {
      string translation = _strings.ToString(section, name);
      if (translation == null || parameters == null || parameters.Length == 0)
        return translation;
      try
      {
        return string.Format(translation, parameters);
      }
      catch (FormatException e)
      {
        ServiceScope.Get<ILogger>().Warn("LocalizationStrings: Error formatting localation '{0}' (Section='{1}', Name='{2}')", e, translation, section, name);
        return translation;
      }
    }

    public ICollection<CultureInfo> AvailableLanguages
    {
      get { return _strings.AvailableLanguages; }
    }

    public CultureInfo GetBestAvailableLanguage()
    {
      return GetBestLanguage(_strings.AvailableLanguages);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when the plugin manager notifies the system about its events.
    /// Adds plugin language resource folders to the directory list when all plugins are initialized.
    /// </summary>
    /// <param name="message">Message containing the notification data.</param>
    private void OnPluginManagerMessageReceived(QueueMessage message)
    {
      if (((PluginManagerMessaging.NotificationType) message.MessageData[PluginManagerMessaging.NOTIFICATION]) ==
          PluginManagerMessaging.NotificationType.PluginsInitialized)
      {
        InitializeLanguageResources();

        ServiceScope.Get<IMessageBroker>().Unregister_Async(PluginManagerMessaging.QUEUE, OnPluginManagerMessageReceived);
      }
    }

    #endregion
  }
}
