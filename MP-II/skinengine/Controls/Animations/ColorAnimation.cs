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
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals;


namespace SkinEngine.Controls.Animations
{
  public class ColorAnimation : Timeline
  {
    Property _fromProperty;
    Property _toProperty;
    Property _byProperty;
    Property _targetProperty;

    public ColorAnimation()
    {
      _targetProperty = new Property(null);
      _fromProperty = new Property(Color.Black);
      _toProperty = new Property(Color.White);
      _byProperty = new Property(Color.Beige);
    }

    public Property FromProperty
    {
      get
      {
        return _fromProperty;
      }
      set
      {
        _fromProperty = value;
      }
    }

    public Color From
    {
      get
      {
        return (Color)_fromProperty.GetValue();
      }
      set
      {
        _fromProperty.SetValue(value);
      }
    }


    public Property ToProperty
    {
      get
      {
        return _toProperty;
      }
      set
      {
        _toProperty = value;
      }
    }

    public Color To
    {
      get
      {
        return (Color)_toProperty.GetValue();
      }
      set
      {
        _toProperty.SetValue(value);
      }
    }

    public Property ByProperty
    {
      get
      {
        return _byProperty;
      }
      set
      {
        _byProperty = value;
      }
    }

    public Color By
    {
      get
      {
        return (Color)_byProperty.GetValue();
      }
      set
      {
        _byProperty.SetValue(value);
      }
    }

    public Property TargetProperty
    {
      get
      {
        return _targetProperty;
      }
      set
      {
        _targetProperty = value;
      }
    }

    public UIElement Target
    {
      get
      {
        return _targetProperty.GetValue() as UIElement;
      }
      set
      {
        _targetProperty.SetValue(value);
      }
    }

  }
}

