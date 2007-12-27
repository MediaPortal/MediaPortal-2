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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Animations
{
  public class TimelineGroup : Timeline
  {
    Property _childrenProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelineGroup"/> class.
    /// </summary>
    public TimelineGroup()
    {
      _childrenProperty = new Property(new TimelineCollection());
    }

    /// <summary>
    /// Gets or sets the children property.
    /// </summary>
    /// <value>The children property.</value>
    public Property ChildrenProperty
    {
      get
      {
        return _childrenProperty;
      }
      set
      {
        _childrenProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the children.
    /// </summary>
    /// <value>The children.</value>
    public TimelineCollection Children
    {
      get
      {
        return (TimelineCollection)_childrenProperty.GetValue();
      }
      set
      {
        _childrenProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Animate
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public override void Animate(uint timePassed)
    {
      foreach (Timeline child in Children)
      {
        child.Animate(timePassed);
      }
    }

    /// <summary>
    /// Starts the animation
    /// </summary>
    /// <param name="timePassed">The time passed.</param>
    public override void Start(uint timePassed)
    {
      foreach (Timeline child in Children)
      {
        child.Start(timePassed);
      }
    }

    /// <summary>
    /// Stops the animation.
    /// </summary>
    public override void Stop()
    {
      foreach (Timeline child in Children)
      {
        child.Stop();
      }
    }
  }
}
