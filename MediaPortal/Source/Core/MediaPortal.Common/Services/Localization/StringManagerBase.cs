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

using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.Services.PluginManager;

namespace MediaPortal.Common.Services.Localization
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

    #region Protected fields

    protected IPluginItemStateTracker _languagePluginStateTracker;
    protected IItemRegistrationChangeListener _languageResourcesRegistrationChangeListener;
    protected ICollection<CultureInfo> _availableLanguages = null;
    protected ICollection<string> _languageDirectories = null;
    protected object _syncObj = new object();

    #endregion

    #region Ctor/dtor

    public StringManagerBase()
    {
      _languagePluginStateTracker = new DefaultItemStateTracker("Localization system: Language resources")
        {
            // We don't care about strings which are currently in use; we don't have an overview which strings are still needed. So we don't
            // provide an implementation of RequestEnd.

            Stopped = RemoveLanguageResource
        };
      _languageResourcesRegistrationChangeListener = new DefaultItemRegistrationChangeListener("Localization system: Language resources")
        {
            ItemsWereAdded = (location, items) => AddLanguageResources(items)
            // Item removals are handled by the SkinResourcesPluginItemStateTracker
        };
    }

    public virtual void Dispose()
    {
      ServiceRegistration.Get<IPluginManager>().RevokeAllPluginItems(LANGUAGE_RESOURCES_REGISTRATION_PATH,
          _languagePluginStateTracker);
    }

    #endregion

    #region Protected methods

    protected void InitializeLanguageResources()
    {
      try
      {
        // Add language directories
        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        ICollection<PluginResource> languageResources = pluginManager.RequestAllPluginItems<PluginResource>(
            LANGUAGE_RESOURCES_REGISTRATION_PATH, _languagePluginStateTracker);
        pluginManager.AddItemRegistrationChangeListener(
            LANGUAGE_RESOURCES_REGISTRATION_PATH, _languageResourcesRegistrationChangeListener);

        lock (_syncObj)
        {
          _languageDirectories = new List<string>();

          ILogger logger = ServiceRegistration.Get<ILogger>();
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
        ServiceRegistration.Get<ILogger>().Error("{0}: Error initializing language resources", e, GetType().Name);
      }
    }

    protected void AddLanguageResources(ICollection<PluginItemMetadata> items)
    {
      lock (_syncObj)
        foreach (PluginItemMetadata itemMetadata in items)
        {
          try
          {
            PluginResource resource = ServiceRegistration.Get<IPluginManager>().RequestPluginItem<PluginResource>(
                  itemMetadata.RegistrationLocation, itemMetadata.Id, _languagePluginStateTracker);
            if (resource != null && Directory.Exists(resource.Path))
              _languageDirectories.Add(resource.Path);
          }
          catch (PluginInvalidStateException e)
          {
            ServiceRegistration.Get<ILogger>().Warn("Cannot add language resource for {0}", e, itemMetadata);
          }
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

    #region Public properties

    public ICollection<CultureInfo> AvailableLanguages
    {
      get { return _availableLanguages; }
    }

    #endregion

    #region Public methods

    public void AddLanguageDirectory(string directory)
    {
      lock (_syncObj)
        _languageDirectories.Add(directory);
      ReLoad();
    }

    #endregion
  }
}
