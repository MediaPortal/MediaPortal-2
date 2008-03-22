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
          //Trace.WriteLine(String.Format("StopStoryboard {0} {1}", ((UIElement)element).Name, beginAction.Storyboard.Key));
          element.StopStoryboard(beginAction.Storyboard as Storyboard);

        }
      }
    }
  }
}
