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

using MediaPortal.Common.Settings;

namespace MediaPortal.UiComponents.RemovableMediaManager.Settings
{
  public enum AutoPlayType
  {
    /// <summary>
    /// If a media volume is inserted, the autoplay function will ignore that event.
    /// </summary>
    NoAutoPlay,

    /// <summary>
    /// If a media volume is inserted, the autoplay function will automatically try to play the media.
    /// </summary>
    AutoPlay
  }

  public class RemovableMediaManagerSettings
  {
    #region Protected fields

    protected AutoPlayType _autoPlay;

    #endregion

    [Setting(SettingScope.User, AutoPlayType.AutoPlay)]
    public AutoPlayType AutoPlay
    {
      get { return _autoPlay; }
      set { _autoPlay = value; }
    }
  }
}