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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Animations;

namespace Presentation.SkinEngine.Controls.Visuals.Triggers
{
  public class BeginStoryboard : TriggerAction
  {
    Property _storyBoardProperty;
    Property _nameProperty;

    public BeginStoryboard()
    {
      Init();
    }

    public BeginStoryboard(BeginStoryboard action)
      : base(action)
    {
      Init();
      Storyboard = action.Storyboard;
      Name = action.Name;
    }

    public override object Clone()
    {
      return new BeginStoryboard(this);
    }

    void Init()
    {
      _storyBoardProperty = new Property(null);
      _nameProperty = new Property("");
    }
    /// <summary>
    /// Gets or sets the storyboard property.
    /// </summary>
    /// <value>The storyboard property.</value>
    public Property StoryboardProperty
    {
      get
      {
        return _storyBoardProperty;
      }
      set
      {
        _storyBoardProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the storyboard.
    /// </summary>
    /// <value>The storyboard.</value>
    public Timeline Storyboard
    {
      get
      {
        return _storyBoardProperty.GetValue() as Timeline;
      }
      set
      {
        _storyBoardProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the name property.
    /// </summary>
    /// <value>The name property.</value>
    public Property NameProperty
    {
      get
      {
        return _nameProperty;
      }
      set
      {
        _nameProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get
      {
        return _nameProperty.GetValue() as string;
      }
      set
      {
        _nameProperty.SetValue(value);
      }
    }

    public override void Execute(UIElement element, Trigger trigger)
    {
      if (Storyboard != null)
      {
        //Trace.WriteLine(String.Format("StartStoryboard {0} {1}", ((UIElement)element).Name, this.Storyboard.Key));
        element.StartStoryboard(this.Storyboard as Storyboard);
        return;

      }
    }
    public override void Setup(UIElement element)
    {
      if (Storyboard != null)
      {
        Storyboard.Initialize(element);
      }
    }
  }
}
