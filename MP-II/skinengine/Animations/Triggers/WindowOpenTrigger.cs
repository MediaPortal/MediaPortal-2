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

using SkinEngine.Controls;

namespace SkinEngine.Animations
{
  public class WindowOpenTrigger : ITrigger
  {
    private Control _control;
    private bool _wasOpened = false;
    private bool _negate = false;

    public WindowOpenTrigger(Control control)
    {
      _control = control;
    }

    public WindowOpenTrigger(Control control, bool negate)
    {
      _control = control;
      Negate = negate;
    }

    #region ITrigger Members

    public bool Condition
    {
      get
      {
        if (Negate)
        {
          return _control.Window.WindowState == Window.State.Closing;
        }
        return true;
      }
    }

    public void Reset()
    {
      _wasOpened = Negate;
    }

    public Control Control
    {
      get { return _control; }
    }

    public bool Negate
    {
      get { return _negate; }
      set
      {
        _negate = value;
        _wasOpened = _negate;
      }
    }

    public bool CanTrigger
    {
      get
      {
        if (Negate)
        {
          return (_control.Window.HasFocus == false && _wasOpened);
        }
        return (_control.Window.HasFocus && _wasOpened == false);
      }
    }

    public bool IsTriggered
    {
      get
      {
        if (CanTrigger)
        {
          _wasOpened = _control.Window.HasFocus;
          return true;
        }
        _wasOpened = _control.Window.HasFocus;
        return false;
      }
    }

    #endregion
  }
}
