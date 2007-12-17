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

using MediaPortal.Core.Properties;
using SkinEngine.Controls;

namespace SkinEngine.Animations
{
  public class ScriptTrigger : ITrigger
  {
    private Control _control;
    private bool _wasTrue = false;
    private bool _negate = false;
    private Property _property;

    public ScriptTrigger(Control control, Property property)
    {
      _control = control;
      _property = property;
    }

    public ScriptTrigger(Control control, Property property, bool negate)
    {
      _control = control;
      _property = property;
      _negate = negate;
    }

    #region ITrigger Members

    public void Reset()
    {
      _wasTrue = false;
    }

    public bool Condition
    {
      get
      {
        if (_negate)
        {
          return ! (bool) _property.GetValue();
        }
        else
        {
          if (_property == null) return false;
          return (bool) _property.GetValue();
        }
      }
    }

    public bool CanTrigger
    {
      get { return ((bool) _property.GetValue() != _wasTrue); }
    }

    public Control Control
    {
      get { return _control; }
    }

    public Property Property
    {
      get { return _property; }
    }

    public bool Negate
    {
      get { return _negate; }
      set { _negate = value; }
    }

    public bool IsTriggered
    {
      get
      {
        if (Negate)
        {
          if (false == Condition)
          {
            if (_wasTrue == Condition)
            {
              return false;
            }
            if (true == _wasTrue)
            {
              _wasTrue = Condition;
            }
            return true;
          }
          _wasTrue = Condition;
          return false;
        }
        else
        {
          if (Condition)
          {
            if (_wasTrue == Condition)
            {
              return false;
            }
            if (false == _wasTrue)
            {
              _wasTrue = Condition;
            }
            return true;
          }
          _wasTrue = Condition;
          return false;
        }
      }
    }

    #endregion
  }
}