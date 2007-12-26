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

    public TimelineGroup()
    {
      _childrenProperty = new Property(new TimelineCollection());
    }

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

    public override void Animate(uint timePassed)
    {
      foreach (Timeline child in Children)
      {
        child.Animate(timePassed);
      }
    }

    public override void Start(uint timePassed)
    {
      foreach (Timeline child in Children)
      {
        child.Start(timePassed);
      }
    }

    public override void Stop()
    {
      foreach (Timeline child in Children)
      {
        child.Stop();
      }
    }
  }
}
