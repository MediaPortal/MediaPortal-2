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

namespace SkinEngine.Controls.Animations
{
  public class DoubleAnimation : Timeline
  {
    Property _fromProperty;
    Property _toProperty;
    Property _byProperty;
    Property _targetProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleAnimation"/> class.
    /// </summary>
    public DoubleAnimation()
    {
      _fromProperty = new Property(0.0);
      _toProperty = new Property(1.0);
      _byProperty = new Property(0.1);
    }

    /// <summary>
    /// Gets or sets from property.
    /// </summary>
    /// <value>From property.</value>
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

    /// <summary>
    /// Gets or sets from.
    /// </summary>
    /// <value>From.</value>
    public double From
    {
      get
      {
        return (double)_fromProperty.GetValue();
      }
      set
      {
        _fromProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets to property.
    /// </summary>
    /// <value>To property.</value>
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

    /// <summary>
    /// Gets or sets to.
    /// </summary>
    /// <value>To.</value>
    public double To
    {
      get
      {
        return (double)_toProperty.GetValue();
      }
      set
      {
        _toProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the by property.
    /// </summary>
    /// <value>The by property.</value>
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

    /// <summary>
    /// Gets or sets the by.
    /// </summary>
    /// <value>The by.</value>
    public double By
    {
      get
      {
        return (double)_byProperty.GetValue();
      }
      set
      {
        _byProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the target property.
    /// </summary>
    /// <value>The target property.</value>
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

    protected override void AnimateProperty(uint timepassed)
    {
      double dist = (To - From) / Duration.TotalMilliseconds;
      dist *= timepassed;
      dist += From;
      TargetProperty.SetValue( (double) dist);
    }

  }
}
