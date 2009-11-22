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

using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class ListView : ItemsControl
  {
    #region Ctor

    public ListView() { }

    #endregion

    protected override UIElement PrepareItemContainer(object dataItem)
    {
      ListViewItem container = new ListViewItem();
      container.Style = ItemContainerStyle;
      container.Context = dataItem;
      // We need to copy the item data template for the child containers, because the
      // data template contains specific data for each container. We need to "personalize" the
      // data template copy by assigning its LogicalParent.
      DataTemplate childItemTemplate = MpfCopyManager.DeepCopyCutLP(ItemTemplate);
      childItemTemplate.LogicalParent = container;
      container.ContentTemplate = childItemTemplate;
      container.VisualParent = _itemsHostPanel;
      return container;
    }
  }
}
