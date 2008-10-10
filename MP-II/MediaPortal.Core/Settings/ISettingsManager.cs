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

namespace MediaPortal.Core.Settings
{
  /// <summary>
  /// Global service interface for loading and saving settings for application modules.
  /// The settings manager provides methods to load and save module specific settings objects,
  /// which should have a special structure.
  /// The implementation of this interface defines where the setting object will be stored and
  /// how it will be serialized/deserialized.
  /// </summary>
  /// <remarks>
  /// Every application part, which needs a setting, will define its own settings class to
  /// contain the settings values. Those classes will use the <see cref="SettingScope"/> meta
  /// attribute to define if a settings entry will be stored as global or user setting.
  /// There must be at most one instance of every settings class for holding application settings;
  /// a settings class must not be reused for different application settings of a similar
  /// type. The settings system will use the class name to find a settings object in the settings
  /// store.
  /// TODO: Document settings structure: [SettingScope] meta attribute, supported types, etc.
  /// </remarks>
  public interface ISettingsManager
  {
    /// <summary>
    /// Retrieves an object's public properties from the application's settings store.
    /// This is a convenience method for <see cref="Load(Type)"/>.
    /// </summary>
    /// <typeparam name="SettingsType">Type of the settings object to load.</typeparam>
    /// <returns>Application settings of the specified <typeparamref name="SettingsType"/>, if
    /// present. Else, returns an empty instance of that type.</returns>
    SettingsType Load<SettingsType>() where SettingsType: class;

    /// <summary>
    /// Retrieves an object's public properties from the application's settings store.
    /// </summary>
    /// <param name="settingsType">Type of settings to load.</param>
    /// <returns>Application settings of the specified <paramref name="settingsType"/>, if
    /// present. Else, returns an empty instance of that type.</returns>
    object Load(Type settingsType);

    /// <summary>
    /// Stores an object's public properties in the application's settings store.
    /// </summary>
    /// <exception cref="ArgumentNullException">If the specified <paramref name="settingsObject"/>
    /// is null.</exception>
    /// <param name="settingsObject">Settings object's instance to be saved.</param>
    void Save(object settingsObject);

    /// <summary>
    /// Switches on the batch update mode. In batch update mode, the settings manager neither won't dispose
    /// loaded settings objects in its cache, nor will it write settings objects to disk.
    /// To stop the batch update mode, call <see cref="EndBatchUpdate"/>.
    /// </summary>
    void StartBatchUpdate();

    /// <summary>
    /// Leaves the batch update mode. Any settings for which method <see cref="Save"/> was called will be
    /// saved on disk.
    /// </summary>
    void EndBatchUpdate();

    /// <summary>
    /// Removes all application configuration data from the system.
    /// </summary>
    /// <param name="user">If set to <c>true</c>, all user data will be removed.</param>
    /// <param name="global">If set to <c>true</c>, all global data will be removed.</param>
    void RemoveAllSettingsData(bool user, bool global);
  }
}
