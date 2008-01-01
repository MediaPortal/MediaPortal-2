using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Animations;

namespace SkinEngine.Controls.Visuals.Triggers
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
      if (action.Storyboard != null)
        Storyboard = (Storyboard)action.Storyboard.Clone();
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
        Trace.WriteLine(String.Format("StartStoryboard {0} {1}", ((UIElement)element).Name, this.Storyboard.Key));
        element.StartStoryboard(this.Storyboard);
        return;
      }
    }
    public override void Setup(UIElement element)
    {
      if (Storyboard != null)
      {
        Storyboard.Setup(element);
      }
    }
  }
}
