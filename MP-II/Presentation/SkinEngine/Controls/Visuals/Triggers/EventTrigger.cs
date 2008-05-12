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

using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Animations;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Visuals.Triggers
{
  public class EventTrigger : Trigger
  {
    Property _routedEventProperty;
    Property _storyBoardProperty;

    public EventTrigger()
    {
      Init();
    }

    public EventTrigger(EventTrigger t)
      : base(t)
    {
      Init();
      RoutedEvent = t.RoutedEvent;
      Storyboard = t.Storyboard;

    }

    void Init()
    {
      _routedEventProperty = new Property(typeof(string), "");
      _storyBoardProperty = new Property(typeof(Timeline), null);
    }

    public override object Clone()
    {
      EventTrigger result = new EventTrigger(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }

    /// <summary>
    /// Gets or sets the routed event property.
    /// </summary>
    /// <value>The routed event property.</value>
    public Property RoutedEventProperty
    {
      get
      {
        return _routedEventProperty;
      }
      set
      {
        _routedEventProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the routed event.
    /// </summary>
    /// <value>The routed event.</value>
    public string RoutedEvent
    {
      get
      {
        return (string)_routedEventProperty.GetValue();
      }
      set
      {
        _routedEventProperty.SetValue(value);
      }
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
    public override void Setup(UIElement element)
    {
      /*
      if (element.VisualParent is ContentPresenter)
      {
        element = element.VisualParent;
        while (element.VisualParent != null)
        {
          if (element is ControlTemplate) break;
          element = element.VisualParent;
        }
      }
      if (element as ControlTemplate != null)
      {
        element = element.VisualParent;
      }*/
      if (Storyboard != null)
      {
        Storyboard.Initialize(element);
      }
      base.Setup(element);
    }
  }
}
