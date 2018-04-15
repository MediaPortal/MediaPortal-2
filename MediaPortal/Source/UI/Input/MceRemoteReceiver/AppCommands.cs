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

namespace MediaPortal.Plugins.MceRemoteReceiver
{
  internal enum AppCommands
  {
    None = 0,
    BrowserBackward = 1,
    BrowserForward = 2,
    BrowserRefresh = 3,
    BrowserStop = 4,
    BrowserSearch = 5,
    BrowserFavorites = 6,
    BrowserHome = 7,
    VolumeMute = 8,
    VolumeDown = 9,
    VolumeUp = 10,
    MediaNextTrack = 11,
    MediaPreviousTrack = 12,
    MediaStop = 13,
    MediaPlayPause = 14,
    LaunchMail = 15,
    LaunchMediaSelect = 16,
    LaunchApp1 = 17,
    LaunchApp2 = 18,
    BassDown = 19,
    BassBoost = 20,
    BassUp = 21,
    TrebleDown = 22,
    TrebleUp = 23,
    MicrophoneVolumeMute = 24,
    MicrophoneVolumeDown = 25,
    MicrophoneVolumeUp = 26,
    Help = 27,
    Find = 28,
    New = 29,
    Open = 30,
    Close = 31,
    Save = 32,
    Print = 33,
    Undo = 34,
    Redo = 35,
    Copy = 36,
    Cut = 37,
    Paste = 38,
    ReplyToMail = 39,
    ForwardMail = 40,
    SendMail = 41,
    SpellCheck = 42,
    DictateOrCommandControlToggle = 43,
    MicrophoneOnOffToggle = 44,
    CorrectionList = 45,
    MediaPlay = 46,
    MediaPause = 47,
    MediaRecord = 48,
    MediaFastForward = 49,
    MediaRewind = 50,
    MediaChannelUp = 51,
    MediaChannelDown = 52,
  }
}