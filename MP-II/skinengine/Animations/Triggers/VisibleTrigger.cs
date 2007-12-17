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
  public class VisibleTrigger : ITrigger
  {
    private Control _control;
    private bool _wasVisible = false;
    private bool _negate = false;

    public VisibleTrigger(Control control)
    {
      _control = control;
    }

    public VisibleTrigger(Control control, bool negate)
    {
      _control = control;
      _negate = negate;
    }

    #region ITrigger Members

    public void Reset()
    {
      _wasVisible = false;
    }

    public bool Condition
    {
      get
      {
        if (_negate)
        {
          return !_control.IsVisible;
        }
        else
        {
          return _control.IsVisible;
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
      get { return (_wasVisible != _control.IsVisible); }
    }

    public bool IsTriggered
    {
      get
      {
        bool isVisible = _control.IsVisible;
        if (Negate)
        {
          if (false == isVisible)
          {
            if (_wasVisible == isVisible)
            {
              return false;
            }
            if (true == _wasVisible)
            {
              _wasVisible = isVisible;
            }
            return true;
          }
          _wasVisible = isVisible;
          return false;
        }
        else
        {
          if (isVisible)
          {
            if (_wasVisible == isVisible)
            {
              return false;
            }
            if (false == _wasVisible)
            {
              _wasVisible = isVisible;
            }
            return true;
          }
          _wasVisible = isVisible;
          return false;
        }
      }
    }

    #endregion
  }
}