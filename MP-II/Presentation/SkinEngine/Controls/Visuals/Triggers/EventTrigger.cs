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
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Visuals.Triggers
{
  public class EventTrigger : Trigger
  {
    #region Private fields

    Property _routedEventProperty;
    Property _storyBoardProperty;

    #endregion

    #region Ctor

    public EventTrigger()
    {
      Init();
    }

    void Init()
    {
      _routedEventProperty = new Property(typeof(string), "");
      _storyBoardProperty = new Property(typeof(Timeline), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      EventTrigger t = source as EventTrigger;
      RoutedEvent = copyManager.GetCopy(t.RoutedEvent);
      Storyboard = copyManager.GetCopy(t.Storyboard);
    }

    #endregion

    #region Public properties

    public Property RoutedEventProperty
    {
      get { return _routedEventProperty; }
    }

    public string RoutedEvent
    {
      get { return (string)_routedEventProperty.GetValue(); }
      set { _routedEventProperty.SetValue(value); }
    }

    public Property StoryboardProperty
    {
      get { return _storyBoardProperty; }
      set { _storyBoardProperty = value; }
    }

    public Timeline Storyboard
    {
      get { return _storyBoardProperty.GetValue() as Timeline; }
      set { _storyBoardProperty.SetValue(value);
      }
    }

    #endregion

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
