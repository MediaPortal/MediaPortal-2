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

namespace MediaPortal.Plugins.MceRemoteReceiver.Hardware
{
  public enum RemoteButton
  {
    None = -1,
    Power1 = 165,
    Power2 = 12,
    PowerTV = 101,
    Record = 23,
    Stop = 25,
    Pause = 24,
    Rewind = 21,
    Play = 22,
    Forward = 20,
    Replay = 27,
    Skip = 26,
    Back = 35,
    Up = 30,
    Info = 15,
    Left = 32,
    Ok = 34,
    Right = 33,
    Down = 31,
    VolumeUp = 16,
    VolumeDown = 17,
    Start = 13,
    ChannelUp = 18,
    ChannelDown = 19,
    Mute = 14,
    RecordedTV = 72,
    Guide = 38,
    LiveTV = 37,
    DVDMenu = 36,
    NumPad1 = 1,
    NumPad2 = 2,
    NumPad3 = 3,
    NumPad4 = 4,
    NumPad5 = 5,
    NumPad6 = 6,
    NumPad7 = 7,
    NumPad8 = 8,
    NumPad9 = 9,
    NumPad0 = 0,
    Oem8 = 29,
    OemGate = 28,
    Clear = 10,
    Enter = 11,
    Teletext = 90,
    Red = 91,
    Green = 92,
    Yellow = 93,
    Blue = 94,

    // MCE keyboard specific
    MyTV = 70,
    MyMusic = 71,
    MyPictures = 73,
    MyVideos = 74,
    MyRadio = 80,
    Messenger = 105,

    // Special OEM buttons
    AspectRatio = 39, // FIC Spectra
    Print = 78, // Hewlett Packard MCE Edition
  }
}