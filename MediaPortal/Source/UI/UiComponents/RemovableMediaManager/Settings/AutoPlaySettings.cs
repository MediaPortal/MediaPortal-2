#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using MediaPortal.Core.Settings;

namespace MediaPortal.UiComponents.AutoPlay.Settings
{
  public enum AutoPlayType
  {
    NoAutoPlay,
    Ask,
    Auto
  }

  public class AutoPlaySettings
  {
    #region Protected fields

    protected AutoPlayType _autoPlayDVD;
    protected AutoPlayType _autoPlayCD;

    #endregion

    [Setting(SettingScope.User, AutoPlayType.Ask)]
    public AutoPlayType AutoPlayDVD
    {
      get { return _autoPlayDVD; }
      set { _autoPlayDVD = value; }
    }

    [Setting(SettingScope.User, AutoPlayType.Ask)]
    public AutoPlayType AutoPlayCD
    {
      get { return _autoPlayCD; }
      set { _autoPlayCD = value; }
    }
  }
}