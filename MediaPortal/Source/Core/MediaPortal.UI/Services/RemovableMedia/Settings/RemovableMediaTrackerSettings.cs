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

namespace MediaPortal.UI.Services.RemovableMedia.Settings
{
  public class RemovableMediaTrackerSettings
  {
    #region Consts

    public const bool DEFAULT_TRACK_REMOVABLE_MEDIA = true;

    #endregion

    #region Protected fields

    protected bool _trackRemovableMedia = DEFAULT_TRACK_REMOVABLE_MEDIA;

    #endregion

    /// <summary>
    /// Configures if the removable media manager should track removable media in general.
    /// </summary>
    [Setting(SettingScope.User, DEFAULT_TRACK_REMOVABLE_MEDIA)]
    public bool TrackRemovableMedia
    {
      get { return _trackRemovableMedia; }
      set { _trackRemovableMedia = value; }
    }
  }
}