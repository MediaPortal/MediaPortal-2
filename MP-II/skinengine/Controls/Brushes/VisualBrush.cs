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
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals;

namespace SkinEngine.Controls.Brushes
{
  public class VisualBrush : TileBrush
  {
    Property _visualProperty;
    /// <summary>
    /// Initializes a new instance of the <see cref="VisualBrush"/> class.
    /// </summary>
    public VisualBrush()
    {
      Init();
    }

    public VisualBrush(VisualBrush b)
      : base(b)
    {
      Init();
      Visual = b.Visual;
    }
    void Init()
    {
      _visualProperty = new Property(null);
    }
    public override object Clone()
    {
      return new VisualBrush(this);
    }

    /// <summary>
    /// Gets or sets the visual property.
    /// </summary>
    /// <value>The visual property.</value>
    public Property VisualProperty
    {
      get
      {
        return _visualProperty;
      }
      set
      {
        _visualProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the visual.
    /// </summary>
    /// <value>The visual.</value>
    public Visual Visual
    {
      get
      {
        return (Visual)_visualProperty.GetValue();
      }
      set
      {
        _visualProperty.SetValue(value);
      }
    }
  }
}
