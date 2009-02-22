#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Runtime.InteropServices;

namespace Ui.Players.VideoPlayer.Teletext
{

  enum TeletextEvent
  {
    SEEK_START = 0,
    SEEK_END = 1,
    RESET = 2,
    BUFFER_IN_UPDATE = 3,
    BUFFER_OUT_UPDATE = 4,
    PACKET_PCR_UPDATE = 5,
    //CURRENT_PCR_UPDATE = 6,
    COMPENSATION_UPDATE = 7
  }

  [Guid("3AB7E208-7962-11DC-9F76-850456D89593"),
  InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface ITeletextSource
  {
    void SetTeletextTSPacketCallback(IntPtr callback);
    void SetTeletextEventCallback(IntPtr callback);
    void SetTeletextServiceInfoCallback(IntPtr callback);
  }
}
