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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.UiComponents.PartyMusicPlayer.General
{
  public class Consts
  {
    #region Workflow states

    public const string STR_WF_STATE_ID_PARTY_MUSIC_PLAYER = "6D81BA15-25FA-4D15-9780-6C84CC4551A6";
    public static readonly Guid WF_STATE_ID_PARTY_MUSIC_PLAYER = new Guid(STR_WF_STATE_ID_PARTY_MUSIC_PLAYER);

    public const string STR_WF_STATE_ID_PARTY_MUSIC_CONFIG = "53278FDC-C3B1-4AE5-B767-9C6864D0E84C";
    public static readonly Guid WF_STATE_ID_PARTY_MUSIC_CONFIG = new Guid(STR_WF_STATE_ID_PARTY_MUSIC_CONFIG);

    #endregion

    #region Screens

    public const string DIALOG_QUERY_ESCAPE_PASSWORD = "DialogQueryEscapePassword";
    public const string DIALOG_CHOOSE_PLAYLIST = "DialogChoosePlaylist";

    #endregion

    #region Localization strings

    public const string RES_WRONG_ESCAPE_PASSWORD_DIALOG_HEADER = "[PartyMusicPlayer.WrongEscapePasswordDialogHeader]";
    public const string RES_WRONG_ESCAPE_PASSWORD_DIALOG_TEXT = "[PartyMusicPlayer.WrongEscapePasswordDialogText]";

    public const string RES_SERVER_NOT_CONNECTED_DIALOG_HEADER = "[PartyMusicPlayer.ServerNotConnectedDialogHeader]";
    public const string RES_SERVER_NOT_CONNECTED_DIALOG_TEXT = "[PartyMusicPlayer.ServerNotConnectedDialogText]";

    public const string RES_PLAYER_CONTEXT_NAME = "[PartyMusicPlayer.PlayerContextName]";

    #endregion

    public const string KEY_NAME = "Name";
    public const string KEY_PLAYLIST_ID = "PlaylistId";

    public const string STR_MODULE_ID_PARTY_MUSIC_PLAYER = "DDDA96AB-CAAF-482D-A6FA-9934E3F39B1E";
    public static readonly Guid MODULE_ID_PARTY_MUSIC_PLAYER = new Guid(STR_MODULE_ID_PARTY_MUSIC_PLAYER);

    public static readonly Guid[] NECESSARY_AUDIO_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          AudioAspect.ASPECT_ID,
      };

    public static readonly Guid[] EMPTY_GUID_ENUMERATION = new Guid[] {};
  }
}