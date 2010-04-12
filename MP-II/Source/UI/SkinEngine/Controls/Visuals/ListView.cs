#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class ListView : ItemsControl, IAddChild<ListViewItem>
  {
    protected override UIElement PrepareItemContainer(object dataItem)
    {
      ListViewItem container = new ListViewItem
        {
            Style = ItemContainerStyle,
            Context = dataItem,
            Content = dataItem,
            Screen = Screen
        };
      // We need to copy the item data template for the child containers, because the
      // data template contains specific data for each container. We need to "personalize" the
      // data template copy by assigning its LogicalParent.
      DataTemplate childItemTemplate = MpfCopyManager.DeepCopyCutLP(ItemTemplate);
      childItemTemplate.LogicalParent = container;
      container.ContentTemplate = childItemTemplate;
      return container;
    }

    #region IAddChild<ListViewItem> Members

    public void AddChild(ListViewItem o)
    {
      // This is not solved in an optimal way because every time the Items collection is changed, the whole
      // Children collection of our items host panel is built newly in the change handler of the Items property.
      // Maybe we should solve that rebuild during the XAML loading time by avoiding the event handler until we
      // receive a call to Initialize (+ make class implement IInitializable)?
      Items.Add(o);
    }

    #endregion
  }
}
