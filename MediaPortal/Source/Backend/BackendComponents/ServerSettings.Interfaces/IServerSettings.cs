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

namespace MediaPortal.Plugins.ServerSettings
{
  public interface IServerSettings
  {
    /// <summary>
    /// Retrieves an object's public properties from the application's settings store.
    /// </summary>
    /// <param name="settingsTypeName">Assembly qualified name of Type of settings to load.</param>
    /// <returns>Loaded Application settings of the specified <paramref name="settingsTypeName"/>, if
    /// present. Else, returns an empty instance of that type.</returns>
    object Load(string settingsTypeName);

    /// <summary>
    /// Stores an object's public properties in the application's settings store.
    /// </summary>
    /// <param name="settingsTypeName">Assembly qualified name of Type of settings to save.</param>
    /// <exception cref="ArgumentNullException">If the specified <paramref name="settings"/>
    /// is null.</exception>
    /// <param name="settings">Settings object's instance to be saved.</param>
    void Save(string settingsTypeName, string settings);
  }

  public interface IServerSettingsClient : IServerSettings
  {
    /// <summary>
    /// Retrieves an object's public properties from the application's settings store.
    /// This is a convenience method for <see cref="Load(Type)"/>.
    /// </summary>
    /// <typeparam name="SettingsType">Type of the settings object to load.</typeparam>
    /// <returns>Loaded Application settings of the specified <typeparamref name="SettingsType"/>, if
    /// present. Else, returns an empty instance of that type.</returns>
    SettingsType Load<SettingsType>() where SettingsType : class;

    /// <summary>
    /// Stores an object's public properties in the application's settings store.
    /// </summary>
    /// <exception cref="ArgumentNullException">If the specified <paramref name="settingsObject"/>
    /// is null.</exception>
    /// <param name="settingsObject">Settings object's instance to be saved.</param>
    void Save(object settingsObject);
  }
}
