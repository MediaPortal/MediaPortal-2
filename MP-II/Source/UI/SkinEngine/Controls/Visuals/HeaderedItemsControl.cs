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

using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class HeaderedItemsControl : ItemsControl
  {
    #region Protected fields

    protected Property _isExpandedProperty;

    #endregion

    #region Ctor

    public HeaderedItemsControl()
    {
      Init();
    }

    void Init()
    {
      _isExpandedProperty = new Property(typeof(bool), false);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      HeaderedItemsControl c = (HeaderedItemsControl) source;
      IsExpanded = copyManager.GetCopy(c.IsExpanded);
    }

    #endregion

    #region Public properties

    public bool IsExpanded
    {
      get { return (bool)_isExpandedProperty.GetValue(); }
      set { _isExpandedProperty.SetValue(value); }
    }

    public Property IsExpandedProperty
    {
      get { return _isExpandedProperty; }
    }

    #endregion

    protected override UIElement PrepareItemContainer(object dataItem)
    {
      TreeViewItem container = new TreeViewItem();
      container.Content = dataItem;
      container.Context = dataItem;
      container.Style = ItemContainerStyle;

      DataTemplate childItemTemplate = MpfCopyManager.DeepCopyCutLP(ItemTemplate);
      childItemTemplate.LogicalParent = container;
      container.ContentTemplate = childItemTemplate;

      // Re-use some properties for our children
      container.ItemContainerStyle = ItemContainerStyle;
      container.ItemsPanel = ItemsPanel;
      container.ItemTemplate = ItemTemplate;
      return container;
    }
  }
}
