#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using MediaPortal.Common.Settings;

namespace MediaPortal.Plugins.MP2Web
{
  public class MP2WebSettings
  {
    #region Consts

    internal const int DEFAULT_PORT = 80;

    #endregion

    #region Public properties

    /// <summary>
    /// Tcp-Port on which MP2WebService listens
    /// </summary>
    [Setting(SettingScope.Global, DEFAULT_PORT)]
    public int TcpPort { get; set; }

    /// <summary>
    /// How many Media items are shown in one row
    /// </summary>
    [Setting(SettingScope.Global, 5)]
    public int MediaItemsPerRow { get; set; }

    /// <summary>
    /// How many Media items are loaded on each request
    /// </summary>
    [Setting(SettingScope.Global, 6)]
    public int MediaItemsPerQuery { get; set; }

    /// <summary>
    /// ID of the group which is shown when intering the EPG
    /// </summary>
    [Setting(SettingScope.Global, 1)]
    public int DefaultEpgGroupId { get; set; }

    #endregion
  }
}
