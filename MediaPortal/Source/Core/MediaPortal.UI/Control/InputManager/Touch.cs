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

namespace MediaPortal.UI.Control.InputManager
{
  // Touch event flags ((TouchInput.dwFlags) [winuser.h]
  [Flags]
  public enum TouchEventFlags
  {
    Move = 0x0001,
    Down = 0x0002,
    Up = 0x0004,
    Inrange = 0x0008,
    Primary = 0x0010,
    NoCoalesce = 0x0020,
    Pen = 0x0040
  }

  [Flags]
  public enum TouchInputMask
  {
    // the dwTime field contains a system generated value
    TimeFromSystem = 0x0001,
    // the dwExtraInfo field is valid
    ExtraInfo = 0x0002,
    // the cxContact and cyContact fields are valid
    ContactAread = 0x0004
  }

  public abstract class TouchEvent
  {
    // touch X client coordinate in pixels
    public float LocationX { get; set; }

    // touch Y client coordinate in pixels
    public float LocationY { get; set; }

    // contact ID
    public int Id { get; set; }

    // flags
    public TouchEventFlags Flags { get; set; }

    // mask which fields in the structure are valid
    public TouchInputMask Mask { get; set; }

    // touch event time
    public int Time { get; set; }

    // X size of the contact area in pixels
    public float ContactX { get; set; }

    // Y size of the contact area in pixels
    public float ContactY { get; set; }

    public bool IsPrimaryContact
    {
      get { return (Flags.HasFlag(TouchEventFlags.Primary)); }
    }

    public override string ToString()
    {
      return string.Format("Touch: F: {0} M: {1} LX: {2} LY: {3}", Flags, Mask, LocationX, LocationY);
    }
  }

  public class TouchDownEvent : TouchEvent { }
  public class TouchUpEvent : TouchEvent { }
  public class TouchMoveEvent : TouchEvent { }
}
