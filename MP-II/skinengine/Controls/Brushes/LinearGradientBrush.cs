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
  public class LinearGradientBrush : GradientBrush
  {
    Property _startPointProperty;
    Property _endPointProperty;
    public LinearGradientBrush()
    {
      _startPointProperty = new Property((double)0.0f);
      _endPointProperty = new Property((double)1.0f);
    }

    public Property StartPointProperty
    {
      get
      {
        return _startPointProperty;
      }
      set
      {
        _startPointProperty = value;
      }
    }

    public Point StartPoint
    {
      get
      {
        return (Point)_startPointProperty.GetValue();
      }
      set
      {
        _startPointProperty.SetValue(value);
        OnPropertyChanged();
      }
    }
    public Property EndPointProperty
    {
      get
      {
        return _endPointProperty;
      }
      set
      {
        _endPointProperty = value;
      }
    }

    public Point EndPoint
    {
      get
      {
        return (Point)_endPointProperty.GetValue();
      }
      set
      {
        _endPointProperty.SetValue(value);
        OnPropertyChanged();
      }
    }
  }
}
