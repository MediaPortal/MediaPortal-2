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
using MediaPortal.Core;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Brushes
{
  public enum AlignmentX { Left, Center, Right };
  public enum AlignmentY { Top, Center, Bottom };

  public enum Stretch
  {
    //The content preserves its original size.
    None,

    //The content is resized to fill the destination dimensions. The aspect ratio is not preserved.
    Fill,

    //The content is resized to fit in the destination dimensions while it preserves its native aspect ratio.
    Uniform,

    //The content is resized to fill the destination dimensions while it preserves its native aspect ratio. 
    //If the aspect ratio of the destination rectangle differs from the source, the source content is 
    //clipped to fit in the destination dimensions (zoom-in)
    UniformToFill
  };

  public class TileBrush : Brush
  {
    Property _alignmentXProperty;
    Property _alignmentYProperty;
    Property _stretchProperty;

    public TileBrush()
    {
      _alignmentXProperty = new Property(AlignmentX.Center);
      _alignmentYProperty = new Property(AlignmentY.Center);
      _stretchProperty = new Property(Stretch.Fill);
    }

    public Property AlignmentXProperty
    {
      get
      {
        return _alignmentXProperty;
      }
      set
      {
        _alignmentXProperty = value;
      }
    }

    public AlignmentX AlignmentX
    {
      get
      {
        return (AlignmentX)_alignmentXProperty.GetValue();
      }
      set
      {
        _alignmentXProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public Property AlignmentYProperty
    {
      get
      {
        return _alignmentYProperty;
      }
      set
      {
        _alignmentYProperty = value;
      }
    }

    public AlignmentY AlignmentY
    {
      get
      {
        return (AlignmentY)_alignmentYProperty.GetValue();
      }
      set
      {
        _alignmentYProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public Property StretchProperty
    {
      get
      {
        return _stretchProperty;
      }
      set
      {
        _stretchProperty = value;
      }
    }

    public Stretch Stretch
    {
      get
      {
        return (Stretch)_stretchProperty.GetValue();
      }
      set
      {
        _stretchProperty.SetValue(value);
        OnPropertyChanged();
      }
    }
  }
}
