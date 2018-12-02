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
using System.IO;
using System.Reflection;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.SystemResolver.Settings;
using MediaPortal.Common.Settings;


namespace MediaPortal.Common.Services.Settings
{
  /// <summary>
  /// Settings service.
  /// </summary>
  public class SettingsManager : ISettingsManager, IDisposable
  {
    #region Protected fields

    protected string _currentUserName = null;
    protected bool _batchUpdate = false;
    protected SettingsCache _cache;
    protected readonly object _syncObj = new object();

    #endregion

    #region Ctor

    public SettingsManager()
    {
      _cache = new SettingsCache();
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Gets the SettingAttribute of a property.
    /// </summary>
    /// <param name="property">Property to assess.</param>
    /// <returns>Propertie's meta attribute or <c>null</c>, if no <see cref="SettingAttribute"/> was
    /// attached to the specified <paramref name="property"/>.</returns>
    protected static SettingAttribute GetSettingAttribute(ICustomAttributeProvider property)
    {
      // Get the info stored in the SettingAttribute, if available
      object[] attributes = property.GetCustomAttributes(typeof(SettingAttribute), false);
      if (attributes.Length != 0)
        return (SettingAttribute)attributes[0];
      return null;
    }

    /// <summary>
    /// Returns the full file path for a user setting object of the specified
    /// <paramref name="settingsType"/>.
    /// </summary>
    /// <param name="settingsType">Type of the settings class to map to a filename.</param>
    /// <returns>File name without path of a file which will store the setting instance of the
    /// specified <paramref name="settingsType"/>.</returns>
    protected string GetUserFilePath(Type settingsType)
    {
      var userName = _currentUserName ?? (_currentUserName = GetDefaultUserName());
      string fullUserFileName = String.Format(@"<CONFIG>\{0}\{1}", userName, settingsType.FullName + ".xml");
      return ServiceRegistration.Get<IPathManager>().GetPath(fullUserFileName);
    }

    /// <summary>
    /// Returns the default user name which will be used if no other user is logged in. By default this will be the SystemID.
    /// </summary>
    /// <returns></returns>
    private string GetDefaultUserName()
    {
      lock (_syncObj)
      {
        // Load only system scope settings, as we need this information already to reach user scope
        SystemResolverSettings settings = (SystemResolverSettings)LoadSettingsObject(typeof(SystemResolverSettings), true, false);
        if (string.IsNullOrEmpty(settings.SystemId))
        {
          // Create a new id for our local system
          settings.SystemId = Guid.NewGuid().ToString("D");
          SaveSettingsObject(settings, true, false);
        }
        return settings.SystemId;
      }
    }

    /// <summary>
    /// Returns the full file path for a global setting object of the specified
    /// <paramref name="settingsType"/>.
    /// </summary>
    /// <param name="settingsType">Type of the settings class to map to a filename.</param>
    /// <returns>File name without path of a file which will store the setting instance of the
    /// specified <paramref name="settingsType"/>.</returns>
    protected static string GetGlobalFilePath(Type settingsType)
    {
      string fullFileName = String.Format(@"<CONFIG>\{0}", settingsType.FullName + ".xml");
      return ServiceRegistration.Get<IPathManager>().GetPath(fullFileName);
    }

    #endregion

    protected object LoadSettingsObject(Type settingsType)
    {
      return LoadSettingsObject(settingsType, true, true);
    }

    protected object LoadSettingsObject(Type settingsType, bool useGlobaleScope, bool useUserScope)
    {
      SettingsFileHandler globalHandler = null;
      SettingsFileHandler userHandler = null;
      if (useGlobaleScope)
      {
        globalHandler = new SettingsFileHandler(GetGlobalFilePath(settingsType));
        try
        {
          globalHandler.Load();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("SettingsManager: Error loading global settings file for setting type '{0}'... Will clear this settings file.", e, settingsType.Name);
          globalHandler.Clear();
          RemoveSettingsData(settingsType, false, true);
        }
      }
      if (useUserScope)
      {
        userHandler = new SettingsFileHandler(GetUserFilePath(settingsType));
        try
        {
          userHandler.Load();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("SettingsManager: Error loading user settings file for setting type '{0}'... Will clear this settings file.", e, settingsType.Name);
          userHandler.Clear();
          RemoveSettingsData(settingsType, true, false);
        }
      }
      try
      {
        object result = Activator.CreateInstance(settingsType);
        foreach (PropertyInfo property in result.GetType().GetProperties())
        {
          SettingAttribute att = GetSettingAttribute(property);
          if (att == null) continue;
          if (att.SettingScope == SettingScope.Global && !useGlobaleScope) continue;
          if (att.SettingScope == SettingScope.User && !useUserScope) continue;

          SettingsFileHandler s = (att.SettingScope == SettingScope.Global ? globalHandler : userHandler);
          try
          {
            object value = s.GetValue(property.Name, property.PropertyType);
            if (value == null)
              if (att.HasDefault)
                value = att.DefaultValue;
              else
                continue;
            property.SetValue(result, value, null);
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Error("SettingsManager: Error setting property '{0}' in settings of type '{1}'" +
                (att.HasDefault ? ", using default value" : string.Empty), e, property.Name, settingsType.Name);
            if (att.HasDefault)
              property.SetValue(result, att.DefaultValue, null);
          }
        }
        if (
          useGlobaleScope && !globalHandler.SettingsFileExists &&
          useUserScope && !userHandler.SettingsFileExists
          )
          SaveSettingsObject(result, useGlobaleScope, useUserScope);
        return result;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("SettingsManager: Error loading settings of type '{0}'", e, settingsType.Name);
        return null;
      }
    }

    protected void SaveSettingsObject(object settingsObject)
    {
      SaveSettingsObject(settingsObject, true, true);
    }
    protected void SaveSettingsObject(object settingsObject, bool useGlobaleScope, bool useUserScope)
    {
      try
      {
        Type t = settingsObject.GetType();
        SettingsFileHandler globalSerializer = null;
        SettingsFileHandler userSerializer = null;

        if (useGlobaleScope)
          globalSerializer = new SettingsFileHandler(GetGlobalFilePath(t));
        if (useUserScope)
          userSerializer = new SettingsFileHandler(GetUserFilePath(t));
        foreach (PropertyInfo property in t.GetProperties())
        {
          SettingAttribute att = GetSettingAttribute(property);
          if (att == null) continue;
          if (att.SettingScope == SettingScope.Global && !useGlobaleScope) continue;
          if (att.SettingScope == SettingScope.User && !useUserScope) continue;
          SettingsFileHandler s = (att.SettingScope == SettingScope.Global ? globalSerializer : userSerializer);
          object value = property.GetValue(settingsObject, null);
          s.SetValue(property.Name, value);
        }
        globalSerializer?.Close();
        userSerializer?.Close();

        // Send a message that the setting has been changed.
        SettingsManagerMessaging.SendSettingsChangeMessage(t);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("SettingsManager: Error writing settings of type '{0}'... Will clear settings files for this setting", e, settingsObject.GetType().Name);
        RemoveSettingsData(settingsObject.GetType(), true, true);
      }
    }

    #region ISettingsManager implementation

    public SettingsType Load<SettingsType>() where SettingsType : class
    {
      return (SettingsType)Load(typeof(SettingsType));
    }

    public object Load(Type settingsType)
    {
      object result;
      lock (_cache.SyncObj)
      {
        if ((result = _cache.Get(settingsType)) != null)
          return result;
        result = LoadSettingsObject(settingsType);
        _cache.Set(result);
      }
      return result;
    }

    public void Save(object settingsObject)
    {
      if (settingsObject == null)
        throw new ArgumentNullException("settingsObject");
      lock (_cache.SyncObj)
      {
        _cache.Set(settingsObject);
        if (_batchUpdate)
          return;
        SaveSettingsObject(settingsObject);
      }
    }

    public void StartBatchUpdate()
    {
      lock (_cache.SyncObj)
      {
        _cache.KeepAll();
        _batchUpdate = true;
      }
    }

    public void CancelBatchUpdate()
    {
      lock (_cache.SyncObj)
      {
        if (!_batchUpdate)
          return;
        _cache.StopKeep();
        _cache.Clear();
      }
    }

    public void EndBatchUpdate()
    {
      lock (_cache.SyncObj)
      {
        if (!_batchUpdate)
          return;
        foreach (SettingsObjectWrapper objectWrapper in _cache)
          Save(objectWrapper.SettingsObject);
        _cache.StopKeep();
      }
    }

    public void ClearCache()
    {
      _cache.Clear();
    }

    public void ChangeUserContext(string userName)
    {
      _currentUserName = userName;
      ClearCache();
    }

    public void RemoveSettingsData(Type settingsType, bool user, bool global)
    {
      lock (_cache.SyncObj)
      {
        if (user)
        {
          string userPath = GetUserFilePath(settingsType);
          FileInfo userConfigFile = new FileInfo(userPath);
          if (userConfigFile.Exists)
            userConfigFile.Delete();
        }
        if (global)
        {
          string globalPath = GetGlobalFilePath(settingsType);
          FileInfo globalConfigFile = new FileInfo(globalPath);
          if (globalConfigFile.Exists)
            globalConfigFile.Delete();
        }
      }
    }

    public void RemoveAllSettingsData(bool user, bool global)
    {
      lock (_cache.SyncObj)
      {
        if (user)
        {
          string userPath = ServiceRegistration.Get<IPathManager>().GetPath(string.Format(@"<CONFIG>\{0}", Environment.UserName));
          DirectoryInfo userConfigDirectory = new DirectoryInfo(userPath);
          if (userConfigDirectory.Exists)
            userConfigDirectory.Delete(true);
        }
        if (global)
        {
          string globalPath = ServiceRegistration.Get<IPathManager>().GetPath("<CONFIG>");
          DirectoryInfo globalConfigDirectory = new DirectoryInfo(globalPath);
          if (globalConfigDirectory.Exists)
            globalConfigDirectory.Delete(true);
        }
      }
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      _cache.Dispose();
      _cache = null;
    }

    #endregion
  }
}
