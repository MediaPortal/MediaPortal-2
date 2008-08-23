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

namespace MediaPortal.Core.Settings
{
  public interface ISettingsManager
  {
    /// <summary>
    /// Retrieves an object's public properties from a given Xml file 
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <param name="settingsObject">Object's instance</param>
    /// <param name="filename">Xml file wich contains stored datas</param>
    void Load(object settingsObject);

    /// <summary>
    /// Stores an object's public properties to a given Xml file 
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <param name="settingsObject">Object's instance</param>
    /// <param name="filename">Xml file where we wanna store datas</param>
    void Save(object settingsObject);
  }
}
