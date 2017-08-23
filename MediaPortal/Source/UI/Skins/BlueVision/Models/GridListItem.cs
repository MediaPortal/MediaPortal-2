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
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.BlueVision.Models
{
  /// <summary>
  /// Extends a generic <see cref="ListItem"/> with position and size information.
  /// </summary>
  public class GridListItem : ListItem
  {
    readonly AbstractProperty _gridRowProperty = new WProperty(typeof(int), 0);
    readonly AbstractProperty _gridRowSpanProperty = new WProperty(typeof(int), 1);
    readonly AbstractProperty _gridColumnProperty = new WProperty(typeof(int), 0);
    readonly AbstractProperty _gridColumnSpanProperty = new WProperty(typeof(int), 1);

    public int GridRow
    {
      get { return (int)_gridRowProperty.GetValue(); }
      set { _gridRowProperty.SetValue(value); }
    }

    public AbstractProperty GridRowProperty
    {
      get { return _gridRowProperty; }
    }

    public int GridRowSpan
    {
      get { return (int)_gridRowSpanProperty.GetValue(); }
      set { _gridRowSpanProperty.SetValue(value); }
    }
    public AbstractProperty GridRowSpanProperty
    {
      get { return _gridRowSpanProperty; }
    }

    public int GridColumn
    {
      get { return (int)_gridColumnProperty.GetValue(); }
      set { _gridColumnProperty.SetValue(value); }
    }

    public AbstractProperty GridColumnProperty
    {
      get { return _gridColumnProperty; }
    }

    public int GridColumnSpan
    {
      get { return (int)_gridColumnSpanProperty.GetValue(); }
      set { _gridColumnSpanProperty.SetValue(value); }
    }

    public AbstractProperty GridColumnSpanProperty
    {
      get { return _gridColumnSpanProperty; }
    }

    #region Constructor

    public GridListItem()
    {
    }

    public GridListItem(ListItem origItem)
    {
      AdditionalProperties = origItem.AdditionalProperties;
      Enabled = origItem.Enabled;
      Command = origItem.Command;
      IsVisible = origItem.IsVisible;
      Labels = origItem.Labels;
      Selected = origItem.Selected;
    }

    #endregion
  }
}
