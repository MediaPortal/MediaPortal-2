using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Animations;
using SkinEngine.Controls.Visuals.Styles;
namespace SkinEngine.Controls.Visuals.Triggers
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
      _routedEventProperty = new Property("");
      _storyBoardProperty = new Property(null);
    }

    public override object Clone()
    {
      return new EventTrigger(this);
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
      if (element as ControlTemplate != null)
      {
        element = element.VisualParent;
      }
      if (Storyboard != null)
      {
        Storyboard.Setup(element);
      }
      base.Setup(element);
    }
  }
}
