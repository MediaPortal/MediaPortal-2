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
using System.Collections.Generic;
using System.Text;
using SkinEngine.Controls;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
namespace SkinEngine.Animations
{
  public class OverlapsTrigger : ITrigger
  {
    Control _control;
    bool _wasTrue = false;
    bool _negate = false;
    string _property;
    Window _window;
    public OverlapsTrigger(Window window, Control control, string property)
    {
      _window = window;
      _control = control;
      _property = property;
    }
    public OverlapsTrigger(Window window, Control control, string property, bool negate)
    {
      _window = window;
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
          return !Overlaps();
        }
        else
        {
          return Overlaps();
        }
      }
    }
    bool Overlaps()
    {
      Control c = _window.GetControlByName(_property);
      if (c == null) return false;
      /*
       *    c:           [----------------]
       *    control:  [-------]
       *    control:  [----------------------]
       *    control:          [------]
       *    control:          [----------------------]
       */

      //transpose : m41=x     m42=y   m43=z
      //scale     : m11=fX    m22=fY  m33=fZ
      Matrix m = SkinContext.FinalMatrix.Matrix;

      float cWidth = c.Width;
      float cHeight = c.Height;
      Vector3 cPos = c.FinalPosition;
      cPos.X += 5;
      cPos.Y += 5;
      cWidth -= 10;
      cHeight -= 10;
      float conWidth = _control.Width;
      float conHeight = _control.Height;
      Vector3 conPos = _control.FinalPosition;
      conPos.Add(new Vector3(m.M41, m.M42, m.M43));
      if ((conPos.X <= cPos.X && conPos.X + conWidth >= cPos.X) || (conPos.X >= cPos.X && conPos.X <= cPos.X + cWidth))
      {
        if ((conPos.Y <= cPos.Y && conPos.Y + conHeight >= cPos.Y) || (conPos.Y >= cPos.Y && conPos.Y <= cPos.Y + cHeight))
        {
          return true;
        }
      }
      return false;
    }

    public bool CanTrigger
    {
      get
      {
        return (Overlaps() != _wasTrue);
      }
    }
    public Control Control
    {
      get
      {
        return _control;
      }
    }

    public bool Negate
    {
      get
      {
        return _negate;
      }
      set
      {
        _negate = value;
      }
    }

    public bool IsTriggered
    {
      get
      {
        if (Negate)
        {
          if (false == Condition)
          {
            if (_wasTrue == Condition) return false;
            if (true == _wasTrue)
              _wasTrue = Condition;
            return true;
          }
          _wasTrue = Condition;
          return false;
        }
        else
        {
          if (Condition)
          {
            if (_wasTrue == Condition) return false;
            if (false == _wasTrue)
              _wasTrue = Condition;
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
