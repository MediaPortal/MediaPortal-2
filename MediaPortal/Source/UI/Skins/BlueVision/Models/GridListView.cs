#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals;

namespace MediaPortal.UiComponents.BlueVision.Models
{
  public class GridListView : ListView
  {
    private AbstractProperty _animationCompletedProperty;
    private AbstractProperty _animationStartedProperty;

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
      _animationStartedProperty = new SProperty(typeof(bool), false);
      _animationCompletedProperty = new SProperty(typeof(bool), false);
    }

    protected override FrameworkElement PrepareItemContainer(object dataItem)
    {
      FrameworkElement container = base.PrepareItemContainer(dataItem);
      GridListItem gli = dataItem  as GridListItem;
      if (gli != null)
      {
        Grid.SetColumn(container, gli.GridColumn);
        Grid.SetColumnSpan(container, gli.GridColumnSpan);
        Grid.SetRow(container, gli.GridRow);
        Grid.SetRowSpan(container, gli.GridRowSpan);
      }
      return container;
    }
  }
}
