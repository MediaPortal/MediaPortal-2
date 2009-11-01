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
using System.IO;
using System.Reflection;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.Settings;


namespace MediaPortal.Core.Services.Settings
{
  /// <summary>
  /// Settings service.
  /// </summary>
  public class SettingsManager : ISettingsManager, IDisposable
  {
    #region Protected fields

    protected bool _batchUpdate = false;
    protected SettingsCache _cache;

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
        return (SettingAttribute) attributes[0];
      return null;
    }

    /// <summary>
    /// Returns the full file path for a user setting object of the specified
    /// <paramref name="settingsType"/>.
    /// </summary>
    /// <param name="settingsType">Type of the settings class to map to a filename.</param>
    /// <returns>File name without path of a file which will store the setting instance of the
    /// specified <paramref name="settingsType"/>.</returns>
    protected static string GetUserFilePath(Type settingsType)
    {
      string fullUserFileName = String.Format(@"<CONFIG>\{0}\{1}", Environment.UserName, settingsType.FullName + ".xml");
      return ServiceScope.Get<IPathManager>().GetPath(fullUserFileName);
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
      return ServiceScope.Get<IPathManager>().GetPath(fullFileName);
    }

    #endregion

    protected object LoadSettingsObject(Type settingsType)
    {
      SettingsFileHandler globalHandler = new SettingsFileHandler(GetGlobalFilePath(settingsType));
      SettingsFileHandler userHandler = new SettingsFileHandler(GetUserFilePath(settingsType));
      try
      {
        globalHandler.Load();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("SettingsManager: Error loading global settings file for setting type '{0}'... Will clear this settings file.", e, settingsType.Name);
        globalHandler.Clear();
        RemoveSettingsData(settingsType, false, true);
      }
      try
      {
        userHandler.Load();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("SettingsManager: Error loading user settings file for setting type '{0}'... Will clear this settings file.", e, settingsType.Name);
        userHandler.Clear();
        RemoveSettingsData(settingsType, true, false);
      }
      try
      {
        object result = Activator.CreateInstance(settingsType);
        foreach (PropertyInfo property in result.GetType().GetProperties())
        {
          SettingAttribute att = GetSettingAttribute(property);
          if (att == null) continue;
          SettingsFileHandler s = (att.SettingScope == SettingScope.Global ? globalHandler : userHandler);
          object value = s.GetValue(property.Name, property.PropertyType);
          if (value == null)
            if (att.HasDefault)
              value = att.DefaultValue;
            else
              continue;
          property.SetValue(result, value, null);
        }
        return result;
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("SettingsManager: Error loading settings of type '{0}'", e, settingsType.Name);
        return null;
      }
    }

    protected void SaveSettingsObject(object settingsObject)
    {
      try
      {
        Type t = settingsObject.GetType();
        SettingsFileHandler globalSerializer = new SettingsFileHandler(GetGlobalFilePath(t));
        SettingsFileHandler userSerializer = new SettingsFileHandler(GetUserFilePath(t));
        foreach (PropertyInfo property in t.GetProperties())
        {
          SettingAttribute att = GetSettingAttribute(property);
          if (att == null) continue;
          SettingsFileHandler s = (att.SettingScope == SettingScope.Global ? globalSerializer : userSerializer);
          object value = property.GetValue(settingsObject, null);
          s.SetValue(property.Name, value);
        }
        globalSerializer.Close();
        userSerializer.Close();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("SettingsManager: Error writing settings of type '{0}'... Will clear settings files for this setting", e, settingsObject.GetType().Name);
        RemoveSettingsData(settingsObject.GetType(), true, true);
      }
    }

    #region ISettingsManager implementation

    public SettingsType Load<SettingsType>() where SettingsType : class
    {
      return (SettingsType) Load(typeof(SettingsType));
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
          string userPath = ServiceScope.Get<IPathManager>().GetPath(string.Format(@"<CONFIG>\{0}", Environment.UserName));
          DirectoryInfo userConfigDirectory = new DirectoryInfo(userPath);
          if (userConfigDirectory.Exists)
            userConfigDirectory.Delete(true);
        }
        if (global)
        {
          string globalPath = ServiceScope.Get<IPathManager>().GetPath("<CONFIG>");
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
