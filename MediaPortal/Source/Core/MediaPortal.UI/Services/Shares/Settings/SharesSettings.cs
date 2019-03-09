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

using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Settings;

namespace MediaPortal.UI.Services.Shares.Settings
{
  /// <summary>
  /// Settings data for the local shares management of a MediaPortal client's MediaManager.
  /// </summary>
  public class SharesSettings
  {
    #region Protected fields

    protected HashSet<Share> _shares = new HashSet<Share>();

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of local shares. Change the returned collection to modify the setting.
    /// </summary>
    [Setting(SettingScope.Global)]
    public HashSet<Share> LocalShares
    {
      get { return _shares; }
      internal set { _shares = value; }
    }

    #endregion
  }
}