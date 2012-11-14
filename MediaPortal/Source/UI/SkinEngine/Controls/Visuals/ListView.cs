#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class ListView : ItemsControl, IAddChild<object>
  {
    protected override FrameworkElement PrepareItemContainer(object dataItem)
    {
// ReSharper disable UseObjectOrCollectionInitializer
      ListViewItem container = new ListViewItem
// ReSharper restore UseObjectOrCollectionInitializer
        {
            Context = dataItem,
            Content = dataItem,
            Screen = Screen,
            ElementState = _elementState,
            LogicalParent = this,
        };
      // Set this after the other properties have been initialized to avoid duplicate work
      container.Style = MpfCopyManager.DeepCopyCutLVPs(ItemContainerStyle) ?? container.CopyDefaultStyle();
      container.ContentTemplate = MpfCopyManager.DeepCopyCutLVPs(ItemTemplate);
      return container;
    }

    #region IAddChild<object> implementation

    public void AddChild(object o)
    {
      _items.Add(o);
    }

    #endregion
  }
}
