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

namespace MediaPortal.Core.Settings
{
  /// <summary>
  /// Default <see cref="ISettingsManager"/> implementation that does absolutely nothing
  /// </summary>
  /// <remarks>
  /// </remarks>
  internal class NoSettingsManager : ISettingsManager
  {
    #region ISettingsManager Members

    public SettingsType Load<SettingsType>() where SettingsType : class
    {
      return (SettingsType) Load(typeof(SettingsType));
    }

    public object Load(Type settingsType)
    {
      return Activator.CreateInstance(settingsType);
    }

    public void Save(object settingsObject) { }

    public void StartBatchUpdate() { }

    public void EndBatchUpdate() { }

    public void CancelBatchUpdate() { }

    public void RemoveSettingsData(Type settingsType, bool user, bool global) { }

    public void RemoveAllSettingsData(bool user, bool global) { }

    #endregion
  }
}
