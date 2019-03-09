#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers
{
  public class StoryboardContinuationTrigger : TriggerAction
  {
    #region Protected fields

    protected AbstractProperty _beginStoryBoardProperty;

    #endregion

    #region Ctor

    public StoryboardContinuationTrigger()
    {
      Init();
    }

    void Init()
    {
      _beginStoryBoardProperty = new SProperty(typeof(string), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      StopStoryboard s = (StopStoryboard) source;
      BeginStoryboardName = s.BeginStoryboardName;
    }

    #endregion

    public AbstractProperty BeginStoryboardNameProperty
    {
      get { return _beginStoryBoardProperty; }
      set { _beginStoryBoardProperty = value; }
    }

    public string BeginStoryboardName
    {
      get { return _beginStoryBoardProperty.GetValue() as string; }
      set { _beginStoryBoardProperty.SetValue(value); }
    }

    public BeginStoryboard FindStoryboard(UIElement element)
    {
      INameScope ns = FindNameScope();
      if (ns != null)
        return ns.FindName(BeginStoryboardName) as BeginStoryboard;
      return null;
    }
  }
}
