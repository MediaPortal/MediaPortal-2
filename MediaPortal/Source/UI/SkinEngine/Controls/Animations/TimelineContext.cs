#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/
#endregion

using MediaPortal.UI.SkinEngine.Controls.Visuals;

namespace MediaPortal.UI.SkinEngine.Controls.Animations
{
  public enum State
  {
    /// <summary>
    /// The animation didn't start yet or was stopped.
    /// </summary>
    Idle,

    /// <summary>
    /// The animation was already set up and is ready to be started.
    /// </summary>
    Setup,

    /// <summary>
    /// The animation is waiting for its <see cref="Timeline.BeginTime"/> to arrive.
    /// </summary>
    WaitBegin,

    /// <summary>
    /// The animation is running.
    /// </summary>
    Running,

    /// <summary>
    /// The animation is in reverse mode.
    /// </summary>
    Reverse,

    /// <summary>
    /// The animation has ended but was not stopped.
    /// </summary>
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
