#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements.Input;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UiComponents.BlueVision.Models
{
  public class GridListView : ListView
  {
    private readonly AbstractProperty _beginNavigationProperty;
    private readonly AbstractProperty _animationCompletedProperty;
    private readonly AbstractProperty _animationStartedProperty;

    public AbstractProperty BeginNavigationProperty
    {
      get { return _beginNavigationProperty; }
    }

    public HomeMenuModel.NavigationTypeEnum BeginNavigation
    {
      get { return (HomeMenuModel.NavigationTypeEnum)_beginNavigationProperty.GetValue(); }
      set { _beginNavigationProperty.SetValue(value); }
    }

    public AbstractProperty AnimationStartedProperty
    {
      get { return _animationStartedProperty; }
    }

    public bool AnimationStarted
    {
      get { return (bool)_animationStartedProperty.GetValue(); }
      set { _animationStartedProperty.SetValue(value); }
    }

    public AbstractProperty AnimationCompletedProperty
    {
      get { return _animationCompletedProperty; }
    }

    public bool AnimationCompleted
    {
      get { return (bool)_animationCompletedProperty.GetValue(); }
      set { _animationCompletedProperty.SetValue(value); }
    }

    public GridListView()
    {
      _beginNavigationProperty = new SProperty(typeof(HomeMenuModel.NavigationTypeEnum), HomeMenuModel.NavigationTypeEnum.None);
      _animationStartedProperty = new SProperty(typeof(bool), false);
      _animationCompletedProperty = new SProperty(typeof(bool), false);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      GridListView c = (GridListView)source;
      BeginNavigation = c.BeginNavigation;
      AnimationStarted = c.AnimationStarted;
      AnimationCompleted = c.AnimationCompleted;
    }

    protected override FrameworkElement PrepareItemContainer(object dataItem)
    {
      FrameworkElement container = base.PrepareItemContainer(dataItem);
      GridListItem gli = dataItem as GridListItem;
      if (gli != null)
      {
        Grid.SetColumn(container, gli.GridColumn);
        Grid.SetColumnSpan(container, gli.GridColumnSpan);
        Grid.SetRow(container, gli.GridRow);
        Grid.SetRowSpan(container, gli.GridRowSpan);
      }
      return container;
    }

    protected override void OnKeyPress(KeyEventArgs e)
    {
      e.Handled =
        e.Key == Key.Left && OnLeft() ||
        e.Key == Key.Right && OnRight();
    }

    private bool OnRight()
    {
      if (!MoveFocus1(MoveFocusDirection.Right))
      {
        BeginNavigation = HomeMenuModel.NavigationTypeEnum.PageRight;
      }
      return true;
    }

    private bool OnLeft()
    {
      if (!MoveFocus1(MoveFocusDirection.Left))
      {
        BeginNavigation = HomeMenuModel.NavigationTypeEnum.PageLeft;
      }
      return true;
    }
  }
}
