#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using MediaPortal.Core.Settings;

namespace MediaPortal.Configuration.Settings
{

  /// <summary>
  /// Class to save some sample settings, used for debugging.
  /// </summary>
  public class SampleSettings
  {

    #region Variables

    private string _entry;
    private string[] _multiEntry;
    private int[] _multiSelection;
    private string _path;
    private int[] _preferenceList;

    #endregion

    #region Properties

    [Setting(SettingScope.Global, "")]
    public string Entry
    {
      get { return _entry; }
      set { _entry = value; }
    }

    [Setting(SettingScope.Global, new string[] { })]
    public string[] MultiEntry
    {
      get { return _multiEntry; }
      set { _multiEntry = value; }
    }

    [Setting(SettingScope.Global, new int[] { })]
    public int[] MultiSelection
    {
      get { return _multiSelection; }
      set { _multiSelection = value; }
    }

    [Setting(SettingScope.Global, "")]
    public string Path
    {
      get { return _path; }
      set { _path = value; }
    }

    [Setting(SettingScope.Global, new int[] { })] // irl: set a default order here
    public int[] PreferenceList
    {
      get { return _preferenceList; }
      set { _preferenceList = value; }
    }

    #endregion

  }

}
