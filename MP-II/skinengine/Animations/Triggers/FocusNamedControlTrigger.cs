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

using MediaPortal.Core;
using MediaPortal.Core.WindowManager;
using SkinEngine.Controls;

namespace SkinEngine.Animations
{
  public class FocusNamedControlTrigger : ITrigger
  {
    private Control _control;
    private bool _hadFocus = false;
    private bool _negate = false;
    private string _controlName;

    public FocusNamedControlTrigger(string controlName)
    {
      _controlName = controlName;
    }

    public FocusNamedControlTrigger(string controlName, bool negate)
    {
      _controlName = controlName;
      _negate = negate;
    }

    private void GetControl()
    {
      if (_control == null)
      {
        WindowManager manager = (WindowManager) ServiceScope.Get<IWindowManager>();
        IWindow window = manager.CurrentWindow;
        _control = ((Window) window).GetControlByName(_controlName);
      }
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
        GetControl();
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
      get
      {
        GetControl();
        return _control;
      }
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
        GetControl();
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
