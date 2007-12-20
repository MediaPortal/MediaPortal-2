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
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Brushes
{
  public class GradientStop : Property
  {
    Property _colorProperty;
    Property _offsetProperty;
    public GradientStop()
    {
      _colorProperty = new Property(Color.White);
      _offsetProperty = new Property((double)0.0f);
    }

    public GradientStop(float offset, Color color)
    {
      _colorProperty = new Property(color);
      _offsetProperty = new Property((double)offset);
    }

    public void OnPropertyChanged()
    {
      Fire();
    }

    public Property ColorProperty
    {
      get
      {
        return _colorProperty;
      }
      set
      {
        _colorProperty = value;
      }
    }

    public Color Color
    {
      get
      {
        return (Color)_colorProperty.GetValue();
      }
      set
      {
        _colorProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public Property OffsetProperty
    {
      get
      {
        return _offsetProperty;
      }
      set
      {
        _offsetProperty = value;
      }
    }

    public double Offset
    {
      get
      {
        return (double)_offsetProperty.GetValue();
      }
      set
      {
        _offsetProperty.SetValue(value);
        OnPropertyChanged();
      }
    }
  }
}
