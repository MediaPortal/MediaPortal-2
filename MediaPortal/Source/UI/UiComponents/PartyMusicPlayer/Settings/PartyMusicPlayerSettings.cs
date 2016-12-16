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
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UiComponents.PartyMusicPlayer.Settings
{
  public class PartyMusicPlayerSettings
  {
    #region Defaults

    public const string DEFAULT_ESCAPE_PASSWORD = "exit";

    #endregion

    #region Protected fields

    protected bool _useEscapePassword = true;
    protected string _escapePassword = DEFAULT_ESCAPE_PASSWORD;
    protected bool _disableScreenSaver = true;
    protected PlayMode _playMode = PlayMode.Continuous;
    protected RepeatMode _repeatMode = RepeatMode.All;
    protected Guid _playlistId = Guid.Empty;
    protected string _playlistName = null;

    #endregion

    [Setting(SettingScope.User, true)]
    public bool UseEscapePassword
    {
      get { return _useEscapePassword; }
      set { _useEscapePassword = value; }
    }

    [Setting(SettingScope.User, DEFAULT_ESCAPE_PASSWORD)]
    public string EscapePassword
    {
      get { return _escapePassword; }
      set { _escapePassword = value; }
    }

    [Setting(SettingScope.User, true)]
    public bool DisableScreenSaver
    {
      get { return _disableScreenSaver; }
      set { _disableScreenSaver = value; }
    }

    [Setting(SettingScope.User, PlayMode.Continuous)]
    public PlayMode PlayMode
    {
      get { return _playMode; }
      set { _playMode = value; }
    }

    [Setting(SettingScope.User, RepeatMode.All)]
    public RepeatMode RepeatMode
    {
      get { return _repeatMode; }
      set { _repeatMode = value; }
    }

    [Setting(SettingScope.User, null)]
    public string PlaylistIdStr
    {
      get { return _playlistId.ToString(); }
      set { _playlistId = string.IsNullOrEmpty(value) ? Guid.Empty : new Guid(value); }
    }

    public Guid PlaylistId
    {
      get { return _playlistId; }
      set { _playlistId = value; }
    }

    [Setting(SettingScope.User, null)]
    public string PlaylistName
    {
      get { return _playlistName; }
      set { _playlistName = value; }
    }
  }
}