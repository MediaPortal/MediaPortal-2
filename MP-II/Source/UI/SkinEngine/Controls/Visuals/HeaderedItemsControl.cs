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

using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.MpfElements;

namespace MediaPortal.SkinEngine.Controls.Visuals
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
      container.Context = dataItem;
      container.Style = ItemContainerStyle;
      container.ItemContainerStyle = ItemContainerStyle; // TreeItems also have to build containers, so we re-use the container style for our children
      container.ItemsPanel = ItemsPanel; // Re-use the panel for our children

      // We need to copy the item data template for the child containers, because the
      // data template contains specific data for each container. We need to "personalize" the
      // data template copy by assigning its LogicalParent.
      DataTemplate childItemTemplate = MpfCopyManager.DeepCopyCutLP(ItemTemplate);
      childItemTemplate.LogicalParent = container;
      container.ItemTemplate = childItemTemplate;

      FrameworkElement containerTemplateControl = container.TemplateControl; // TemplateControl was set by the change handler of container.Style
      containerTemplateControl.Context = dataItem;
      ContentPresenter headerContentPresenter = containerTemplateControl.FindElement(
          new TypeFinder(typeof(ContentPresenter))) as ContentPresenter;
      if (headerContentPresenter != null)
        headerContentPresenter.ContentTemplate = container.ItemTemplate;
      return container;
    }
  }
}
