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

using Presentation.SkinEngine.Controls.Visuals;

namespace Presentation.SkinEngine.Controls.Animations
{
  public enum State
  {
    Idle,
    Starting,
    WaitBegin,
    Running,
    Reverse,
    Ended
  };

  public class TimelineContext
  {
    protected UIElement _visualParent;
    protected uint _timeStarted;
    protected State _state = State.Idle;

    public TimelineContext(UIElement visualParent)
    {
      VisualParent = visualParent;
    }

    public UIElement VisualParent
    {
      get { return _visualParent; }
      set { _visualParent = value; }
    }

    public uint TimeStarted
    {
      get { return _timeStarted; }
      set { _timeStarted = value; }
    }

    public State State
    {
      get { return _state; }
      set { _state = value; }
    }
  }
}
