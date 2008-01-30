using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Animations;

namespace SkinEngine.Controls.Visuals.Triggers
{
  public class StopStoryboard : TriggerAction
  {
    Property _storyBoardProperty;

    public StopStoryboard()
    {
      Init();
    }

    public StopStoryboard(StopStoryboard action)
      : base(action)
    {
      Init();
      BeginStoryboardName = action.BeginStoryboardName;
    }

    public override object Clone()
    {
      return new StopStoryboard(this);
    }

    void Init()
    {
      _storyBoardProperty = new Property(null);
    }
    /// <summary>
    /// Gets or sets the storyboard property.
    /// </summary>
    /// <value>The storyboard property.</value>
    public Property BeginStoryboardNameProperty
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
    public string BeginStoryboardName
    {
      get
      {
        return _storyBoardProperty.GetValue() as string;
      }
      set
      {
        _storyBoardProperty.SetValue(value);
      }
    }
    public override void Execute(UIElement element, Trigger trigger)
    {
      foreach (TriggerAction action in trigger.EnterActions)
      {
        BeginStoryboard beginAction = action as BeginStoryboard;
        if (beginAction != null && beginAction.Name == BeginStoryboardName)
        {
          if (beginAction.Storyboard.IsStopped == false)
          {
            Trace.WriteLine(String.Format("StopStoryboard {0} {1}", ((UIElement)element).Name, beginAction.Storyboard.Key));
            element.StopStoryboard(beginAction.Storyboard);
            return;
          }
        }
      }
    }
  }
}
