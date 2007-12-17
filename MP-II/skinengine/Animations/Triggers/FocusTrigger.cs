#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
  public class FocusTrigger : ITrigger
  {
    private Control _control;
    private bool _hadFocus = false;
    private bool _negate = false;

    public FocusTrigger(Control control)
    {
      _control = control;
    }

    public FocusTrigger(Control control, bool negate)
    {
      _control = control;
      _negate = negate;
    }

    #region ITrigger Members

    public void Reset()
    {
      _hadFocus = false;
    }

    public bool Condition
    {
      get
      {
        if (_negate)
        {
          return !_control.HasFocus;
        }
        else
        {
          return _control.HasFocus;
        }
      }
    }

    public Control Control
    {
      get { return _control; }
    }

    public bool Negate
    {
      get { return _negate; }
      set { _negate = value; }
    }

    public bool CanTrigger
    {
      get { return (_hadFocus != _control.HasFocus); }
    }

    public bool IsTriggered
    {
      get
      {
        if (Negate)
        {
          if (false == _control.HasFocus)
          {
            if (_hadFocus == _control.HasFocus)
            {
              return false;
            }
            if (true == _hadFocus)
            {
              _hadFocus = _control.HasFocus;
            }
            return true;
          }
          _hadFocus = _control.HasFocus;
          return false;
        }
        else
        {
          if (_control.HasFocus)
          {
            if (_hadFocus == _control.HasFocus)
            {
              return false;
            }
            if (false == _hadFocus)
            {
              _hadFocus = _control.HasFocus;
            }
            return true;
          }
          _hadFocus = _control.HasFocus;
          return false;
        }
      }
    }

    #endregion
  }
}