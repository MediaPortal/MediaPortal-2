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
  public class BeginStoryboard : TriggerAction
  {
    #region Private fields

    Property _storyBoardProperty;
    Property _nameProperty;

    #endregion

    #region Ctor

    public BeginStoryboard()
    {
      Init();
    }

    void Init()
    {
      _storyBoardProperty = new Property(typeof(Timeline), null);
      _nameProperty = new Property(typeof(string), "");
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      BeginStoryboard s = source as BeginStoryboard;
      Storyboard = copyManager.GetCopy(s.Storyboard);
      Name = copyManager.GetCopy(s.Name);
    }

    #endregion

    #region Public properties

    public Property StoryboardProperty
    {
      get { return _storyBoardProperty; }
    }

    public Timeline Storyboard
    {
      get { return _storyBoardProperty.GetValue() as Timeline; }
      set { _storyBoardProperty.SetValue(value); }
    }


    public Property NameProperty
    {
      get { return _nameProperty; }
    }

    public string Name
    {
      get { return _nameProperty.GetValue() as string; }
      set { _nameProperty.SetValue(value); }
    }

    #endregion

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
