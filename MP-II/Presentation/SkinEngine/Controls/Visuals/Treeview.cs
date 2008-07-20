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
using MediaPortal.Control.InputManager;
using Presentation.SkinEngine.Commands;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.Presentation.Collections;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class TreeView : ItemsControl
  {
    #region Protected fields

    protected Property _selectionChangedProperty;

    #endregion

    #region Ctor

    public TreeView()
    {
      Init();
    }

    void Init()
    {
      _selectionChangedProperty = new Property(typeof(ICommandStencil), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      TreeView tv = source as TreeView;
      SelectionChanged = copyManager.GetCopy(tv.SelectionChanged);
    }

    #endregion

    #region Events

    public Property SelectionChangedProperty
    {
      get { return _selectionChangedProperty; }
    }

    public ICommandStencil SelectionChanged
    {
      get { return (ICommandStencil)_selectionChangedProperty.GetValue(); }
      set { _selectionChangedProperty.SetValue(value); }
    }

    #endregion

    #region Input handling

    public override void OnMouseMove(float x, float y)
    {
      base.OnMouseMove(x, y);
      UpdateCurrentItem();
    }

    public override void OnKeyPressed(ref Key key)
    {
      UpdateCurrentItem();
      base.OnKeyPressed(ref key);
    }

    void UpdateCurrentItem()
    {
      UIElement element = FindElement(FocusFinder.Instance);
      if (element == null)
        CurrentItem = null;
      else
      {
        // FIXME Albert78: This does not necessarily find the right TreeViewItem
        while (!(element is TreeViewItem) && element.VisualParent != null)
          element = element.VisualParent as UIElement;
        CurrentItem = element.Context;
      }
      if (SelectionChanged != null)
        SelectionChanged.Execute(new object[] { CurrentItem });
    }

    #endregion

    protected override FrameworkElement PrepareItemContainer(object dataItem)
    {
      TreeViewItem container = new TreeViewItem();
      container.Style = ItemContainerStyle;
      container.ItemContainerStyle = ItemContainerStyle; // TreeItems also have to build containers...
      container.ItemsPanel = ItemsPanel;
      container.Context = dataItem;
      // FIXME: Are the next 3 lines debugging code?
      container.TemplateControl = new ItemsPresenter();
      container.TemplateControl.Margin = new Thickness(64, 0, 0, 0);
      container.TemplateControl.VisualParent = container;
      
      if (dataItem is ListItem)
      {
        ListItem listItem = (ListItem) dataItem;
        container.ItemsSource = listItem.SubItems;
      }

      container.HeaderTemplateSelector = ItemTemplateSelector;
      container.HeaderTemplate = ItemTemplate;
      FrameworkElement containerTemplateControl = ItemContainerStyle.Get();
      containerTemplateControl.Context = dataItem;
      ContentPresenter headerContentPresenter = containerTemplateControl.FindElement(new TypeFinder(typeof(ContentPresenter))) as ContentPresenter;
      headerContentPresenter.Content = (FrameworkElement)container.HeaderTemplate.LoadContent();

      container.Header = containerTemplateControl;

      ItemsPresenter p = container.Header.FindElement(new TypeFinder(typeof(ItemsPresenter))) as ItemsPresenter;
      if (p != null) p.IsVisible = false;
      return container;
    }
  }
}
