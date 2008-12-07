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
using MediaPortal.Core.PathManager;
using MediaPortal.Core.Services.PluginManager;
using MediaPortal.Presentation.Localization;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Utilities;

namespace MediaPortal.Services.Localization
{
  /// <summary>
  /// This class manages localization strings.
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

      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(PluginManagerMessaging.Queue);
      queue.OnMessageReceive += OnPluginManagerMessageReceived;
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
        IPathManager pathManager = ServiceScope.Get<IPathManager>();
        if (pathManager.Exists("<LANGUAGE>"))
        {
          string systemLanguageDirectory = pathManager.GetPath("<LANGUAGE>");
          languageDirectories.Add(systemLanguageDirectory);
        }

        ICollection<PluginResource> languageResources = ServiceScope.Get<IPluginManager>().RequestAllPluginItems<PluginResource>(
            "/Resources/Language", new FixedItemStateTracker());

        ILogger logger = ServiceScope.Get<ILogger>();
        foreach (PluginResource resource in languageResources)
        {
          logger.Debug("StringManager: Adding language directory '{0}'", resource.Path);
          if (Directory.Exists(resource.Path))
            languageDirectories.Add(resource.Path);
          else
            logger.Error("StringManager: Language directory doesn't exist: {0}", resource.Path);
        }

        if (string.IsNullOrEmpty(settings.Culture))
        {
          ICollection<CultureInfo> availableLanguages = new List<CultureInfo>();
          foreach (string directory in languageDirectories)
            CollectionUtils.AddAll(availableLanguages, LocalizationStrings.FindAvailableLanguages(directory));
          CultureInfo bestCulture = GetBestLanguage(availableLanguages);
          settings.Culture = bestCulture.Name;
          ServiceScope.Get<ILogger>().Info("StringManager: Culture set to '{0}'", bestCulture);
          ServiceScope.Get<ISettingsManager>().Save(settings);
        }
        else
          ServiceScope.Get<ILogger>().Debug("StringManager: Using culture: " + settings.Culture);

        _strings = new LocalizationStrings(settings.Culture);
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
      if (availableLanguages.Contains(CultureInfo.CurrentCulture))
        return CultureInfo.CurrentCulture;

      // Try Language Parent if it has one
      if (!CultureInfo.CurrentCulture.IsNeutralCulture &&
        availableLanguages.Contains(CultureInfo.CurrentCulture.Parent))
        return CultureInfo.CurrentCulture.Parent;

      // Default to English
      CultureInfo englishCulture = new CultureInfo("en");
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
      LanguageChange(this);
    }

    public string ToString(string section, string name, object[] parameters)
    {
      string translation = _strings.ToString(section, name);
      if ((translation == null) || (parameters == null))
        return translation;

      try
      {
        return string.Format(translation, parameters);
      }
      catch (FormatException e)
      {
        ServiceScope.Get<ILogger>().Warn("LocalizationStrings: Error formatting localation '{0}' (Section={1}, Name={2})", e, translation, section, name);
        return translation;
      }
    }

    public string ToString(string section, string name)
    {
      return _strings.ToString(section, name);
    }

    public string ToString(StringId id)
    {
      return _strings.ToString(id.Section, id.Name);
    }

    public ICollection<CultureInfo> AvailableLanguages
    {
      get { return _strings.AvailableLanguages; }
    }

    public CultureInfo GuessBestLanguage()
    {
      return GetBestLanguage(_strings.AvailableLanguages);
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
